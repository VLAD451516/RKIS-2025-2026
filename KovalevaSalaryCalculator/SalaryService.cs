using KovalevaSalaryCalculator.Models;

namespace KovalevaSalaryCalculator.Services
{
    public class SalaryService
    {
        public SalaryCalculation CalculateSalary(Employee employee, decimal performanceValue, int workedDays, int normDays, DateTime period)
        {
            decimal bonus = 0;

            switch (employee.Type)
            {
                case PositionType.Retail:
                    // 2% of sales
                    bonus = Math.Round(performanceValue * 0.02m, 2);
                    break;
                case PositionType.Logistics:
                    // 50 rubles per cargo unit handled
                    bonus = performanceValue * 50m;
                    break;
                case PositionType.Admin:
                    // Fixed bonus
                    bonus = performanceValue;
                    break;
            }

            return new SalaryCalculation
            {
                EmployeeId = employee.Id,
                EmployeeName = employee.Name,
                Period = period,
                BaseSalary = employee.BaseSalary,
                Bonus = bonus,
                WorkedDays = workedDays,
                NormDays = normDays,
                ChildrenCount = employee.ChildrenCount
            };
        }
    }
}
