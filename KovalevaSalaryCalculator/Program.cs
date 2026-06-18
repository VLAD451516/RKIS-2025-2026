using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using KovalevaSalaryCalculator.Models;
using KovalevaSalaryCalculator.Services;
using KovalevaSalaryCalculator.Tests;

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
            Console.Title = "ИП Ковалева Т.С. - Расчет зарплаты 2025";

            if (args.Length > 0)
            {
                if (args[0] == "--test")
                {
                    ManualTests.Run();
                    return;
                }
                if (args[0] == "--reset")
                {
                    ResetAllData();
                    return;
                }
            }

            // Migrating old files if they exist in root
            MigrateDataFiles();

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
            Console.WriteLine("ИП Ковалева Татьяна Сергеевна (Версия 9.1 FINAL)");
            Console.WriteLine("ОКВЭД: 47.11 | 52.24.2 | 52.29");
            Console.WriteLine($"МРОТ: {settings.MROT:N2} | Лимит вычета: {settings.MaxDeductionIncome:N2}");
            Console.WriteLine("================================================================");
        }

        static void MigrateDataFiles()
        {
            string[] files = { "data.json", "history.json", "settings.json" };
            foreach (var f in files)
            {
                if (File.Exists(f))
                {
                    try { File.Move(f, Path.Combine("Data", f), true); } catch { }
                }
            }
        }

        static void ResetAllData()
        {
            Console.WriteLine("ВНИМАНИЕ! Вы собираетесь полностью удалить все данные (сотрудников, историю, настройки).");
            Console.Write("Введите 'RESET' для подтверждения: ");
            if (Console.ReadLine() == "RESET")
            {
                if (Directory.Exists("Data"))
                {
                    Directory.Delete("Data", true);
                    Console.WriteLine("Все данные успешно удалены.");
                }
                else
                {
                    Console.WriteLine("Папка с данными не найдена.");
                }
            }
            else
            {
                Console.WriteLine("Сброс отменен.");
            }
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

            decimal salary = ReadDecimal("Базовый оклад: ", 0, 10_000_000);
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

            decimal? salaryInput = ReadDecimalNullable($"Новый оклад (Enter чтобы оставить {emp.BaseSalary:N2}): ", 0, 10_000_000);
            if (salaryInput.HasValue) emp.BaseSalary = salaryInput.Value;

            int? kidsInput = ReadIntNullable($"Количество детей (Enter чтобы оставить {emp.ChildrenCount}): ", 0, 20);
            if (kidsInput.HasValue) emp.ChildrenCount = kidsInput.Value;

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

            // Chronological Integrity Checks
            var existingCalculations = history.Where(h => h.EmployeeId == emp.Id && h.Period.Year == year && h.Period.Month == month).ToList();
            bool finalExists = existingCalculations.Any(h => !h.IsAdvance);
            bool advanceExists = existingCalculations.Any(h => h.IsAdvance);

            if (isAdv && finalExists)
            {
                Console.WriteLine("Ошибка! Итоговый расчет уже существует. Аванс невозможен.");
                return;
            }

            if (isAdv && advanceExists)
            {
                Console.WriteLine("Предупреждение: Аванс уже рассчитывался для этого периода.");
                if (ReadString("Пересчитать аванс (удалить старый)? (y/n): ").ToLower() != "y") return;
                history.RemoveAll(h => h.EmployeeId == emp.Id && h.Period.Year == year && h.Period.Month == month && h.IsAdvance);
                storageService.SaveHistory(history);
            }

            if (!isAdv && finalExists)
            {
                Console.WriteLine("Предупреждение: Итоговый расчет уже существует.");
                if (ReadString("Пересчитать месяц (удалить старый итог)? (y/n): ").ToLower() != "y") return;
                history.RemoveAll(h => h.EmployeeId == emp.Id && h.Period.Year == year && h.Period.Month == month && !h.IsAdvance);
                storageService.SaveHistory(history);
            }

            decimal perf = isAdv ? 0 : ReadDecimal("Введите показатели (продажи/грузы/премия): ", 0, 100_000_000);
            int norm = ReadInt("Норма рабочих дней: ", 1, 31);
            int worked = ReadInt("Отработано дней: ", 0, norm);

            // Calculation cumulative values
            var (grossBefore, taxableBefore) = GetCumulativeTotals(emp.Id, year, month, isAdv);

            var calc = salaryService.CalculateSalary(emp, perf, worked, norm, period, isAdv, grossBefore, taxableBefore, settings, history);

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
            Console.WriteLine("3. Просмотреть ставки НДФЛ");
            Console.WriteLine("0. Назад");
            Console.WriteLine("\nПРИМЕЧАНИЕ: Изменения не влияют на уже сохраненные расчеты (Snapshot-принцип).");

            string choice = Console.ReadLine() ?? "";
            if (choice == "1") settings.MROT = ReadDecimal("Новый МРОТ: ", 0, 1_000_000);
            else if (choice == "2") settings.MaxDeductionIncome = ReadDecimal("Новый лимит: ", 0, 100_000_000);
            else if (choice == "3")
            {
                foreach(var tier in settings.NDFLTiers)
                    Console.WriteLine($"Лимит до: {tier.Limit,12:N0} | Ставка: {tier.Rate:P0}");
            }

            if (choice != "0" && choice != "3") storageService.SaveSettings(settings);
        }

        static void ManageHistory()
        {
            if (!history.Any()) { Console.WriteLine("История пуста."); return; }
            while (true)
            {
                Console.Clear();
                Console.WriteLine("\n--- Управление историей ---");
                // Stable sorting for consistent indexing
                var displayList = history.OrderBy(h => h.Period).ThenBy(h => h.EmployeeName).ToList();

                for (int i = 0; i < displayList.Count; i++)
                    Console.WriteLine($"{i + 1,3}. {displayList[i].EmployeeName} | {displayList[i].Period:MM.yyyy} | {(displayList[i].IsAdvance ? "Аванс" : "Итог")} | Net: {displayList[i].NetSalary:N2}");

                Console.WriteLine("\n1. Удалить запись по номеру");
                Console.WriteLine("2. Полная очистка истории");
                Console.WriteLine("0. Назад");

                string choice = Console.ReadLine() ?? "";
                if (choice == "1")
                {
                    int num = ReadInt("Введите НОМЕР записи из списка выше (или 0): ", 0, displayList.Count);
                    if (num > 0)
                    {
                        var itemToRemove = displayList[num - 1];
                        history.Remove(itemToRemove);
                        storageService.SaveHistory(history);
                        Console.WriteLine("Запись успешно удалена. Список обновлен.");
                    }
                    if (!history.Any()) break;
                }
                else if (choice == "2")
                {
                    Console.Write("Вы уверены, что хотите очистить ВСЮ историю? (y/n): ");
                    if (Console.ReadLine()?.ToLower() == "y") { history.Clear(); storageService.SaveHistory(history); break; }
                }
                else if (choice == "0") break;
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
                if (final != null)
                {
                    // Full monthly calculation exists
                    // Gross for final calculation already includes total monthly gross.
                    // NDFL in summary should be the SUM of NDFL for the month.
                    return new {
                        Name = final.EmployeeName,
                        Gross = final.GrossSalary,
                        NDFL = g.Sum(h => h.NDFL),
                        Ins = final.InsurancePremiums,
                        Net = g.Sum(h => h.NetSalary) // Total payout for the month
                    };
                }
                else
                {
                    // Only advances calculated so far
                    return new {
                        Name = g.First().EmployeeName,
                        Gross = g.Sum(h => h.GrossSalary),
                        NDFL = g.Sum(h => h.NDFL),
                        Ins = 0m,
                        Net = g.Sum(h => h.NetSalary)
                    };
                }
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
            int year = ReadInt("Год: ", 2020, 2100);
            int month = ReadInt("Месяц: ", 1, 12);
            var list = history.Where(h => h.Period.Year == year && h.Period.Month == month).ToList();
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
            $"Оклад:     {c.BaseSalary,15:N2}\nНорма/Факт дн: {c.NormDays,5} / {c.WorkedDays}\nНачислено:     {c.GrossSalary,15:N2}\nНДФЛ (уд.):    {c.NDFL,15:N2}\n" +
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

        /// <summary>
        /// Calculates cumulative totals for an employee from the start of the year up to the current calculation point.
        /// </summary>
        static (decimal gross, decimal taxable) GetCumulativeTotals(Guid empId, int year, int month, bool isCurrentCalcAdvance)
        {
            // Rule 1: Sum 'Final' settlements for all PREVIOUS months of the year.
            var prevMonthsFinals = history.Where(h => h.EmployeeId == empId && h.Period.Year == year && h.Period.Month < month && !h.IsAdvance);

            // Rule 2: Sum all 'Advance' payments for the CURRENT month that were already calculated.
            var currentMonthAdvances = history.Where(h => h.EmployeeId == empId && h.Period.Year == year && h.Period.Month == month && h.IsAdvance);

            decimal gross = prevMonthsFinals.Sum(h => h.GrossSalary);
            decimal taxable = prevMonthsFinals.Sum(h => h.TaxableBase);

            // Both Advances and Final calculations for the same month need to see the cumulative total
            // of previous months' Final settlements.
            // BUT: The Final calculation MUST also see the CURRENT month's Advances.
            // AND: If there are multiple advances (rare but possible), subsequent advances should see previous ones?
            // Actually, usually there is only 1 advance and 1 final.

            if (!isCurrentCalcAdvance)
            {
                // If we are calculating FINAL, we must include all advances of this month in the "Before" total
                // so the SalaryService knows how much was already taxed.
                gross += currentMonthAdvances.Sum(h => h.GrossSalary);
                taxable += currentMonthAdvances.Sum(h => h.TaxableBase);
            }
            else
            {
                // If we are calculating an ADVANCE, and somehow there are already advances, sum them.
                gross += currentMonthAdvances.Sum(h => h.GrossSalary);
                taxable += currentMonthAdvances.Sum(h => h.TaxableBase);
            }

            return (gross, taxable);
        }

        static string ReadString(string prompt) {
            while (true) {
                Console.Write(prompt); string s = Console.ReadLine() ?? "";
                if (!string.IsNullOrWhiteSpace(s)) return s;
                Console.WriteLine("Ошибка! Значение не может быть пустым.");
            }
        }

        static decimal ReadDecimal(string prompt, decimal min, decimal max = 100_000_000) {
            while (true) {
                Console.Write(prompt); string s = Console.ReadLine()?.Replace(",", ".") ?? "";
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal r) && r >= min && r <= max) return r;
                Console.WriteLine($"Ошибка! Введите число от {min} до {max}.");
            }
        }

        static decimal? ReadDecimalNullable(string prompt, decimal min = 0, decimal max = 100_000_000)
        {
            while (true)
            {
                Console.Write(prompt);
                string s = Console.ReadLine()?.Replace(",", ".") ?? "";
                if (string.IsNullOrWhiteSpace(s)) return null; // Friendly: Just press Enter to keep current
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal r) && r >= min && r <= max) return r;
                Console.WriteLine($"Ошибка! Пожалуйста, введите корректное число от {min} до {max} или просто нажмите Enter для пропуска.");
            }
        }

        static int ReadInt(string prompt, int min, int max) {
            while (true) {
                Console.Write(prompt); string s = Console.ReadLine() ?? "";
                if (int.TryParse(s, out int r) && r >= min && r <= max) return r;
                Console.WriteLine($"Ошибка! Введите целое число от {min} до {max}.");
            }
        }

        static int? ReadIntNullable(string prompt, int min, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                string s = Console.ReadLine() ?? "";
                if (string.IsNullOrWhiteSpace(s)) return null;
                if (int.TryParse(s, out int r) && r >= min && r <= max) return r;
                Console.WriteLine($"Ошибка! Введите число от {min} до {max} или оставьте пустым (Enter).");
            }
        }
    }
}
