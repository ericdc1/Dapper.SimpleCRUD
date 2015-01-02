using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Web.Mvc;
using System.Web.Routing;
using Dapper;

namespace SampleWebsite
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

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
                    connection.Execute(@" DROP DATABASE SimplecrudWebsite; ");
                    connection.Execute(@" CREATE DATABASE SimplecrudWebsite; ");
                    dbrecreated = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("database drop and create failed - close and reopen VS and try again:" + ex.Message );
                }

            }
            if (!dbrecreated) return;
            using (var connection = Utilities.GetOpenConnection())
            {
                connection.Execute(@" create table Users (Id int IDENTITY(1,1) not null Primary Key, FirstName nvarchar(100) not null, LastName nvarchar(100) not null, Age int not null) ");
                connection.Execute(@" create table Car (CarId int IDENTITY(1,1) not null  Primary Key, Make nvarchar(100) not null, Model nvarchar(100) not null) ");
                connection.Execute(@" insert Into Car (make,model) Values  ('Honda','Civic')");
                connection.Execute(@" insert Into Users (firstname,lastname,age) Values  ('Jim','Smith',42)");
            }
        }
    }


}
