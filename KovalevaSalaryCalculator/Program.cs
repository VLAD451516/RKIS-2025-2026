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
                    case "0": return;
                    default:
                        Console.WriteLine("Неверный ввод. Пожалуйста, выберите пункт из меню.");
                        break;
                }
                Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                Console.ReadKey();
            }
        }

        static void PrintHeader()
        {
            Console.WriteLine("================================================================");
            Console.WriteLine("ИП Ковалева Татьяна Сергеевна");
            Console.WriteLine("ОКВЭД: 47.11 (Торговля розничная преимущественно пищевыми продуктами)");
            Console.WriteLine("       52.24.2 (Транспортная обработка прочих грузов)");
            Console.WriteLine("       52.29 (Деятельность вспомогательная прочая, связанная с перевозками)");
            Console.WriteLine("Программа расчета заработной платы (Версия 6.0 ЭТАЛОН)");
            Console.WriteLine("================================================================");
            Console.WriteLine($"МРОТ: {settings.MROT:N2} | Порог НДФЛ: {settings.NDFLThreshold:N2}");
            Console.WriteLine("================================================================");
        }

        static void ListEmployees()
        {
            Console.WriteLine("\n--- Список сотрудников ---");
            if (employees.Count == 0)
            {
                Console.WriteLine("Сотрудники не найдены.");
                return;
            }

            for (int i = 0; i < employees.Count; i++)
            {
                var e = employees[i];
                Console.WriteLine($"{i + 1}. {e.Name} - {e.Position} ({e.Type}, Оклад: {e.BaseSalary:N2}, Детей: {e.ChildrenCount})");
            }
        }

        static void AddEmployee()
        {
            Console.WriteLine("\n--- Добавление сотрудника ---");
            Console.Write("ФИО: ");
            string name = Console.ReadLine() ?? "";
            Console.Write("Должность: ");
            string position = Console.ReadLine() ?? "";

            PositionType type = ReadInt("Тип деятельности (1-Розница, 2-Логистика, 3-Админ): ", 1, 3) switch
            {
                1 => PositionType.Retail,
                2 => PositionType.Logistics,
                _ => PositionType.Admin
            };

            decimal baseSalary = ReadDecimal("Базовый оклад: ", 0);
            int childrenCount = ReadInt("Количество детей: ", 0, 20);

            employees.Add(new Employee { Name = name, Position = position, Type = type, BaseSalary = baseSalary, ChildrenCount = childrenCount });
            storageService.SaveEmployees(employees);
            Console.WriteLine("Сотрудник добавлен.");
        }

        static void EditEmployee()
        {
            ListEmployees();
            if (employees.Count == 0) return;

            int index = ReadInt("\nВыберите номер сотрудника для редактирования: ", 1, employees.Count);
            var emp = employees[index - 1];

            Console.WriteLine($"Редактирование: {emp.Name}");
            Console.Write($"Новая должность (оставьте пустым для пропуска, сейчас: {emp.Position}): ");
            string pos = Console.ReadLine() ?? "";
            if (!string.IsNullOrWhiteSpace(pos)) emp.Position = pos;

            int typeChoice = ReadInt("Новый тип деятельности (1-Розница, 2-Логистика, 3-Админ, 0-не менять): ", 0, 3);
            if (typeChoice > 0) emp.Type = typeChoice switch { 1 => PositionType.Retail, 2 => PositionType.Logistics, _ => PositionType.Admin };

            Console.Write("Новый оклад (введите -1 чтобы не менять): ");
            decimal salary = ReadDecimal("", -1);
            if (salary >= 0) emp.BaseSalary = salary;

            Console.Write("Новое количество детей (введите -1 чтобы не менять): ");
            int children = ReadInt("", -1, 20);
            if (children >= 0) emp.ChildrenCount = children;

            storageService.SaveEmployees(employees);
            Console.WriteLine("Данные обновлены.");
        }

        static void CalculateSalaryMenu()
        {
            ListEmployees();
            if (employees.Count == 0) return;

            int index = ReadInt("\nВыберите номер сотрудника: ", 1, employees.Count);
            var emp = employees[index - 1];

            int year = ReadInt("Введите год расчета: ", 2000, 2100);
            int month = ReadInt("Введите месяц расчета (1-12): ", 1, 12);
            DateTime period = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            bool isAdvance = ReadInt("1. Расчет аванса\n2. Итоговый расчет за месяц\nВыбор: ", 1, 2) == 1;

            if (history.Any(h => h.EmployeeId == emp.Id && h.Period.Year == year && h.Period.Month == month && h.IsAdvance == isAdvance))
            {
                Console.WriteLine($"Ошибка! Расчет типа '{(isAdvance ? "Аванс" : "Итого")}' уже существует для этого сотрудника за данный период.");
                return;
            }

            decimal performance = 0;
            if (!isAdvance)
            {
                string prompt = emp.Type switch { PositionType.Retail => "Введите объем продаж: ", PositionType.Logistics => "Введите количество грузов: ", _ => "Введите премию: " };
                performance = ReadDecimal(prompt, 0);
            }

            int normDays = ReadInt("Введите норму рабочих дней в месяце: ", 1, 31);
            string dayPrompt = isAdvance ? "Введите отработано дней (для аванса): " : "Введите ВСЕГО отработанных дней за ПОЛНЫЙ месяц: ";
            int workedDays = ReadInt(dayPrompt, 0, normDays);

            // Correct Cumulative Taxable Base (Exclude current month)
            decimal currentTaxableBase = history
                .Where(h => h.EmployeeId == emp.Id && h.Period.Year == year && h.Period.Month < month)
                .Sum(h => h.TaxableBase);

            var calc = salaryService.CalculateSalary(emp, performance, workedDays, normDays, period, isAdvance, currentTaxableBase, settings, history);

            if (calc.EmployeeDebt > 0)
            {
                Console.WriteLine("\n[ПРЕДУПРЕЖДЕНИЕ] Сумма аванса превышает начисления. Возник долг сотрудника!");
                Console.WriteLine($"Долг сотрудника: {calc.EmployeeDebt:N2}");
            }

            history.Add(calc);
            storageService.SaveHistory(history);
            PrintSalarySlip(calc);
        }

        static void ShowSummaryStatement()
        {
            if (!history.Any()) { Console.WriteLine("История пуста."); return; }
            int year = ReadInt("Введите год: ", 2000, 2100);
            int month = ReadInt("Введите месяц (1-12): ", 1, 12);

            var periodHistory = history.Where(h => h.Period.Year == year && h.Period.Month == month).ToList();
            if (!periodHistory.Any()) { Console.WriteLine("Нет записей за этот период."); return; }

            var grouped = periodHistory.GroupBy(h => h.EmployeeId).Select(g => {
                var final = g.FirstOrDefault(h => !h.IsAdvance);
                if (final != null)
                {
                    return new {
                        Name = final.EmployeeName,
                        Gross = final.GrossSalary,
                        NDFL = final.NDFL,
                        Insurance = final.InsurancePremiums,
                        Net = g.Sum(h => h.NetSalary)
                    };
                }
                else
                {
                    return new {
                        Name = g.First().EmployeeName,
                        Gross = g.Sum(h => h.GrossSalary),
                        NDFL = g.Sum(h => h.NDFL),
                        Insurance = 0m,
                        Net = g.Sum(h => h.NetSalary)
                    };
                }
            }).ToList();

            Console.WriteLine($"\n--- Сводная ведомость за {new DateTime(year, month, 1):MMMM yyyy} ---");
            Console.WriteLine("{0,-25} | {1,10} | {2,10} | {3,10} | {4,10}", "ФИО", "Начислено", "НДФЛ", "Взносы", "К выплате");
            Console.WriteLine(new string('-', 75));
            foreach (var item in grouped)
            {
                Console.WriteLine("{0,-25} | {1,10:N0} | {2,10:N0} | {3,10:N0} | {4,10:N0}", item.Name, item.Gross, item.NDFL, item.Insurance, item.Net);
            }
            Console.WriteLine(new string('-', 75));
            Console.WriteLine("{0,-25} | {1,10:N2} | {2,10:N2} | {3,10:N2} | {4,10:N2}", "ИТОГО", grouped.Sum(x => x.Gross), grouped.Sum(x => x.NDFL), grouped.Sum(x => x.Insurance), grouped.Sum(x => x.Net));
        }

        static void ExportSalarySlip()
        {
            if (!history.Any()) { Console.WriteLine("История пуста."); return; }
            int year = ReadInt("Введите год: ", 2000, 2100);
            int month = ReadInt("Введите месяц (1-12): ", 1, 12);

            var periodHistory = history.Where(h => h.Period.Year == year && h.Period.Month == month).ToList();
            if (!periodHistory.Any()) { Console.WriteLine("Нет записей за этот период."); return; }

            for (int i = 0; i < periodHistory.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {periodHistory[i].EmployeeName} ({(periodHistory[i].IsAdvance ? "Аванс" : "ЗП")})");
            }

            int index = ReadInt("Выберите номер для экспорта: ", 1, periodHistory.Count);
            var c = periodHistory[index - 1];

            string fileName = $"{c.EmployeeName.Replace(" ", "_")}_{c.Period:MMMM_yyyy}_{(c.IsAdvance ? "Adv" : "Full")}.txt";
            File.WriteAllText(fileName, GetSalarySlipText(c));
            Console.WriteLine($"Экспортировано в {fileName}");
        }

        static string GetSalarySlipText(SalaryCalculation c)
        {
            return $"ИП Ковалева Татьяна Сергеевна\n" +
                   $"РАСЧЕТНЫЙ ЛИСТОК ЗА {c.Period:MMMM yyyy} ({(c.IsAdvance ? "АВАНС" : "ИТОГ")})\n" +
                   $"Сотрудник: {c.EmployeeName}\n" +
                   $"-------------------------------------------\n" +
                   $"Норма дней:           {c.NormDays,15}\n" +
                   $"Отработано дней:      {c.WorkedDays,15}\n" +
                   $"Начислено по окладу:  {c.ProportionalSalary,15:N2}\n" +
                   $"Премия/Бонус:         {c.Bonus,15:N2}\n" +
                   $"Начислено (Грязными): {c.GrossSalary,15:N2}\n" +
                   $"НДФЛ:                 {c.NDFL,15:N2}\n" +
                   (c.AdvanceDeduction > 0 ? $"Выплачен аванс:       {c.AdvanceDeduction,15:N2}\n" : "") +
                   (c.EmployeeDebt > 0 ? $"ДОЛГ СОТРУДНИКА:      {c.EmployeeDebt,15:N2}\n" : "") +
                   $"-------------------------------------------\n" +
                   $"К ВЫПЛАТЕ (Чистыми):  {c.NetSalary,15:N2}\n" +
                   $"-------------------------------------------\n" +
                   $"Страховые взносы:     {c.InsurancePremiums,15:N2}\n" +
                   $"-------------------------------------------";
        }

        static void PrintSalarySlip(SalaryCalculation c) => Console.WriteLine("\n" + GetSalarySlipText(c));

        static void DeleteEmployee()
        {
            ListEmployees();
            if (employees.Count == 0) return;
            int index = ReadInt("\nВыберите номер сотрудника для удаления: ", 1, employees.Count);
            employees.RemoveAt(index - 1);
            storageService.SaveEmployees(employees);
            Console.WriteLine("Удалено.");
        }

        static decimal ReadDecimal(string prompt, decimal min)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine()?.Replace(",", ".") ?? "";
                if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal res) && res >= min) return res;
                Console.WriteLine($"Ошибка! Введите число не меньше {min}.");
            }
        }

        static int ReadInt(string prompt, int min, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out int res) && res >= min && res <= max) return res;
                Console.WriteLine($"Ошибка! Введите целое число от {min} до {max}.");
            }
        }
    }
}
