using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;

namespace Dapper.SimpleCRUD.Tests
{
    //For .Net 4.5> [System.ComponentModel.DataAnnotations.Schema.Table("Users")]  or the attribute built into SimpleCRUD
    [Table("Users")]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        //we modified so enums were automatically handled, we should also automatically handle nullable enums
        public DayOfWeek? ScheduledDayOff { get; set; }
    }

    [Table("Users")]
    public class User1
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public int? ScheduledDayOff { get; set; }
    }


    public class Car
    {
        #region DatabaseFields
        //System.ComponentModel.DataAnnotations.Key
        [Key]
        public int CarId { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        #endregion

        #region RelatedTables
        public List<User> Users { get; set; }
        #endregion

        #region AdditionalFields
        [Editable(false)]
        public string MakeWithModel{get { return Make + " (" + Model + ")"; }}
        #endregion

    }

    public class BigCar
    {
        #region DatabaseFields
        //System.ComponentModel.DataAnnotations.Key
        [Key]
        public long CarId { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        #endregion

    }


    [Table("CarLog", Schema = "Log")]
    public class CarLog
    {
        public int Id { get; set; }
        public string LogNotes { get; set; }
    }

    /// <summary>
    /// This class should be used for failing tests, since no schema is specified and 'CarLog' is not on dbo
    /// </summary>
    [Table("CarLog")]
    public class SchemalessCarLog
    {
        public int Id { get; set; }
        public string LogNotes { get; set; }
    }

    public class City
    {
        [Key]
        public string Name { get; set; }
        public int Population { get; set; }
    }

    public class Tests
    {
        private IDbConnection GetOpenConnection()
        {
            var projLoc = Assembly.GetAssembly(GetType()).Location;
            var projFolder = Path.GetDirectoryName(projLoc);

            var connection = new SqlConnection(@"Data Source = (LocalDB)\v11.0;Initial Catalog=DapperSimpleCrudTestDb;Integrated Security=True");
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

        public void InsertUsingBigIntPrimaryKey()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert<long>(new BigCar() { Make = "Big", Model = "Car" });
                id.IsEqualTo(2147483650);
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
                connection.Delete<User>(1);
                connection.Get<User>(1).IsNull();
            }
        }

        public void TestDeleteByObject()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "User2", Age = 10 });
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
                connection.Delete<User>(5);
                connection.Delete<User>(6);
                connection.Delete<User>(7);
                connection.Delete<User>(8);
            }
        }

        public void TestGetListWithNullWhere()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "User9", Age = 10 });
                var user = connection.GetList<User>(null);
                user.Count().IsEqualTo(1);
                connection.Delete<User>(9);
            }
        }

        public void TestGetListWithoutWhere()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "User10", Age = 10 });
                var user = connection.GetList<User>();
                user.Count().IsEqualTo(1);
                connection.Delete<User>(10);
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
                var id = connection.Insert(new Car { Make = "Honda", Model = "Civic", Users = new List<User> { new User { Age = 12, Name = "test" } } });
                id.IsEqualTo(2);
            }
        }

        public void TestUpdate()
        {
            using (var connection = GetOpenConnection())
            {
                var newid = (int)connection.Insert(new Car { Make = "Honda", Model = "Civic"});
                var newitem = connection.Get<Car>(3);
                newitem.Make = "Toyota";
                connection.Update(newitem);
                var updateditem = connection.Get<Car>(newid);
                updateditem.Make.IsEqualTo("Toyota");
                connection.Delete<Car>(newid);
            }
        }

        public void TestDeleteByObjectWithAttributes()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new Car { Make = "Honda", Model ="Civic" });
                var car = connection.Get<Car>(4);
                connection.Delete(car);
                connection.Get<Car>(4).IsNull();
            }
        }

        public void TestComplexTypesMarkedEditableAreSaved()
        {
            using (var connection = GetOpenConnection())
            {
                var id = (int)connection.Insert(new User { Name = "User", Age = 11, ScheduledDayOff = DayOfWeek.Friday });
                var user1 = connection.Get<User>(id);
                user1.ScheduledDayOff.IsEqualTo(DayOfWeek.Friday);
                connection.Delete(user1);
            }
        }

        public void TestNullableSimpleTypesAreSaved()
        {
            using (var connection = GetOpenConnection())
            {
                var id = (int)connection.Insert(new User1 { Name = "User", Age = 11, ScheduledDayOff = 2 });
                var user1 = connection.Get<User1>(id);
                user1.ScheduledDayOff.IsEqualTo(2);
                connection.Delete(user1);
            }
        }

        public void TestInsertIntoDifferentSchema()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new CarLog { LogNotes = "blah blah blah"});
                id.IsEqualTo(1);
            }
        }

        public void TestGetFromDifferentSchema()
        {
            using (var connection = GetOpenConnection())
            {
                var carlog = connection.Get<CarLog>(1);
                carlog.LogNotes.IsEqualTo("blah blah blah");
            }
        }

        public void TestTryingToGetFromTableInSchemaWithoutDataAnnotationShouldFail()
        {
            using (var connection = GetOpenConnection())
            {
                try
                {
                    connection.Get<SchemalessCarLog>(1);
                }
                catch (Exception)
                {
                   //we expect to get an exception, so return
                    return;
                }
               
                //if we get here without throwing an exception, the test failed.
                throw new ApplicationException("Expected exception");
            }
        }

        public void TestGetFromTableWithNonIntPrimaryKey()
        {
            using (var connection = GetOpenConnection())
            {
                //note - there's not yet support for inserts without a non-int id, so drop down to a normal execute
                connection.Execute("INSERT INTO CITY (NAME, POPULATION) VALUES ('Morgantown', 31000)");
                var city = connection.Get<City>("Morgantown");
                city.Population.IsEqualTo(31000);
            }
        }

        public void TestDeleteFromTableWithNonIntPrimaryKey()
        {
            using (var connection = GetOpenConnection())
            {
                //note - there's not yet support for inserts without a non-int id, so drop down to a normal execute
                connection.Execute("INSERT INTO CITY (NAME, POPULATION) VALUES ('Fairmont', 18737)");
                connection.Delete<City>("Fairmont").IsEqualTo(1);
            }
        }

        public void TestNullableEnumInsert()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "Enum-y", Age = 10, ScheduledDayOff = DayOfWeek.Thursday });
                var user = connection.GetList<User>(new { Name = "Enum-y" }).FirstOrDefault() ?? new User();
                user.ScheduledDayOff.IsEqualTo(DayOfWeek.Thursday);
                connection.Delete<User>(user.Id);
            }
        }
    }
}
