using KovalevaSalaryCalculator.Models;
using KovalevaSalaryCalculator.Services;
using System;
using System.Collections.Generic;

namespace KovalevaSalaryCalculator.Tests
{
    public class ManualTests
    {
        public static void Run()
        {
            var salaryService = new SalaryService();
            var settings = new AppSettings();
            var history = new List<SalaryCalculation>();

            // 1. Double Child Deduction (2025 rates)
            // Children: 1. Deduction: 2800.
            // Gross: 10000. Taxable: 7200. NDFL (13%): 936.
            var emp1 = new Employee { Id = Guid.NewGuid(), Name = "Deduction Test", BaseSalary = 10000, ChildrenCount = 1 };
            var calc1 = salaryService.CalculateSalary(emp1, 0, 20, 20, new DateTime(2025, 1, 31), false, 0, 0, settings, history);
            Console.WriteLine($"Test 1 (2800 Deduction): NDFL={calc1.NDFL} (Expected 936)");

            // 2. Progressive NDFL (18% tier)
            // Threshold: 5,000,000.
            // Prev base: 4,950,000. Current base: 100,000.
            // 50,000 @ 15% = 7500.
            // 50,000 @ 18% = 9000.
            // Total: 16500.
            var emp2 = new Employee { Id = Guid.NewGuid(), Name = "Pro Test", BaseSalary = 100000, ChildrenCount = 0 };
            var calc2 = salaryService.CalculateSalary(emp2, 0, 20, 20, new DateTime(2025, 1, 31), false, 0, 4950000, settings, history);
            Console.WriteLine($"Test 2 (18% Tier): NDFL={calc2.NDFL} (Expected 16500)");

            // 3. Advance -> Final Settlement Sequence
            // Month 1: Base 100k. Advance 10/20 days = 50k.
            // Adv: Taxable 50k. NDFL 6500. Net 43500.
            // Final: Taxable 100k. Total Month NDFL 13000. Delta NDFL (13000 - 6500) = 6500.
            // Net Payout: (100k - 13000) - 43500 = 43500.
            var emp3 = new Employee { Id = Guid.NewGuid(), Name = "Sequence Test", BaseSalary = 100000, ChildrenCount = 0 };
            var adv3 = salaryService.CalculateSalary(emp3, 0, 10, 20, new DateTime(2025, 2, 28), true, 0, 0, settings, history);
            history.Add(adv3);
            var final3 = salaryService.CalculateSalary(emp3, 0, 20, 20, new DateTime(2025, 2, 28), false, 100000, 100000, settings, history);

            Console.WriteLine($"Test 3 (Advance): NDFL={adv3.NDFL} (Expected 6500), Net={adv3.NetSalary}");
            Console.WriteLine($"Test 3 (Final): NDFL={final3.NDFL} (Expected 6500), Net={final3.NetSalary} (Expected 43500)");

            if (calc1.NDFL == 936 && calc2.NDFL == 16500 && final3.NDFL == 6500 && final3.NetSalary == 43500)
            {
                Console.WriteLine("FINAL PRO LOGIC TEST PASSED (All 3 Scenarios)");
            }
            else
            {
                Console.WriteLine("FINAL PRO LOGIC TEST FAILED");
            }
        }
    }
}
