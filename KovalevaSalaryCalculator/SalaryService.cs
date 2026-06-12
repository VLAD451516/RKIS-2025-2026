using KovalevaSalaryCalculator.Models;

namespace KovalevaSalaryCalculator.Services
{
    public class SalaryService
    {
        public SalaryCalculation CalculateSalary(Employee employee, decimal performanceValue, DateTime period)
        {
            decimal bonus = 0;

            switch (employee.Type)
            {
                case PositionType.Retail:
                    // Example: 2% of sales
                    bonus = Math.Round(performanceValue * 0.02m, 2);
                    break;
                case PositionType.Logistics:
                    // Example: 50 rubles per cargo unit handled
                    bonus = performanceValue * 50m;
                    break;
                case PositionType.Admin:
                    // Admin might have a fixed bonus or none
                    bonus = performanceValue;
                    break;
            }

            return new SalaryCalculation
            {
                EmployeeId = employee.Id,
                EmployeeName = employee.Name,
                Period = period,
                BaseSalary = employee.BaseSalary,
                Bonus = bonus
            };
        }
    }
}
