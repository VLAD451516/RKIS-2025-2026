using System;
using System.Collections.Generic;
using System.Globalization;
using KovalevaSalaryCalculator.Models;
using KovalevaSalaryCalculator.Services;

namespace KovalevaSalaryCalculator
{
    class Program
    {
        private static List<Employee> employees = new();
        private static StorageService storageService = new();
        private static SalaryService salaryService = new();

        static void Main(string[] args)
        {
            employees = storageService.LoadEmployees();

            while (true)
            {
                Console.Clear();
                PrintHeader();
                Console.WriteLine("1. Список сотрудников");
                Console.WriteLine("2. Добавить сотрудника");
                Console.WriteLine("3. Рассчитать зарплату");
                Console.WriteLine("4. Удалить сотрудника");
                Console.WriteLine("0. Выход");
                Console.Write("\nВыберите действие: ");

                var choice = Console.ReadLine();
                switch (choice)
                {
                    case "1": ListEmployees(); break;
                    case "2": AddEmployee(); break;
                    case "3": CalculateSalary(); break;
                    case "4": DeleteEmployee(); break;
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
            Console.WriteLine("Программа расчета заработной платы");
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
                Console.WriteLine($"{i + 1}. {e.Name} - {e.Position} (Тип: {e.Type}, Оклад: {e.BaseSalary:C})");
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
            Console.Write("Выбор: ");
            PositionType type = Console.ReadLine() switch
            {
                "1" => PositionType.Retail,
                "2" => PositionType.Logistics,
                _ => PositionType.Admin
            };

            Console.Write("Базовый оклад: ");
            if (decimal.TryParse(Console.ReadLine(), out decimal baseSalary))
            {
                employees.Add(new Employee { Name = name, Position = position, Type = type, BaseSalary = baseSalary });
                storageService.SaveEmployees(employees);
                Console.WriteLine("Сотрудник успешно добавлен.");
            }
            else
            {
                Console.WriteLine("Ошибка ввода оклада.");
            }
        }

        static void CalculateSalary()
        {
            ListEmployees();
            if (employees.Count == 0) return;

            Console.Write("\nВыберите номер сотрудника для расчета: ");
            if (int.TryParse(Console.ReadLine(), out int index) && index > 0 && index <= employees.Count)
            {
                var emp = employees[index - 1];
                string prompt = emp.Type switch
                {
                    PositionType.Retail => "Введите объем продаж за месяц: ",
                    PositionType.Logistics => "Введите количество обработанных грузов: ",
                    _ => "Введите сумму премии: "
                };

                Console.Write(prompt);
                if (decimal.TryParse(Console.ReadLine(), out decimal performance))
                {
                    var calc = salaryService.CalculateSalary(emp, performance, DateTime.Now);
                    PrintSalarySlip(calc);
                }
                else
                {
                    Console.WriteLine("Ошибка ввода показателей.");
                }
            }
        }

        static void DeleteEmployee()
        {
            ListEmployees();
            if (employees.Count == 0) return;

            Console.Write("\nВыберите номер сотрудника для удаления: ");
            if (int.TryParse(Console.ReadLine(), out int index) && index > 0 && index <= employees.Count)
            {
                employees.RemoveAt(index - 1);
                storageService.SaveEmployees(employees);
                Console.WriteLine("Сотрудник удален.");
            }
        }

        static void PrintSalarySlip(SalaryCalculation c)
        {
            Console.WriteLine("\n-------------------------------------------");
            Console.WriteLine($"РАСЧЕТНЫЙ ЛИСТОК ЗА {c.Period:MMMM yyyy}");
            Console.WriteLine($"Сотрудник: {c.EmployeeName}");
            Console.WriteLine("-------------------------------------------");
            Console.WriteLine($"Оклад:                {c.BaseSalary,15:N2}");
            Console.WriteLine($"Премия/Бонус:         {c.Bonus,15:N2}");
            Console.WriteLine($"Начислено (Грязными): {c.GrossSalary,15:N2}");
            Console.WriteLine($"НДФЛ (13%):           {c.NDFL,15:N2}");
            Console.WriteLine("-------------------------------------------");
            Console.WriteLine($"К ВЫПЛАТЕ (Чистыми):  {c.NetSalary,15:N2}");
            Console.WriteLine("-------------------------------------------");
            Console.WriteLine($"Страховые взносы(30%):{c.InsurancePremiums,15:N2}");
            Console.WriteLine($"Всего затрат на сотр: {c.TotalEmployerCost,15:N2}");
            Console.WriteLine("-------------------------------------------");
        }
    }
}
