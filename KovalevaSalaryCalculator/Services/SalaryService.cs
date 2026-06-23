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
            AppSettings settings,
            List<SalaryCalculation> history)
        {
            // --- Validation ---
            if (normDays <= 0) throw new ArgumentException("Норма рабочих дней должна быть больше 0.");
            if (workedDays < 0) throw new ArgumentException("Количество отработанных дней не может быть отрицательным.");
            if (workedDays > normDays) throw new ArgumentException("Количество отработанных дней не может превышать норму.");

            // --- 1. Cumulative Calculation Logic (Optimized) ---
            // We need sums of gross and taxable base from history for the current year.
            // Rule: Sum 'Final' settlements for all PREVIOUS months + all 'Advances' of CURRENT month.

            decimal grossBefore = 0;
            decimal taxableBefore = 0;
            decimal advanceNetPaidThisMonth = 0;
            decimal advanceNDFLPaidThisMonth = 0;
            decimal advanceTaxablePaidThisMonth = 0;

            foreach (var h in history)
            {
                if (h.EmployeeId != employee.Id || h.Period.Year != period.Year) continue;

                if (h.Period.Month < period.Month)
                {
                    if (!h.IsAdvance) // Only final monthly records count towards the cumulative year-to-date total
                    {
                        grossBefore += h.GrossSalary;
                        taxableBefore += h.TaxableBase;
                    }
                }
                else if (h.Period.Month == period.Month && h.IsAdvance)
                {
                    // For the current month, we track advances to subtract them later
                    advanceNetPaidThisMonth += h.NetSalary;
                    advanceNDFLPaidThisMonth += h.NDFL;
                    advanceTaxablePaidThisMonth += h.TaxableBase;

                    // If we are currently calculating another advance, previous advances of this month also count as "Before"
                    if (isAdvance)
                    {
                        grossBefore += h.GrossSalary;
                        taxableBefore += h.TaxableBase;
                    }
                }
            }

            // If we are calculating FINAL monthly settlement, "Before" must include this month's advances
            if (!isAdvance)
            {
                grossBefore += advanceTaxablePaidThisMonth; // taxable == gross for advances
                taxableBefore += advanceTaxablePaidThisMonth;
            }

            // --- 2. Current Transaction Calculation ---

            decimal proportionalSalary = Math.Round(employee.BaseSalary / normDays * workedDays, 2);
            decimal bonus = 0;

            if (!isAdvance)
            {
                switch (employee.Type)
                {
                    case PositionType.Retail:
                        bonus = Math.Round(performanceValue * 0.02m, 2);
                        break;
                    case PositionType.Logistics:
                        bonus = performanceValue * 50m;
                        break;
                    case PositionType.Admin:
                        bonus = performanceValue;
                        break;
                }
            }

            decimal grossSalary = Math.Round(proportionalSalary + (isAdvance ? 0 : bonus), 2);

            // --- 3. Taxable Base & Deductions ---
            decimal taxableBase;
            if (isAdvance)
            {
                taxableBase = grossSalary; // No deductions on advances
            }
            else
            {
                decimal childDeduction = 0;
                if (grossBefore <= settings.MaxDeductionIncome)
                {
                    for (int i = 1; i <= employee.ChildrenCount; i++)
                    {
                        if (i == 1 || i == 2) childDeduction += 2800m;
                        else childDeduction += 6000m;
                    }
                }
                taxableBase = Math.Max(0, grossSalary - childDeduction);
            }

            // --- 4. Progressive NDFL ---
            decimal ndfl;
            decimal totalMonthlyTax;

            if (isAdvance)
            {
                ndfl = CalculateNDFL(taxableBefore, taxableBase, settings);
                totalMonthlyTax = ndfl;
            }
            else
            {
                // taxableBefore at this point includes advances of the current month.
                // We want to calculate the total tax for the entire month's income (relative to year start).
                decimal taxableBeforeCurrentMonth = taxableBefore - advanceTaxablePaidThisMonth;
                totalMonthlyTax = CalculateNDFL(taxableBeforeCurrentMonth, taxableBase, settings);

                // Transactional NDFL is the delta.
                ndfl = Math.Max(0, totalMonthlyTax - advanceNDFLPaidThisMonth);
            }

            // --- 5. Net Payout Logic (Hardened) ---
            // For Advance: Net = Gross - NDFL
            // For Final: Net = TotalMonthGross - TotalMonthNDFL - TotalMonthAdvancesNet
            decimal netPayout;
            if (isAdvance)
            {
                netPayout = grossSalary - ndfl;
            }
            else
            {
                netPayout = grossSalary - totalMonthlyTax - advanceNetPaidThisMonth;
            }

            decimal netSalary = Math.Max(0, netPayout);
            decimal debt = netPayout < 0 ? Math.Abs(netPayout) : 0;

            // --- 6. Insurance Premiums ---
            decimal insurance = 0;
            if (!isAdvance)
            {
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
                CurrentYearGrossBefore = grossBefore,
                CurrentYearTaxableBaseBefore = taxableBefore,
                MROTSnapshot = settings.MROT,
                ProportionalSalary = proportionalSalary,
                GrossSalary = grossSalary,
                TaxableBase = taxableBase,
                NDFL = ndfl,
                AdvanceDeduction = isAdvance ? 0 : advanceNetPaidThisMonth,
                NetSalary = netSalary,
                EmployeeDebt = debt,
                InsurancePremiums = insurance
            };
        }

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

            return Math.Round(totalTax, 0, MidpointRounding.AwayFromZero);
        }
    }
}
