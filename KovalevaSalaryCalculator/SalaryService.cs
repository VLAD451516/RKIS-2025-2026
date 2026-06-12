using KovalevaSalaryCalculator.Models;
using System.Collections.Generic;
using System.Linq;

namespace KovalevaSalaryCalculator.Services
{
    public class SalaryService
    {
        public SalaryCalculation CalculateSalary(Employee employee, decimal performanceValue, int workedDays, int normDays, DateTime period, bool isAdvance, decimal currentYearIncome, AppSettings settings, List<SalaryCalculation> history)
        {
            decimal bonus = 0;
            decimal advanceDeduction = 0;

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

                // Find previous advance (net amount) for this month/year
                var previousAdvance = history.FirstOrDefault(h =>
                    h.EmployeeId == employee.Id &&
                    h.IsAdvance &&
                    h.Period.Year == period.Year &&
                    h.Period.Month == period.Month);

                if (previousAdvance != null)
                {
                    advanceDeduction = previousAdvance.NetSalary;
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
                WorkedDays = workedDays,
                NormDays = normDays,
                ChildrenCount = employee.ChildrenCount,
                CurrentYearIncomeBefore = currentYearIncome,
                MROT = settings.MROT,
                MaxDeductionIncome = settings.MaxDeductionIncome,
                AdvanceDeduction = advanceDeduction
            };
        }
    }
}
