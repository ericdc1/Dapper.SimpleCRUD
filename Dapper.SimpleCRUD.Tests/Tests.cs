using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System;
using Dapper;


namespace Dapper.SimpleCRUD.Tests
{
    //For .Net 4.5> [System.ComponentModel.DataAnnotations.Schema.Table(Name = "Users")]
    [System.Data.Linq.Mapping.Table(Name = "Users")]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    [System.Data.Linq.Mapping.Table(Name="Users")]
    public class User1
    {
        [Key]
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }

        //Additional properties not in database
        [Editable(false)]
        public string FullName { get { return string.Format("{0} {1}", FirstName, LastName); } }
        public List<User> Friends { get; set; }

    }




    public class Car
    {
        #region DatabaseFields
        [System.ComponentModel.DataAnnotations.Key]
        public int CarId { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        #endregion

        #region RelatedTables
        public List<User> Users { get; set; }
        #endregion

        #region AdditionalFields
        [System.ComponentModel.DataAnnotations.Editable(false)]
        public string MakeWithModel{get { return Make + " (" + Model + ")"; }}
        #endregion

    }

    public class Tests
    {
        private IDbConnection GetOpenConnection()
        {
            var projLoc = Assembly.GetAssembly(GetType()).Location;
            var projFolder = Path.GetDirectoryName(projLoc);

            var connection = new SqlCeConnection("Data Source = " + projFolder + "\\Test.sdf;");
            connection.Open();
            return connection;
        }


        public void InsertWithSpecifiedTableName()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new User { Name = "User1", Age = 10 });
                id.IsEqualTo(1);
            }
        }


        public void TestSimpleGet()
        {
            using (var connection = GetOpenConnection())
            {
                var user = connection.Get<User>(1);
                user.Name.IsEqualTo("User1");
            }
        }

        public void TestDeleteById()
        {
            using (var connection = GetOpenConnection())
            {
                var user = connection.Delete<User>(1);
                connection.Get<User>(1).IsNull();
            }
        }

        public void TestDeleteByObject()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new User { Name = "User2", Age = 10 });
                var user = connection.Get<User>(2);
                connection.Delete(user);
                connection.Get<User>(2).IsNull();
            }
        }

        public void TestSimpleGetList()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "User3", Age = 10 });
                connection.Insert(new User { Name = "User4", Age = 10 });
                var user = connection.GetList<User>(new { });
                user.Count().IsEqualTo(2);
                connection.Delete<User>(3);
                connection.Delete<User>(4);
            }
        }

        public void TestFilteredGetList()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "User5", Age = 10 });
                connection.Insert(new User { Name = "User6", Age = 10 });
                connection.Insert(new User { Name = "User7", Age = 10 });
                connection.Insert(new User { Name = "User8", Age = 11 });

                var user = connection.GetList<User>(new { Age = 10 });
                user.Count().IsEqualTo(3);
            }
        }

        public void InsertWithSpecifiedKey()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new Car { Make = "Honda", Model = "Civic" });
                id.IsEqualTo(1);
            }
        }


        public void InsertWithExtraPropertiesShouldSkipNonSimpleTypesAndPropertiesMarkedEditableFalse()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new Car { Make = "Honda", Model = "Civic", Users = new List<User>() { new User() { Age = 12, Name = "test" } } });
                id.IsEqualTo(2);
            }
        }

        public void TestUpdate()
        {
            using (var connection = GetOpenConnection())
            {
                var newid = connection.Insert(new Car { Make = "Honda", Model = "Civic"});
                var newitem = connection.Get<Car>(3);
                newitem.Make = "Toyota";
                connection.Update(newitem);
                var updateditem = connection.Get<Car>(newid);
                updateditem.Make.IsEqualTo("Toyota");
                connection.Delete<Car>(newid);
            }
        }


    }
}
