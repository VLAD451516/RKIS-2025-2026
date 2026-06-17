using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using KovalevaSalaryCalculator.Models;
using KovalevaSalaryCalculator.Services;

namespace KovalevaSalaryCalculator
{
    class Program
    {
        private static List<Employee> employees = new();
        private static List<SalaryCalculation> history = new();
        private static AppSettings settings = new();
        private static StorageService storageService = new();
        private static SalaryService salaryService = new();

        static void Main(string[] args)
        {
            employees = storageService.LoadEmployees();
            history = storageService.LoadHistory();
            settings = storageService.LoadSettings();

            while (true)
            {
                Console.Clear();
                PrintHeader();
                Console.WriteLine("1. Список сотрудников");
                Console.WriteLine("2. Добавить сотрудника");
                Console.WriteLine("3. Изменить данные сотрудника");
                Console.WriteLine("4. Рассчитать зарплату");
                Console.WriteLine("5. Удалить сотрудника");
                Console.WriteLine("6. Экспорт расчетного листка");
                Console.WriteLine("7. Сводная зарплатная ведомость");
                Console.WriteLine("8. Настройки системы");
                Console.WriteLine("9. Управление историей");
                Console.WriteLine("0. Выход");
                Console.Write("\nВыберите действие: ");

                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;

                switch (input)
                {
                    case "1": ListEmployees(); break;
                    case "2": AddEmployee(); break;
                    case "3": EditEmployee(); break;
                    case "4": CalculateSalaryMenu(); break;
                    case "5": DeleteEmployee(); break;
                    case "6": ExportSalarySlip(); break;
                    case "7": ShowSummaryStatement(); break;
                    case "8": ManageSettings(); break;
                    case "9": ManageHistory(); break;
                    case "0": return;
                    default:
                        Console.WriteLine("Неверный ввод. Попробуйте еще раз.");
                        break;
                }
                Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                Console.ReadKey();
            }
        }

        static void PrintHeader()
        {
            Console.WriteLine("================================================================");
            Console.WriteLine("ИП Ковалева Татьяна Сергеевна (Версия 8.0 FINAL)");
            Console.WriteLine("ОКВЭД: 47.11 | 52.24.2 | 52.29");
            Console.WriteLine($"МРОТ: {settings.MROT:N2} | Лимит вычета: {settings.MaxDeductionIncome:N2}");
            Console.WriteLine("================================================================");
        }

        static void ListEmployees()
        {
            Console.WriteLine("\n--- Список сотрудников ---");
            if (!employees.Any()) { Console.WriteLine("Сотрудники не найдены."); return; }
            for (int i = 0; i < employees.Count; i++)
            {
                var e = employees[i];
                Console.WriteLine($"{i + 1}. {e.Name} | {e.Position} | Оклад: {e.BaseSalary:N2} | Детей: {e.ChildrenCount}");
            }
        }

        static void AddEmployee()
        {
            Console.WriteLine("\n--- Добавление сотрудника ---");
            string name = ReadString("ФИО: ");
            string pos = ReadString("Должность: ");

            PositionType type = ReadInt("Тип (1-Розница, 2-Логистика, 3-Админ): ", 1, 3) switch
            {
                1 => PositionType.Retail,
                2 => PositionType.Logistics,
                _ => PositionType.Admin
            };

            decimal salary = ReadDecimal("Базовый оклад: ", 0);
            int kids = ReadInt("Количество детей: ", 0, 20);

            employees.Add(new Employee { Name = name, Position = pos, Type = type, BaseSalary = salary, ChildrenCount = kids });
            storageService.SaveEmployees(employees);
            Console.WriteLine("Сотрудник добавлен.");
        }

        static void EditEmployee()
        {
            ListEmployees(); if (!employees.Any()) return;
            int idx = ReadInt("Выберите номер сотрудника: ", 1, employees.Count) - 1;
            var emp = employees[idx];

            Console.WriteLine($"Редактирование: {emp.Name}");
            Console.Write($"Новая должность (Enter чтобы оставить '{emp.Position}'): ");
            string pos = Console.ReadLine() ?? "";
            if (!string.IsNullOrWhiteSpace(pos)) emp.Position = pos;

            decimal salary = ReadDecimal($"Новый оклад (Enter чтобы оставить {emp.BaseSalary:N2}): ", -1, true);
            if (salary >= 0) emp.BaseSalary = salary;

            int kids = ReadInt($"Количество детей (Enter чтобы оставить {emp.ChildrenCount}): ", -1, 20, true);
            if (kids >= 0) emp.ChildrenCount = kids;

            storageService.SaveEmployees(employees);
            Console.WriteLine("Данные обновлены.");
        }

        static void CalculateSalaryMenu()
        {
            ListEmployees(); if (!employees.Any()) return;
            var emp = employees[ReadInt("Выберите номер сотрудника: ", 1, employees.Count) - 1];

            int year = ReadInt("Введите год расчета: ", 2020, 2100);
            int month = ReadInt("Введите месяц расчета (1-12): ", 1, 12);
            DateTime period = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            bool isAdv = ReadInt("1. Расчет аванса | 2. Итоговый расчет за месяц\nВыбор: ", 1, 2) == 1;

            if (history.Any(h => h.EmployeeId == emp.Id && h.Period.Year == year && h.Period.Month == month && h.IsAdvance == isAdv))
            {
                Console.WriteLine("Ошибка! Расчет такого типа уже существует для этого периода.");
                return;
            }

            decimal perf = isAdv ? 0 : ReadDecimal("Введите показатели (продажи/грузы/премия): ", 0);
            int norm = ReadInt("Норма рабочих дней: ", 1, 31);
            int worked = ReadInt("Отработано дней: ", 0, norm);

            // Cumulative income for thresholds (Exclude current month, only Final records to avoid doubling)
            decimal currentYearGrossBefore = history
                .Where(h => h.EmployeeId == emp.Id && h.Period.Year == year && h.Period.Month < month && !h.IsAdvance)
                .Sum(h => h.GrossSalary);

            decimal currentYearTaxableBefore = history
                .Where(h => h.EmployeeId == emp.Id && h.Period.Year == year && h.Period.Month < month && !h.IsAdvance)
                .Sum(h => h.TaxableBase);

            var calc = salaryService.CalculateSalary(emp, perf, worked, norm, period, isAdv, currentYearGrossBefore, currentYearTaxableBefore, settings, history);

            if (calc.EmployeeDebt > 0)
            {
                Console.WriteLine($"\n[ВНИМАНИЕ] Аванс превысил начисления! Долг сотрудника: {calc.EmployeeDebt:N2}");
            }

            history.Add(calc);
            storageService.SaveHistory(history);
            PrintSalarySlip(calc);
        }

        static void ManageSettings()
        {
            Console.WriteLine("\n--- Настройки системы ---");
            Console.WriteLine($"1. Изменить МРОТ (сейчас {settings.MROT:N2})");
            Console.WriteLine($"2. Изменить лимит детских вычетов (сейчас {settings.MaxDeductionIncome:N2})");
            Console.WriteLine("0. Назад");

            string choice = Console.ReadLine() ?? "";
            if (choice == "1") settings.MROT = ReadDecimal("Новый МРОТ: ", 0);
            else if (choice == "2") settings.MaxDeductionIncome = ReadDecimal("Новый лимит: ", 0);

            if (choice != "0") storageService.SaveSettings(settings);
        }

        static void ManageHistory()
        {
            if (!history.Any()) { Console.WriteLine("История пуста."); return; }
            Console.WriteLine("\n--- Управление историей ---");
            Console.WriteLine("1. Показать последние 10 записей");
            Console.WriteLine("2. Удалить конкретную запись");
            Console.WriteLine("3. Полная очистка истории");
            Console.WriteLine("0. Назад");

            string choice = Console.ReadLine() ?? "";
            if (choice == "1")
            {
                var list = history.Skip(Math.Max(0, history.Count - 10)).ToList();
                for (int i = 0; i < list.Count; i++)
                    Console.WriteLine($"{i + 1}. {list[i].EmployeeName} | {list[i].Period:MM.yyyy} | {(list[i].IsAdvance ? "Аванс" : "Итог")} | К выплате: {list[i].NetSalary:N2}");
            }
            else if (choice == "2")
            {
                int idx = ReadInt("Введите номер записи для удаления (или 0): ", 0, history.Count);
                if (idx > 0) { history.RemoveAt(idx - 1); storageService.SaveHistory(history); Console.WriteLine("Удалено."); }
            }
            else if (choice == "3")
            {
                Console.Write("Вы уверены, что хотите удалить ВСЮ историю? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y") { history.Clear(); storageService.SaveHistory(history); }
            }
        }

        static void ShowSummaryStatement()
        {
            if (!history.Any()) { Console.WriteLine("История пуста."); return; }
            int year = ReadInt("Год: ", 2020, 2100);
            int month = ReadInt("Месяц: ", 1, 12);
            var periodHistory = history.Where(h => h.Period.Year == year && h.Period.Month == month).ToList();
            if (!periodHistory.Any()) { Console.WriteLine("Нет записей за этот период."); return; }

            var grouped = periodHistory.GroupBy(h => h.EmployeeId).Select(g => {
                var final = g.FirstOrDefault(h => !h.IsAdvance);
                return final != null
                    ? new { Name = final.EmployeeName, Gross = final.GrossSalary, NDFL = final.NDFL, Ins = final.InsurancePremiums, Net = g.Sum(h => h.NetSalary) }
                    : new { Name = g.First().EmployeeName, Gross = g.Sum(h => h.GrossSalary), NDFL = g.Sum(h => h.NDFL), Ins = 0m, Net = g.Sum(h => h.NetSalary) };
            }).ToList();

            Console.WriteLine($"\n--- Сводная ведомость за {month:D2}.{year} ---");
            Console.WriteLine("{0,-20} | {1,12} | {2,10} | {3,10} | {4,12}", "ФИО", "Начислено", "НДФЛ", "Взносы", "На руки");
            Console.WriteLine(new string('-', 75));
            foreach (var i in grouped)
                Console.WriteLine("{0,-20} | {1,12:N2} | {2,10:N2} | {3,10:N2} | {4,12:N2}", i.Name, i.Gross, i.NDFL, i.Ins, i.Net);

            Console.WriteLine(new string('-', 75));
            Console.WriteLine("{0,-20} | {1,12:N2} | {2,10:N2} | {3,10:N2} | {4,12:N2}", "ИТОГО", grouped.Sum(x => x.Gross), grouped.Sum(x => x.NDFL), grouped.Sum(x => x.Ins), grouped.Sum(x => x.Net));
        }

        static void ExportSalarySlip()
        {
            if (!history.Any()) return;
            int y = ReadInt("Год: ", 2020, 2100);
            int m = ReadInt("Месяц: ", 1, 12);
            var list = history.Where(h => h.Period.Year == y && h.Period.Month == m).ToList();
            if (!list.Any()) { Console.WriteLine("Записей не найдено."); return; }

            for (int i = 0; i < list.Count; i++)
                Console.WriteLine($"{i + 1}. {list[i].EmployeeName} ({(list[i].IsAdvance ? "Аванс" : "Итог")})");

            int choice = ReadInt("Выберите номер: ", 1, list.Count) - 1;
            var c = list[choice];
            string fileName = $"{c.EmployeeName.Replace(" ", "_")}_{c.Period:yyyy_MM}_{(c.IsAdvance ? "Adv" : "Full")}.txt";
            File.WriteAllText(fileName, GetSalarySlipText(c));
            Console.WriteLine($"Файл сохранен: {fileName}");
        }

        static string GetSalarySlipText(SalaryCalculation c) =>
            $"ИП Ковалева Татьяна Сергеевна\nРАСЧЕТНЫЙ ЛИСТОК ЗА {c.Period:MM.yyyy} ({(c.IsAdvance ? "АВАНС" : "ИТОГ")})\nСотрудник: {c.EmployeeName}\n" +
            $"-------------------------------------------\n" +
            $"Оклад:         {c.BaseSalary,15:N2}\nНорма/Факт дн: {c.NormDays,5} / {c.WorkedDays}\nНачислено:     {c.GrossSalary,15:N2}\nНДФЛ (уд.):    {c.NDFL,15:N2}\n" +
            (c.AdvanceDeduction > 0 ? $"Удерж. аванс:  {c.AdvanceDeduction,15:N2}\n" : "") +
            (c.EmployeeDebt > 0 ? $"ДОЛГ СОТР.:    {c.EmployeeDebt,15:N2}\n" : "") +
            $"К ВЫПЛАТЕ:     {c.NetSalary,15:N2}\n-------------------------------------------\n" +
            $"Взносы (ПФР):  {c.InsurancePremiums,15:N2}\n-------------------------------------------";

        static void PrintSalarySlip(SalaryCalculation c) => Console.WriteLine("\n" + GetSalarySlipText(c));

        static void DeleteEmployee()
        {
            ListEmployees(); if (!employees.Any()) return;
            int idx = ReadInt("Введите номер для удаления: ", 1, employees.Count) - 1;
            employees.RemoveAt(idx);
            storageService.SaveEmployees(employees);
            Console.WriteLine("Сотрудник удален.");
        }

        static string ReadString(string prompt) {
            while (true) {
                Console.Write(prompt); string s = Console.ReadLine() ?? "";
                if (!string.IsNullOrWhiteSpace(s)) return s;
                Console.WriteLine("Ошибка! Значение не может быть пустым.");
            }
        }

        static decimal ReadDecimal(string prompt, decimal min, bool allowEmpty = false) {
            while (true) {
                Console.Write(prompt); string s = Console.ReadLine()?.Replace(",", ".") ?? "";
                if (allowEmpty && string.IsNullOrWhiteSpace(s)) return -1;
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal r) && r >= min) return r;
                Console.WriteLine($"Ошибка! Введите число >= {min}.");
            }
        }

        static int ReadInt(string prompt, int min, int max, bool allowEmpty = false) {
            while (true) {
                Console.Write(prompt); string s = Console.ReadLine() ?? "";
                if (allowEmpty && string.IsNullOrWhiteSpace(s)) return -1;
                if (int.TryParse(s, out int r) && r >= min && r <= max) return r;
                Console.WriteLine($"Ошибка! Введите целое число от {min} до {max}.");
            }
        }
    }
}
