using GenerateJsonTableSchema;
using System.Configuration;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static GenerateJsonTableSchema.SqlInfo;

string databaseName = "Sugarfoot";

string connectionString = ConfigurationManager.ConnectionStrings["Database"].ConnectionString.Replace("{Database}", databaseName);

SqlInfo si = new SqlInfo(connectionString);

IEnumerable<TableAndViewNames> tableNames = si.GetTableAndViewNames(databaseName);

foreach (TableAndViewNames t in tableNames)
{
    Console.WriteLine(t.DatabaseName + ":" + t.TableName);

    IEnumerable<TableColumns> tableColumns = si.GetTableColumns(t.DatabaseName, t.TableName);
    CheckForMoreThanOnePrimaryKey(t.TableName, tableColumns);

    IEnumerable<TableColumns> queryColumns = tableColumns.Where(tc => tc.PrimaryKey == "True");
    string primaryKeyCSDeclaration = createColumnList(t.TableName, queryColumns, Helper.ColumnList.CsKeyDeclaration);
    string primaryKeyCSAssignment = createColumnList(t.TableName, queryColumns, Helper.ColumnList.CsKeyAssignment);
    string primaryKeyCSDeclaration2 = createColumnList(t.TableName, queryColumns, Helper.ColumnList.CsDeclaration);
    string primaryKeyCSAssignment2 = createColumnList(t.TableName, queryColumns, Helper.ColumnList.CsAssignment);
    string primaryKeySqlDeclaration = createColumnList(t.TableName, queryColumns, Helper.ColumnList.SqlDeclaration);
    string primaryKeySqlAssignment = createColumnList(t.TableName, queryColumns, Helper.ColumnList.SqlAssignment);
    string csKeyName = createColumnList(t.TableName, queryColumns, Helper.ColumnList.CsKeyName);
    string csKeyType = createColumnList(t.TableName, queryColumns, Helper.ColumnList.CsKeyType);
    string modelKeyName = createColumnList(t.TableName, queryColumns, Helper.ColumnList.modelKeyName);

    queryColumns = tableColumns.Where(tc => tc.Identity != "True");
    string columnNamesSqlList = createColumnList(t.TableName, queryColumns, Helper.ColumnList.columnNamesSqlList);
    string columnValuesSqlList = createColumnList(t.TableName, queryColumns, Helper.ColumnList.columnValuesSqlList);
    string columnValuesAssignmentSqlList = createColumnList(t.TableName, queryColumns, Helper.ColumnList.SqlAssignment);
    string modelColumnNames = createColumnList(t.TableName, queryColumns, Helper.ColumnList.modelColumnNames);
    string columnSqlDeclarationsNoIdentity = createColumnList(t.TableName, queryColumns, Helper.ColumnList.SqlDeclaration);

    // All columns.
    string columnSqlDeclarations = createColumnList(t.TableName, tableColumns, Helper.ColumnList.SqlDeclaration);

    TableSchema ts = new TableSchema(
        t.DatabaseName,
        t.TableName,
        (List<TableColumns>)tableColumns,
        primaryKeyCSDeclaration,
        primaryKeyCSAssignment,
        primaryKeySqlDeclaration,
        primaryKeySqlAssignment,
        columnSqlDeclarations,
        columnSqlDeclarationsNoIdentity,
        columnNamesSqlList,
        columnValuesSqlList,
        columnValuesAssignmentSqlList,
        modelColumnNames,
        modelKeyName,
        csKeyName,
        csKeyType
    );

    writeJsonFile(ts, t);
}

void writeJsonFile(TableSchema ts, TableAndViewNames t)
{
    var options = new JsonSerializerOptions { WriteIndented = true };
    string jsonString = JsonSerializer.Serialize(ts, options);
    string outputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    string outputFilename = Path.Combine(outputPath, $"{t.DatabaseName}-{t.TableName}.json");

    File.WriteAllText(outputFilename, jsonString);
    Console.WriteLine($"Output file written to {outputFilename}");
}

string createColumnList(string TableName, IEnumerable<TableColumns> queryColumns, Helper.ColumnList listType)
{
    int columnCounter = 0;
    StringBuilder buffer = new();

    foreach (var queryColumn in queryColumns)
    {
        injectCarriageReturnIfNeeded(buffer, columnCounter);

        switch (listType)
        {
            case Helper.ColumnList.CsKeyDeclaration:
                buffer.Append($"{queryColumn.CSType} {queryColumn.ColumnName}");
                break;

            case Helper.ColumnList.CsKeyAssignment:
                buffer.Append($"{queryColumn.ColumnName} = {queryColumn.ColumnName}");
                break;

            case Helper.ColumnList.CsDeclaration:
                buffer.Append($"{queryColumn.CSType} {queryColumn.ColumnName},");
                break;

            case Helper.ColumnList.CsAssignment:
                buffer.Append($"{queryColumn.ColumnName} = {queryColumn.ColumnName}");
                break;

            case Helper.ColumnList.SqlDeclaration:
                buffer.Append($"@{queryColumn.ColumnName} {queryColumn.DDLType},");
                break;

            case Helper.ColumnList.SqlAssignment:
                buffer.Append($"[{queryColumn.ColumnName}] = @{queryColumn.ColumnName},");
                break;

            case Helper.ColumnList.columnNamesSqlList:
                buffer.Append($"[{queryColumn.ColumnName}],");
                break;

            case Helper.ColumnList.columnValuesSqlList:
                 buffer.Append($"@{queryColumn.ColumnName},");
                 break;

            case Helper.ColumnList.modelColumnNames:
                    buffer.Append($"model.{queryColumn.ColumnName}, ");
                break;

            case Helper.ColumnList.modelKeyName:
                buffer.Append($"{TableName.ToLower()}.{queryColumn.ColumnName}, ");
                break;

            case Helper.ColumnList.CsKeyName:
                buffer.Append($"{queryColumn.ColumnName}");
                break;

            case Helper.ColumnList.CsKeyType:
                buffer.Append($"{queryColumn.CSType}");
                break;

        }

        columnCounter++;
    }

    if (TableName == "States") { 
       string b = buffer.ToString();
    }
    return Regex.Replace(buffer.ToString(), "(,.*$)?", "");
}

void injectCarriageReturnIfNeeded(StringBuilder buffer, int columnCounter)
{
    if (columnCounter > 0)
    {
        buffer.Append("\n");
    }
}

void CheckForMoreThanOnePrimaryKey(string TableName, IEnumerable<TableColumns> tableColumns)
{
    if (tableColumns.Count(tc => tc.PrimaryKey == "True") > 1)  
    {
        throw new ArgumentException($"{TableName} has more than one primary key.");
    }
}

record TableSchema (
    string DatabaseName,
    string TableName,
    List<TableColumns> columns,
    string primaryKeyCSDeclaration,
    string primaryKeyCSAssignment,
    string primaryKeySqlDeclaration,
    string primaryKeySqlAssignment,
    string columnSqlDeclarations,
    string columnSqlDeclarationsNoIdentity,
    string columnNamesSqlList,
    string columnValuesSqlList,
    string columnValuesAssignmentSqlList,
    string modelColumnNames,
    string modelKeyName,
    string csKeyName,
    string csKeyType
);


public class Helper {
    public enum ColumnList
    {
        CsDeclaration,
        CsKeyDeclaration,
        CsAssignment,
        CsKeyAssignment,
        SqlDeclaration,
        SqlAssignment,
        columnsSqlDeclarations,
        columnNamesSqlList,
        columnValuesSqlList,
        modelColumnNames,
        modelKeyName,
        CsKeyName,
        CsKeyType
    }
}
