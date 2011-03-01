﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Xtensive.Sql.Drivers.MySql.Resources;
using Xtensive.Sql.Model;
using Constraint = Xtensive.Sql.Model.Constraint;


namespace Xtensive.Sql.Drivers.MySql.v5
{
    internal partial class Extractor : Model.Extractor
    {
        private const int DefaultPrecision = 38;
        private const int DefaultScale = 0;

        private Catalog theCatalog;
        private string targetSchema;

        private readonly Dictionary<string, string> replacementsRegistry = new Dictionary<string, string>();

        protected override void Initialize()
        {
            theCatalog = new Catalog(Driver.CoreServerInfo.DatabaseName);
        }

        public override Catalog ExtractCatalog()
        {
            targetSchema = null;

            RegisterReplacements(replacementsRegistry);
            ExtractSchemas();
            ExtractCatalogContents();
            return theCatalog;
        }

        public override Schema ExtractSchema(string schemaName)
        {
            targetSchema = schemaName.ToUpperInvariant();
            theCatalog.CreateSchema(targetSchema);

            RegisterReplacements(replacementsRegistry);
            ExtractCatalogContents();
            return theCatalog.Schemas[targetSchema];
        }

        private void ExtractCatalogContents()
        {
            ExtractTables();
            ExtractTableColumns();
            ExtractViews();
            ExtractViewColumns();
            ExtractIndexes();
            ExtractForeignKeys();
            ExtractCheckConstaints();
            ExtractUniqueAndPrimaryKeyConstraints();
            ExtractSequences();
        }

        private void ExtractSchemas()
        {
            // oracle does not clearly distinct users and schemas.
            // so we extract just users.
            using (var reader = ExecuteReader(GetExtractSchemasQuery()))
                while (reader.Read())
                    theCatalog.CreateSchema(reader.GetString(0));
            // choosing the default schema
            var defaultSchemaName = Driver.CoreServerInfo.DefaultSchemaName.ToUpperInvariant();
            var defaultSchema = theCatalog.Schemas[defaultSchemaName];
            theCatalog.DefaultSchema = defaultSchema;
        }

        private void ExtractTables()
        {
            using (var reader = ExecuteReader(GetExtractTablesQuery()))
            {
                while (reader.Read())
                {
                    //TODO : Think about temporary tables (Malisa)
                    var schema = theCatalog.Schemas[reader.GetString(0)];
                    string tableName = reader.GetString(1);
                    schema.CreateTable(tableName);
                }
            }
        }

        private void ExtractTableColumns()
        {
            using (var reader = ExecuteReader(GetExtractTableColumnsQuery()))
            {
                Table table = null;
                int lastColumnId = int.MaxValue;
                while (reader.Read())
                {
                    int columnId = ReadInt(reader, 9);
                    if (columnId <= lastColumnId)
                    {
                        var schema = theCatalog.Schemas[reader.GetString(0)];
                        table = schema.Tables[reader.GetString(1)];
                    }
                    var column = table.CreateColumn(reader.GetString(2));
                    column.DataType = CreateValueType(reader, 3, 4, 5, 6);
                    column.IsNullable = ReadBool(reader, 7);
                    var defaultValue = ReadStringOrNull(reader, 8);
                    if (!string.IsNullOrEmpty(defaultValue))
                        column.DefaultValue = SqlDml.Native(defaultValue);
                    lastColumnId = columnId;
                }
            }
        }

        private void ExtractViews()
        {
            using (var reader = ExecuteReader(GetExtractViewsQuery()))
            {
                while (reader.Read())
                {
                    var schema = theCatalog.Schemas[reader.GetString(0)];
                    var view = reader.GetString(1);
                    var definition = ReadStringOrNull(reader, 2);
                    if (string.IsNullOrEmpty(definition))
                        schema.CreateView(view);
                    else
                        schema.CreateView(view, SqlDml.Native(definition));
                }
            }
        }

        private void ExtractViewColumns()
        {
            using (var reader = ExecuteReader(GetExtractViewColumnsQuery()))
            {
                int lastColumnId = int.MaxValue;
                View view = null;
                while (reader.Read())
                {
                    int columnId = ReadInt(reader, 3);
                    if (columnId <= lastColumnId)
                    {
                        var schema = theCatalog.Schemas[reader.GetString(0)];
                        view = schema.Views[reader.GetString(1)];
                    }
                    view.CreateColumn(reader.GetString(2));
                    lastColumnId = columnId;
                }
            }
        }

        private void ExtractIndexes()
        {
            // it's possible to have table and index in different schemas in oracle.
            // we silently ignore this, indexes are always belong to the same schema as its table.
            using (var reader = ExecuteReader(GetExtractIndexesQuery()))
            {
                int lastColumnPosition = int.MaxValue;
                Table table = null;
                Index index = null;
                while (reader.Read())
                {
                    int columnPosition = ReadInt(reader, 6);
                    if (columnPosition <= lastColumnPosition)
                    {
                        var schema = theCatalog.Schemas[reader.GetString(0)];
                        table = schema.Tables[reader.GetString(1)];
                        index = table.CreateIndex(reader.GetString(2));
                        index.IsUnique = ReadBool(reader, 3);
                        index.IsBitmap = reader.GetString(4) == "BITMAP";
                        if (!reader.IsDBNull(5))
                        {
                            int pctFree = ReadInt(reader, 5);
                            index.FillFactor = (byte)(100 - pctFree);
                        }
                    }
                    var column = table.TableColumns[reader.GetString(7)];
                    bool isAscending = reader.GetString(8) == "ASC";
                    index.CreateIndexColumn(column, isAscending);
                    lastColumnPosition = columnPosition;
                }
            }
        }

        private void ExtractForeignKeys()
        {
            using (var reader = ExecuteReader(GetExtractForeignKeysQuery()))
            {
                int lastColumnPosition = int.MaxValue;
                ForeignKey constraint = null;
                Table referencingTable = null;
                Table referencedTable = null;
                while (reader.Read())
                {
                    int columnPosition = ReadInt(reader, 7);
                    if (columnPosition <= lastColumnPosition)
                    {
                        var referencingSchema = theCatalog.Schemas[reader.GetString(0)];
                        referencingTable = referencingSchema.Tables[reader.GetString(1)];
                        constraint = referencingTable.CreateForeignKey(reader.GetString(2));
                        ReadConstraintProperties(constraint, reader, 3, 4);
                        ReadCascadeAction(constraint, reader, 5);
                        var referencedSchema = theCatalog.Schemas[reader.GetString(8)];
                        referencedTable = referencedSchema.Tables[reader.GetString(9)];
                        constraint.ReferencedTable = referencedTable;
                    }
                    var referencingColumn = referencingTable.TableColumns[reader.GetString(6)];
                    var referencedColumn = referencedTable.TableColumns[reader.GetString(10)];
                    constraint.Columns.Add(referencingColumn);
                    constraint.ReferencedColumns.Add(referencedColumn);
                    lastColumnPosition = columnPosition;
                }
            }
        }

        private void ExtractUniqueAndPrimaryKeyConstraints()
        {
            using (var reader = ExecuteReader(GetExtractUniqueAndPrimaryKeyConstraintsQuery()))
            {
                Table table = null;
                string constraintName = null;
                string constraintType = null;
                var columns = new List<TableColumn>();
                int lastColumnPosition = -1;
                while (reader.Read())
                {
                    int columnPosition = ReadInt(reader, 5);
                    if (columnPosition <= lastColumnPosition)
                    {
                        CreateIndexBasedConstraint(table, constraintName, constraintType, columns);
                        columns.Clear();
                    }
                    if (columns.Count == 0)
                    {
                        var schema = theCatalog.Schemas[reader.GetString(0)];
                        table = schema.Tables[reader.GetString(1)];
                        constraintName = reader.GetString(2);
                        constraintType = reader.GetString(3);
                    }
                    columns.Add(table.TableColumns[reader.GetString(4)]);
                    lastColumnPosition = columnPosition;
                }
                if (columns.Count > 0)
                    CreateIndexBasedConstraint(table, constraintName, constraintType, columns);
            }
        }

        private void ExtractCheckConstaints()
        {
            using (var reader = ExecuteReader(GetExtractCheckConstraintsQuery()))
            {
                while (reader.Read())
                {
                    var schema = theCatalog.Schemas[reader.GetString(0)];
                    var table = schema.Tables[reader.GetString(1)];
                    var name = reader.GetString(2);
                    // It looks like ODP.NET sometimes fail to read a LONG column via GetString
                    // It returns empty string instead of the actual value.
                    var condition = string.IsNullOrEmpty(reader.GetString(3))
                      ? null
                      : SqlDml.Native(reader.GetString(3));
                    var constraint = table.CreateCheckConstraint(name, condition);
                    ReadConstraintProperties(constraint, reader, 4, 5);
                }
            }
        }

        private void ExtractSequences()
        {
            using (var reader = ExecuteReader(GetExtractSequencesQuery()))
            {
                while (reader.Read())
                {
                    var schema = theCatalog.Schemas[reader.GetString(0)];
                    var sequence = schema.CreateSequence(reader.GetString(1));
                    sequence.DataType = new SqlValueType(SqlType.Decimal, DefaultPrecision, DefaultScale);
                    var descriptor = sequence.SequenceDescriptor;
                    descriptor.MinValue = ReadLong(reader, 2);
                    descriptor.MaxValue = ReadLong(reader, 3);
                    descriptor.Increment = ReadLong(reader, 4);
                    descriptor.IsCyclic = ReadBool(reader, 5);
                }
            }
        }

        private SqlValueType CreateValueType(IDataRecord row,
          int typeNameIndex, int precisionIndex, int scaleIndex, int charLengthIndex)
        {
            string typeName = row.GetString(typeNameIndex);
            if (typeName == "NUMBER")
            {
                int precision = row.IsDBNull(precisionIndex) ? DefaultPrecision : ReadInt(row, precisionIndex);
                int scale = row.IsDBNull(scaleIndex) ? DefaultScale : ReadInt(row, scaleIndex);
                return new SqlValueType(SqlType.Decimal, precision, scale);
            }
            if (typeName.StartsWith("INTERVAL DAY"))
            {
                // ignoring "day precision" and "second precision"
                // although they can be read as "scale" and "precision"
                return new SqlValueType(SqlType.Interval);
            }
            if (typeName.StartsWith("TIMESTAMP"))
            {
                // "timestamp precision" is saved as "scale", ignoring too
                return new SqlValueType(SqlType.DateTime);
            }
            if (typeName == "NVARCHAR2" || typeName == "NCHAR")
            {
                int length = ReadInt(row, charLengthIndex);
                var sqlType = typeName.Length == 5 ? SqlType.Char : SqlType.VarChar;
                return new SqlValueType(sqlType, length);
            }
            var typeInfo = Driver.ServerInfo.DataTypes[typeName];
            return typeInfo != null
              ? new SqlValueType(typeInfo.Type)
              : new SqlValueType(typeName);
        }

        private static void CreateIndexBasedConstraint(
          Table table, string constraintName, string constraintType, List<TableColumn> columns)
        {
            switch (constraintType)
            {
                case "P":
                    table.CreatePrimaryKey(constraintName, columns.ToArray());
                    return;
                case "U":
                    table.CreateUniqueConstraint(constraintName, columns.ToArray());
                    return;
                default:
                    throw new ArgumentOutOfRangeException("constraintType");
            }
        }

        private static bool ReadBool(IDataRecord row, int index)
        {
            var value = row.GetString(index);
            switch (value)
            {
                case "Y":
                case "YES":
                case "ENABLED":
                case "UNIQUE":
                    return true;
                case "N":
                case "NO":
                case "DISABLED":
                case "NONUNIQUE":
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(string.Format(Strings.ExInvalidBooleanStringX, value));
            }
        }

        private static long ReadLong(IDataRecord row, int index)
        {
            decimal value = row.GetDecimal(index);
            return value > long.MaxValue ? long.MaxValue : (long)value;
        }

        private static int ReadInt(IDataRecord row, int index)
        {
            decimal value = row.GetDecimal(index);
            return value > int.MaxValue ? int.MaxValue : (int)value;
        }

        private static string ReadStringOrNull(IDataRecord row, int index)
        {
            return row.IsDBNull(index) ? null : row.GetString(index);
        }

        private static void ReadConstraintProperties(Constraint constraint,
          IDataRecord row, int isDeferrableIndex, int isInitiallyDeferredIndex)
        {
            constraint.IsDeferrable = row.GetString(isDeferrableIndex) == "DEFERRABLE";
            constraint.IsInitiallyDeferred = row.GetString(isInitiallyDeferredIndex) == "DEFERRED";
        }

        private static void ReadCascadeAction(ForeignKey foreignKey, IDataRecord row, int deleteRuleIndex)
        {
            var deleteRule = row.GetString(deleteRuleIndex);
            switch (deleteRule)
            {
                case "CASCADE":
                    foreignKey.OnDelete = ReferentialAction.Cascade;
                    return;
                case "SET NULL":
                    foreignKey.OnDelete = ReferentialAction.SetNull;
                    return;
                case "NO ACTION":
                    foreignKey.OnDelete = ReferentialAction.NoAction;
                    return;
            }
        }

        protected virtual void RegisterReplacements(Dictionary<string, string> replacements)
        {
            var schemaFilter = targetSchema != null
              ? "= " + SqlHelper.QuoteString(targetSchema)
              : "NOT IN ('SYS', 'SYSTEM')";

            replacements[SchemaFilterPlaceholder] = schemaFilter;
            replacements[IndexesFilterPlaceholder] = "1 > 0";
            replacements[TableFilterPlaceholder] = "IS NOT NULL";
        }

        private string PerformReplacements(string query)
        {
            foreach (var registry in replacementsRegistry)
                query = query.Replace(registry.Key, registry.Value);
            return query;
        }

        protected override DbDataReader ExecuteReader(string commandText)
        {
            return base.ExecuteReader(PerformReplacements(commandText));
        }

        protected override DbDataReader ExecuteReader(ISqlCompileUnit statement)
        {
            var commandText = Connection.Driver.Compile(statement).GetCommandText();
            return base.ExecuteReader(PerformReplacements(commandText));
        }


        // Constructors

        public Extractor(SqlDriver driver)
            : base(driver)
        {
        }
    }
}
