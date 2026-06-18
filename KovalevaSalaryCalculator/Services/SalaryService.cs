using KovalevaSalaryCalculator.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KovalevaSalaryCalculator.Services
{
    /// <summary>
    /// Core service for calculating salaries, taxes, and insurance premiums.
    /// Implements 2025 Russian tax legislation including progressive NDFL and SME benefits.
    /// </summary>
    public class SalaryService
    {
        public SalaryCalculation CalculateSalary(
            Employee employee,
            decimal performanceValue,
            int workedDays,
            int normDays,
            DateTime period,
            bool isAdvance,
            decimal currentYearGrossBefore,
            decimal currentYearTaxableBaseBefore,
            AppSettings settings,
            List<SalaryCalculation> history)
        {
            // 1. Calculate proportional part of the base salary based on worked days
            decimal proportionalSalary = normDays > 0 ? Math.Round(employee.BaseSalary / normDays * workedDays, 2) : 0;
            decimal bonus = 0;
            decimal advanceDeduction = 0;

            // 2. Performance-based bonuses (only for final monthly settlement)
            if (!isAdvance)
            {
                switch (employee.Type)
                {
                    case PositionType.Retail:
                        // 2% of sales
                        bonus = Math.Round(performanceValue * 0.02m, 2);
                        break;
                    case PositionType.Logistics:
                        // 50 RUB per load handled
                        bonus = performanceValue * 50m;
                        break;
                    case PositionType.Admin:
                        // Fixed discretionary bonus
                        bonus = performanceValue;
                        break;
                }

                // Sum all net payments made as advances in the current month to subtract from final payout
                advanceDeduction = history
                    .Where(h => h.EmployeeId == employee.Id && h.IsAdvance && h.Period.Year == period.Year && h.Period.Month == period.Month)
                    .Sum(h => h.NetSalary);
            }

            // 3. Gross Calculation
            // Advance is typically just a part of the proportional salary.
            // Final settlement includes the full proportional salary + bonuses.
            decimal grossSalary = Math.Round(proportionalSalary + (isAdvance ? 0 : bonus), 2);

            // 4. Taxable Base Calculation
            decimal taxableBase;
            if (isAdvance)
            {
                // Advances don't usually apply child deductions until the final monthly settlement
                taxableBase = grossSalary;
            }
            else
            {
                decimal childDeduction = 0;
                // Child deduction logic (2025 rules):
                // Limits apply to cumulative GROSS income since start of the year.
                if (currentYearGrossBefore <= settings.MaxDeductionIncome)
                {
                    for (int i = 1; i <= employee.ChildrenCount; i++)
                    {
                        if (i == 1 || i == 2) childDeduction += 2800m; // 2800 RUB for 1st and 2nd child
                        else childDeduction += 6000m; // 6000 RUB for 3rd and subsequent children
                    }
                }
                taxableBase = Math.Max(0, grossSalary - childDeduction);
            }

            // 5. Progressive NDFL Calculation
            // Thresholds: 2.4M, 5M, 20M, 50M RUB cumulative taxable income.
            // currentYearTaxableBaseBefore already includes all previous payments (Finals of prev months + Advances of current month)

            decimal ndfl;
            decimal totalMonthlyTaxSoFar;

            if (isAdvance)
            {
                // Calculate tax for this specific advance based on what was already earned this year.
                ndfl = CalculateNDFL(currentYearTaxableBaseBefore, taxableBase, settings);
                totalMonthlyTaxSoFar = ndfl;
            }
            else
            {
                // Calculating FINAL Monthly Settlement.
                // currentYearTaxableBaseBefore passed from Program.cs includes previous months AND current month's advances.
                // taxableBase is the TOTAL taxable income for the current month (Gross - Child Deductions).

                // We need to know how much tax (and taxable base) was ALREADY paid in current month advances.
                var currentMonthAdvances = history
                    .Where(h => h.EmployeeId == employee.Id && h.IsAdvance && h.Period.Year == period.Year && h.Period.Month == period.Month)
                    .ToList();

                decimal paidTaxOnAdvances = currentMonthAdvances.Sum(h => h.NDFL);
                decimal paidTaxableOnAdvances = currentMonthAdvances.Sum(h => h.TaxableBase);

                // Start calculation from the beginning of the month (excluding current month's advances).
                decimal taxableBeforeMonth = currentYearTaxableBaseBefore - paidTaxableOnAdvances;

                // Calculate total tax for the month (from start of year up to end of this month)
                totalMonthlyTaxSoFar = CalculateNDFL(taxableBeforeMonth, taxableBase, settings);

                // Transactional NDFL is total monthly tax minus tax already paid in advances.
                ndfl = Math.Max(0, totalMonthlyTaxSoFar - paidTaxOnAdvances);
            }

            // 6. Net Payout Calculation
            // For Advance: Net = Gross(Adv) - NDFL(Adv)
            // For Final: Net = Gross(Total Monthly) - TotalMonthlyTaxSoFar - Net(Paid in Advances)
            decimal netPayout = isAdvance
                ? (grossSalary - ndfl)
                : (grossSalary - totalMonthlyTaxSoFar - advanceDeduction);

            decimal netSalary = Math.Max(0, netPayout);
            decimal debt = netPayout < 0 ? Math.Abs(netPayout) : 0;

            // 7. Employer Insurance Premiums (SME/MSP rates)
            decimal insurance = 0;
            if (!isAdvance)
            {
                // SME threshold is 1 MROT (22,440 RUB in 2025)
                // Below MROT: 30%, Above MROT: 15%
                decimal threshold = settings.MROT;
                if (grossSalary <= threshold)
                {
                    insurance = Math.Round(grossSalary * 0.30m, 2);
                }
                else
                {
                    decimal lowPart = threshold * 0.30m;
                    decimal highPart = (grossSalary - threshold) * 0.15m;
                    insurance = Math.Round(lowPart + highPart, 2);
                }
            }

            // 8. Resulting Snapshot
            return new SalaryCalculation
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                EmployeeName = employee.Name,
                Period = period,
                IsAdvance = isAdvance,
                BaseSalary = employee.BaseSalary,
                Bonus = bonus,
                WorkedDays = workedDays,
                NormDays = normDays,
                ChildrenCount = employee.ChildrenCount,
                CurrentYearGrossBefore = currentYearGrossBefore,
                CurrentYearTaxableBaseBefore = currentYearTaxableBaseBefore,
                MROTSnapshot = settings.MROT,
                ProportionalSalary = proportionalSalary,
                GrossSalary = grossSalary,
                TaxableBase = taxableBase,
                NDFL = ndfl,
                AdvanceDeduction = advanceDeduction,
                NetSalary = netSalary,
                EmployeeDebt = debt,
                InsurancePremiums = insurance
            };
        }

        /// <summary>
        /// Calculates tax across the progressive scale tiers.
        /// </summary>
        private decimal CalculateNDFL(decimal prevTaxableTotal, decimal currentTaxableAmount, AppSettings settings)
        {
            decimal totalTax = 0;
            decimal remainingAmount = currentTaxableAmount;
            decimal cumulativeTotal = prevTaxableTotal;

            foreach (var tier in settings.NDFLTiers)
            {
                if (remainingAmount <= 0) break;

                if (cumulativeTotal < tier.Limit)
                {
                    decimal availableSpace = tier.Limit - cumulativeTotal;
                    decimal amountInThisTier = Math.Min(remainingAmount, availableSpace);

                    totalTax += amountInThisTier * tier.Rate;

                    remainingAmount -= amountInThisTier;
                    cumulativeTotal += amountInThisTier;
                }
            }

            // Standard mathematical rounding for tax as per Russian legislation
            return Math.Round(totalTax, 0, MidpointRounding.AwayFromZero);
        }
    }
}
