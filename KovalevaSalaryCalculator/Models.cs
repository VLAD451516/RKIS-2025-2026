using System;

namespace KovalevaSalaryCalculator.Models
{
    public enum PositionType
    {
        Retail,    // Торговля розничная
        Logistics, // Транспортная обработка, перевозки
        Admin      // Административный персонал
    }

    public class Employee
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public PositionType Type { get; set; }
        public decimal BaseSalary { get; set; }
        public int ChildrenCount { get; set; }
    }

    public class AppSettings
    {
        public decimal MROT { get; set; } = 22440m; // Actual for 2025+
        public decimal MaxDeductionIncome { get; set; } = 450000m; // Increased for 2025+
        public decimal NDFLThreshold { get; set; } = 2400000m; // 13% vs 15% threshold
    }

    public class SalaryCalculation
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime Period { get; set; }
        public bool IsAdvance { get; set; }

        public decimal BaseSalary { get; set; }
        public decimal Bonus { get; set; }
        public int WorkedDays { get; set; }
        public int NormDays { get; set; }
        public int ChildrenCount { get; set; }
        public decimal CurrentYearIncomeBefore { get; set; }
        public decimal MROT { get; set; } = 22440m;
        public decimal MaxDeductionIncome { get; set; } = 450000m;
        public decimal NDFLThreshold { get; set; } = 2400000m;
        public decimal AdvanceDeduction { get; set; } = 0;

        public decimal ProportionalSalary => NormDays > 0 ? Math.Round(BaseSalary / NormDays * WorkedDays, 2) : 0;
        public decimal GrossSalary => Math.Round(ProportionalSalary + (IsAdvance ? 0 : Bonus), 2);

        public decimal NDFL
        {
            get
            {
                if (IsAdvance)
                {
                    // For advance payments in RF, NDFL is calculated on the full amount without deductions
                    return Math.Round(GrossSalary * 0.13m, 0, MidpointRounding.AwayFromZero);
                }

                // Child deduction logic (Progressive: 1st-1400, 2nd-2800, 3rd+-6000)
                decimal totalDeduction = 0;
                if (CurrentYearIncomeBefore + GrossSalary <= MaxDeductionIncome)
                {
                    for (int i = 1; i <= ChildrenCount; i++)
                    {
                        if (i == 1) totalDeduction += 1400m;
                        else if (i == 2) totalDeduction += 2800m;
                        else totalDeduction += 6000m;
                    }
                }

                decimal taxableBase = GrossSalary - totalDeduction;
                if (taxableBase < 0) taxableBase = 0;

                decimal result = 0;
                decimal cumulativeTotal = CurrentYearIncomeBefore + taxableBase;

                if (CurrentYearIncomeBefore >= NDFLThreshold)
                {
                    // Already in 15% bracket
                    result = taxableBase * 0.15m;
                }
                else if (cumulativeTotal > NDFLThreshold)
                {
                    // Partial 13%, partial 15%
                    decimal lowPart = NDFLThreshold - CurrentYearIncomeBefore;
                    decimal highPart = taxableBase - lowPart;
                    result = (lowPart * 0.13m) + (highPart * 0.15m);
                }
                else
                {
                    // Fully 13%
                    result = taxableBase * 0.13m;
                }

                return Math.Round(result, 0, MidpointRounding.AwayFromZero);
            }
        }

        public decimal NetSalary
        {
            get
            {
                decimal res = GrossSalary - NDFL - (IsAdvance ? 0 : AdvanceDeduction);
                return res > 0 ? res : 0;
            }
        }

        public decimal EmployeeDebt => (GrossSalary - NDFL - (IsAdvance ? 0 : AdvanceDeduction)) < 0
            ? Math.Abs(GrossSalary - NDFL - (IsAdvance ? 0 : AdvanceDeduction))
            : 0;

        public decimal InsurancePremiums
        {
            get
            {
                if (IsAdvance) return 0;

                if (GrossSalary <= MROT)
                {
                    return Math.Round(GrossSalary * 0.30m, 2);
                }
                else
                {
                    decimal lowPart = MROT * 0.30m;
                    decimal highPart = (GrossSalary - MROT) * 0.15m;
                    return Math.Round(lowPart + highPart, 2);
                }
            }
        }

        public decimal TotalEmployerCost => GrossSalary + InsurancePremiums;
    }
}
