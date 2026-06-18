using System;
using System.Collections.Generic;
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

    public class NDFLTier
    {
        public decimal Limit { get; set; }
        public decimal Rate { get; set; }

        public NDFLTier() { }
        public NDFLTier(decimal limit, decimal rate) { Limit = limit; Rate = rate; }
    }

    public class AppSettings
    {
        public decimal MROT { get; set; } = 22440m;
        public decimal MaxDeductionIncome { get; set; } = 450000m;
        public List<NDFLTier> NDFLTiers { get; set; } = new() {
            new NDFLTier(2400000m, 0.13m),
            new NDFLTier(5000000m, 0.15m),
            new NDFLTier(20000000m, 0.18m),
            new NDFLTier(50000000m, 0.20m),
            new NDFLTier(decimal.MaxValue, 0.22m)
        };
    }

    public class SalaryCalculation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime Period { get; set; }
        public bool IsAdvance { get; set; }

        // Snapshots (Input values used at calculation time)
        public decimal BaseSalary { get; set; }
        public decimal Bonus { get; set; }
        public int WorkedDays { get; set; }
        public int NormDays { get; set; }
        public int ChildrenCount { get; set; }

        public decimal CurrentYearGrossBefore { get; set; }
        public decimal CurrentYearTaxableBaseBefore { get; set; }

        public decimal MROTSnapshot { get; set; }

        // Calculation Results
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
