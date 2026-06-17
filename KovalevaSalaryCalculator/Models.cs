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
        public decimal MROT { get; set; } = 22440m;
        public decimal MaxDeductionIncome { get; set; } = 450000m;
        public decimal NDFLThreshold { get; set; } = 2400000m;
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
        public decimal CurrentYearTaxableBaseBefore { get; set; }
        public decimal MROT { get; set; } = 22440m;
        public decimal MaxDeductionIncome { get; set; } = 450000m;
        public decimal NDFLThreshold { get; set; } = 2400000m;
        public decimal AdvanceDeduction { get; set; } = 0;

        public decimal ProportionalSalary => NormDays > 0 ? Math.Round(BaseSalary / NormDays * WorkedDays, 2) : 0;
        public decimal GrossSalary => Math.Round(ProportionalSalary + (IsAdvance ? 0 : Bonus), 2);

        public decimal TaxableBase
        {
            get
            {
                if (IsAdvance) return GrossSalary;

                decimal totalDeduction = 0;
                // Note: Deduction check should use Gross income per tax rules, but threshold uses taxable base
                // Here we assume simple yearly Gross for deduction limit check to keep it distinct
                if (CurrentYearTaxableBaseBefore + GrossSalary <= MaxDeductionIncome)
                {
                    for (int i = 1; i <= ChildrenCount; i++)
                    {
                        if (i == 1) totalDeduction += 1400m;
                        else if (i == 2) totalDeduction += 2800m;
                        else totalDeduction += 6000m;
                    }
                }

                decimal res = GrossSalary - totalDeduction;
                return res > 0 ? res : 0;
            }
        }

        public decimal NDFL
        {
            get
            {
                decimal result = 0;
                decimal currentBase = TaxableBase;
                decimal cumulativeTotal = CurrentYearTaxableBaseBefore + currentBase;

                if (CurrentYearTaxableBaseBefore >= NDFLThreshold)
                {
                    result = currentBase * 0.15m;
                }
                else if (cumulativeTotal > NDFLThreshold)
                {
                    decimal lowPart = NDFLThreshold - CurrentYearTaxableBaseBefore;
                    decimal highPart = currentBase - lowPart;
                    result = (lowPart * 0.13m) + (highPart * 0.15m);
                }
                else
                {
                    result = currentBase * 0.13m;
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

                decimal threshold = MROT * 1.5m; // Updated for 2025-2026 legislation

                if (GrossSalary <= threshold)
                {
                    return Math.Round(GrossSalary * 0.30m, 2);
                }
                else
                {
                    decimal lowPart = threshold * 0.30m;
                    decimal highPart = (GrossSalary - threshold) * 0.15m;
                    return Math.Round(lowPart + highPart, 2);
                }
            }
        }

        public decimal TotalEmployerCost => GrossSalary + InsurancePremiums;
    }
}
