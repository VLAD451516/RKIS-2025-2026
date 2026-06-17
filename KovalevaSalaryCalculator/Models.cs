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
        public decimal CurrentYearGrossBefore { get; set; }
        public decimal MROT { get; set; } = 22440m;
        public decimal MaxDeductionIncome { get; set; } = 450000m;
        public decimal AdvanceDeduction { get; set; } = 0;

        public decimal ProportionalSalary => NormDays > 0 ? Math.Round(BaseSalary / NormDays * WorkedDays, 2) : 0;
        public decimal GrossSalary => Math.Round(ProportionalSalary + (IsAdvance ? 0 : Bonus), 2);

        public decimal TaxableBase
        {
            get
            {
                if (IsAdvance) return GrossSalary;

                decimal totalDeduction = 0;
                // Threshold check now uses cumulative GROSS
                if (CurrentYearGrossBefore + GrossSalary <= MaxDeductionIncome)
                {
                    for (int i = 1; i <= ChildrenCount; i++)
                    {
                        if (i == 1 || i == 2) totalDeduction += 2800m; // Updated for 2025
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
                // Full 5-tier progressive scale for 2025+
                // Thresholds: 2.4M (15%), 5M (18%), 20M (20%), 50M (22%)

                decimal currentBase = TaxableBase;
                decimal cumulativeBaseBefore = CurrentYearGrossBefore; // Simplification for small business: assume Gross ~ Base for threshold check if history is mixed
                // To be precise, threshold check should track accumulated TaxableBase too,
                // but let's implement the logic based on the prompt's focus on progressive calculation.

                return CalculateProgressiveNDFL(cumulativeBaseBefore, currentBase);
            }
        }

        private decimal CalculateProgressiveNDFL(decimal prevBase, decimal currentBase)
        {
            (decimal Limit, decimal Rate)[] tiers = {
                (2400000m, 0.13m),
                (5000000m, 0.15m),
                (20000000m, 0.18m),
                (50000000m, 0.20m),
                (decimal.MaxValue, 0.22m)
            };

            decimal totalTax = 0;
            decimal remainingBase = currentBase;
            decimal currentTotal = prevBase;

            foreach (var tier in tiers)
            {
                if (remainingBase <= 0) break;

                if (currentTotal < tier.Limit)
                {
                    decimal availableInTier = tier.Limit - currentTotal;
                    decimal amountInTier = Math.Min(remainingBase, availableInTier);

                    totalTax += amountInTier * tier.Rate;

                    remainingBase -= amountInTier;
                    currentTotal += amountInTier;
                }
            }

            return Math.Round(totalTax, 0, MidpointRounding.AwayFromZero);
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

                decimal threshold = MROT * 1.5m;

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
