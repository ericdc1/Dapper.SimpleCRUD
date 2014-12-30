using System;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.IO;
using System.Reflection;
using Npgsql;

namespace Dapper.SimpleCRUDTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Setup();
            RunTests();   
            
            //postgres tests assume port 5432 with username postgres and password postgrespass
            //they are commented out by default since postgres setup is required to run tests
            //SetupPG(); 
            //RunTestsPG();    
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
                connection.Execute(@" CREATE TABLE [dbo].[GUIDTest]([guid] [uniqueidentifier] NOT NULL,[name] [varchar](50) NOT NULL, CONSTRAINT [PK_GUIDTest] PRIMARY KEY CLUSTERED ([guid] ASC))");
                connection.Execute(@" ALTER TABLE [dbo].[GUIDTest] ADD  CONSTRAINT [DF_GUIDTest_guid]  DEFAULT (newid()) FOR [guid]");
            }
            Console.WriteLine("Created database");
        }

        private static void SetupPG()
        {
            using (var connection = new NpgsqlConnection(String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4};","localhost", "5432", "postgres","postgrespass", "postgres")))
            {
                connection.Open();
                // drop  database 
                connection.Execute("DROP DATABASE IF EXISTS  testdb;");
                connection.Execute("CREATE DATABASE testdb  WITH OWNER = postgres ENCODING = 'UTF8' CONNECTION LIMIT = -1;");
            }
            System.Threading.Thread.Sleep(1000);

            using (var connection = new NpgsqlConnection(String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4};", "localhost", "5432", "postgres", "postgrespass", "testdb")))
            {
                connection.Open(); 
                connection.Execute(@" create table Users (Id SERIAL, Name varchar not null, Age int not null, ScheduledDayOff int null) ");
                connection.Execute(@" create table Car (CarId SERIAL, Make varchar not null, Model varchar not null) ");
                connection.Execute(@" create table BigCar (CarId BIGSERIAL, Make varchar not null, Model varchar not null) ");
                connection.Execute(@" alter sequence bigcar_carid_seq RESTART WITH 2147483650");
                connection.Execute(@" create table City (Name varchar not null, Population int not null) ");
                connection.Execute(@" CREATE SCHEMA Log; ");
                connection.Execute(@" create table Log.CarLog (Id SERIAL, LogNotes varchar NOT NULL) ");

            }
          
        }


        private static void RunTestsPG()
        {
          
            var pgtester = new Tests(Dbtypes.Postgres);
            foreach (var method in typeof(Tests).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Console.Write("Running " + method.Name + " in postgres");
                method.Invoke(pgtester, null);
                Console.WriteLine(" - OK!");
            }   

            Console.Write("Postgres testing complete.");
            Console.ReadKey();
        }

        private static void RunTests()
        {
            var sqltester = new Tests(Dbtypes.Sqlserver);
            foreach (var method in typeof(Tests).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Console.Write("Running " + method.Name + " in sql server");
                method.Invoke(sqltester, null);
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
            Console.Write("SQL Server testing complete.");
            Console.ReadKey();
        }

    }
}
