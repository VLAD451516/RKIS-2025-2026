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
        public decimal MROT { get; set; } = 19242m;
        public decimal MaxDeductionIncome { get; set; } = 350000m;
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
        public decimal MROT { get; set; } = 19242m;
        public decimal MaxDeductionIncome { get; set; } = 350000m;
        public decimal AdvanceDeduction { get; set; } = 0;

        public decimal ProportionalSalary => NormDays > 0 ? Math.Round(BaseSalary / NormDays * WorkedDays, 2) : 0;
        public decimal GrossSalary => Math.Round(ProportionalSalary + (IsAdvance ? 0 : Bonus) - AdvanceDeduction, 2);

        public decimal NDFL
        {
            get
            {
                // Child deduction limit check
                bool applyDeduction = (CurrentYearIncomeBefore + GrossSalary <= MaxDeductionIncome);
                decimal deduction = applyDeduction ? 1400m * ChildrenCount : 0;

                decimal taxableBase = GrossSalary - deduction;
                if (taxableBase < 0) taxableBase = 0;
                return Math.Round(taxableBase * 0.13m, 0);
            }
        }

        public decimal NetSalary => GrossSalary - NDFL;

        public decimal InsurancePremiums
        {
            get
            {
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
