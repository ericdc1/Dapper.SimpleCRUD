using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System;
using MySql.Data.MySqlClient;
using Npgsql;

namespace Dapper.SimpleCRUDTests
{
    #region DTOClasses
    [Table("Users")]
    public class UserEditableSettings
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    //For .Net 4.5> [System.ComponentModel.DataAnnotations.Schema.Table("Users")]  or the attribute built into SimpleCRUD
    [Table("Users")]
    public class User : UserEditableSettings
{
        //we modified so enums were automatically handled, we should also automatically handle nullable enums
        public DayOfWeek? ScheduledDayOff { get; set; }

        [ReadOnly(true)]
        public DateTime CreatedDate { get; set; }

        [NotMapped]
        public int NotMappedInt { get; set; }
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
        public int? Id { get; set; }
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
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class StrangeColumnNames
    {
        [Key]
        [Column("ItemId")]
        public int Id { get; set; }
        public string Word { get; set; }
        [Column("colstringstrangeword")]
        public string StrangeWord { get; set; }
        [Column("KeywordedProperty")]
        public string Select { get; set; }
        [Editable(false)]
        public string ExtraProperty { get; set; }
    }

    public class IgnoreColumns
    {
        [Key]
        public int Id { get; set; }
        [IgnoreInsert]
        public string IgnoreInsert { get; set; }
        [IgnoreUpdate]
        public string IgnoreUpdate { get; set; }
        [IgnoreSelect]
        public string IgnoreSelect { get; set; }
        [IgnoreInsert]
        [IgnoreUpdate]
        [IgnoreSelect]
        public string IgnoreAll { get; set; }
    }

    public class UserWithoutAutoIdentity
    {
        [Key]
        [Required]
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
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
            else if (_dbtype == SimpleCRUD.Dialect.MySQL)
            {
                connection = new MySqlConnection(String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4};", "localhost", "3306", "admin", "admin", "testdb"));
                SimpleCRUD.SetDialect(SimpleCRUD.Dialect.MySQL);
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
        public void TestInsertWithSpecifiedTableName()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new User { Name = "User1", Age = 10 });
                connection.Delete<User>(id);

            }
        }

        public void TestInsertUsingBigIntPrimaryKey()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert<long, BigCar>(new BigCar { Make = "Big", Model = "Car" });
                connection.Delete<BigCar>(id);

            }
        }

        public void TestInsertUsingGenericLimitedFields()
        {
            using (var connection = GetOpenConnection())
            {
                //arrange
                var user = new User {Name = "User1", Age = 10, ScheduledDayOff = DayOfWeek.Friday};

                //act
                var id = connection.Insert<int?, UserEditableSettings>(user);

                //assert
                var insertedUser = connection.Get<User>(id);
                insertedUser.ScheduledDayOff.IsNull();

                connection.Delete<User>(id);
            }
        }

        public void TestInsertUsingGenericLimitedFieldsAsync()
        {
            using (var connection = GetOpenConnection())
            {
                //arrange
                var user = new User { Name = "User1", Age = 10, ScheduledDayOff = DayOfWeek.Friday };

                //act
                var idTask = connection.InsertAsync<int?, UserEditableSettings>(user);
                idTask.Wait();
                var id = idTask.Result;

                //assert
                var insertedUser = connection.Get<User>(id);
                insertedUser.ScheduledDayOff.IsNull();

                connection.Delete<User>(id);
            }
        }

        public void TestSimpleGet()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new User { Name = "UserTestSimpleGet", Age = 10 });
                var user = connection.Get<User>(id);
                user.Name.IsEqualTo("UserTestSimpleGet");
                connection.Delete<User>(id);

            }
        }

        public void TestDeleteById()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new User { Name = "UserTestDeleteById", Age = 10 });
                connection.Delete<User>(id);
                connection.Get<User>(id).IsNull();
            }
        }

        public void TestDeleteByObject()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new User { Name = "TestDeleteByObject", Age = 10 });
                var user = connection.Get<User>(id);
                connection.Delete(user);
                connection.Get<User>(id).IsNull();
            }
        }

        public void TestSimpleGetList()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "TestSimpleGetList1", Age = 10 });
                connection.Insert(new User { Name = "TestSimpleGetList2", Age = 10 });
                var user = connection.GetList<User>(new { });
                user.Count().IsEqualTo(2);
                connection.Execute("Delete from Users");
            }
        }

        public void TestFilteredGetList()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "TestFilteredGetList1", Age = 10 });
                connection.Insert(new User { Name = "TestFilteredGetList2", Age = 10 });
                connection.Insert(new User { Name = "TestFilteredGetList3", Age = 10 });
                connection.Insert(new User { Name = "TestFilteredGetList4", Age = 11 });

                var user = connection.GetList<User>(new { Age = 10 });
                user.Count().IsEqualTo(3);
                connection.Execute("Delete from Users");
            }
        }

        public void TestFilteredWithSQLGetList()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "TestFilteredWithSQLGetList1", Age = 10 });
                connection.Insert(new User { Name = "TestFilteredWithSQLGetList2", Age = 10 });
                connection.Insert(new User { Name = "TestFilteredWithSQLGetList3", Age = 10 });
                connection.Insert(new User { Name = "TestFilteredWithSQLGetList4", Age = 11 });

                var user = connection.GetList<User>("where Name like 'TestFilteredWithSQLGetList%' and Age = 10");
                user.Count().IsEqualTo(3);
                connection.Execute("Delete from Users");
            }
        }

        public void TestGetListWithNullWhere()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "TestGetListWithNullWhere", Age = 10 });
                var user = connection.GetList<User>(null);
                user.Count().IsEqualTo(1);
                connection.Execute("Delete from Users");
            }
        }

        public void TestGetListWithoutWhere()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "TestGetListWithoutWhere", Age = 10 });
                var user = connection.GetList<User>();
                user.Count().IsEqualTo(1);
                connection.Execute("Delete from Users");
            }
        }

        public void TestsGetListWithParameters()
       {
           using (var connection = GetOpenConnection())
           {
               connection.Insert(new User { Name = "TestsGetListWithParameters1", Age = 10 });
               connection.Insert(new User { Name = "TestsGetListWithParameters2", Age = 10 });
               connection.Insert(new User { Name = "TestsGetListWithParameters3", Age = 10 });
               connection.Insert(new User { Name = "TestsGetListWithParameters4", Age = 11 });

               var user = connection.GetList<User>("where Age > @Age", new { Age = 10 });
               user.Count().IsEqualTo(1);
               connection.Execute("Delete from Users");
           }
       }

        public void TestGetWithReadonlyProperty()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new User { Name = "TestGetWithReadonlyProperty", Age = 10 });
                var user = connection.Get<User>(id);
                user.CreatedDate.Year.IsEqualTo(DateTime.Now.Year);
                connection.Execute("Delete from Users");
            }
        }

        public void TestInsertWithReadonlyProperty()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new User { Name = "TestInsertWithReadonlyProperty", Age = 10, CreatedDate = new DateTime(2001, 1, 1) });
                var user = connection.Get<User>(id);
                //the date can't be 2001 - it should be the autogenerated date from the database
                user.CreatedDate.Year.IsEqualTo(DateTime.Now.Year);
                connection.Execute("Delete from Users");
            }
        }

        public void TestUpdateWithReadonlyProperty()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new User { Name = "TestUpdateWithReadonlyProperty", Age = 10 });
                var user = connection.Get<User>(id);
                user.Age = 11;
                user.CreatedDate = new DateTime(2001, 1, 1);
                connection.Update(user);
                user = connection.Get<User>(id);
                //don't allow changing created date since it has a readonly attribute
                user.CreatedDate.Year.IsEqualTo(DateTime.Now.Year);
                connection.Execute("Delete from Users");
            }
        }

        public void TestGetWithNotMappedProperty()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new User { Name = "TestGetWithNotMappedProperty", Age = 10, NotMappedInt = 1000 });
                var user = connection.Get<User>(id);
                user.NotMappedInt.IsEqualTo(0);
                connection.Execute("Delete from Users");
            }
        }

        public void TestInsertWithNotMappedProperty()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new User { Name = "TestInsertWithNotMappedProperty", Age = 10, CreatedDate = new DateTime(2001, 1, 1), NotMappedInt = 1000 });
                var user = connection.Get<User>(id);
                user.NotMappedInt.IsEqualTo(0);
                connection.Execute("Delete from Users");
            }
        }

        public void TestUpdateWithNotMappedProperty()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new User { Name = "TestUpdateWithNotMappedProperty", Age = 10 });
                var user = connection.Get<User>(id);
                user.Age = 11;
                user.CreatedDate = new DateTime(2001, 1, 1);
                user.NotMappedInt = 1234;
                connection.Update(user);
                user = connection.Get<User>(id);

                user.NotMappedInt.IsEqualTo(0);

                connection.Execute("Delete from Users");
            }
        }

        public void TestInsertWithSpecifiedKey()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new Car { Make = "Honda", Model = "Civic" });
                id.IsEqualTo(1);
            }
        }

        public void TestInsertWithExtraPropertiesShouldSkipNonSimpleTypesAndPropertiesMarkedEditableFalse()
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

        /// <summary>
        /// We expect scheduled day off to NOT be updated, since it's not a property of UserEditableSettings
        /// </summary>
        public void TestUpdateUsingGenericLimitedFields()
        {
            using (var connection = GetOpenConnection())
            {
                //arrange
                var user = new User { Name = "User1", Age = 10, ScheduledDayOff = DayOfWeek.Friday };
                user.Id = connection.Insert(user) ?? 0;

                user.ScheduledDayOff = DayOfWeek.Thursday;
                var userAsEditableSettings = (UserEditableSettings)user;
                userAsEditableSettings.Name = "User++";

                connection.Update(userAsEditableSettings);

                //act
                var insertedUser = connection.Get<User>(user.Id);

                //assert
                insertedUser.Name.IsEqualTo("User++");
                insertedUser.ScheduledDayOff.IsEqualTo(DayOfWeek.Friday);

                connection.Delete<User>(user.Id);
            }
        }

        /// <summary>
        /// We expect scheduled day off to NOT be updated, since it's not a property of UserEditableSettings
        /// </summary>
        public void TestUpdateUsingGenericLimitedFieldsAsync()
        {
            using (var connection = GetOpenConnection())
            {
                //arrange
                var user = new User { Name = "User1", Age = 10, ScheduledDayOff = DayOfWeek.Friday };
                user.Id = connection.Insert(user) ?? 0;

                user.ScheduledDayOff = DayOfWeek.Thursday;
                var userAsEditableSettings = (UserEditableSettings)user;
                userAsEditableSettings.Name = "User++";

                connection.UpdateAsync(userAsEditableSettings).Wait();

                //act
                var insertedUser = connection.Get<User>(user.Id);

                //assert
                insertedUser.Name.IsEqualTo("User++");
                insertedUser.ScheduledDayOff.IsEqualTo(DayOfWeek.Friday);

                connection.Delete<User>(user.Id);
            }
        }

        public void TestDeleteByObjectWithAttributes()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new Car { Make = "Honda", Model = "Civic" });
                var car = connection.Get<Car>(id);
                connection.Delete(car);
                connection.Get<Car>(id).IsNull();
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
                connection.Delete<CarLog>(id);

            }
        }

        public void TestGetFromDifferentSchema()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new CarLog { LogNotes = "TestGetFromDifferentSchema" });
                var carlog = connection.Get<CarLog>(id);
                carlog.LogNotes.IsEqualTo("TestGetFromDifferentSchema");
                connection.Delete<CarLog>(id);
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

        public void TestChangeDialect()
        {
            SimpleCRUD.SetDialect(SimpleCRUD.Dialect.SQLServer);
            SimpleCRUD.GetDialect().IsEqualTo(SimpleCRUD.Dialect.SQLServer.ToString());
            SimpleCRUD.SetDialect(SimpleCRUD.Dialect.PostgreSQL);
            SimpleCRUD.GetDialect().IsEqualTo(SimpleCRUD.Dialect.PostgreSQL.ToString());
        }


        //        A GUID is being created and returned on insert but never actually
        //applied to the insert query.

        //This can be seen on a table where the key
        //is a GUID and defaults to (newid()) and no GUID is provided on the
        //insert. Dapper will generate a GUID but it is not applied so the GUID is
        //generated by newid() but the Dapper GUID is returned instead which is
        //incorrect.


        //GUID primary key tests

        public void TestInsertIntoTableWithUnspecifiedGuidKey()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert<Guid, GUIDTest>(new GUIDTest { Name = "GuidUser" });
                id.GetType().Name.IsEqualTo("Guid");
                var record = connection.Get<GUIDTest>(id);
                record.Name.IsEqualTo("GuidUser");
                connection.Delete<GUIDTest>(id);
            }
        }

        public void TestInsertIntoTableWithGuidKey()
        {
            using (var connection = GetOpenConnection())
            {
                var guid = new Guid("1a6fb33d-7141-47a0-b9fa-86a1a1945da9");
                var id = connection.Insert<Guid, GUIDTest>(new GUIDTest { Name = "InsertIntoTableWithGuidKey", Id = guid });
                id.IsEqualTo(guid);
                connection.Delete<GUIDTest>(id);
            }
        }

        public void TestGetRecordWithGuidKey()
        {
            using (var connection = GetOpenConnection())
            {
                var guid = new Guid("2a6fb33d-7141-47a0-b9fa-86a1a1945da9");
                connection.Insert<Guid, GUIDTest>(new GUIDTest { Name = "GetRecordWithGuidKey", Id = guid });
                var id = connection.GetList<GUIDTest>().First().Id;
                var record = connection.Get<GUIDTest>(id);
                record.Name.IsEqualTo("GetRecordWithGuidKey");
                connection.Delete<GUIDTest>(id);

            }
        }

        public void TestDeleteRecordWithGuidKey()
        {
            using (var connection = GetOpenConnection())
            {
                var guid = new Guid("3a6fb33d-7141-47a0-b9fa-86a1a1945da9");
                connection.Insert<Guid, GUIDTest>(new GUIDTest { Name = "DeleteRecordWithGuidKey", Id = guid });
                var id = connection.GetList<GUIDTest>().First().Id;
                connection.Delete<GUIDTest>(id);
                connection.Get<GUIDTest>(id).IsNull();
            }
        }

        //async  tests
        public void TestMultiInsertASync()
        {
            using (var connection = GetOpenConnection())
            {
                connection.InsertAsync(new User { Name = "TestMultiInsertASync1", Age = 10 });
                connection.InsertAsync(new User { Name = "TestMultiInsertASync2", Age = 10 });
                connection.InsertAsync(new User { Name = "TestMultiInsertASync3", Age = 10 });
                connection.InsertAsync(new User { Name = "TestMultiInsertASync4", Age = 11 });
                System.Threading.Thread.Sleep(300);
                //tiny wait to let the inserts happen
                var list = connection.GetList<User>(new { Age = 10 });
                list.Count().IsEqualTo(3);
                connection.Execute("Delete from Users");

            }
        }

        public void MultiInsertWithGuidAsync()
        {
            using (var connection = GetOpenConnection())
            {
                connection.InsertAsync<Guid, GUIDTest>(new GUIDTest { Name = "MultiInsertWithGuidAsync" });
                connection.InsertAsync<Guid, GUIDTest>(new GUIDTest { Name = "MultiInsertWithGuidAsync" });
                connection.InsertAsync<Guid, GUIDTest>(new GUIDTest { Name = "MultiInsertWithGuidAsync" });
                connection.InsertAsync<Guid, GUIDTest>(new GUIDTest { Name = "MultiInsertWithGuidAsync" });
                //tiny wait to let the inserts happen
                System.Threading.Thread.Sleep(300);
                var list = connection.GetList<GUIDTest>(new { Name = "MultiInsertWithGuidAsync" });
                list.Count().IsEqualTo(4);
                connection.Execute("Delete from GUIDTest");
            }
        }

        public void TestSimpleGetAsync()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new User { Name = "TestSimpleGetAsync", Age = 10 });
                var user = connection.GetAsync<User>(id);
                user.Result.Name.IsEqualTo("TestSimpleGetAsync");
                connection.Delete<User>(id);
            }
        }

        public void TestDeleteByIdAsync()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new User { Name = "UserAsyncDelete", Age = 10 });
                connection.DeleteAsync<User>(id);
                //tiny wait to let the delete happen
                System.Threading.Thread.Sleep(300);
                connection.Get<User>(id).IsNull();
            }
        }

        public void TestDeleteByObjectAsync()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new User { Name = "TestDeleteByObjectAsync", Age = 10 });
                var user = connection.Get<User>(id);
                connection.DeleteAsync(user);
                connection.Get<User>(id).IsNull();
                connection.Delete<User>(id);
            }
        }

        public void TestSimpleGetListAsync()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "TestSimpleGetListAsync1", Age = 10 });
                connection.Insert(new User { Name = "TestSimpleGetListAsync2", Age = 10 });
                var user = connection.GetListAsync<User>(new { });
                user.Result.Count().IsEqualTo(2);
                connection.Execute("Delete from Users");
            }
        }

        public void TestFilteredGetListAsync()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "TestFilteredGetListAsync1", Age = 10 });
                connection.Insert(new User { Name = "TestFilteredGetListAsync2", Age = 10 });
                connection.Insert(new User { Name = "TestFilteredGetListAsync3", Age = 10 });
                connection.Insert(new User { Name = "TestFilteredGetListAsync4", Age = 11 });

                var user = connection.GetListAsync<User>(new { Age = 10 });
                user.Result.Count().IsEqualTo(3);
                connection.Execute("Delete from Users");
            }
        }

        public void TestFilteredGetListParametersAsync()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "TestFilteredGetListParametersAsync1", Age = 10 });
                connection.Insert(new User { Name = "TestFilteredGetListParametersAsync2", Age = 10 });
                connection.Insert(new User { Name = "TestFilteredGetListParametersAsync3", Age = 10 });
                connection.Insert(new User { Name = "TestFilteredGetListParametersAsync4", Age = 11 });

                var user = connection.GetListAsync<User>("where Age = @Age", new { Age = 10 });
                user.Result.Count().IsEqualTo(3);
                connection.Execute("Delete from Users");
            }
        }

        public void TestRecordCountAsync()
        {
            using (var connection = GetOpenConnection())
            {
                int x = 0;
                do
                {
                    connection.Insert(new User { Name = "Person " + x, Age = x, CreatedDate = DateTime.Now, ScheduledDayOff = DayOfWeek.Thursday });
                    x++;
                } while (x < 30);

                var resultlist = connection.GetList<User>();
                resultlist.Count().IsEqualTo(30);
                connection.RecordCountAsync<User>().Result.IsEqualTo(30);

                connection.RecordCountAsync<User>("where age = 10 or age = 11").Result.IsEqualTo(2);


                connection.Execute("Delete from Users");
            }

        }

        public void TestRecordCountByObjectAsync()
        {
            using (var connection = GetOpenConnection())
            {
                int x = 0;
                do
                {
                    connection.Insert(new User { Name = "Person " + x, Age = x, CreatedDate = DateTime.Now, ScheduledDayOff = DayOfWeek.Thursday });
                    x++;
                } while (x < 30);

                var resultlist = connection.GetList<User>();
                resultlist.Count().IsEqualTo(30);
                connection.RecordCountAsync<User>().Result.IsEqualTo(30);

                connection.RecordCountAsync<User>(new { age = 10 }).Result.IsEqualTo(1);


                connection.Execute("Delete from Users");
            }

        }

        //column attribute tests

        public void TestInsertWithSpecifiedColumnName()
        {
            using (var connection = GetOpenConnection())
            {
                var itemId = connection.Insert(new StrangeColumnNames { Word = "InsertWithSpecifiedColumnName", StrangeWord = "Strange 1" });
                itemId.IsEqualTo(1);
                connection.Delete<StrangeColumnNames>(itemId);

            }
        }

        public void TestDeleteByObjectWithSpecifiedColumnName()
        {
            using (var connection = GetOpenConnection())
            {
                var itemId = connection.Insert(new StrangeColumnNames { Word = "TestDeleteByObjectWithSpecifiedColumnName", StrangeWord = "Strange 1" });
                var strange = connection.Get<StrangeColumnNames>(itemId);
                connection.Delete(strange);
                connection.Get<StrangeColumnNames>(itemId).IsNull();
            }
        }

        public void TestSimpleGetListWithSpecifiedColumnName()
        {
            using (var connection = GetOpenConnection())
            {
                var id1 = connection.Insert(new StrangeColumnNames { Word = "TestSimpleGetListWithSpecifiedColumnName1", StrangeWord = "Strange 2", });
                var id2 = connection.Insert(new StrangeColumnNames { Word = "TestSimpleGetListWithSpecifiedColumnName2", StrangeWord = "Strange 3", });
                var strange = connection.GetList<StrangeColumnNames>(new { });
                strange.First().StrangeWord.IsEqualTo("Strange 2");
                strange.Count().IsEqualTo(2);
                connection.Delete<StrangeColumnNames>(id1);
                connection.Delete<StrangeColumnNames>(id2);
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
                connection.Execute("Delete from StrangeColumnNames");
            }
        }

        public void TestGetListPaged()
        {
            using (var connection = GetOpenConnection())
            {
                int x = 0;
                do
                {
                    connection.Insert(new User { Name = "Person " + x, Age = x, CreatedDate = DateTime.Now, ScheduledDayOff = DayOfWeek.Thursday });
                    x++;
                } while (x < 30);

                var resultlist = connection.GetListPaged<User>(2, 10, null, null);
                resultlist.Count().IsEqualTo(10);
                resultlist.Skip(4).First().Name.IsEqualTo("Person 14");
                connection.Execute("Delete from Users");
            }
        }

        public void TestGetListPagedWithParameters()
        {
            using (var connection = GetOpenConnection())
            {
                int x = 0;
                do
                {
                    connection.Insert(new User { Name = "Person " + x, Age = x, CreatedDate = DateTime.Now, ScheduledDayOff = DayOfWeek.Thursday });
                    x++;
                } while (x < 30);

                var resultlist = connection.GetListPaged<User>(1, 30, "where Age > @Age", null, new {Age = 14});
                resultlist.Count().IsEqualTo(15);
                resultlist.First().Name.IsEqualTo("Person 15");
                connection.Execute("Delete from Users");
            }
        }


        public void TestGetListPagedWithSpecifiedPrimaryKey()
        {
            using (var connection = GetOpenConnection())
            {
                int x = 0;
                do
                {
                    connection.Insert(new StrangeColumnNames { Word = "Word " + x, StrangeWord = "Strange " + x });
                    x++;
                } while (x < 30);

                var resultlist = connection.GetListPaged<StrangeColumnNames>(2, 10, null, null);
                resultlist.Count().IsEqualTo(10);
                resultlist.Skip(4).First().Word.IsEqualTo("Word 14");
                connection.Execute("Delete from StrangeColumnNames");
            }
        }
        public void TestGetListPagedWithWhereClause()
        {
            using (var connection = GetOpenConnection())
            {
                int x = 0;
                do
                {
                    connection.Insert(new User { Name = "Person " + x, Age = x, CreatedDate = DateTime.Now, ScheduledDayOff = DayOfWeek.Thursday });
                    x++;
                } while (x < 30);

                var resultlist1 = connection.GetListPaged<User>(1, 3, "Where Name LIKE 'Person 2%'", "age desc");
                resultlist1.Count().IsEqualTo(3);

                var resultlist = connection.GetListPaged<User>(2, 3, "Where Name LIKE 'Person 2%'", "age desc");
                resultlist.Count().IsEqualTo(3);
                resultlist.Skip(1).First().Name.IsEqualTo("Person 25");

                connection.Execute("Delete from Users");
            }
        }

        public void TestDeleteListWithWhereClause()
        {
            using (var connection = GetOpenConnection())
            {
                int x = 0;
                do
                {
                    connection.Insert(new User { Name = "Person " + x, Age = x, CreatedDate = DateTime.Now, ScheduledDayOff = DayOfWeek.Thursday });
                    x++;
                } while (x < 30);

                connection.DeleteList<User>("Where age > 9");
                var resultlist = connection.GetList<User>();
                resultlist.Count().IsEqualTo(10);
                connection.Execute("Delete from Users");
            }
        }

        public void TestDeleteListWithWhereObject()
        {
            using (var connection = GetOpenConnection())
            {
                int x = 0;
                do
                {
                    connection.Insert(new User { Name = "Person " + x, Age = x, CreatedDate = DateTime.Now, ScheduledDayOff = DayOfWeek.Thursday });
                    x++;
                } while (x < 10);

                connection.DeleteList<User>(new {age = 9});
                var resultlist = connection.GetList<User>();
                resultlist.Count().IsEqualTo(9);
                connection.Execute("Delete from Users");
            }
        }

        public void TestDeleteListWithParameters()
        {
            using (var connection = GetOpenConnection())
            {
                int x = 1;
                do
                {
                    connection.Insert(new User { Name = "Person " + x, Age = x, CreatedDate = DateTime.Now, ScheduledDayOff = DayOfWeek.Thursday });
                    x++;
                } while (x < 10);

                connection.DeleteList<User>("where age >= @Age", new { Age = 9 });
                var resultlist = connection.GetList<User>();
                resultlist.Count().IsEqualTo(8);
                connection.Execute("Delete from Users");
            }
        }

        public void TestRecordCountWhereClause()
        {
            using (var connection = GetOpenConnection())
            {
                int x = 0;
                do
                {
                    connection.Insert(new User { Name = "Person " + x, Age = x, CreatedDate = DateTime.Now, ScheduledDayOff = DayOfWeek.Thursday });
                    x++;
                } while (x < 30);

                var resultlist = connection.GetList<User>();
                resultlist.Count().IsEqualTo(30);
                connection.RecordCount<User>().IsEqualTo(30);

                connection.RecordCount<User>("where age = 10 or age = 11").IsEqualTo(2);


                connection.Execute("Delete from Users");
            }

        }

        public void TestRecordCountWhereObject()
        {
            using (var connection = GetOpenConnection())
            {
                int x = 0;
                do
                {
                    connection.Insert(new User { Name = "Person " + x, Age = x, CreatedDate = DateTime.Now, ScheduledDayOff = DayOfWeek.Thursday });
                    x++;
                } while (x < 30);

                var resultlist = connection.GetList<User>();
                resultlist.Count().IsEqualTo(30);
                connection.RecordCount<User>().IsEqualTo(30);

                connection.RecordCount<User>(new { age = 10}).IsEqualTo(1);


                connection.Execute("Delete from Users");
            }

        }

        public void TestRecordCountParameters()
        {
            using (var connection = GetOpenConnection())
            {
                int x = 0;
                do
                {
                    connection.Insert(new User { Name = "Person " + x, Age = x, CreatedDate = DateTime.Now, ScheduledDayOff = DayOfWeek.Thursday });
                    x++;
                } while (x < 30);

                var resultlist = connection.GetList<User>();
                resultlist.Count().IsEqualTo(30);
                connection.RecordCount<User>("where Age > 15").IsEqualTo(14);


                connection.Execute("Delete from Users");
            }

        }

        public void TestInsertWithSpecifiedPrimaryKey()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert(new UserWithoutAutoIdentity() { Id = 999, Name = "User999", Age = 10 });
                id.IsEqualTo(999);
                var user = connection.Get<UserWithoutAutoIdentity>(999);
                user.Name.IsEqualTo("User999");
                connection.Execute("Delete from UserWithoutAutoIdentity");
            }
        }


        public void TestInsertWithSpecifiedPrimaryKeyAsync()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.InsertAsync(new UserWithoutAutoIdentity() { Id = 999, Name = "User999Async", Age = 10 });
                id.Result.IsEqualTo(999);
                var user = connection.GetAsync<UserWithoutAutoIdentity>(999);
                user.Result.Name.IsEqualTo("User999Async");
                connection.Execute("Delete from UserWithoutAutoIdentity");
            }
        }


        public void TestGetListNullableWhere()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Insert(new User { Name = "TestGetListWithoutWhere", Age = 10, ScheduledDayOff = DayOfWeek.Friday });
                connection.Insert(new User { Name = "TestGetListWithoutWhere", Age = 10 });

                //test with null property
                var list = connection.GetList<User>(new { ScheduledDayOff = (DayOfWeek?)null });
                list.Count().IsEqualTo(1);


                // test with db.null value
                list = connection.GetList<User>(new { ScheduledDayOff = DBNull.Value });
                list.Count().IsEqualTo(1);

                connection.Execute("Delete from Users");
            }
        }

        //ignore attribute tests
        //i cheated here and stuffed all of these in one test
        //didn't implement in postgres or mysql tests yet
        public void IgnoreProperties()
        {
            using (var connection = GetOpenConnection())
            {
                var itemId = connection.Insert(new IgnoreColumns() { IgnoreInsert = "OriginalInsert", IgnoreUpdate = "OriginalUpdate", IgnoreSelect = "OriginalSelect", IgnoreAll = "OriginalAll" });
                var item = connection.Get<IgnoreColumns>(itemId);
                //verify insert column was ignored
                item.IgnoreInsert.IsNull(); 

                //verify select value wasn't selected 
                item.IgnoreSelect.IsNull();

                //verify the column is really there via straight dapper
                var fromDapper = connection.Query<IgnoreColumns>("Select * from IgnoreColumns where Id = @Id", new{id = itemId}).First();
                fromDapper.IgnoreSelect.IsEqualTo("OriginalSelect");
               
                //change value and update
                item.IgnoreUpdate = "ChangedUpdate";
                connection.Update(item);
                
                //verify that update didn't take effect
                item = connection.Get<IgnoreColumns>(itemId);
                item.IgnoreUpdate.IsEqualTo("OriginalUpdate");

                var allColumnDapper = connection.Query<IgnoreColumns>("Select IgnoreAll from IgnoreColumns where Id = @Id", new { id = itemId }).First();
                allColumnDapper.IgnoreAll.IsNull();

                connection.Delete<IgnoreColumns>(itemId);
            }
        }

    }
}
