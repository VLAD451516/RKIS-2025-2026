using System.Text.Json;
using KovalevaSalaryCalculator.Models;

namespace KovalevaSalaryCalculator.Services
{
    public class StorageService
    {
        private const string FileName = "data.json";

        public void SaveEmployees(List<Employee> employees)
        {
            string json = JsonSerializer.Serialize(employees, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FileName, json);
        }

        public List<Employee> LoadEmployees()
        {
            if (!File.Exists(FileName))
            {
                return new List<Employee>();
            }

            string json = File.ReadAllText(FileName);
            return JsonSerializer.Deserialize<List<Employee>>(json) ?? new List<Employee>();
        }
    }
}
