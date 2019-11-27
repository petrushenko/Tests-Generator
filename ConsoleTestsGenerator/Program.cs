﻿using System;
using System.Diagnostics;
using TestsGenerator;

namespace ConsoleTestsGenerator
{
    public static class Program
    {
        private const string PathToSave = @"..\..\Tests\";
        public static void Main()
        {
            Console.WriteLine("Threads count in start: " + Process.GetCurrentProcess().Threads.Count);
            var generator = new NUnitTestsGenerator();
            var waiter = generator.GenerateTests(new[] {@"..\..\Program.cs"}, PathToSave, 1, 1, 1);
            waiter.Wait();
            Console.WriteLine("Threads count after finish: " + Process.GetCurrentProcess().Threads.Count);
            Console.WriteLine("Finished. Press any key to exit.");
            Console.ReadKey();
        }
    }
}