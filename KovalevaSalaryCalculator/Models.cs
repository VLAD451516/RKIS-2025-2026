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
    }

    public class SalaryCalculation
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime Period { get; set; }

        public decimal BaseSalary { get; set; }
        public decimal Bonus { get; set; }

        public decimal GrossSalary => BaseSalary + Bonus;
        public decimal NDFL => Math.Round(GrossSalary * 0.13m, 2);
        public decimal NetSalary => GrossSalary - NDFL;

        public decimal InsurancePremiums => Math.Round(GrossSalary * 0.30m, 2); // Standard 30%
        public decimal TotalEmployerCost => GrossSalary + InsurancePremiums;
    }
}
