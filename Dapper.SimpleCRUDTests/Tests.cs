﻿using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Collections.Generic;
using System;
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
        [Column("ItemId")]
        public int Id { get; set; }
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
                connection.Delete<User>(id);

            }
        }

        public void InsertUsingBigIntPrimaryKey()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert<long>(new BigCar { Make = "Big", Model = "Car" });
                id.IsEqualTo(2147483650);
                connection.Delete<BigCar>(id);

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

        public void TestUpsert()
        {
            using (var connection = GetOpenConnection())
            {
                connection.GetList<Car>("WHERE Make = 'McLaren' AND Model = 'F1'").Count().IsEqualTo(0);

                var car = new Car { Make = "McLaren", Model = "F1" };
                connection.Upsert(car).IsEqualTo(1);
                connection.GetList<Car>("WHERE Make = 'McLaren' AND Model = 'F1'").Count().IsEqualTo(1);
                car = connection.GetList<Car>("WHERE Make = 'McLaren' AND Model = 'F1'").ToList()[0];
                car.Make.IsEqualTo("McLaren");
                car.Model.IsEqualTo("F1");

                car.Make = "Bugatti";
                connection.Upsert(car).IsEqualTo(1);
                connection.GetList<Car>("WHERE Make = 'McLaren' AND Model = 'F1'").Count().IsEqualTo(0);
                connection.GetList<Car>("WHERE Make = 'Bugatti' AND Model = 'F1'").Count().IsEqualTo(1);
                car = connection.Get<Car>(car.CarId);
                car.Make.IsEqualTo("Bugatti");
                car.Model.IsEqualTo("F1");

                connection.Delete<Car>(car.CarId);
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
                connection.Delete<GUIDTest>(id);
            }
        }

        public void InsertIntoTableWithGuidKey()
        {
            using (var connection = GetOpenConnection())
            {
                var guid = new Guid("1a6fb33d-7141-47a0-b9fa-86a1a1945da9");
                var id = connection.Insert<Guid>(new GUIDTest { Name = "InsertIntoTableWithGuidKey", Guid = guid });
                id.IsEqualTo(guid);
                connection.Delete<GUIDTest>(id);
            }
        }

        public void GetRecordWithGuidKey()
        {
            using (var connection = GetOpenConnection())
            {
                var guid = new Guid("2a6fb33d-7141-47a0-b9fa-86a1a1945da9");
                connection.Insert<Guid>(new GUIDTest { Name = "GetRecordWithGuidKey", Guid = guid });
                var id = connection.GetList<GUIDTest>().First().Guid;
                var record = connection.Get<GUIDTest>(id);
                record.Name.IsEqualTo("GetRecordWithGuidKey");
                connection.Delete<GUIDTest>(id);

            }
        }

        public void DeleteRecordWithGuidKey()
        {
            using (var connection = GetOpenConnection())
            {
                var guid = new Guid("3a6fb33d-7141-47a0-b9fa-86a1a1945da9");
                connection.Insert<Guid>(new GUIDTest { Name = "DeleteRecordWithGuidKey", Guid = guid });
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
                connection.InsertAsync<Guid>(new GUIDTest { Name = "MultiInsertWithGuidAsync" });
                connection.InsertAsync<Guid>(new GUIDTest { Name = "MultiInsertWithGuidAsync" });
                connection.InsertAsync<Guid>(new GUIDTest { Name = "MultiInsertWithGuidAsync" });
                connection.InsertAsync<Guid>(new GUIDTest { Name = "MultiInsertWithGuidAsync" });
                //tiny wait to let the inserts happen
                System.Threading.Thread.Sleep(300);
                var list = connection.GetList<GUIDTest>(new { Name = "MultiInsertWithGuidAsync" });
                list.Count().IsEqualTo(4);
                connection.Execute("Delete from GUIDTest");
            }
        }

        public void TestMultiUpsertASync()
        {
            using (var connection = GetOpenConnection())
            {
                connection.UpsertAsync(new User { Name = "TestMultiUpsertASync1", Age = 10 });
                connection.UpsertAsync(new User { Name = "TestMultiUpsertASync2", Age = 10 });
                connection.UpsertAsync(new User { Name = "TestMultiUpsertASync3", Age = 10 });
                connection.UpsertAsync(new User { Name = "TestMultiUpsertASync4", Age = 11 });
                System.Threading.Thread.Sleep(300);
                //tiny wait to let the inserts happen
                var list = connection.GetList<User>(new { Age = 10 }).ToList();
                list.Count().IsEqualTo(3);
                list[0].Age = 8;
                list[1].Age = 9;
                connection.UpsertAsync(list[0]);
                connection.UpsertAsync(list[1]);
                list = connection.GetList<User>(new { Age = 10 }).ToList();
                list.Count().IsEqualTo(1);
                connection.Execute("Delete from Users");
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


        //column attribute tests

        public void InsertWithSpecifiedColumnName()
        {
            using (var connection = GetOpenConnection())
            {
                var itemId = connection.Insert(new StrangeColumnNames { Word = "InsertWithSpecifiedColumnName", StrangeWord = "Strange 1", });
                itemId.IsEqualTo(1);
                connection.Delete<StrangeColumnNames>(itemId);

            }
        }

        public void TestDeleteByObjectWithSpecifiedColumnName()
        {
            using (var connection = GetOpenConnection())
            {
                var itemId = connection.Insert(new StrangeColumnNames { Word = "TestDeleteByObjectWithSpecifiedColumnName", StrangeWord = "Strange 1", });
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

    }
}
