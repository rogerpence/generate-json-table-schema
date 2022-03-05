## GenerateJsonTableSchema

Given a database name, this program generates a Json schema for tables and views. This Json document can then be used with a template engine to create various source files. The LibrettoX utility uses Python and Jinja2 templates to create Dapper models and other source from these schemas. 

> This version is constrainted to work with tables that have only one primary key.

For example, for this SQL table:

```
CREATE TABLE [dbo].[Artist]
(
    [Id] INT NOT NULL PRIMARY KEY IDENTITY,
    [Name] NVARCHAR(100) NOT NULL,
    [PrefixWithThe] bit NOT NULL DEFAULT 0,
    [Added] datetime,
    [Updated] datetime
)
ALTER TABLE [dbo].[Song]  
WITH CHECK ADD FOREIGN KEY([ArtistId]) REFERENCES [dbo].[Artist] ([Id])
GO
```

the following schema is produced (the figure below is truncated to show only one column of the table).

```
{
  "DatabaseName": "Sugarfoot",
  "TableName": "Artist",
  "Type": "view" 
  "columns": [
    {
      "ColumnName": "Id",
      "Type": "int",
      "DDLType": "int",
      "CSType": "int",
      "NETType": "Int32",
      "MaxLength": 4,
      "Precision": 10,
      "Scale": 0,
      "Nullable": "False",
      "PrimaryKey": "True",
      "Identity": "True"
    }...
  ],
  "primaryKeyCSDeclaration": "int Id",
  "primaryKeyCSAssignment": "Id = Id",
  "primaryKeySqlDeclaration": "@Id int",
  "primaryKeySqlAssignment": "[Id] = @Id",
  "columnSqlDeclarations": "@Id int,\n@Name nvarchar(100),\n@PrefixWithThe bit,\n@Added datetime,\n@Updated datetime",
  "columnSqlDeclarationsNoIdentity": "@Name nvarchar(100),\n@PrefixWithThe bit,\n@Added datetime,\n@Updated datetime",
  "columnNamesSqlList": "[Name],\n[PrefixWithThe],\n[Added],\n[Updated]",
  "columnValuesSqlList": "@Name,\n@PrefixWithThe,\n@Added,\n@Updated",
  "columnValuesAssignmentSqlList": "[Name] = @Name,\n[PrefixWithThe] = @PrefixWithThe,\n[Added] = @Added,\n[Updated] = @Updated",
  "modelColumnNames": "model.Name, \nmodel.PrefixWithThe, \nmodel.Added, \nmodel.Updated",
  "modelKeyName": "artist.Id",
  "csKeyName": "Id",
  "csKeyType": "int"
}
```

### Tokens

Tokens are case-sensitive. The SQL explained in the "Implementation details" below produces the column data. The `GenerateJsonTableSchema` produces the column meta tokens. 

The token values available are:

#### Internal tokens

`_datetime` - Time template was rendered
`_template` - Template used
`_schema` - Schema used

#### Table tokens

`DatabaseName`: Database name
`TableName`: Table name
`Type`: "table" or "view"

#### Column tokens

`ColumnName`: Name
Example: `Id`

`Type`: Type
Example: `int`

`DDLType`: SQL data type
Example: `int`

`CSType`: CX# data type
Example:`int`

`NETType`: .NET data type 
Example:`Int32`

`MaxLength`: Max length
Example:`4`

`Precision`: Precision 
Example: `10`

`Scale`: Scale
Example: `0`

`Nullable`: Nullable 
Example: `False`

`PrimaryKey`: Primary key 
Example: `True`

`Identity`: Identity column
Example:`True`

#### Column meta tokens

Most of these tokens are intended for either SQL Server or C# code generation. 
For 

`primaryKeyCSDeclaration`: C# primary key declaration
Example:`int Id`

`primaryKeyCSAssignment`: C# primary key assignment
Example:`Id = Id`

`primaryKeySqlDeclaration`: SQL primary key declaration
Example:`@Id int`

`primaryKeySqlAssignment`: SQL primary key assignment
Example: `[Id] = @Id`

`modelColumnNames`: C# column model names
Example:`model.Name, \nmodel.PrefixWithThe`

`modelKeyName`: C# model key full name
Example:`artist.Id`

`csKeyName`:  C# model key name
Example:`Id`

`csKeyType`:  C# model key data type
Example:`int`

The following tokens are intended primarily for generating SQL stored procedures. Columns named `date_added`, `date_updated`, `added`, and `updated` are assumed to be row time stamps that have hard-coded assignments in the stored procedures. 

`columnSqlDeclarations`: SQL column declarations -- includes identity column
Example:`@Id int,\n@Name nvarchar(100),\n@PrefixWithThe bit`

`columnSqlDeclarationsNoIdentity`:  SQL column declarations -- no identity column
Example:`@Name nvarchar(100),\n@PrefixWithThe bit`

`columnNamesSqlList`: SQL columns name list 
Example:`[Name],\n[PrefixWithThe]`

`columnValuesSqlList`:  SQL columns value list
Example:`@Name,\n@PrefixWithThe`

`columnValuesAssignmentSqlList`:  SQL values assignment list
Example:`[Name] = @Name,\n[PrefixWithThe] = @PrefixWithThe`

### Examples

#### C# entity model

The template below: 

```
/*
Model definition for an entity. 

Database......{{DatabaseName}}
Table.........{{TableName}}
Generated on..{{_datetime}}
Template......{{_template}}
Schema........{{_schema}}
*/

namespace DataAccess.Models
{
    public class {{TableName}}Model
    {
        {% for column in columns %}
        public {{column.CSType}} {{column.ColumnName}} {get;set;}
        {% endfor %}
    }
}
```

Produces the C# entity model below:
```
/*
Model definition for an entity. 

Database......sugarfoot
Table.........Artist
Generated on..2022-Mar-05 13:54:54
Template......dapper\dapper-database-model.tpl.cs
Schema........sql-server\sugarfoot_db\*.json
*/

namespace DataAccess.Models
{
    public class ArtistModel
    {
        public int Id {get;set;}
        public string Name {get;set;}
        public bool PrefixWithThe {get;set;}
        public System.DateTime Added {get;set;}
        public System.DateTime Updated {get;set;}
    }
}
```

#### Insert stored procedure

The following template:

```
CREATE OR ALTER PROCEDURE [dbo].[{{TableName}}_Insert]
{{columnSqlDeclarations | indent(4, True)}}
AS
BEGIN
    SET NOCOUNT ON
    INSERT INTO [dbo].[{{TableName}}] 
    (
{{columnNamesSqlList | indent(8,True)}}, 
        [Added]
    ) 
    VALUES(
{{columnValuesSqlList | indent(8,True)}}, 
        CURRENT_TIMESTAMP
    );
END
SELECT CAST(SCOPE_IDENTITY() as int) as 'id'
GO
```

The `Artist` table used in this example has `Added` and `Updated` columns to timestamp insert and update operations. The `columnNamesSqlList` and the `columnValuesSqlList` omit those columns. Note how the `Added` value is hard-coded in the template above.

The template above produces the following stored procedure:

```
CREATE OR ALTER PROCEDURE [dbo].[Artist_Insert]
    @Id int,
    @Name nvarchar(100),
    @PrefixWithThe bit
AS
BEGIN
    SET NOCOUNT ON
    INSERT INTO [dbo].[Artist] 
    (
        [Name],
        [PrefixWithThe], 
        [Added]
    ) 
    VALUES(
        @Name,
        @PrefixWithThe, 
        CURRENT_TIMESTAMP
    );
END
SELECT CAST(SCOPE_IDENTITY() as int) as 'id'
GO
```

### Template tips

Many of the templates are run against all of the schemas in a database at once. This quickly produces lots of members. However, there may be times when you want to, at template rendering time, exclude a given schema from producing output. 

Generating the CRUD stored procedures is an example of that. You want to have both table and view schemas available, but don't want to produce CRUD stored procedures for views. In that case, putting this test at the top of the template

`{% if Type == 'table' %}`

and then closing the `if` statement with this at the bottom of the template

`{% endif %}`

renders a zero byte template. When template produces zero bytes, its output file is not produced. 


### Implementation details

> Note that the SQL above omits tables that start with '__'. The '__' table prefix indicates that the table contains meta data (ie, column descriptions). These meta data tables aren't production tables and don't need schema files generated.

To get column details, this SQL is performed for a given table. It queries the sys.columns, sys.types, sys.index_columns, and sys.indexes tables: 

```
SELECT
c.name 'ColumnName',
t.name 'Type',
t.name +
CASE WHEN t.name IN ('char', 'varchar', 'nchar', 'nvarchar') THEN '('+
      CASE WHEN c.max_length=-1 THEN 'MAX'
           ELSE CONVERT(VARCHAR(4),
                        CASE WHEN t.name IN('nchar','nvarchar')
                        THEN c.max_length/2 ELSE c.max_length END)
           END +')'
      WHEN t.name IN ('decimal','numeric')
           THEN '('+ CONVERT(VARCHAR(4),c.precision)+','
                   + CONVERT(VARCHAR(4),c.Scale)+')'
           ELSE '' END
as "DDLType",
'' as "CSType",
'' as "NETType",
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
   c.object_id = OBJECT_ID('{TableOrViewName}')
```   