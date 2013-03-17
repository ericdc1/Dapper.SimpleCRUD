﻿using System;
using System.Data.SqlServerCe;
using System.IO;
using System.Reflection;

namespace Dapper.SimpleCRUD.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            Setup();
            RunTests();
        }

        private static void Setup()
        {
            var projLoc = Assembly.GetAssembly(typeof(Program)).Location;
            var projFolder = Path.GetDirectoryName(projLoc);

            if (File.Exists(projFolder + "\\Test.sdf"))
                File.Delete(projFolder + "\\Test.sdf");
            var connectionString = "Data Source = " + projFolder + "\\Test.sdf;";
            var engine = new SqlCeEngine(connectionString);
            engine.CreateDatabase();
            using (var connection = new SqlCeConnection(connectionString))
            {
                connection.Open();
                connection.Execute(@" create table Users (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null, Age int not null, ScheduledDayOff int null) ");
                connection.Execute(@" create table Car (CarId int IDENTITY(1,1) not null, Make nvarchar(100) not null, Model nvarchar(100) not null) ");
            }
            Console.WriteLine("Created database");
        }

        private static void RunTests()
        {
            var tester = new Tests();
            foreach (var method in typeof(Tests).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Console.Write("Running " + method.Name);
                method.Invoke(tester, null);
                Console.WriteLine(" - OK!");
            }
            Console.ReadKey();
        }

    }
}
