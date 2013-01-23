using System.Configuration;
using System.Data.SqlServerCe;

namespace SampleWebsite
{
    public class Utilities
    {
        private static readonly ConnectionStringSettings Connection = ConfigurationManager.ConnectionStrings["testdb"];
        private static readonly string ConnectionString = Connection.ConnectionString;

        public static SqlCeConnection GetOpenConnection()
        {
            var connection = new SqlCeConnection(ConnectionString);
            connection.Open();
            return connection;
        }


    }
}
