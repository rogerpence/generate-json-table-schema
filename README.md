## GenerateJsonTableSchema

Given a database name, this program generates a Json document with the table's schema and several other schema-derived values. This Json document can then be used with a template engine to create various source files. My LibrettoX utility uses Python and Jinja2 templates to create Dapper models and other source from these schemas. 

> This version is constrainted to work with tables that have only one primary key.

For example, for this SQL table:

```
USE [Sugarfoot]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Song](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [ArtistId] [int] NULL,
    [Title] [nvarchar](200) NOT NULL,
    [Added] [datetime] NULL,
    [Updated] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
    [Id] ASC
) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Song]  WITH CHECK ADD FOREIGN KEY([ArtistId])
REFERENCES [dbo].[Artist] ([Id])
GO
```

this program creates this Json schema document: 

```
{
  "DatabaseName": "Sugarfoot",
  "TableName": "Artist",
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
    },
    {
      "ColumnName": "Name",
      "Type": "nvarchar",
      "DDLType": "nvarchar(100)",
      "CSType": "string",
      "NETType": "String",
      "MaxLength": 200,
      "Precision": 0,
      "Scale": 0,
      "Nullable": "False",
      "PrimaryKey": "False",
      "Identity": "False"
    },
    {
      "ColumnName": "PrefixWithThe",
      "Type": "bit",
      "DDLType": "bit",
      "CSType": "bool",
      "NETType": "Boolean",
      "MaxLength": 1,
      "Precision": 1,
      "Scale": 0,
      "Nullable": "False",
      "PrimaryKey": "False",
      "Identity": "False"
    },
    {
      "ColumnName": "Added",
      "Type": "datetime",
      "DDLType": "datetime",
      "CSType": "System.DateTime",
      "NETType": "DateTime",
      "MaxLength": 8,
      "Precision": 23,
      "Scale": 3,
      "Nullable": "True",
      "PrimaryKey": "False",
      "Identity": "False"
    },
    {
      "ColumnName": "Updated",
      "Type": "datetime",
      "DDLType": "datetime",
      "CSType": "System.DateTime",
      "NETType": "DateTime",
      "MaxLength": 8,
      "Precision": 23,
      "Scale": 3,
      "Nullable": "True",
      "PrimaryKey": "False",
      "Identity": "False"
    }
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

To get table and view names in a database, this query is performed:

To get a list of tables and views, 

```
SELECT
'{{DatabaseName}}' as DatabaseName, TABLE_NAME as TableName 
FROM
{{DatabaseName}}.INFORMATION_SCHEMA.TABLES
WHERE SUBSTRING(TABLE_NAME, 1,2)  <> '__'
```

### Implementation details

> Note that the SQL above omits tables that start with '__'. The '__' table prefix indicates that the table contains meta data (ie, column descriptions). These meta data tables aren't production tables and don't need schema files generated.

To get column details, this SQL is performed for a given table. It queries the sys.columns, sys.types, sys.index_columns, and sys.indexes tables: 

````
SELECT
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
   c.object_id = OBJECT_ID('{{TableOrViewName')
```   