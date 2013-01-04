using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Reflection;
using System.Text;
using Dapper;

namespace Dapper    
{

    public static class SimpleCRUD
    {
        /// <summary>
        /// Returns a single entity by a single id from table T. 
        /// By default queries the table matching the class name
        /// Table name can be overridden by adding an attribute on your class [Table("YourTableName")]
        /// By default filters on the Id column
        /// Id column name can be overridden by adding an attribute on your primary key property [Key]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="id"></param>
        /// <returns>Returns a single entity by a single id from table T.</returns>
        public static T Get<T>(this IDbConnection connection, int id)
        {
            var currenttype = typeof(T);
            var idProps = GetIdProperties(currenttype);

            if (idProps.Count() == 0)
                throw new ArgumentException("Get<T> only supports an entity with a [Key] or Id property");
            if (idProps.Count() > 1)
                throw new ArgumentException("Get<T> only supports an entity with a single [Key] or Id property");

            var OnlyKey = idProps.First();
            var name = GetTableName(currenttype);

            var sb = new StringBuilder();
            sb.AppendFormat("Select * from [{0}]", name);
            sb.Append(" where " + OnlyKey.Name + " = @Id");

            var dynParms = new DynamicParameters();
            dynParms.Add("@id", id);

            return connection.Query<T>(sb.ToString(), dynParms).FirstOrDefault();
        }

        /// <summary>
        /// Gets a list of entities with optional exact match where conditions
        /// By default queries the table matching the class name
        /// Table name can be overridden by adding an attribute on your class [Table("YourTableName")]
        /// whereConditions is an anonymous type to filter the results ex: new {Category = 1, SubCategory=2}
        /// To get all records use an empty anonymous object ex: new{}
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="whereConditions"></param>
        /// <returns>Gets a list of entities with optional exact match where conditions</returns>
        public static IEnumerable<T> GetList<T>(this IDbConnection connection, object whereConditions)
        {
            var currenttype = typeof(T);
            var idProps = GetIdProperties(currenttype);

            if (idProps.Count() == 0)
                throw new ArgumentException("Entity must have at least one [Key] property");

            var name = GetTableName(currenttype);

            var sb = new StringBuilder();
            var whereprops = GetAllProperties(whereConditions);
            sb.AppendFormat("Select * from [{0}]", name);

            if (whereprops.Count() > 0)
            {
                sb.Append(" where ");
                BuildWhere(sb, whereprops.ToArray());
            }

            return connection.Query<T>(sb.ToString(), whereConditions);
        }

        /// <summary>
        /// Gets a list of all entities
        /// By default queries the table matching the class name
        /// Table name can be overridden by adding an attribute on your class [Table("YourTableName")]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <returns>Gets a list of all entities</returns>
        public static IEnumerable<T> GetList<T>(this IDbConnection connection)
        {
            return connection.GetList<T>(new {});
        }

        /// <summary>
        /// Inserts a row into the database
        /// By default inserts into the table matching the class name
        /// Table name can be overridden by adding an attribute on your class [Table("YourTableName")]
        /// Insert filters out Id column and any columns with the [Key] attribute
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="entityToInsert"></param>
        /// <returns>The ID (primary key) of the newly inserted record</returns>
        public static int Insert(this IDbConnection connection, object entityToInsert)
        {
            var name = GetTableName(entityToInsert);

            var sb = new StringBuilder();
            sb.AppendFormat("insert into [{0}]", name);
            sb.Append(" (");
            BuildInsertParameters(entityToInsert, sb);
            sb.Append(") values (");
            BuildInsertValues(entityToInsert, sb);
            sb.Append(")");
         
            //sqlce doesn't support scope_identity so we have to dumb it down
            //sb.Append("; select cast(scope_identity() as int)");
            //var newId = connection.Query<int?>(sb.ToString(), entityToInsert).Single();
            //return (newId == null) ? 0 : (int)newId;

            connection.Execute(sb.ToString(), entityToInsert);
            var r = connection.Query("select @@IDENTITY id");
            return (int)r.First().id;
        }

        /// <summary>
        /// Updates a record or records in the database
        /// By default updates records in the table matching the class name
        /// Table name can be overridden by adding an attribute on your class [Table("YourTableName")]
        /// Updates records where the Id property and properties with the [Key] attribute match those in the database
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="entityToUpdate"></param>
        /// <returns>The number of effected records</returns>
        public static int Update(this IDbConnection connection, object entityToUpdate)
        {
            var idProps = GetIdProperties(entityToUpdate);
            if (idProps.Count() == 0)
                throw new ArgumentException("Entity must have at least one [Key] or Id property");

            var name = GetTableName(entityToUpdate);

            var sb = new StringBuilder();
            sb.AppendFormat("update [{0}]", name);

            sb.AppendFormat(" set ");
            BuildUpdateSet(entityToUpdate, sb);
            sb.Append(" where ");
            BuildWhere(sb, idProps.ToArray());

            return connection.Execute(sb.ToString(), entityToUpdate);
        }

        /// <summary>
        /// Deletes a record or records in the database that match the object passed in
        /// By default deletes records in the table matching the class name
        /// Table name can be overridden by adding an attribute on your class [Table("YourTableName")]
        /// Deletes records that match the entity passed in
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="entityToDelete"></param>
        /// <returns>The number of records effected</returns>
        public static int Delete<T>(this IDbConnection connection, T entityToDelete)
        {
            var idProps = GetIdProperties(entityToDelete);

            if (idProps.Count() == 0)
                throw new ArgumentException("Entity must have at least one [Key] or Id property");

            var name = GetTableName(entityToDelete);

            var sb = new StringBuilder();
            sb.AppendFormat("delete from [{0}]", name);

            sb.Append(" where ");
            BuildWhere(sb, idProps);

            return connection.Execute(sb.ToString(), entityToDelete);
        }

        /// <summary>
        /// Deletes a record or records in the database by ID
        /// By default deletes records in the table matching the class name
        /// Table name can be overridden by adding an attribute on your class [Table("YourTableName")]
        /// Deletes records where the Id property and properties with the [Key] attribute match those in the database
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="id"></param>
        /// <returns>The number of records effected</returns>
        public static int Delete<T>(this IDbConnection connection, int id)
        {
            var currenttype = typeof(T);
            var idProps = GetIdProperties(currenttype);

            if (idProps.Count() == 0)
                throw new ArgumentException("Delete<T> only supports an entity with a [Key] or Id property");
            if (idProps.Count() > 1)
                throw new ArgumentException("Delete<T> only supports an entity with a single [Key] or Id property");

            var onlyKey = idProps.First();
            var name = GetTableName(currenttype);

            var sb = new StringBuilder();
            sb.AppendFormat("Delete from [{0}]", name);
            sb.Append(" where " + onlyKey.Name + " = @Id");

            var dynParms = new DynamicParameters();
            dynParms.Add("@id", id);

            return connection.Execute(sb.ToString(), dynParms);
        }

        //build update statement based on list on an entity
        private static void BuildUpdateSet(object entityToUpdate, StringBuilder sb)
        {
            var nonIdProps = GetNonIdProperties(entityToUpdate).ToArray();

            for (var i = 0; i < nonIdProps.Length; i++)
            {
                var property = nonIdProps[i];

                sb.AppendFormat("{0} = @{1}", property.Name, property.Name);
                if (i < nonIdProps.Length - 1)
                    sb.AppendFormat(", ");
            }
        }

        //build where clause based on list of properties
        private static void BuildWhere(StringBuilder sb, IEnumerable<PropertyInfo> idProps)
        {
            for (var i = 0; i < idProps.Count(); i++)
            {
                sb.AppendFormat("[{0}] = @{1}", idProps.ElementAt(i).Name, idProps.ElementAt(i).Name);
                if (i < idProps.Count() - 1)
                    sb.AppendFormat(" and ");
            }
        }

        //build insert values which include all properties in the class that are not marked with the Editable(false) attribute,
        //are not marked with the [Key] attribute, and are not named Id
        private static void BuildInsertValues(object entityToInsert, StringBuilder sb)
        {
            var props = GetScaffoldableProperties(entityToInsert);
            for (var i = 0; i < props.Count(); i++)
            {
                var property = props.ElementAt(i);
                if (property.GetCustomAttributes(true).Any(attr => attr.GetType().Name == "KeyAttribute")) continue;
                if (property.Name == "Id") continue;
                sb.AppendFormat("@{0}", property.Name);
                if (i < props.Count() - 1)
                    sb.Append(", ");
            }
        }

        //build insert parameters which include all properties in the class that are not marked with the Editable(false) attribute,
        //are not marked with the [Key] attribute, and are not named Id
        private static void BuildInsertParameters(object entityToInsert, StringBuilder sb)
        {
            var props = GetScaffoldableProperties(entityToInsert);

            for (var i = 0; i < props.Count(); i++)
            {
                var property = props.ElementAt(i);
                if (property.GetCustomAttributes(true).Any(attr => attr.GetType().Name == "KeyAttribute")) continue;
                if (property.Name == "Id") continue;
                sb.Append(property.Name);
                if (i < props.Count() - 1)
                    sb.Append(", ");
            }
        }

        //Get all properties in an entity
        private static IEnumerable<PropertyInfo> GetAllProperties(object entity)
        {
            if (entity == null) entity = new {};
            return entity.GetType().GetProperties();
        }

        //Get all properties that are not decorated with the Editable(false) attribute
        private static IEnumerable<PropertyInfo> GetScaffoldableProperties(object entity)
        {
            var props = entity.GetType().GetProperties().Where(p => p.GetCustomAttributes(true).Any(attr => attr.GetType().Name == "EditableAttribute" && !IsEditable(p)) == false);
            return props.Where(p => p.PropertyType.IsSimpleType() || IsEditable(p));
        }

        //Determine if the Attribute has an AllowEdit key and return its boolean state
        //fake the funk and try to mimick EditableAttribute in System.ComponentModel.DataAnnotations 
        //This allows use of the DataAnnotations property in the model and have the SimpleCRUD engine just figure it out without a reference
        public static bool IsEditable(PropertyInfo pi)
        {
            object[] attributes = pi.GetCustomAttributes(false);
            if (attributes.Length == 1)
            {
                dynamic write = attributes[0];
                return write.AllowEdit;
            }
            return false;
        }

        //Get all properties that are NOT named Id and DO NOT have the Key attribute
        private static IEnumerable<PropertyInfo> GetNonIdProperties(object entity)
        {
            return GetScaffoldableProperties(entity).Where(p => p.Name != "Id" && p.GetCustomAttributes(true).Any(attr => attr.GetType().Name == "KeyAttribute") == false);
        }

        //Get all properties that are named Id or have the Key attribute
        //For Inserts and updates we have a whole entity so this method is used
        private static IEnumerable<PropertyInfo> GetIdProperties(object entity)
        {
            var type = entity.GetType();
            return GetIdProperties(type);
        }

        //Get all properties that are named Id or have the Key attribute
        //For Get(id) and Delete(id) we don't have an entity, just the type so this method is used
        private static IEnumerable<PropertyInfo> GetIdProperties(Type type)
        {
            return type.GetProperties().Where(p => p.Name == "Id" || p.GetCustomAttributes(true).Any(attr => attr.GetType().Name == "KeyAttribute"));
        }


        //Gets the table name for this entity
        //For Inserts and updates we have a whole entity so this method is used
        //Uses class name by default and overrides if the class has a Table attribute
        private static string GetTableName(object entity)
        {
            var type = entity.GetType();
            return GetTableName(type);
        }

        //Gets the table name for this type
        //For Get(id) and Delete(id) we don't have an entity, just the type so this method is used
        //Use dynamic type to be able to handle both our Table-attribute and the DataAnnotation
        //Uses class name by default and overrides if the class has a Table attribute
        private static string GetTableName(Type type)
        {
            var tableName = type.Name;

            var tableattr = type.GetCustomAttributes(false).SingleOrDefault(attr => attr.GetType().Name == "TableAttribute") as dynamic;
            if (tableattr != null)
                tableName = tableattr.Name;

            return tableName;
        }
    }

    // Specify the table name of a poco
    //Don't depend on System.ComponentModel.DataAnnotations
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public TableAttribute(string tableName)
        {
            Name = tableName;
        }
        public string Name { get; private set; }
    }

    // Specify the primary key name of a poco
    //Don't depend on System.ComponentModel.DataAnnotations
    [AttributeUsage(AttributeTargets.Property)]
    public class KeyAttribute : Attribute
    {
    }

    // Specify the properties that are editable
    //Don't depend on System.ComponentModel.DataAnnotations
    [AttributeUsage(AttributeTargets.Property)]
    public class EditableAttribute : Attribute
    {
        public EditableAttribute(bool iseditable)
        {
            AllowEdit = iseditable;
        }
        public bool AllowEdit { get; private set; }
    }

}
public static class TypeExtension
{
    //You can't insert or update complex types. Lets filter them out.
    public static bool IsSimpleType(this Type type)
    {
        var simpleTypes = new List<Type>
                               {
                                   typeof(byte),
                                   typeof(sbyte),
                                   typeof(short),
                                   typeof(ushort),
                                   typeof(int),
                                   typeof(uint),
                                   typeof(long),
                                   typeof(ulong),
                                   typeof(float),
                                   typeof(double),
                                   typeof(decimal),
                                   typeof(bool),
                                   typeof(string),
                                   typeof(char),
                                   typeof(Guid),
                                   typeof(DateTime),
                                   typeof(DateTimeOffset),
                                   typeof(byte[])
                               };
        return simpleTypes.Contains(type);
    }
}