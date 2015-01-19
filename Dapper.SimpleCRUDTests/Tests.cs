using System.Collections;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Npgsql;


namespace Dapper.SimpleCRUDTests
{
    #region DTOClasses
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
        public string MakeWithModel { get { return Make + " (" + Model + ")"; } }
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

    public class GUIDTest
    {
        [Key]
        public Guid Guid { get; set; }
        public string Name { get; set; }
    }

    public class StrangeColumnNames
    {
        [Key]
        public int ItemId { get; set; }
        public string Word { get; set; }
        [Column("colstringstrangeword")]
        public string StrangeWord { get; set; }
        [Editable(false)]
        public string ExtraProperty { get; set; }

    }

    #endregion

    public class Tests
    {
        public Tests(SimpleCRUD.Dialect dbtype)
        {
            _dbtype = dbtype;
        }
        private SimpleCRUD.Dialect _dbtype;

        private IDbConnection GetOpenConnection()
        {

            IDbConnection connection;
            if (_dbtype == SimpleCRUD.Dialect.PostgreSQL)
            {
                connection = new NpgsqlConnection(String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4};", "localhost", "5432", "postgres", "postgrespass", "testdb"));
                SimpleCRUD.SetDialect(SimpleCRUD.Dialect.PostgreSQL);
            }
            else if (_dbtype == SimpleCRUD.Dialect.SQLite)
            {
                connection = new SQLiteConnection("Data Source=MyDatabase.sqlite;Version=3;");
                SimpleCRUD.SetDialect(SimpleCRUD.Dialect.SQLite);
            }
            else
            {
                connection = new SqlConnection(@"Data Source = (LocalDB)\v11.0;Initial Catalog=DapperSimpleCrudTestDb;Integrated Security=True;MultipleActiveResultSets=true;");
                SimpleCRUD.SetDialect(SimpleCRUD.Dialect.SQLServer);
            }

            connection.Open();
            return connection;
        }

        //basic tests
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
                var id = connection.Insert<long>(new BigCar { Make = "Big", Model = "Car" });
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
                connection.Execute("Delete from Users");
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
                connection.Execute("Delete from Users");
            }
        }

        public void TestFilteredWithSQLGetList()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "User5", Age = 10 });
                connection.Insert(new User { Name = "User6", Age = 10 });
                connection.Insert(new User { Name = "User7", Age = 10 });
                connection.Insert(new User { Name = "User8", Age = 11 });

                var user = connection.GetList<User>("where Name like 'User%' and Age = 10");
                user.Count().IsEqualTo(3);
                connection.Execute("Delete from Users");
            }
        }

        public void TestGetListWithNullWhere()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "User9", Age = 10 });
                var user = connection.GetList<User>(null);
                user.Count().IsEqualTo(1);
                connection.Execute("Delete from Users");
            }
        }

        public void TestGetListWithoutWhere()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "User10", Age = 10 });
                var user = connection.GetList<User>();
                user.Count().IsEqualTo(1);
                connection.Execute("Delete from Users");
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
                var newid = (int)connection.Insert(new Car { Make = "Honda", Model = "Civic" });
                var newitem = connection.Get<Car>(newid);
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
                connection.Insert(new Car { Make = "Honda", Model = "Civic" });
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
                var id = connection.Insert(new CarLog { LogNotes = "blah blah blah" });
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
                //note - there's not support for inserts without a non-int id, so drop down to a normal execute
                connection.Execute("INSERT INTO CITY (NAME, POPULATION) VALUES ('Morgantown', 31000)");
                var city = connection.Get<City>("Morgantown");
                city.Population.IsEqualTo(31000);
            }
        }

        public void TestDeleteFromTableWithNonIntPrimaryKey()
        {
            using (var connection = GetOpenConnection())
            {
                //note - there's not support for inserts without a non-int id, so drop down to a normal execute
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

        //dialect test 

        public void ChangeDialect()
        {
            SimpleCRUD.SetDialect(SimpleCRUD.Dialect.SQLServer);
            SimpleCRUD.GetDialect().IsEqualTo(SimpleCRUD.Dialect.SQLServer.ToString());
            SimpleCRUD.SetDialect(SimpleCRUD.Dialect.PostgreSQL);
            SimpleCRUD.GetDialect().IsEqualTo(SimpleCRUD.Dialect.PostgreSQL.ToString());
        }

        //GUID primary key tests

        public void InsertIntoTableWithUnspecifiedGuidKey()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert<Guid>(new GUIDTest { Name = "GuidUser" });
                id.GetType().Name.IsEqualTo("Guid");
            }
        }

        public void InsertIntoTableWithGuidKey()
        {
            using (var connection = GetOpenConnection())
            {
                var guid = new Guid("2a6fb33d-7141-47a0-b9fa-86a1a1945da9");
                var id = connection.Insert<Guid>(new GUIDTest { Name = "GuidUser", Guid = guid });
                id.IsEqualTo(guid);
            }
        }

        public void GetRecordWithGuidKey()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.GetList<GUIDTest>().First().Guid;
                var record = connection.Get<GUIDTest>(id);
                record.Name.IsEqualTo("GuidUser");
            }
        }

        public void DeleteRecordWithGuidKey()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.GetList<GUIDTest>().First().Guid;
                connection.Delete<GUIDTest>(id);
                connection.Get<GUIDTest>(id).IsNull();
            }
        }

        //async  tests
        public void TestMultiInsertASync()
        {
            using (var connection = GetOpenConnection())
            {
                connection.InsertAsync(new User { Name = "AsyncUser1", Age = 10 });
                connection.InsertAsync(new User { Name = "AsyncUser2", Age = 10 });
                connection.InsertAsync(new User { Name = "AsyncUser3", Age = 10 });
                connection.InsertAsync(new User { Name = "AsyncUser4", Age = 11 });
                System.Threading.Thread.Sleep(300);
                //tiny wait to let the inserts happen
                var list = connection.GetList<User>(new { Age = 10 });
                list.Count().IsEqualTo(3);
            }
        }

        public void MultiInsertWithGuidAsync()
        {
            using (var connection = GetOpenConnection())
            {
                connection.InsertAsync<Guid>(new GUIDTest { Name = "AsyncGUIDUser" });
                connection.InsertAsync<Guid>(new GUIDTest { Name = "AsyncGUIDUser" });
                connection.InsertAsync<Guid>(new GUIDTest { Name = "AsyncGUIDUser" });
                connection.InsertAsync<Guid>(new GUIDTest { Name = "AsyncGUIDUser" });
                //tiny wait to let the inserts happen
                System.Threading.Thread.Sleep(300);
                var list = connection.GetList<GUIDTest>(new { Name = "AsyncGUIDUser" });
                list.Count().IsEqualTo(4);
            }
        }

        //column attribute tests

        public void InsertWithSpecifiedColumnName()
        {
            using (var connection = GetOpenConnection())
            {
                var itemId = connection.Insert(new StrangeColumnNames { Word = "Word 1", StrangeWord = "Strange 1", });
                itemId.IsEqualTo(1);
            }
        }

        public void TestDeleteByObjectWithSpecifiedColumnName()
        {
            using (var connection = GetOpenConnection())
            {
                var strange = connection.Get<StrangeColumnNames>(1);
                connection.Delete(strange);
                connection.Get<StrangeColumnNames>(1).IsNull();
            }
        }

        public void TestSimpleGetListWithSpecifiedColumnName()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new StrangeColumnNames { Word = "Word 2", StrangeWord = "Strange 2", });
                connection.Insert(new StrangeColumnNames { Word = "Word 3", StrangeWord = "Strange 3", });
                var strange = connection.GetList<StrangeColumnNames>(new { });
                strange.First().StrangeWord.IsEqualTo("Strange 2");
                strange.Count().IsEqualTo(2);
                connection.Delete<StrangeColumnNames>(2);
                connection.Delete<StrangeColumnNames>(3);
            }
        }

        public void TestUpdateWithSpecifiedColumnName()
        {
            using (var connection = GetOpenConnection())
            {
                var newid = (int)connection.Insert(new StrangeColumnNames { Word = "Word Insert", StrangeWord = "Strange Insert" });
                var newitem = connection.Get<StrangeColumnNames>(newid);
                newitem.Word = "Word Update";
                connection.Update(newitem);
                var updateditem = connection.Get<StrangeColumnNames>(newid);
                updateditem.Word.IsEqualTo("Word Update");
                connection.Delete<StrangeColumnNames>(newid);
            }
        }

        public void TestFilteredGetListWithSpecifiedColumnName()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new StrangeColumnNames { Word = "Word 5", StrangeWord = "Strange 1", });
                connection.Insert(new StrangeColumnNames { Word = "Word 6", StrangeWord = "Strange 2", });
                connection.Insert(new StrangeColumnNames { Word = "Word 7", StrangeWord = "Strange 2", });
                connection.Insert(new StrangeColumnNames { Word = "Word 8", StrangeWord = "Strange 2", });

                var strange = connection.GetList<StrangeColumnNames>(new { StrangeWord = "Strange 2" });
                strange.Count().IsEqualTo(3);
                connection.Execute("Delete from Users");
            }
        }

    }
}
