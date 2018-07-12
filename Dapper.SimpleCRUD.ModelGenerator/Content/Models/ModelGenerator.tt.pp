<#@ template hostspecific="True" #>
<#@ assembly name="EnvDTE" #>
<#@ assembly name="System.Core.dll" #>
<#@ assembly name="System.Data" #>
<#@ assembly name="System.Data.Entity.Design" #>
<#@ assembly name="System.Xml" #>
<#@ assembly name="System.Configuration" #>
<#@ assembly name="System.Windows.Forms" #>
<#@ import namespace="System.Collections.Generic" #>
<#@ import namespace="System.Data" #>
<#@ import namespace="System.Data.SqlClient" #>
<#@ import namespace="System.Data.Common" #>
<#@ import namespace="System.Diagnostics" #>
<#@ import namespace="System.Globalization" #>
<#@ import namespace="System.IO" #>
<#@ import namespace="System.Linq" #>
<#@ import namespace="System.Text" #>
<#@ import namespace="System.Text.RegularExpressions" #>
<#@ import namespace="System.Configuration" #>
<#@ import namespace="System.Windows.Forms" #>
<#
/*
This code is part of the Dapper.SimpleCRUD project
It is based on the T4 template from the PetaPoco project which in turn is based on the subsonic project.
 -----------------------------------------------------------------------------------------
 This template can read minimal schema information from the following databases:

	* SQL Server
 -----------------------------------------------------------------------------------------
*/
	// Settings
    ConnectionStringName = ""; // Uses last connection string in config if not specified
	ConfigPath = @""; //Looks in current project for web.config or app.config by default. This overrides to a relative path - useful for seperate class library projects.
    Namespace = "$rootnamespace$.Models";
	ClassPrefix = "";
	ClassSuffix = "";
    IncludeViews = true;
    IncludeRelationships = true;
	ExcludeTablePrefixes = new string[]{"aspnet_","webpages_"};

    // Read schema
	var tables = LoadTables();

/*
	// Tweak Schema
	tables["tablename"].Ignore = true;							// To ignore a table
	tables["tablename"].ClassName = "newname";					// To change the class name of a table
	tables["tablename"]["columnname"].Ignore = true;			// To ignore a column
	tables["tablename"]["columnname"].PropertyName="newname";	// To change the property name of a column
	tables["tablename"]["columnname"].PropertyType="bool";		// To change the property type of a column
*/
#>
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace <#=Namespace #>
{
<#
foreach(Table tbl in from t in tables where !t.Ignore select t){
		if(IsExcluded(tbl.Name, ExcludeTablePrefixes)) continue;
#>
    /// <summary>
    /// A class which represents the <#=tbl.Name#> <#=(tbl.IsView)?"view":"table"#>.
    /// </summary>
	[Table("<#=tbl.Name#>")]
	public partial class <#=tbl.ClassName#>
	{
<#foreach(Column col in from c in tbl.Columns where !c.Ignore select c)
{#>
	<# if (tbl.IsPrimaryKeyColumn(col.PropertyName)) { #>
	[Key]
	<#}#>
	public virtual <#=col.PropertyType #><#=CheckNullable(col)#> <#=col.PropertyName #> { get; set; }
<#}#>
<# if (IncludeRelationships) { #>
<#foreach(Key key in from k in tbl.OuterKeys select k)
{#>
		public virtual <#=tables[key.ReferencedTableName].ClassName #> <# if (tables[key.ReferencedTableName].ClassName==tbl.ClassName) { #>FK_<#}#><#=tables[key.ReferencedTableName].ClassName #> { get; set; }
<#}#>
<#foreach(Key key in from k in tbl.InnerKeys select k)
{#>
		public virtual IEnumerable<<#=tables[key.ReferencingTableName].ClassName #>> <#=tables[key.ReferencingTableName].CleanName #> { get; set; }
<#}#>
<#}#>
	}

<#}#>
}
<#+
/*
The contents of this file are subject to the New BSD
 License (the "License"); you may not use this file
 except in compliance with the License. You may obtain a copy of
 the License at http://www.opensource.org/licenses/bsd-license.php
 
 Software distributed under the License is distributed on an 
 "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or
 implied. See the License for the specific language governing
 rights and limitations under the License.
*/

string ConnectionStringName = "";
string ConfigPath = "";
string Namespace = "";
string ClassPrefix = "";
string ClassSuffix = "";
string SchemaName = null;
bool IncludeViews;
bool IncludeRelationships;
string[] ExcludeTablePrefixes = new string[]{};

public class Table
{
    public List<Column> Columns;	
	public List<Key> InnerKeys = new List<Key>();
    public List<Key> OuterKeys = new List<Key>(); 
    public string Name;
	public string Schema;
	public bool IsView;
    public string CleanName;
    public string ClassName;
	public string SequenceName;
	public bool Ignore;

	public bool IsPrimaryKeyColumn(string columnName)
	{
		//return Columns.Single(x=>string.Compare(x.Name, columnName, true)==0).IsPK;
		var found = Columns.FirstOrDefault(x=> string.Compare(x.Name, columnName, true)==0);		
		return found == null ? false : found.IsPK;
	}

	public Column GetColumn(string columnName)
	{
		return Columns.Single(x=>string.Compare(x.Name, columnName, true)==0);
	}

	public Column this[string columnName]
	{
		get
		{
			return GetColumn(columnName);
		}
	}

}

public class Column
{
    public string Name;
    public string PropertyName;
    public string PropertyType;
    public bool IsPK;
    public bool IsNullable;
	public bool IsAutoIncrement;
	public bool Ignore;
}

public class Key
{
    public string Name;
    public string ReferencedTableName;
    public string ReferencedTableColumnName;
    public string ReferencingTableName;
    public string ReferencingTableColumnName;
}

public class Tables : List<Table>
{
	public Tables()
	{
	}
	
	public Table GetTable(string tableName)
	{
		return this.Single(x=>string.Compare(x.Name, tableName, true)==0);
	}

	public Table this[string tableName]
	{
		get
		{
			return GetTable(tableName);
		}
	}

}

static Regex rxCleanUp = new Regex(@"[^\w\d_]", RegexOptions.Compiled);

static Func<string, string> CleanUp = (str) =>
{
	str = rxCleanUp.Replace(str, "_");
	if (char.IsDigit(str[0])) str = "_" + str;
	
    return str;
};

string CheckNullable(Column col)
{
    string result="";
    if(col.IsNullable && 
		col.PropertyType !="byte[]" && 
		col.PropertyType !="string" &&
		col.PropertyType !="Microsoft.SqlServer.Types.SqlGeography" &&
		col.PropertyType !="Microsoft.SqlServer.Types.SqlGeometry"
		)
        result="?";
    return result;
}

string GetConnectionString(ref string connectionStringName, out string providerName)
{
    var _CurrentProject = GetCurrentProject();

	providerName=null;
    
    string result="";
    ExeConfigurationFileMap configFile = new ExeConfigurationFileMap();
    configFile.ExeConfigFilename = GetConfigPath();

    if (string.IsNullOrEmpty(configFile.ExeConfigFilename))
        throw new ArgumentNullException("The project does not contain App.config or Web.config file.");
    
    
    var config = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(configFile, ConfigurationUserLevel.None);
    var connSection=config.ConnectionStrings;

    //if the connectionString is empty - which is the defauls
    //look for count-1 - this is the last connection string
    //and takes into account AppServices and LocalSqlServer
    if(string.IsNullOrEmpty(connectionStringName))
    {
        if(connSection.ConnectionStrings.Count>1)
        {
			connectionStringName = connSection.ConnectionStrings[connSection.ConnectionStrings.Count-1].Name;
            result=connSection.ConnectionStrings[connSection.ConnectionStrings.Count-1].ConnectionString;
            providerName=connSection.ConnectionStrings[connSection.ConnectionStrings.Count-1].ProviderName;
        }            
    }
    else
    {
        try
        {
            result=connSection.ConnectionStrings[connectionStringName].ConnectionString;
            providerName=connSection.ConnectionStrings[connectionStringName].ProviderName;
        }
        catch
        {
            result="There is no connection string name called '"+connectionStringName+"'";
        }
    }

//	if (String.IsNullOrEmpty(providerName))
//		providerName="System.Data.SqlClient";
    
    return result;
}

string _connectionString="";
string _providerName="";

void InitConnectionString()
{
    if(String.IsNullOrEmpty(_connectionString))
    {
        _connectionString=GetConnectionString(ref ConnectionStringName, out _providerName);

		if(_connectionString.Contains("|DataDirectory|"))
		{
			//have to replace it
			string dataFilePath=GetDataDirectory();
			_connectionString=_connectionString.Replace("|DataDirectory|",dataFilePath);
		}    
	}
}

public string ConnectionString
{
    get 
    {
		InitConnectionString();
        return _connectionString;
    }
}

public string ProviderName
{
    get 
    {
		InitConnectionString();
        return _providerName;
    }
}

public EnvDTE.Project GetCurrentProject()  {

    IServiceProvider _ServiceProvider = (IServiceProvider)Host;
    if (_ServiceProvider == null)
        throw new Exception("Host property returned unexpected value (null)");
	
    EnvDTE.DTE dte = (EnvDTE.DTE)_ServiceProvider.GetService(typeof(EnvDTE.DTE));
    if (dte == null)
        throw new Exception("Unable to retrieve EnvDTE.DTE");
	
    Array activeSolutionProjects = (Array)dte.ActiveSolutionProjects;
    if (activeSolutionProjects == null)
        throw new Exception("DTE.ActiveSolutionProjects returned null");
	
    EnvDTE.Project dteProject = (EnvDTE.Project)activeSolutionProjects.GetValue(0);
    if (dteProject == null)
        throw new Exception("DTE.ActiveSolutionProjects[0] returned null");
	
    return dteProject;

}

private string GetProjectPath()
{
    EnvDTE.Project project = GetCurrentProject();
    System.IO.FileInfo info = new System.IO.FileInfo(project.FullName);
    return info.Directory.FullName;
}

private string GetConfigPath()
{
	if(ConfigPath !="")
		return Host.ResolvePath(ConfigPath);

    EnvDTE.Project project = GetCurrentProject();
    foreach (EnvDTE.ProjectItem item in project.ProjectItems)
    {
        // if it is the app.config file, then open it up
        if (item.Name.Equals("App.config",StringComparison.InvariantCultureIgnoreCase) || item.Name.Equals("Web.config",StringComparison.InvariantCultureIgnoreCase))
			return GetProjectPath() + "\\" + item.Name;
    }
    return String.Empty;
}

public string GetDataDirectory()
{
    EnvDTE.Project project=GetCurrentProject();
    return System.IO.Path.GetDirectoryName(project.FileName)+"\\App_Data\\";
}

static string zap_password(string connectionString)
{
	var rx = new Regex("Password=.*;", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);
	return rx.Replace(connectionString, "Password=******;");
}

static string Singularize(string word)
{
	var singularword = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(System.Globalization.CultureInfo.GetCultureInfo("en-us")).Singularize(word);
	return singularword;
}
		
static string RemoveTablePrefixes(string word)
{
	var cleanword = word;
	if(cleanword.StartsWith("tbl_")) cleanword = cleanword.Replace("tbl_",""); 
	if(cleanword.StartsWith("tbl")) cleanword = cleanword.Replace("tbl",""); 
	cleanword = cleanword.Replace("_","");
	return cleanword;
}

static bool IsExcluded(string tablename, string[] ExcludeTablePrefixes)
{
	for (int i = 0; i < ExcludeTablePrefixes.Length; i++)
	{
		string s = ExcludeTablePrefixes[i];
		if(tablename.StartsWith(s)) return true;
	}
	return false;
}

Tables LoadTables()
{
	InitConnectionString();

	WriteLine("// This file was automatically generated by the Dapper.SimpleCRUD T4 Template");
	WriteLine("// Do not make changes directly to this file - edit the template instead");
	WriteLine("// ");
	WriteLine("// The following connection settings were used to generate this file");
	WriteLine("// ");
	WriteLine("//     Connection String Name: `{0}`", ConnectionStringName);
	WriteLine("//     Provider:               `{0}`", ProviderName);
	WriteLine("//     Connection String:      `{0}`", zap_password(ConnectionString));
	WriteLine("//     Include Views:          `{0}`", IncludeViews);
	WriteLine("");

	DbProviderFactory _factory;
	try
	{
		_factory = DbProviderFactories.GetFactory(ProviderName);
	}
	catch (Exception x)
	{
		var error=x.Message.Replace("\r\n", "\n").Replace("\n", " ");
		Warning(string.Format("Failed to load provider `{0}` - {1}", ProviderName, error));
		WriteLine("");
		WriteLine("// -----------------------------------------------------------------------------------------");
		WriteLine("// Failed to load provider `{0}` - {1}", ProviderName, error);
		WriteLine("// -----------------------------------------------------------------------------------------");
		WriteLine("");
		return new Tables();
	}

	try
	{
		Tables result;
		using(var conn=_factory.CreateConnection())
		{
			conn.ConnectionString=ConnectionString;         
			conn.Open();
        
			SchemaReader reader=null;
        
			// Assume SQL Server
			reader=new SqlServerSchemaReader();
			
			reader.outer=this;
			result=reader.ReadSchema(conn, _factory);

			// Remove unrequired tables/views
			for (int i=result.Count-1; i>=0; i--)
			{
				if (SchemaName!=null && string.Compare(result[i].Schema, SchemaName, true)!=0)
				{
					result.RemoveAt(i);
					continue;
				}
				if (!IncludeViews && result[i].IsView)
				{
					result.RemoveAt(i);
					continue;
				}
			}

			conn.Close();


			var rxClean = new Regex("^(Equals|GetHashCode|GetType|ToString|repo|Save|IsNew|Insert|Update|Delete|Exists|SingleOrDefault|Single|First|FirstOrDefault|Fetch|Page|Query)$");
			foreach (var t in result)
			{
				t.ClassName = ClassPrefix + t.ClassName + ClassSuffix;
				foreach (var c in t.Columns)
				{
					c.PropertyName = rxClean.Replace(c.PropertyName, "_$1");

					// Make sure property name doesn't clash with class name
					if (c.PropertyName == t.ClassName)
						c.PropertyName = "_" + c.PropertyName;
				}
			}

		    return result;
		}
	}
	catch (Exception x)
	{
		var error=x.Message.Replace("\r\n", "\n").Replace("\n", " ");
		Warning(string.Format("Failed to read database schema - {0}", error));
		WriteLine("");
		WriteLine("// -----------------------------------------------------------------------------------------");
		WriteLine("// Failed to read database schema - {0}", error);
		WriteLine("// -----------------------------------------------------------------------------------------");
		WriteLine("");
		return new Tables();
	}

        
}

abstract class SchemaReader
{
	public abstract Tables ReadSchema(DbConnection connection, DbProviderFactory factory);
	public GeneratedTextTransformation outer;
	public void WriteLine(string o)
	{
		outer.WriteLine(o);
	}

}

class SqlServerSchemaReader : SchemaReader
{
	// SchemaReader.ReadSchema
	public override Tables ReadSchema(DbConnection connection, DbProviderFactory factory)
	{
		var result=new Tables();
		
		_connection=connection;
		_factory=factory;

		var cmd=_factory.CreateCommand();
		cmd.Connection=connection;
		cmd.CommandText=TABLE_SQL;

		//pull the tables in a reader
		using(cmd)
		{

			using (var rdr=cmd.ExecuteReader())
			{
				while(rdr.Read())
				{
					Table tbl=new Table();
					tbl.Name=rdr["TABLE_NAME"].ToString();
					tbl.Schema=rdr["TABLE_SCHEMA"].ToString();
					tbl.IsView=string.Compare(rdr["TABLE_TYPE"].ToString(), "View", true)==0;
					tbl.CleanName=CleanUp(tbl.Name);
					if(tbl.CleanName.StartsWith("tbl_")) tbl.CleanName = tbl.CleanName.Replace("tbl_",""); 
					if(tbl.CleanName.StartsWith("tbl")) tbl.CleanName = tbl.CleanName.Replace("tbl",""); 
					tbl.CleanName = tbl.CleanName.Replace("_","");
					tbl.ClassName=Singularize(RemoveTablePrefixes(tbl.CleanName));

					result.Add(tbl);
				}
			}
		}

		foreach (var tbl in result)
		{
			tbl.Columns=LoadColumns(tbl);
		            
			// Mark the primary key
			string[] PrimaryKeys=GetPK(tbl.Name);

			foreach (string primaryKey in PrimaryKeys)
			{
				var pkColumn=tbl.Columns.SingleOrDefault(x=>x.Name.ToLower().Trim()==primaryKey.ToLower().Trim());
				if(pkColumn!=null)
				{
					pkColumn.IsPK=true;
				}
			}

			try
		    {
		        tbl.OuterKeys = LoadOuterKeys(tbl);
		        tbl.InnerKeys = LoadInnerKeys(tbl);
		    }
		    catch (Exception x)
		    {
		        var error=x.Message.Replace("\r\n", "\n").Replace("\n", " ");
				WriteLine("");
				WriteLine("// -----------------------------------------------------------------------------------------");
				WriteLine(String.Format("// Failed to get relationships for `{0}` - {1}", tbl.Name, error));
				WriteLine("// -----------------------------------------------------------------------------------------");
				WriteLine("");
		    }
		}
	    

		return result;
	}
	
	DbConnection _connection;
	DbProviderFactory _factory;
	

	List<Column> LoadColumns(Table tbl)
	{
	
		using (var cmd=_factory.CreateCommand())
		{
			cmd.Connection=_connection;
			cmd.CommandText=COLUMN_SQL;

			var p = cmd.CreateParameter();
			p.ParameterName = "@tableName";
			p.Value=tbl.Name;
			cmd.Parameters.Add(p);

			p = cmd.CreateParameter();
			p.ParameterName = "@schemaName";
			p.Value=tbl.Schema;
			cmd.Parameters.Add(p);

			var result=new List<Column>();
			using (IDataReader rdr=cmd.ExecuteReader())
			{
				while(rdr.Read())
				{
					Column col=new Column();
					col.Name=rdr["ColumnName"].ToString();
					col.PropertyName=CleanUp(col.Name);
					col.PropertyType=GetPropertyType(rdr["DataType"].ToString());
					col.IsNullable=rdr["IsNullable"].ToString()=="YES";
					col.IsAutoIncrement=((int)rdr["IsIdentity"])==1;
					result.Add(col);
				}
			}

			return result;
		}
	}

	List<Key> LoadOuterKeys(Table tbl)
	{
		using (var cmd=_factory.CreateCommand())
		{
			cmd.Connection=_connection;
			cmd.CommandText=OUTER_KEYS_SQL;

			var p = cmd.CreateParameter();
			p.ParameterName = "@tableName";
			p.Value=tbl.Name;
			cmd.Parameters.Add(p);

			var result=new List<Key>();
			using (IDataReader rdr=cmd.ExecuteReader())
			{
				while(rdr.Read())
				{
					var key=new Key();
					key.Name=rdr["FK"].ToString();
				    key.ReferencedTableName = rdr["Referenced_tbl"].ToString();
				    key.ReferencedTableColumnName = rdr["Referenced_col"].ToString();
				    key.ReferencingTableColumnName = rdr["Referencing_col"].ToString();
					result.Add(key);
				}
			}

			return result;
		}
	}

	List<Key> LoadInnerKeys(Table tbl)
	{
		using (var cmd=_factory.CreateCommand())
		{
			cmd.Connection=_connection;
			cmd.CommandText=INNER_KEYS_SQL;

			var p = cmd.CreateParameter();
			p.ParameterName = "@tableName";
			p.Value=tbl.Name;
			cmd.Parameters.Add(p);

			var result=new List<Key>();
			using (IDataReader rdr=cmd.ExecuteReader())
			{
				while(rdr.Read())
				{
					var key=new Key();
					key.Name=rdr["FK"].ToString();
				    key.ReferencingTableName = rdr["Referencing_tbl"].ToString();
				    key.ReferencedTableColumnName = rdr["Referenced_col"].ToString();
				    key.ReferencingTableColumnName = rdr["Referencing_col"].ToString();
					result.Add(key);
				}
			}

			return result;
		}
	}

	string[] GetPK(string table){
		
		string sql=@"SELECT c.name AS ColumnName
                FROM sys.indexes AS i 
                INNER JOIN sys.index_columns AS ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id 
                INNER JOIN sys.objects AS o ON i.object_id = o.object_id 
                LEFT OUTER JOIN sys.columns AS c ON ic.object_id = c.object_id AND c.column_id = ic.column_id
                WHERE (i.type = 1) AND (o.name = @tableName)";

		List<string> primaryKeys = new List<string>();

		using (var cmd=_factory.CreateCommand())
		{
			cmd.Connection=_connection;
			cmd.CommandText=sql;

			var p = cmd.CreateParameter();
			p.ParameterName = "@tableName";
			p.Value=table;
			cmd.Parameters.Add(p);

			using(var result=cmd.ExecuteReader())
			{
				if(result!=null && result.HasRows)
				{
					while (result.Read())
					{
						primaryKeys.Add(result.GetString(0));
					}   
				}
			}
		}	         
		
		return primaryKeys.ToArray();
	}
	
	string GetPropertyType(string sqlType)
	{
		string sysType="string";
		switch (sqlType) 
		{
			case "bigint":
				sysType = "long";
				break;
			case "smallint":
				sysType= "short";
				break;
			case "int":
				sysType= "int";
				break;
			case "uniqueidentifier":
				sysType=  "Guid";
				 break;
			case "smalldatetime":
			case "datetime":
			case "datetime2":
			case "date":
			case "time":
				sysType=  "DateTime";
				  break;
			case "float":
				sysType="double";
				break;
			case "real":
				sysType="float";
				break;
			case "numeric":
			case "smallmoney":
			case "decimal":
			case "money":
				sysType=  "decimal";
				 break;
			case "tinyint":
				sysType = "byte";
				break;
			case "bit":
				sysType=  "bool";
				   break;
			case "image":
			case "binary":
			case "varbinary":
			case "timestamp":
				sysType=  "byte[]";
				 break;
			case "geography":
				sysType = "Microsoft.SqlServer.Types.SqlGeography";
				break;
			case "geometry":
				sysType = "Microsoft.SqlServer.Types.SqlGeometry";
				break;
		}
		return sysType;
	}



	const string TABLE_SQL=@"SELECT *
		FROM  INFORMATION_SCHEMA.TABLES
		WHERE TABLE_TYPE='BASE TABLE' OR TABLE_TYPE='VIEW'";

	const string COLUMN_SQL=@"SELECT 
			TABLE_CATALOG AS [Database],
			TABLE_SCHEMA AS Owner, 
			TABLE_NAME AS TableName, 
			COLUMN_NAME AS ColumnName, 
			ORDINAL_POSITION AS OrdinalPosition, 
			COLUMN_DEFAULT AS DefaultSetting, 
			IS_NULLABLE AS IsNullable, DATA_TYPE AS DataType, 
			CHARACTER_MAXIMUM_LENGTH AS MaxLength, 
			DATETIME_PRECISION AS DatePrecision,
			COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsIdentity') AS IsIdentity,
			COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsComputed') as IsComputed
		FROM  INFORMATION_SCHEMA.COLUMNS
		WHERE TABLE_NAME=@tableName AND TABLE_SCHEMA=@schemaName
		ORDER BY OrdinalPosition ASC";

	const string OUTER_KEYS_SQL = @"SELECT 
			FK = OBJECT_NAME(pt.constraint_object_id),
			Referenced_tbl = OBJECT_NAME(pt.referenced_object_id),
			Referencing_col = pc.name, 
			Referenced_col = rc.name
		FROM sys.foreign_key_columns AS pt
		INNER JOIN sys.columns AS pc
		ON pt.parent_object_id = pc.[object_id]
		AND pt.parent_column_id = pc.column_id
		INNER JOIN sys.columns AS rc
		ON pt.referenced_column_id = rc.column_id
		AND pt.referenced_object_id = rc.[object_id]
		WHERE pt.parent_object_id = OBJECT_ID(@tableName);";

    const string INNER_KEYS_SQL = @"SELECT 
			[Schema] = OBJECT_SCHEMA_NAME(pt.parent_object_id),
			Referencing_tbl = OBJECT_NAME(pt.parent_object_id),
			FK = OBJECT_NAME(pt.constraint_object_id),
			Referencing_col = pc.name, 
			Referenced_col = rc.name
		FROM sys.foreign_key_columns AS pt
		INNER JOIN sys.columns AS pc
		ON pt.parent_object_id = pc.[object_id]
		AND pt.parent_column_id = pc.column_id
		INNER JOIN sys.columns AS rc
		ON pt.referenced_column_id = rc.column_id
		AND pt.referenced_object_id = rc.[object_id]
		WHERE pt.referenced_object_id = OBJECT_ID(@tableName);";
	  
}

#>
