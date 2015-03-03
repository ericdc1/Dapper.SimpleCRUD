Dapper.SimpleCRUD - simple CRUD helpers for Dapper
========================================
Features
--------
<img  align="right" src="https://raw.githubusercontent.com/ericdc1/Dapper.SimpleCRUD/master/images/SimpleCRUD-200x200.png" alt="SimpleCRUD">
Dapper.SimpleCRUD is a [single file](https://github.com/ericdc1/Dapper.SimpleCRUD/blob/master/Dapper.SimpleCRUD/SimpleCRUD.cs) you can drop in to your project that will extend your IDbConnection interface. (If you want dynamic support, you need an [additional file](https://github.com/ericdc1/Dapper.SimpleCRUD/blob/master/Dapper.SimpleCRUD%20NET45/SimpleCRUDAsync.cs).)

Who wants to write basic read/insert/update/delete statements? 

The existing Dapper extensions did not fit my ideal pattern. I wanted simple CRUD operations with smart defaults without anything extra. I also wanted to have models with additional properties that did not directly map to the database. For example - a FullName property that combines FirstName and LastName in its getter - and not add FullName to the Insert and Update statements.

I wanted the primary key column to be Id in most cases but allow overriding with an attribute.

Finally, I wanted the table name to match the class name by default but allow overriding with an attribute. 

This extension adds the following 8 helpers: 

- Get(id) - gets one record based on the primary key 
- GetList<Type>() - gets list of records all records from a table
- GetList<Type>(anonymous object for where clause) - gets list of all records matching the where options
- GetList<Type>(string for conditions) - gets list of all records matching the conditions
- Insert(entity) - Inserts a record and returns the new primary key
- Update(entity) - Updates a record
- Delete<Type>(id) - Deletes a record based on primary key
- Delete(entity) - Deletes a record based on the typed entity

For projects targeting .NET 4.5 or later, the following 8 helpers exist for async operations:

- GetAsync(id) - gets one record based on the primary key 
- GetListAsync<Type>() - gets list of records all records from a table
- GetListAsync<Type>(anonymous object for where clause) - gets list of all records matching the where options
- GetListAsync<Type>(string for conditions) - gets list of all records matching the conditions
- InsertAsync(entity) - Inserts a record and returns the new primary key
- UpdateAsync(entity) - Updates a record
- DeleteAsync<Type>(id) - Deletes a record based on primary key
- DeleteAsync(entity) - Deletes a record based on the typed entity

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
Select Id, Name, Age from [User] where Id = 1 
```

More complex example: 
```csharp
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserId { get; set; }
        [Column("strFirstName"]
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
    }
    
    var user = connection.Get<User>(1);  
```

Results in executing this SQL 
```sql
Select UserId, strFirstName as FirstName, LastName, Age from [Users] where UserId = @UserID
```

Notes:

- The [Key] attribute can be used from the Dapper namespace or from System.ComponentModel.DataAnnotations
- The [Table] attribute can be used from the Dapper namespace, System.ComponentModel.DataAnnotations.Schema, or System.Data.Linq.Mapping - By default the database table name will match the model name but it can be overridden with this.
- The [Column] attribute can be used from the Dapper namespace, System.ComponentModel.DataAnnotations.Schema, or System.Data.Linq.Mapping - By default the column name will match the property name but it can be overridden with this. You can even use the model property names in the where clause anonymous object and SimpleCRUD will generate a proper where clause to match the database based on the column attribute

- GUID (uniqueidentifier) primary keys are supported (autopopulates if no value is passed in)

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
- If you need > < like, etc simply use the manual where clause method or Dapper's Query method

 
Execute a query with a where clause and map the results to a strongly typed List
------------------------------------------------------------

```csharp
public static IEnumerable<T> GetList<T>(this IDbConnection connection, string conditions)
```

Example usage: 

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}
  
var user = connection.GetList<User>("where age = 10 or Name like '%Smith%'");  
```
Results in 
```sql
Select * from [User] where age = 10 or Name like '%Smith%'
```

Notes:
- This uses your raw SQL so be careful to not create SQL injection holes
- There is nothing stopping you from adding an order by clause using this method 


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
   [ReadOnly(true)]
   public DateTime CreatedDate { get; set; }
}

var newId = connection.Insert(new User { Name = "User", Age = 10 });  
```
Results in executing this SQL 
```sql
Insert into [Users] (FirstName, LastName, Age) VALUES (@FirstName, @LastName, @Age)
```

Notes:
- Default table name would match the class name - The Table attribute overrides this
- Default primary key would be Id - The Key attribute overrides this
- By default the insert statement would include all properties in the class - The Editable(false) and ReadOnly(true) attributes remove items from the insert statement
- Properties decorated with ReadOnly(true) are only used for selects
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
   [Column("strFirstName")]
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
Update [Users] Set (strFirstName=@FirstName, LastName=@LastName, Age=@Age) Where ID = @ID
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

Database support
---------------------
* There is an option to change database dialect. Default is Microsoft SQL Server but can be changed to PostgreSQL or SQLite and possibly others down the road. 
```csharp 
   SimpleCRUD.SetDialect(SimpleCRUD.Dialect.PostgreSQL);
   
   SimpleCRUD.SetDialect(SimpleCRUD.Dialect.SQLite);
```

Do you have a comprehensive list of examples?
---------------------
Dapper.SimpleCRUD has a basic test suite in the [test project](https://github.com/ericdc1/dapper.SimpleCRUD/blob/master/Dapper.SimpleCRUD.Tests/Tests.cs)

There is also a sample website showing working examples of the the core functionality in the [demo website](https://github.com/ericdc1/Dapper.SimpleCRUD/tree/master/DemoWebsite)

Future
---------------------
I am considering the following based on feedback:
* Count methods
* Support for more database types (Firebird, SQLCe, etc) 
* Add paged getlist method for paging long lists

