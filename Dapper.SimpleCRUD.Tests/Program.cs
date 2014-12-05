using System;
using System.Data.SqlClient;
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

            using (var connection = new SqlConnection(@"Data Source=(LocalDB)\v11.0;Initial Catalog=Master;Integrated Security=True"))
            {
                connection.Open();
                try
                {
                    connection.Execute(@" DROP DATABASE DapperSimpleCrudTestDb; ");
                }
                catch {}
                
                connection.Execute(@" CREATE DATABASE DapperSimpleCrudTestDb; ");
            }

            using (var connection = new SqlConnection(@"Data Source = (LocalDB)\v11.0;Initial Catalog=DapperSimpleCrudTestDb;Integrated Security=True"))
            {
                connection.Open();
                connection.Execute(@" create table Users (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null, Age int not null, ScheduledDayOff int null) ");
                connection.Execute(@" create table Car (CarId int IDENTITY(1,1) not null, Make nvarchar(100) not null, Model nvarchar(100) not null) ");
                connection.Execute(@" create table BigCar (CarId bigint IDENTITY(2147483650,1) not null, Make nvarchar(100) not null, Model nvarchar(100) not null) ");
                connection.Execute(@" create table City (Name nvarchar(100) not null, Population int not null) ");
                connection.Execute(@" CREATE SCHEMA Log; ");
                connection.Execute(@" create table Log.CarLog (Id int IDENTITY(1,1) not null, LogNotes nvarchar(100) NOT NULL) ");
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

            using (var connection = new SqlConnection(@"Data Source=(LocalDB)\v11.0;Initial Catalog=Master;Integrated Security=True"))
            {
                connection.Open();
                try
                {
                    //drop any remaining connections, then drop the db.
                    connection.Execute(@" alter database DapperSimpleCrudTestDb set single_user with rollback immediate; DROP DATABASE DapperSimpleCrudTestDb; ");
                }
                catch {}
            }
            Console.Write("Testing complete.");
            Console.ReadKey();
        }

    }
}
