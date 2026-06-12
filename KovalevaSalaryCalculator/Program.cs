using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using KovalevaSalaryCalculator.Models;
using KovalevaSalaryCalculator.Services;

namespace KovalevaSalaryCalculator
{
    class Program
    {
        private static List<Employee> employees = new();
        private static List<SalaryCalculation> history = new();
        private static StorageService storageService = new();
        private static SalaryService salaryService = new();

        static void Main(string[] args)
        {
            employees = storageService.LoadEmployees();
            history = storageService.LoadHistory();

            while (true)
            {
                Console.Clear();
                PrintHeader();
                Console.WriteLine("1. Список сотрудников");
                Console.WriteLine("2. Добавить сотрудника");
                Console.WriteLine("3. Рассчитать зарплату");
                Console.WriteLine("4. Удалить сотрудника");
                Console.WriteLine("5. Экспорт расчетного листка");
                Console.WriteLine("0. Выход");
                Console.Write("\nВыберите действие: ");

                var choice = Console.ReadLine();
                switch (choice)
                {
                    case "1": ListEmployees(); break;
                    case "2": AddEmployee(); break;
                    case "3": CalculateSalary(); break;
                    case "4": DeleteEmployee(); break;
                    case "5": ExportSalarySlip(); break;
                    case "0": return;
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
            Console.WriteLine("Программа расчета заработной платы (Улучшенная версия)");
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
                Console.WriteLine($"{i + 1}. {e.Name} - {e.Position} (Тип: {e.Type}, Оклад: {e.BaseSalary:N2}, Детей: {e.ChildrenCount})");
            }
        }

        static void AddEmployee()
        {
            Console.WriteLine("\n--- Добавление сотрудника ---");
            Console.Write("ФИО: ");
            string name = Console.ReadLine() ?? "";
            Console.Write("Должность: ");
            string position = Console.ReadLine() ?? "";

            Console.WriteLine("Тип деятельности:");
            Console.WriteLine("1. Розница (Бонус от продаж)");
            Console.WriteLine("2. Логистика (Бонус от объема грузов)");
            Console.WriteLine("3. Админ (Фикс. бонус)");
            PositionType type = ReadInt("Выбор: ", 1, 3) switch
            {
                1 => PositionType.Retail,
                2 => PositionType.Logistics,
                _ => PositionType.Admin
            };

            decimal baseSalary = ReadDecimal("Базовый оклад: ");
            int childrenCount = ReadInt("Количество детей (для налогового вычета): ", 0, 20);

            employees.Add(new Employee { Name = name, Position = position, Type = type, BaseSalary = baseSalary, ChildrenCount = childrenCount });
            storageService.SaveEmployees(employees);
            Console.WriteLine("Сотрудник успешно добавлен.");
        }

        static void CalculateSalary()
        {
            ListEmployees();
            if (employees.Count == 0) return;

            int index = ReadInt("\nВыберите номер сотрудника для расчета: ", 1, employees.Count);
            var emp = employees[index - 1];

            string performancePrompt = emp.Type switch
            {
                PositionType.Retail => "Введите объем продаж за месяц: ",
                PositionType.Logistics => "Введите количество обработанных грузов: ",
                _ => "Введите сумму премии: "
            };

            decimal performance = ReadDecimal(performancePrompt);
            int normDays = ReadInt("Введите норму рабочих дней в месяце: ", 1, 31);
            int workedDays = ReadInt("Введите фактически отработанных дней: ", 0, normDays);

            var calc = salaryService.CalculateSalary(emp, performance, workedDays, normDays, DateTime.Now);
            history.Add(calc);
            storageService.SaveHistory(history);

            PrintSalarySlip(calc);
        }

        static void DeleteEmployee()
        {
            ListEmployees();
            if (employees.Count == 0) return;

            int index = ReadInt("\nВыберите номер сотрудника для удаления: ", 1, employees.Count);
            employees.RemoveAt(index - 1);
            storageService.SaveEmployees(employees);
            Console.WriteLine("Сотрудник удален.");
        }

        static void ExportSalarySlip()
        {
            if (history.Count == 0)
            {
                Console.WriteLine("История расчетов пуста. Сначала произведите расчет.");
                return;
            }

            Console.WriteLine("\n--- Последние расчеты ---");
            for (int i = Math.Max(0, history.Count - 5); i < history.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {history[i].EmployeeName} ({history[i].Period:MMMM yyyy})");
            }

            int index = ReadInt("Выберите номер расчета для экспорта: ", 1, history.Count);
            var c = history[index - 1];

            string fileName = $"{c.EmployeeName.Replace(" ", "_")}_{c.Period:MMMM_yyyy}.txt";
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.WriteLine("================================================================");
                sw.WriteLine("ИП Ковалева Татьяна Сергеевна");
                sw.WriteLine("================================================================");
                sw.WriteLine($"РАСЧЕТНЫЙ ЛИСТОК ЗА {c.Period:MMMM yyyy}");
                sw.WriteLine($"Сотрудник: {c.EmployeeName}");
                sw.WriteLine("-------------------------------------------");
                sw.WriteLine($"Норма дней:           {c.NormDays,15}");
                sw.WriteLine($"Отработано дней:      {c.WorkedDays,15}");
                sw.WriteLine($"Оклад (полный):       {c.BaseSalary,15:N2}");
                sw.WriteLine($"Начислено по окладу:  {c.ProportionalSalary,15:N2}");
                sw.WriteLine($"Премия/Бонус:         {c.Bonus,15:N2}");
                sw.WriteLine($"Начислено (Грязными): {c.GrossSalary,15:N2}");
                sw.WriteLine($"Кол-во детей (вычет): {c.ChildrenCount,15}");
                sw.WriteLine($"НДФЛ (13%):           {c.NDFL,15:N2}");
                sw.WriteLine("-------------------------------------------");
                sw.WriteLine($"К ВЫПЛАТЕ (Чистыми):  {c.NetSalary,15:N2}");
                sw.WriteLine("-------------------------------------------");
                sw.WriteLine($"Страховые взносы:     {c.InsurancePremiums,15:N2}");
                sw.WriteLine($"Всего затрат на сотр: {c.TotalEmployerCost,15:N2}");
                sw.WriteLine("-------------------------------------------");
            }

            Console.WriteLine($"Расчетный листок экспортирован в файл: {fileName}");
        }

        static void PrintSalarySlip(SalaryCalculation c)
        {
            Console.WriteLine("\n-------------------------------------------");
            Console.WriteLine($"РАСЧЕТНЫЙ ЛИСТОК ЗА {c.Period:MMMM yyyy}");
            Console.WriteLine($"Сотрудник: {c.EmployeeName}");
            Console.WriteLine("-------------------------------------------");
            Console.WriteLine($"Норма дней:           {c.NormDays,15}");
            Console.WriteLine($"Отработано дней:      {c.WorkedDays,15}");
            Console.WriteLine($"Начислено по окладу:  {c.ProportionalSalary,15:N2}");
            Console.WriteLine($"Премия/Бонус:         {c.Bonus,15:N2}");
            Console.WriteLine($"Начислено (Грязными): {c.GrossSalary,15:N2}");
            Console.WriteLine($"НДФЛ (13%):           {c.NDFL,15:N2}");
            Console.WriteLine("-------------------------------------------");
            Console.WriteLine($"К ВЫПЛАТЕ (Чистыми):  {c.NetSalary,15:N2}");
            Console.WriteLine("-------------------------------------------");
            Console.WriteLine($"Страховые взносы:     {c.InsurancePremiums,15:N2}");
            Console.WriteLine($"Всего затрат на сотр: {c.TotalEmployerCost,15:N2}");
            Console.WriteLine("-------------------------------------------");
        }

        static decimal ReadDecimal(string prompt)
        {
            decimal result;
            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine() ?? "";
                input = input.Replace(",", ".");
                if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }
                Console.WriteLine("Ошибка! Пожалуйста, введите число.");
            }
        }

        static int ReadInt(string prompt, int min, int max)
        {
            int result;
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out result) && result >= min && result <= max)
                {
                    return result;
                }
                Console.WriteLine($"Ошибка! Пожалуйста, введите целое число от {min} до {max}.");
            }
        }
    }
}
