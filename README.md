Dapper.SimpleCRUD - simple CRUD helpers for Dapper
========================================
Features
--------
Dapper.SimpleCRUD is a [single file](https://github.com/ericdc1/Dapper.SimpleCRUD/blob/master/Dapper.SimpleCRUD/SimpleCRUD.cs) you can drop in to your project that will extend your IDbConnection interface.

Who wants to write basic read/insert/update/delete statements? 

The existing Dapper extensions did not fit my ideal pattern. I wanted simple CRUD operations with smart defaults without anything extra. I also wanted to have models with additional properties that did not directly map to the database. For example - a FullName property that combines FirstName and LastName in its getter - and not add FullName to the Insert and Update statements.

I wanted the primary key column to be Id in most cases but allow overriding with an attribute.

Finally, I wanted the table name to match the class name by default but allow overriding with an attribute. 

This extension adds the following 7 helpers: 

- Get(id) - gets one record based on the primary key 
- GetList<Type>() - gets list of records all records from a table
- GetList<Type>(anonymous object for where clause) - gets list of all records matching the where options
- Insert<Type>(entity) - Inserts a record and returns the new primary key
- Update<Type>(entity) - Updates a record
- Delete<Type>(id) - Deletes a record based on primary key
- Delete(entity) - Deletes a record based on the typed entity

If you need something more complex use Dapper's Query or Execute methods!

Note: all extension methods assume the connection is already open, they will fail if the connection is closed.

Install via NuGet - https://nuget.org/packages/Dapper.SimpleCRUD

Check out the model generator [T4 template](https://nuget.org/packages/Dapper.SimpleCRUD.ModelGenerator/) to generate your POCOs. Documentation is at https://github.com/ericdc1/Dapper.SimpleCRUD/wiki/T4-Template

Get a single record mapped to a strongly typed object
------------------------------------------------------------

```csharp
 public static T Get<T>(this IDbConnection connection, int id)
```

Example basic usage:

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}
      
var user = connection.Get<User>(1);   
```
Results in executing this SQL 
```sql
Select * from [User] where Id = 1 
```

More complex example: 
```csharp
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
    }
    
    var user = connection.Get<User>(1);  
```

Results in executing this SQL 
```sql
Select * from [Users] where UserId = @UserID
```

Notes:

- The [Key] attribute can be used from the Dapper namespace or from System.ComponentModel.DataAnnotations
- The [Table] attribute can be used from the Dapper namespace, System.ComponentModel.DataAnnotations.Schema, or System.Data.Linq.Mapping



Execute a query and map the results to a strongly typed List
------------------------------------------------------------

```csharp
public static IEnumerable<T> GetList<T>(this IDbConnection connection)
```

Example usage: 

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}
```

```csharp     
var user = connection.GetList<User>();  
```
Results in 
```sql
Select * from [User]
```

Execute a query with where conditions and map the results to a strongly typed List
------------------------------------------------------------

```csharp
public static IEnumerable<T> GetList<T>(this IDbConnection connection, object whereConditions)
```

Example usage: 

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}
  
var user = connection.GetList<User>(new { Age = 10 });  
```
Results in 
```sql
Select * from [User] where Age = @Age
```

Notes:
- To get all records use an empty anonymous object - new{}
- The where options are mapped as "where [name] = [value]"
- If you need > < like, etc simply use Dapper's Query method


Insert a record
------------------------------------------------------------

```csharp
public static int Insert(this IDbConnection connection, object entityToInsert)
```

Example usage: 

```csharp     
[Table("Users")]
public class User
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

var newId = connection.Insert<User>(new User { Name = "User", Age = 10 });  
```
Results in executing this SQL 
```sql
Insert into [Users] (FirstName, LastName, Age) VALUES (@FirstName, @LastName, @Age)
```

Notes:
- Default table name would match the class name - The Table attribute overrides this
- Default primary key would be Id - The Key attribute overrides this
- By default the insert statement would include all properties in the class - The Editable(false) attribute removes item from the insert statement
- Complex types are not included in the insert statement - This keeps the List<User> out of the insert even without the Editable attribute. You can include complex types if you decorate them with Editable(true). This is useful for enumerators.


Update a record
------------------------------------------------------------

```csharp
public static int Update(this IDbConnection connection, object entityToUpdate)
```

Example usage: 

```csharp    
[Table("Users")]
public class User
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
connection.Update(entity);

```
Results in executing this SQL  
```sql
Update [Users] Set (FirstName=@FirstName, LastName=@LastName, Age=@Age) Where ID = @ID
```


Delete a record
------------------------------------------------------------

```csharp
public static int Delete<T>(this IDbConnection connection, int Id)
```

Example usage: 

```csharp     
public class User
{
   public int Id { get; set; }
   public string FirstName { get; set; }
   public string LastName { get; set; }
   public int Age { get; set; }
}
connection.Delete<User>(newid);

```
Or 

```csharp
public static int Delete<T>(this IDbConnection connection, T entityToDelete)
```

Example usage: 

```csharp 
public class User
{
   public int Id { get; set; }
   public string FirstName { get; set; }
   public string LastName { get; set; }
   public int Age { get; set; }
}

connection.Delete(entity);
```

Results in executing this SQL  
```sql
Delete From [User] Where ID = @ID
```

Postgres support
---------------------
Support for Postgres by using the proper identity ID of newly inserted columns and the option to override schema, table, and column encapsulation. The default setup encapsulates these items with [] characters to work with SQL Server. They can be overridden as such:
```csharp 
   Dapper.SimpleCRUD.SetSchemaNameEncapsulation("", "");
   Dapper.SimpleCRUD.SetColumnNameEncapsulation("", "");
   Dapper.SimpleCRUD.SetTableNameEncapsulation("", "");
'''

Do you have a comprehensive list of examples?
---------------------
Dapper.SimpleCRUD has a basic test suite in the [test project](https://github.com/ericdc1/dapper.SimpleCRUD/blob/master/Dapper.SimpleCRUD.Tests/Tests.cs)
