using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
 

namespace Dapper
{
    public static partial class SimpleCRUD
    {
        public static string GetByIdSql<T>(this T t,object id)
        {
            var currenttype = typeof(T);
            var idProps = GetIdProperties(currenttype).ToList();

            if (!idProps.Any())
                throw new ArgumentException("Get<T> only supports an entity with a [Key] or Id property");
            if (idProps.Count() > 1)
                throw new ArgumentException("Get<T> only supports an entity with a single [Key] or Id property");

            var onlyKey = idProps.First();
            var name = GetTableName(currenttype);
            var sb = new StringBuilder();
            sb.Append("Select ");
            //create a new empty instance of the type to get the base properties
            BuildSelect(sb, GetScaffoldableProperties((T)Activator.CreateInstance(typeof(T))).ToArray());
            sb.AppendFormat(" from {0}", name);
            sb.Append(" where " + GetColumnName(onlyKey) + " = @Id");

            var dynParms = new DynamicParameters();
            dynParms.Add("@id", id);

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("Get<{0}>: {1} with Id: {2}", currenttype, sb, id));

            return sb.ToString();
        }

        public static string GetListSql<T>(this T t, object whereConditions)
        {
            var currenttype = typeof(T);
            var idProps = GetIdProperties(currenttype).ToList();
            if (!idProps.Any())
                throw new ArgumentException("Entity must have at least one [Key] property");

            var name = GetTableName(currenttype);

            var sb = new StringBuilder();
            var whereprops = GetAllProperties(whereConditions).ToArray();
            sb.Append("Select ");
            //create a new empty instance of the type to get the base properties
            BuildSelect(sb, GetScaffoldableProperties((T)Activator.CreateInstance(typeof(T))).ToArray());
            sb.AppendFormat(" from {0}", name);

            if (whereprops.Any())
            {
                sb.Append(" where ");
                BuildWhere(sb, whereprops, (T)Activator.CreateInstance(typeof(T)));
            }

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("GetList<{0}>: {1}", currenttype, sb));

            return sb.ToString();
        }


        public static string InsertSql<T>(this T entity)
        {
            var idProps = GetIdProperties(entity).ToList();

            if (!idProps.Any())
                throw new ArgumentException("Insert<T> only supports an entity with a [Key] or Id property");
            if (idProps.Count() > 1)
                throw new ArgumentException("Insert<T> only supports an entity with a single [Key] or Id property");

           // var keyHasPredefinedValue = false;
 
            var name = GetTableName(entity);
            var sb = new StringBuilder();
            sb.AppendFormat("insert into {0}", name);
            sb.Append(" (");
            BuildInsertParameters(entity, sb);
            sb.Append(") ");
            sb.Append("values");
            sb.Append(" (");
            BuildInsertValues(entity, sb);
            sb.Append(")");

           

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("Insert: {0}", sb));

            return sb.ToString();
        }

        public static string UpdateSql<T>(this T t)
        {
            var idProps = GetIdProperties(t).ToList();

            if (!idProps.Any())
                throw new ArgumentException("Entity must have at least one [Key] or Id property");

            var name = GetTableName(t);

            var sb = new StringBuilder();
            sb.AppendFormat("update {0}", name);

            sb.AppendFormat(" set ");
            BuildUpdateSet(t, sb);
            sb.Append(" where ");
            BuildWhere(sb, idProps, t);

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("Update: {0}", sb));

            return sb.ToString();
            //   return connection.Execute(sb.ToString(), entityToUpdate, transaction, commandTimeout);
        }

        public static string DeleteSql<T>(this T t)
        {
            var idProps = GetIdProperties(t).ToList();


            if (!idProps.Any())
                throw new ArgumentException("Entity must have at least one [Key] or Id property");

            var name = GetTableName(t);

            var sb = new StringBuilder();
            sb.AppendFormat("delete from {0}", name);

            sb.Append(" where ");
            BuildWhere(sb, idProps, t);

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("Delete: {0}", sb));

            // return connection.Execute(sb.ToString(), entityToDelete, transaction, commandTimeout);
            return sb.ToString();
        }

        public static string DeleteByIdSql<T>(this T t, object id)
        {
            var currenttype = typeof(T);
            var idProps = GetIdProperties(currenttype).ToList();


            if (!idProps.Any())
                throw new ArgumentException("Delete<T> only supports an entity with a [Key] or Id property");
            if (idProps.Count() > 1)
                throw new ArgumentException("Delete<T> only supports an entity with a single [Key] or Id property");

            var onlyKey = idProps.First();
            var name = GetTableName(currenttype);

            var sb = new StringBuilder();
            sb.AppendFormat("Delete from {0}", name);
            sb.Append(" where " + GetColumnName(onlyKey) + " = @Id");

            //var dynParms = new DynamicParameters();
            //dynParms.Add("@id", id);

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("Delete<{0}> {1}", currenttype, sb));

           // return connection.Execute(sb.ToString(), dynParms, transaction, commandTimeout);
            return sb.ToString();
        }
    }
}
