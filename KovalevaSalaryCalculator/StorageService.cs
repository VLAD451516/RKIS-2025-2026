using System.Text.Json;
using KovalevaSalaryCalculator.Models;

namespace KovalevaSalaryCalculator.Services
{
    public class StorageService
    {
        private const string EmployeesFile = "data.json";
        private const string HistoryFile = "history.json";
        private const string SettingsFile = "settings.json";

        public void SaveEmployees(List<Employee> employees)
        {
            try
            {
                string json = JsonSerializer.Serialize(employees, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(EmployeesFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении сотрудников: {ex.Message}");
            }
        }

        public List<Employee> LoadEmployees()
        {
            try
            {
                if (!File.Exists(EmployeesFile)) return new List<Employee>();
                string json = File.ReadAllText(EmployeesFile);
                return JsonSerializer.Deserialize<List<Employee>>(json) ?? new List<Employee>();
            }
            catch
            {
                return new List<Employee>();
            }
        }

        public void SaveHistory(List<SalaryCalculation> history)
        {
            try
            {
                string json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(HistoryFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении истории: {ex.Message}");
            }
        }

        public List<SalaryCalculation> LoadHistory()
        {
            try
            {
                if (!File.Exists(HistoryFile)) return new List<SalaryCalculation>();
                string json = File.ReadAllText(HistoryFile);
                return JsonSerializer.Deserialize<List<SalaryCalculation>>(json) ?? new List<SalaryCalculation>();
            }
            catch
            {
                return new List<SalaryCalculation>();
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении настроек: {ex.Message}");
            }
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFile)) return new AppSettings();
                string json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }
    }
}
