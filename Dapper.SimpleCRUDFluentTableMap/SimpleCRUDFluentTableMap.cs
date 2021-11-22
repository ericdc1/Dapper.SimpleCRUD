using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
 
namespace Dapper
{
    public static class SimpleCRUDFluentTableMap
    {
        public interface ITableMapper
        {
            string TableName { get; }
            IDictionary<PropertyInfo, Dapper.SimpleCRUD.IColumnProperties> ColumnPropertiesMaps { get; }
        }
 
        private static Dictionary<Type, ITableMapper> tableMappers = new Dictionary<Type, ITableMapper>();
 
        // public static void AddMappings(IEnumerable<TableMapper<>> mappers)
        // {
        //     foreach(var mapper in mappers) {
        //         AddMapping(mapper);
        //     }
        // }

        public static void AddMapping<T>(TableMapper<T> mapper)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));
            if (tableMappers.ContainsKey(typeof(T)))
            {    
                throw new ArgumentException($"mapper of type {mapper.GetType().FullName} already registered");
            }
            tableMappers.Add(typeof(T), mapper);
        }

        public static void AddAllMappingsFromAssembliesLoaded()
        {
            tableMappers.Clear();
            var iType = typeof(ITableMapper);
            foreach(var m in AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => iType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract && !x.IsGenericType))
            {
                var dt = m.BaseType.GetGenericArguments().First();
                tableMappers.Add(dt, (ITableMapper)Activator.CreateInstance(m));
            }
        }

        public static void RegisterSimpleCrudFluentTableMapResolver(){
            var fluentResolver = new FluentTableMapResolver();
            Dapper.SimpleCRUD.SetColumnPropertiesResolver(fluentResolver);
            Dapper.SimpleCRUD.SetTableNameResolver(fluentResolver);
            Dapper.SimpleCRUD.SetColumnNameResolver(fluentResolver);
        }

        internal static bool IsSimpleType(this Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            type = underlyingType ?? type;
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
                                    typeof(TimeSpan),
                                    typeof(byte[])
                                };
            return simpleTypes.Contains(type) || type.IsEnum;
        }
 
        public class FluentColumnProperties : Dapper.SimpleCRUD.IColumnProperties
        {
            public string AliasName { get; private set; }
            public string ColumnName { get; private set; }
            public bool IsKey { get; private set; }
            public bool IsRequired { get; private set; }
            public bool IsEditable { get; private set; }
            public bool IsNotMapped { get; private set; }
            public bool IsReadOnly { get; private set; }
            public bool IsIgnoredInSelect { get; private set; }
            public bool IsIgnoredInInsert { get; private set; }
            public bool IsIgnoredInUpdate { get; private set; }
            public FluentColumnProperties(PropertyInfo propertyInfo)
            {
                if (propertyInfo == null)
                    throw new ArgumentNullException(nameof(propertyInfo));
                this.ColumnName = propertyInfo.Name;
                IsEditable = propertyInfo.PropertyType.IsSimpleType();                
            }
 
            public FluentColumnProperties WithColumnName(string columnName)
            {
                if (string.IsNullOrEmpty(columnName))
                {
                    throw new ArgumentNullException(nameof(columnName));
                }
                this.AliasName = this.ColumnName;
                this.ColumnName = columnName;
                return this;
            }
 
            public FluentColumnProperties AsKey()
            {
                this.IsKey = true;
                return this;
            }
            public FluentColumnProperties AsRequired()
            {
                this.IsRequired = true;
                return this;
            }
            public FluentColumnProperties AsReadOnly()
            {
                this.IsReadOnly = true;
                return this;
            }
            public FluentColumnProperties AsNotMapped()
            {
                this.IsNotMapped = true;
                return this;
            }
            public FluentColumnProperties AsEditable()
            {
                this.IsEditable = true;
                return this;
            }
            public FluentColumnProperties AsNotEditable()
            {
                this.IsEditable = false;
                return this;
            }
            public FluentColumnProperties AsIgnoredInSelect()
            {
                this.IsIgnoredInSelect = true;
                return this;
            }
            public FluentColumnProperties AsIgnoredInUpdate()
            {
                this.IsIgnoredInUpdate = true;
                return this;
            }
            public FluentColumnProperties AsIgnoredInInsert()
            {
                this.IsIgnoredInInsert = true;
                return this;
            }
        }
 
        public class TableMapper<T> : ITableMapper
        {
            public string TableName { get; private set; }
 
            public IDictionary<PropertyInfo, Dapper.SimpleCRUD.IColumnProperties> ColumnPropertiesMaps { get; private set; }
           
            public TableMapper()
            {
                TableName = typeof(T).Name;
                ColumnPropertiesMaps = new Dictionary<PropertyInfo, Dapper.SimpleCRUD.IColumnProperties>();
            }
 
            protected void ToTableName(string tableName)
            {
                if (string.IsNullOrEmpty(tableName))
                {
                    throw new ArgumentNullException(nameof(tableName));
                }
                TableName = string.Join(".", tableName.Split('.').Select(s => Dapper.SimpleCRUD.Encapsulate(s)));
            }
            protected FluentColumnProperties Map(Expression<Func<T, object>> expression)
            {
                var pInfo = GetPropertyInfo(expression);
                if (ColumnPropertiesMaps.ContainsKey(pInfo))
                    throw new InvalidOperationException($"property {pInfo.Name} already Mapped");
                var colProperties = new FluentColumnProperties(pInfo);
                ColumnPropertiesMaps.Add(pInfo, colProperties);
                return colProperties;
            }
 
            /// <summary>
            ///     Gets the corresponding <see cref="PropertyInfo" /> from an <see cref="Expression" />.
            /// </summary>
            /// <param name="expression">The expression that selects the property to get info on.</param>
            /// <returns>The property info collected from the expression.</returns>
            /// <exception cref="ArgumentNullException">When <paramref name="expression" /> is <c>null</c>.</exception>
            /// <exception cref="ArgumentException">The expression doesn't indicate a valid property."</exception>
            /// https://stackoverflow.com/questions/17115634/get-propertyinfo-of-a-parameter-passed-as-lambda-expression
            private PropertyInfo GetPropertyInfo(Expression<Func<T, object>> expression)
            {
                switch (expression?.Body) {
                    case null:
                        throw new ArgumentNullException(nameof(expression));
                    case UnaryExpression unaryExp when unaryExp.Operand is MemberExpression memberExp:
                        return (PropertyInfo)memberExp.Member;
                    case MemberExpression memberExp:
                        return (PropertyInfo)memberExp.Member;
                    default:
                        throw new ArgumentException($"The expression doesn't indicate a valid property. [ {expression} ]");
                }
            }        
        }

        public class FluentTableMapResolver : Dapper.SimpleCRUD.IColumnPropertiesResolver, Dapper.SimpleCRUD.ITableNameResolver, Dapper.SimpleCRUD.IColumnNameResolver {
            public string ResolveTableName(Type type)
            {
                ITableMapper tm = null;        
                tableMappers.TryGetValue(type, out tm); 
                return tm?.TableName ?? type.Name;
            }
            public Dapper.SimpleCRUD.IColumnProperties ResolveColumnProperties(PropertyInfo propertyInfo)
            {
                ITableMapper tm = null;        
                tableMappers.TryGetValue(propertyInfo.ReflectedType, out tm); 
                var colProperties = tm?.ColumnPropertiesMaps
                    .Where(cpm => cpm.Key.Name == propertyInfo.Name)
                    .Select(cmp => cmp.Value)
                    .FirstOrDefault();
                return colProperties ?? new FluentColumnProperties(propertyInfo);
            }
            public string ResolveColumnName(PropertyInfo propertyInfo)
            {
                return ResolveColumnProperties(propertyInfo).ColumnName;
            }
        }
    }    
}