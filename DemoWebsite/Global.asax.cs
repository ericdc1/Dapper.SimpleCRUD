using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Dapper;
using DemoWebsite.Models;
using DemoWebsite.ViewModels;

namespace DemoWebsite
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            SetupDB();
        }

        private static void SetupDB()
        {
            var dbrecreated = false;
            using (var connection = new SqlConnection(@"Data Source=(LocalDB)\v11.0;Initial Catalog=Master;Integrated Security=True"))
            {
                connection.Open();
                try
                {
                    connection.Execute(@" DROP DATABASE SimplecrudDemoWebsite; ");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("database drop  failed - close and reopen VS and try again:" + ex.Message);
                }

                try
                {
                    connection.Execute(@" CREATE DATABASE SimplecrudDemoWebsite; ");
                    dbrecreated = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("database create failed - close and reopen VS and try again:" + ex.Message);
                }

            }
            if (!dbrecreated) return;
            using (var connection = Utilities.GetOpenConnection())
            {
                connection.Execute(@" create TABLE Car (Id int IDENTITY(1,1) not null  Primary Key, Make nvarchar(100) not null, ModelName nvarchar(100) not null) ");
                connection.Insert(new CarViewModel() { Make = "Honda", ModelName = "Civic" });
                connection.Execute(@" create TABLE Users (UserId int IDENTITY(1,1) not null Primary Key, FirstName nvarchar(100) not null, LastName nvarchar(100) not null, intAge int not null) ");
                connection.Insert(new UserViewModel() {Age = 42, FirstName = "Jim", LastName = "Smith"});
                connection.Execute(@" CREATE TABLE GUIDTest (guid uniqueidentifier NOT NULL,name varchar(50) NOT NULL, CONSTRAINT PK_GUIDTest PRIMARY KEY CLUSTERED (guid ASC))");
                connection.Insert<Guid, GUIDTest>(new GUIDTestViewModel {name = "Example"});

                int x = 1;
                do
                {
                    connection.Insert(new User { FirstName = "Jim ", LastName = "Smith " + x, Age = x });
                    x++;
                } while (x < 101);

            }
        }
    }
}
