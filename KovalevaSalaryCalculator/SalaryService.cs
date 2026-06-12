using KovalevaSalaryCalculator.Models;

namespace KovalevaSalaryCalculator.Services
{
    public class SalaryService
    {
        public SalaryCalculation CalculateSalary(Employee employee, decimal performanceValue, int workedDays, int normDays, DateTime period, bool isAdvance, decimal currentYearIncome, AppSettings settings)
        {
            decimal bonus = 0;
            int actualWorkedDays = isAdvance ? workedDays / 2 : workedDays;

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
            }

            return new SalaryCalculation
            {
                EmployeeId = employee.Id,
                EmployeeName = employee.Name,
                Period = period,
                IsAdvance = isAdvance,
                BaseSalary = employee.BaseSalary,
                Bonus = bonus,
                WorkedDays = actualWorkedDays,
                NormDays = normDays,
                ChildrenCount = employee.ChildrenCount,
                CurrentYearIncomeBefore = currentYearIncome,
                MROT = settings.MROT,
                MaxDeductionIncome = settings.MaxDeductionIncome
            };
        }
    }
}
