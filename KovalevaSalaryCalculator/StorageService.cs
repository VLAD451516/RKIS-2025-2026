using System.Text.Json;
using KovalevaSalaryCalculator.Models;

namespace KovalevaSalaryCalculator.Services
{
    public class StorageService
    {
        private const string EmployeesFile = "data.json";
        private const string HistoryFile = "history.json";

        public void SaveEmployees(List<Employee> employees)
        {
            string json = JsonSerializer.Serialize(employees, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(EmployeesFile, json);
        }

        public List<Employee> LoadEmployees()
        {
            if (!File.Exists(EmployeesFile))
            {
                return new List<Employee>();
            }

            string json = File.ReadAllText(EmployeesFile);
            return JsonSerializer.Deserialize<List<Employee>>(json) ?? new List<Employee>();
        }

        public void SaveHistory(List<SalaryCalculation> history)
        {
            string json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(HistoryFile, json);
        }

        public List<SalaryCalculation> LoadHistory()
        {
            if (!File.Exists(HistoryFile))
            {
                return new List<SalaryCalculation>();
            }

            string json = File.ReadAllText(HistoryFile);
            return JsonSerializer.Deserialize<List<SalaryCalculation>>(json) ?? new List<SalaryCalculation>();
        }
    }
}
