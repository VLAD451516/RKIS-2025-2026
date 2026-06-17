using System;
using System.Text.Json.Serialization;

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
        public (decimal Limit, decimal Rate)[] NDFLTiers { get; set; } = {
            (2400000m, 0.13m),
            (5000000m, 0.15m),
            (20000000m, 0.18m),
            (50000000m, 0.20m),
            (decimal.MaxValue, 0.22m)
        };
    }

    public class SalaryCalculation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime Period { get; set; }
        public bool IsAdvance { get; set; }

        // Input Snapshots
        public decimal BaseSalary { get; set; }
        public decimal Bonus { get; set; }
        public int WorkedDays { get; set; }
        public int NormDays { get; set; }
        public int ChildrenCount { get; set; }
        public decimal CurrentYearGrossBefore { get; set; }
        public decimal CurrentYearTaxableBaseBefore { get; set; }

        // Settings Snapshots
        public decimal MROTSnapshot { get; set; }

        // Calculation Results (Stored as data, no logic here for persistence stability)
        public decimal ProportionalSalary { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal TaxableBase { get; set; }
        public decimal NDFL { get; set; }
        public decimal AdvanceDeduction { get; set; }
        public decimal NetSalary { get; set; }
        public decimal EmployeeDebt { get; set; }
        public decimal InsurancePremiums { get; set; }

        [JsonIgnore]
        public decimal TotalEmployerCost => GrossSalary + InsurancePremiums;
    }
}
