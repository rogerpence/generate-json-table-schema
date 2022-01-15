using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using Dapper;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace GenerateJsonTableSchema;

public class SqlInfo
{
    public SqlInfo(string connectionString)
    {
        PopulateDataTypes();
        this.ConnectionString = connectionString;
    }

    List<DataTypes> dataTypes = new List<DataTypes>();

    string ConnectionString; 

    public record DataTypes(string sql, string dotnet, string cs);
    
    public record TableAndViewNames(string DatabaseName, string TableName);
    public record TableColumns(string ColumnName,
                               string Type,
                               string DDLType,
                               string CSType,
                               string NETType,
                               int MaxLength,
                               int Precision,
                               int Scale, 
                               string Nullable,
                               string PrimaryKey,
                               string Identity);

    public List<TableAndViewNames> GetTableAndViewNames(string DatabaseName)
    {
        string sql =
            @"SELECT
              '{DatabaseName}' as DatabaseName, TABLE_NAME as TableName 
            FROM
              {DatabaseName}.INFORMATION_SCHEMA.TABLES
            WHERE SUBSTRING(TABLE_NAME, 1,2)  <> '__'"
            .Replace("{DatabaseName}", DatabaseName);

        List<TableAndViewNames> tables = new List<TableAndViewNames>();

        using (IDbConnection db = new SqlConnection(ConnectionString))
        {
            tables = db.Query<TableAndViewNames>(sql).ToList();
        }

        return tables;
    }

    public List<TableColumns> GetTableColumns(string databaseName, string tableName)
    {
        string sql =
            @"SELECT
               c.name 'ColumnName',
               t.name 'Type',
               t.name +

               CASE WHEN t.name IN ('char', 'varchar','nchar','nvarchar') THEN '('+
                     CASE WHEN c.max_length=-1 THEN 'MAX'
                          ELSE CONVERT(VARCHAR(4),
                                       CASE WHEN t.name IN('nchar','nvarchar')
                                       THEN c.max_length/2 ELSE c.max_length END)
                          END +')'
                  WHEN t.name IN ('decimal','numeric')
                          THEN '('+ CONVERT(VARCHAR(4),c.precision)+','
                                  + CONVERT(VARCHAR(4),c.Scale)+')'
                          ELSE '' END
             as ""DDLType"",
             '' as ""CSType"",
             '' as ""NETType"",
             cast(c.max_length as int) 'MaxLength',
             cast(c.precision as int) 'Precision',
             cast(c.scale as int) 'Scale',

             iif(c.is_nullable = 1, 'True','False') 'Nullable',
             iif(ISNULL(i.is_primary_key, 0) = 1,'True', 'False') 'PrimaryKey',
             iif(c.is_identity = 1, 'True', 'False') 'Identity'
       
             FROM
                 sys.columns c
             INNER JOIN
                sys.types t ON c.user_type_id = t.user_type_id
             LEFT OUTER JOIN
                sys.index_columns ic ON ic.object_id = c.object_id AND ic.column_id = c.column_id
             LEFT OUTER JOIN
                sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
             WHERE
                c.object_id = OBJECT_ID('{TableName}')"
            .Replace("{TableName}", tableName);

        List<TableColumns> tableColumns = new List<TableColumns>();

        using (IDbConnection db = new SqlConnection(ConnectionString))
        {
            tableColumns = db.Query<TableColumns>(sql).ToList();
        }

        return setTypes(tableColumns, tableName);
    }

    private List<TableColumns> setTypes(List<TableColumns> tableColumns, string tableName)
    {
        List<TableColumns> ammendedTableColumns = new List<TableColumns>();

        foreach (TableColumns tableColumn in tableColumns)
        {
            bool columnFound = false;

            foreach (DataTypes dataType in dataTypes)
            {
                if (tableColumn.DDLType.StartsWith(dataType.sql))
                {
                    columnFound = true;
                    ammendedTableColumns.Add(tableColumn with {NETType = dataType.dotnet, CSType = dataType.cs});
                    break;
                }
            }

            if (! columnFound)
            {
                throw new ArgumentException($"Cannot map {tableName}.{tableColumn.ColumnName}'s data type of {tableColumn.DDLType}");
            }
        }

        return ammendedTableColumns;
    }
  
    private void PopulateDataTypes()
    {
        dataTypes.Add(new DataTypes("bigint", "Int64", "long"));
        dataTypes.Add(new DataTypes("binary", "Byte[]", "byte[]"));
        dataTypes.Add(new DataTypes("bit", "Boolean", "bool"));
        dataTypes.Add(new DataTypes("char", "String", "string"));
        dataTypes.Add(new DataTypes("date", "DateTime", "System.DateTime"));
        dataTypes.Add(new DataTypes("datetime", "DateTime", "System.DateTime"));
        dataTypes.Add(new DataTypes("datetime2", "DateTime", "System.DateTime"));
        dataTypes.Add(new DataTypes("datetimeoffset", "DateTimeOffset", "System.DateTimeOffset"));
        dataTypes.Add(new DataTypes("decimal", "Decimal", "decimal "));
        dataTypes.Add(new DataTypes("float", "Double", "double"));
        dataTypes.Add(new DataTypes("image", "Byte[]", "byte[]"));
        dataTypes.Add(new DataTypes("int", "Int32", "int"));
        dataTypes.Add(new DataTypes("money", "Decimal", "decimal"));
        dataTypes.Add(new DataTypes("nchar", "String", "string"));
        dataTypes.Add(new DataTypes("ntext", "String", "string"));
        dataTypes.Add(new DataTypes("numeric", "Decimal", "decimal"));
        dataTypes.Add(new DataTypes("nvarchar", "String", "string"));
        dataTypes.Add(new DataTypes("real", "Single", "float"));
        dataTypes.Add(new DataTypes("rowversion", "Byte[]", "byte[]"));
        dataTypes.Add(new DataTypes("smalldatetime", "DateTime", "System.DateTime"));
        dataTypes.Add(new DataTypes("smallint", "Int16", "short"));
        dataTypes.Add(new DataTypes("smallmoney", "Decimal", "decimal"));
        dataTypes.Add(new DataTypes("time", "TimeSpan", "System.TimeSpan"));
        dataTypes.Add(new DataTypes("timestamp", "Byte[]", "byte[]"));
        dataTypes.Add(new DataTypes("tinyint", "Byte", "byte"));
        dataTypes.Add(new DataTypes("varbinary", "Byte[]", "byte[]"));
        dataTypes.Add(new DataTypes("varchar", "String", "string"));
    }


}

