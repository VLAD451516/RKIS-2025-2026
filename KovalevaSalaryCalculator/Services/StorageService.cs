using System.Text.Json;
using KovalevaSalaryCalculator.Models;

namespace KovalevaSalaryCalculator.Services
{
    public class StorageService
    {
        private const string DataDir = "Data";
        private const string EmployeesFile = "Data/data.json";
        private const string HistoryFile = "Data/history.json";
        private const string SettingsFile = "Data/settings.json";

        public StorageService()
        {
            if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);
        }

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
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки сотрудников: {ex.Message}");
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
                var history = JsonSerializer.Deserialize<List<SalaryCalculation>>(json);

                // Data Migration / Healing for old records
                if (history != null)
                {
                    foreach (var h in history)
                    {
                        if (h.Id == Guid.Empty) h.Id = Guid.NewGuid();
                    }
                }

                return history ?? new List<SalaryCalculation>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки истории: {ex.Message}");
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
                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                // Heal: Ensure tiers are present
                if (settings != null && (settings.NDFLTiers == null || settings.NDFLTiers.Count == 0))
                {
                    settings.NDFLTiers = new AppSettings().NDFLTiers;
                }

                return settings ?? new AppSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
                return new AppSettings();
            }
        }
    }
}
