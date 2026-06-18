using KovalevaSalaryCalculator.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KovalevaSalaryCalculator.Services
{
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
            decimal proportionalSalary = normDays > 0 ? Math.Round(employee.BaseSalary / normDays * workedDays, 2) : 0;
            decimal bonus = 0;
            decimal advanceDeduction = 0;

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

                // Sum all previous advance net payments for the same month/year
                advanceDeduction = history
                    .Where(h => h.EmployeeId == employee.Id && h.IsAdvance && h.Period.Year == period.Year && h.Period.Month == period.Month)
                    .Sum(h => h.NetSalary);
            }

            decimal grossSalary = Math.Round(proportionalSalary + (isAdvance ? 0 : bonus), 2);

            // Taxable Base Calculation
            decimal taxableBase;
            if (isAdvance)
            {
                taxableBase = grossSalary;
            }
            else
            {
                decimal childDeduction = 0;
                // Deduction check uses cumulative GROSS per legislation
                if (currentYearGrossBefore <= settings.MaxDeductionIncome)
                {
                    for (int i = 1; i <= employee.ChildrenCount; i++)
                    {
                        if (i == 1 || i == 2) childDeduction += 2800m; // 2025 rates
                        else childDeduction += 6000m;
                    }
                }
                taxableBase = Math.Max(0, grossSalary - childDeduction);
            }

            // Progressive NDFL Calculation
            // currentYearTaxableBaseBefore already includes advance of current month (if any)
            // taxableBase is the taxable amount of THIS specific payment (if isAdvance)
            // OR the TOTAL taxable amount of the month (if !isAdvance)
            decimal ndfl;
            if (isAdvance)
            {
                ndfl = CalculateNDFL(currentYearTaxableBaseBefore, taxableBase, settings);
            }
            else
            {
                // For final, we want to find the TOTAL tax for the month and subtract what was paid in advance
                decimal advanceTaxable = history
                    .Where(h => h.EmployeeId == employee.Id && h.IsAdvance && h.Period.Year == period.Year && h.Period.Month == period.Month)
                    .Sum(h => h.TaxableBase);
                decimal advanceNDFL = history
                    .Where(h => h.EmployeeId == employee.Id && h.IsAdvance && h.Period.Year == period.Year && h.Period.Month == period.Month)
                    .Sum(h => h.NDFL);

                decimal yearToDateBeforeMonth = currentYearTaxableBaseBefore - advanceTaxable;
                decimal totalMonthlyNDFL = CalculateNDFL(yearToDateBeforeMonth, taxableBase, settings);
                ndfl = Math.Max(0, totalMonthlyNDFL - advanceNDFL);
            }

            // Net Payout Calculation
            // grossSalary for final is TOTAL monthly gross.
            // We subtract total monthly NDFL (which is advance NDFL + current ndfl delta)
            // and then subtract advance NET payout.
            decimal totalMonthlyNDFLToSubtract = isAdvance ? ndfl : (history
                .Where(h => h.EmployeeId == employee.Id && h.IsAdvance && h.Period.Year == period.Year && h.Period.Month == period.Month)
                .Sum(h => h.NDFL) + ndfl);

            decimal netPayout = grossSalary - totalMonthlyNDFLToSubtract - (isAdvance ? 0 : advanceDeduction);
            decimal netSalary = Math.Max(0, netPayout);
            decimal debt = netPayout < 0 ? Math.Abs(netPayout) : 0;

            // Insurance Premiums (MSP rates)
            decimal insurance = 0;
            if (!isAdvance)
            {
                // SME threshold is exactly 1 MROT (Law No. 176-FZ dated July 12, 2024)
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
                Id = Guid.NewGuid(), // Explicitly set ID for tracking
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
