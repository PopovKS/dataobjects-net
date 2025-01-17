// Copyright (C) 2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xtensive.Core;
using Xtensive.Sql.Dml;
using Xtensive.Sql.Drivers.Firebird.Resources;
using Xtensive.Sql.Model;
using Constraint = Xtensive.Sql.Model.Constraint;
using Index = Xtensive.Sql.Model.Index;

namespace Xtensive.Sql.Drivers.Firebird.v4_0
{
  internal partial class Extractor : v2_5.Extractor
  {
    #region States

    private struct ColumnReaderState<TOwner>
    {
      public readonly Schema Schema;
      public TOwner Owner;
      public int LastColumnIndex;

      public ColumnReaderState(Schema schema) : this()
      {
        Schema = schema;
        LastColumnIndex = int.MaxValue;
      }
    }

    private struct IndexReaderState
    {
      public readonly Schema Schema;
      public string IndexName;
      public string LastIndexName;
      public Table Table;
      public Index Index;

      public IndexReaderState(Schema schema) : this()
      {
        Schema = schema;
        LastIndexName = IndexName = string.Empty;
      }
    }

    private struct ForeignKeyReaderState
    {
      public readonly Schema ReferencingSchema;
      public readonly Schema ReferencedSchema;
      public Table ReferencingTable;
      public Table ReferencedTable;
      public ForeignKey ForeignKey;
      public int LastColumnIndex;

      public ForeignKeyReaderState(Schema referencingSchema, Schema referencedSchema) : this()
      {
        ReferencingSchema = referencingSchema;
        ReferencedSchema = referencedSchema;
        LastColumnIndex = int.MaxValue;
      }
    }

    private struct PrimaryKeyReaderState
    {
      public readonly Schema Schema;
      public readonly List<TableColumn> Columns;
      public Table Table;
      public string ConstraintName;
      public string ConstraintType;
      public int LastColumnIndex;

      public PrimaryKeyReaderState(Schema schema) : this()
      {
        Schema = schema;
        Columns = new List<TableColumn>();
        LastColumnIndex = -1;
      }
    }

    #endregion

    public override Catalog ExtractCatalog(string catalogName) =>
      ExtractSchemes(catalogName, Array.Empty<string>());

    public override Task<Catalog> ExtractCatalogAsync(string catalogName, CancellationToken token = default) =>
      ExtractSchemesAsync(catalogName, Array.Empty<string>(), token);

    public override Catalog ExtractSchemes(string catalogName, string[] schemaNames)
    {
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(catalogName, nameof(catalogName));
      ArgumentValidator.EnsureArgumentNotNull(schemaNames, nameof(schemaNames));

      var targetSchema = schemaNames.Length > 0 ? schemaNames[0] : null;
      var catalog = new Catalog(catalogName);
      ExtractSchemas(catalog, targetSchema);
      ExtractCatalogContents(catalog);
      return catalog;
    }

    public override async Task<Catalog> ExtractSchemesAsync(
      string catalogName, string[] schemaNames, CancellationToken token = default)
    {
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(catalogName, nameof(catalogName));
      ArgumentValidator.EnsureArgumentNotNull(schemaNames, nameof(schemaNames));

      var targetSchema = schemaNames.Length > 0 ? schemaNames[0] : null;
      var catalog = new Catalog(catalogName);
      ExtractSchemas(catalog, targetSchema);
      await ExtractCatalogContentsAsync(catalog, token).ConfigureAwait(false);
      return catalog;
    }

    private void ExtractCatalogContents(Catalog catalog)
    {
      ExtractTables(catalog);
      ExtractTableColumns(catalog);
      ExtractViews(catalog);
      ExtractViewColumns(catalog);
      ExtractIndexes(catalog);
      ExtractForeignKeys(catalog);
      ExtractCheckConstraints(catalog);
      ExtractUniqueAndPrimaryKeyConstraints(catalog);
      ExtractSequences(catalog);
    }

    private async Task ExtractCatalogContentsAsync(Catalog catalog, CancellationToken token)
    {
      await ExtractTablesAsync(catalog, token).ConfigureAwait(false);
      await ExtractTableColumnsAsync(catalog, token).ConfigureAwait(false);
      await ExtractViewsAsync(catalog, token).ConfigureAwait(false);
      await ExtractViewColumnsAsync(catalog, token).ConfigureAwait(false);
      await ExtractIndexesAsync(catalog, token).ConfigureAwait(false);
      await ExtractForeignKeysAsync(catalog, token).ConfigureAwait(false);
      await ExtractCheckConstraintsAsync(catalog, token).ConfigureAwait(false);
      await ExtractUniqueAndPrimaryKeyConstraintsAsync(catalog, token).ConfigureAwait(false);
      await ExtractSequencesAsync(catalog, token).ConfigureAwait(false);
    }

    private void ExtractSchemas(Catalog catalog, string targetSchema)
    {
      if (targetSchema == null) {
        var defaultSchemaName = Driver.CoreServerInfo.DefaultSchemaName.ToUpperInvariant();
        var defaultSchema = catalog.CreateSchema(defaultSchemaName);
        catalog.DefaultSchema = defaultSchema;
      }
      else {
        // since target schema is the only schema to extract
        // it will be set as default for catalog
        _ = catalog.CreateSchema(targetSchema);
      }
    }

    private void ExtractTables(Catalog catalog)
    {
      var query = GetExtractTablesQuery();
      using var command = Connection.CreateCommand(query);
      using var reader = command.ExecuteReader(CommandBehavior.SingleResult);
      while (reader.Read()) {
        ReadTableData(reader, catalog.DefaultSchema);
      }
    }

    private async Task ExtractTablesAsync(Catalog catalog, CancellationToken token)
    {
      var query = GetExtractTablesQuery();
      var command = Connection.CreateCommand(query);
      await using (command.ConfigureAwait(false)) {
        var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, token).ConfigureAwait(false);
        await using (reader.ConfigureAwait(false)) {
          while (await reader.ReadAsync(token).ConfigureAwait(false)) {
            ReadTableData(reader, catalog.DefaultSchema);
          }
        }
      }
    }

    private void ExtractTableColumns(Catalog catalog)
    {
      using var command = Connection.CreateCommand(GetExtractTableColumnsQuery());
      using var reader = command.ExecuteReader(CommandBehavior.SingleResult);
      var readerState = new ColumnReaderState<Table>(catalog.DefaultSchema);
      while (reader.Read()) {
        ReadTableColumnData(reader, ref readerState);
      }
    }

    private async Task ExtractTableColumnsAsync(Catalog catalog, CancellationToken token)
    {
      var command = Connection.CreateCommand(GetExtractTableColumnsQuery());
      await using (command.ConfigureAwait(false)) {
        var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, token).ConfigureAwait(false);
        await using (reader.ConfigureAwait(false)) {
          var readerState = new ColumnReaderState<Table>(catalog.DefaultSchema);
          while (await reader.ReadAsync(token).ConfigureAwait(false)) {
            ReadTableColumnData(reader, ref readerState);
          }
        }
      }
    }

    private void ExtractViews(Catalog catalog)
    {
      using var command = Connection.CreateCommand(GetExtractViewsQuery());
      using var reader = command.ExecuteReader(CommandBehavior.SingleResult);
      while (reader.Read()) {
        ReadViewData(reader, catalog.DefaultSchema);
      }
    }

    private async Task ExtractViewsAsync(Catalog catalog, CancellationToken token)
    {
      var command = Connection.CreateCommand(GetExtractViewsQuery());
      await using (command.ConfigureAwait(false)) {
        var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, token).ConfigureAwait(false);
        await using (reader.ConfigureAwait(false)) {
          while (await reader.ReadAsync(token).ConfigureAwait(false)) {
            ReadViewData(reader, catalog.DefaultSchema);
          }
        }
      }
    }

    private void ExtractViewColumns(Catalog catalog)
    {
      using var command = Connection.CreateCommand(GetExtractViewColumnsQuery());
      using var reader = command.ExecuteReader(CommandBehavior.SingleResult);
      var readerState = new ColumnReaderState<View>(catalog.DefaultSchema);
      while (reader.Read()) {
        ReadViewColumnData(reader, ref readerState);
      }
    }

    private async Task ExtractViewColumnsAsync(Catalog catalog, CancellationToken token)
    {
      var command = Connection.CreateCommand(GetExtractViewColumnsQuery());
      await using (command.ConfigureAwait(false)) {
        var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, token).ConfigureAwait(false);
        await using (reader.ConfigureAwait(false)) {
          var readerState = new ColumnReaderState<View>(catalog.DefaultSchema);
          while (await reader.ReadAsync(token).ConfigureAwait(false)) {
            ReadViewColumnData(reader, ref readerState);
          }
        }
      }
    }

    private void ExtractIndexes(Catalog catalog)
    {
      using var command = Connection.CreateCommand(GetExtractIndexesQuery());
      using var reader = command.ExecuteReader(CommandBehavior.SingleResult);
      var readerState = new IndexReaderState(catalog.DefaultSchema);
      while (reader.Read()) {
        ReadIndexColumnData(reader, ref readerState);
      }
    }

    private async Task ExtractIndexesAsync(Catalog catalog, CancellationToken token)
    {
      var command = Connection.CreateCommand(GetExtractIndexesQuery());
      await using (command.ConfigureAwait(false)) {
        var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, token).ConfigureAwait(false);
        await using (reader.ConfigureAwait(false)) {
          var readerState = new IndexReaderState(catalog.DefaultSchema);
          while (await reader.ReadAsync(token).ConfigureAwait(false)) {
            ReadIndexColumnData(reader, ref readerState);
          }
        }
      }
    }

    private void ExtractForeignKeys(Catalog catalog)
    {
      using var command = Connection.CreateCommand(GetExtractForeignKeysQuery());
      using var reader = command.ExecuteReader(CommandBehavior.SingleResult);
      var readerState = new ForeignKeyReaderState(catalog.DefaultSchema, catalog.DefaultSchema);
      while (reader.Read()) {
        ReadForeignKeyColumnData(reader, ref readerState);
      }
    }

    private async Task ExtractForeignKeysAsync(Catalog catalog, CancellationToken token)
    {
      var command = Connection.CreateCommand(GetExtractForeignKeysQuery());
      await using (command.ConfigureAwait(false)) {
        var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, token).ConfigureAwait(false);
        await using (reader.ConfigureAwait(false)) {
          var readerState = new ForeignKeyReaderState(catalog.DefaultSchema, catalog.DefaultSchema);
          while (await reader.ReadAsync(token).ConfigureAwait(false)) {
            ReadForeignKeyColumnData(reader, ref readerState);
          }
        }
      }
    }

    private void ExtractUniqueAndPrimaryKeyConstraints(Catalog catalog)
    {
      using var command = Connection.CreateCommand(GetExtractUniqueAndPrimaryKeyConstraintsQuery());
      using var reader = command.ExecuteReader(CommandBehavior.SingleResult);
      var readerState = new PrimaryKeyReaderState(catalog.DefaultSchema);
      bool readingCompleted;
      do {
        readingCompleted = !reader.Read();
        ReadPrimaryKeyColumn(reader, readingCompleted, ref readerState);
      } while (!readingCompleted);
    }

    private async Task ExtractUniqueAndPrimaryKeyConstraintsAsync(Catalog catalog, CancellationToken token)
    {
      var command = Connection.CreateCommand(GetExtractUniqueAndPrimaryKeyConstraintsQuery());
      await using (command.ConfigureAwait(false)) {
        var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, token).ConfigureAwait(false);
        await using (reader.ConfigureAwait(false)) {
          var readerState = new PrimaryKeyReaderState(catalog.DefaultSchema);
          bool readingCompleted;
          do {
            readingCompleted = !await reader.ReadAsync(token).ConfigureAwait(false);
            ReadPrimaryKeyColumn(reader, readingCompleted, ref readerState);
          } while (!readingCompleted);
        }
      }
    }

    private void ExtractCheckConstraints(Catalog catalog)
    {
      using var command = Connection.CreateCommand(GetExtractCheckConstraintsQuery());
      using var reader = command.ExecuteReader(CommandBehavior.SingleResult);
      while (reader.Read()) {
        ReadCheckConstraintData(reader, catalog.DefaultSchema);
      }
    }

    private async Task ExtractCheckConstraintsAsync(Catalog catalog, CancellationToken token)
    {
      var command = Connection.CreateCommand(GetExtractCheckConstraintsQuery());
      await using (command.ConfigureAwait(false)) {
        var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, token).ConfigureAwait(false);
        await using (reader.ConfigureAwait(false)) {
          while (await reader.ReadAsync(token).ConfigureAwait(false)) {
            ReadCheckConstraintData(reader, catalog.DefaultSchema);
          }
        }
      }
    }

    private void ExtractSequences(Catalog catalog)
    {
      using (var command = Connection.CreateCommand(GetExtractSequencesQuery()))
      using (var reader = command.ExecuteReader(CommandBehavior.SingleResult)) {
        while (reader.Read()) {
          ReadSequenceData(reader, catalog.DefaultSchema);
        }
      }

      foreach (var sequence in catalog.DefaultSchema.Sequences) {
        var query = string.Format(GetExtractSequenceValueQuery(), Driver.Translator.QuoteIdentifier(sequence.Name));
        using var command = Connection.CreateCommand(query);
        using var reader = command.ExecuteReader(CommandBehavior.SingleResult);
        while (reader.Read()) {
          sequence.SequenceDescriptor.MinValue = reader.GetInt64(0);
        }
      }
    }

    private async Task ExtractSequencesAsync(Catalog catalog, CancellationToken token)
    {
      var command = Connection.CreateCommand(GetExtractSequencesQuery());
      await using (command.ConfigureAwait(false)) {
        var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, token).ConfigureAwait(false);
        await using (reader.ConfigureAwait(false)) {
          while (await reader.ReadAsync(token).ConfigureAwait(false)) {
            ReadSequenceData(reader, catalog.DefaultSchema);
          }
        }
      }

      foreach (var sequence in catalog.DefaultSchema.Sequences) {
        var query = string.Format(GetExtractSequenceValueQuery(), Driver.Translator.QuoteIdentifier(sequence.Name));
        command = Connection.CreateCommand(query);
        await using (command.ConfigureAwait(false)) {
          var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, token).ConfigureAwait(false);
          await using (reader.ConfigureAwait(false)) {
            while (await reader.ReadAsync(token).ConfigureAwait(false)) {
              sequence.SequenceDescriptor.MinValue = reader.GetInt64(0);
            }
          }
        }
      }
    }

    private void ReadTableData(DbDataReader reader, Schema schema)
    {
      var tableName = reader.GetString(1).Trim();
      int tableType = reader.GetInt16(2);
      var isTemporary = tableType == 4 || tableType == 5;
      if (isTemporary) {
        var table = schema.CreateTemporaryTable(tableName);
        table.PreserveRows = tableType == 4;
        table.IsGlobal = true;
      }
      else {
        _ = schema.CreateTable(tableName);
      }
    }

    private void ReadTableColumnData(DbDataReader reader, ref ColumnReaderState<Table> state)
    {
      var columnIndex = reader.GetInt16(2);
      if (columnIndex <= state.LastColumnIndex) {
        state.Owner = state.Schema.Tables[reader.GetString(1).Trim()];
      }
      state.LastColumnIndex = columnIndex;
      var column = state.Owner.CreateColumn(reader.GetString(3));
      column.DataType = CreateValueType(reader, 4, 5, 7, 8, 9);
      column.IsNullable = ReadBool(reader, 10);
      var defaultValue = ReadStringOrNull(reader, 11);
      if (!string.IsNullOrEmpty(defaultValue)) {
        defaultValue = defaultValue.TrimStart(' ');
        if (defaultValue.StartsWith("DEFAULT", StringComparison.OrdinalIgnoreCase)) {
          defaultValue = defaultValue.Substring(7).TrimStart(' ');
        }
        if (!string.IsNullOrEmpty(defaultValue)) {
          column.DefaultValue = SqlDml.Native(defaultValue);
        }
      }
    }

    private void ReadViewData(DbDataReader reader, Schema schema)
    {
      var view = reader.GetString(1).Trim();
      var definition = ReadStringOrNull(reader, 2);
      if (string.IsNullOrEmpty(definition)) {
        _ = schema.CreateView(view);
      }
      else {
        _ = schema.CreateView(view, SqlDml.Native(definition));
      }
    }

    private static void ReadViewColumnData(DbDataReader reader, ref ColumnReaderState<View> state)
    {
      var columnIndex = reader.GetInt16(3);
      if (columnIndex <= state.LastColumnIndex) {
        state.Owner = state.Schema.Views[reader.GetString(1).Trim()];
      }
      state.LastColumnIndex = columnIndex;
      _ = state.Owner.CreateColumn(reader.GetString(2).Trim());
    }

    private static void ReadIndexColumnData(DbDataReader reader, ref IndexReaderState state)
    {
      SqlExpression expression = null;
      state.IndexName = reader.GetString(2).Trim();
      if (state.IndexName != state.LastIndexName) {
        state.Table = state.Schema.Tables[reader.GetString(1).Trim()];
        state.Index = state.Table.CreateIndex(state.IndexName);
        state.Index.IsUnique = ReadBool(reader, 5);
        state.Index.IsBitmap = false;
        state.Index.IsClustered = false;
        if (!reader.IsDBNull(8)) {
          // expression index
          expression = SqlDml.Native(reader.GetString(8).Trim());
        }
      }

      if (expression == null) {
        var column = state.Table.TableColumns[reader.GetString(6).Trim()];
        var isDescending = ReadBool(reader, 4);
        _ = state.Index.CreateIndexColumn(column, !isDescending);
      }
      else {
        var isDescending = ReadBool(reader, 4);
        _ = state.Index.CreateIndexColumn(expression, !isDescending);
      }

      state.LastIndexName = state.IndexName;
    }

    private static void ReadForeignKeyColumnData(DbDataReader reader, ref ForeignKeyReaderState state)
    {
      int columnPosition = reader.GetInt16(7);
      if (columnPosition <= state.LastColumnIndex) {
        state.ReferencingTable = state.ReferencingSchema.Tables[reader.GetString(1).Trim()];
        state.ForeignKey = state.ReferencingTable.CreateForeignKey(reader.GetString(2).Trim());
        ReadConstraintProperties(state.ForeignKey, reader, 3, 4);
        ReadCascadeAction(state.ForeignKey, reader, 5);
        state.ReferencedTable = state.ReferencedSchema.Tables[reader.GetString(9).Trim()];
        state.ForeignKey.ReferencedTable = state.ReferencedTable;
      }

      var referencingColumn = state.ReferencingTable.TableColumns[reader.GetString(6).Trim()];
      var referencedColumn = state.ReferencedTable.TableColumns[reader.GetString(10).Trim()];
      state.ForeignKey.Columns.Add(referencingColumn);
      state.ForeignKey.ReferencedColumns.Add(referencedColumn);
      state.LastColumnIndex = columnPosition;
    }

    private static void ReadPrimaryKeyColumn(DbDataReader reader, bool readingCompleted, ref PrimaryKeyReaderState state)
    {
      if (readingCompleted) {
        if (state.Columns.Count > 0) {
          CreateIndexBasedConstraint(state.Table, state.ConstraintName, state.ConstraintType, state.Columns);
        }
        return;
      }

      int columnPosition = reader.GetInt16(5);
      if (columnPosition <= state.LastColumnIndex) {
        CreateIndexBasedConstraint(state.Table, state.ConstraintName, state.ConstraintType, state.Columns);
        state.Columns.Clear();
      }

      if (state.Columns.Count == 0) {
        state.Table = state.Schema.Tables[reader.GetString(1).Trim()];
        state.ConstraintName = reader.GetString(2).Trim();
        state.ConstraintType = reader.GetString(3).Trim();
      }

      state.Columns.Add(state.Table.TableColumns[reader.GetString(4).Trim()]);
      state.LastColumnIndex = columnPosition;
    }

    private void ReadCheckConstraintData(DbDataReader reader, Schema schema)
    {
      var table = schema.Tables[reader.GetString(1).Trim()];
      var name = reader.GetString(2).Trim();
      var condition = reader.GetString(3).Trim();
      _ = table.CreateCheckConstraint(name, condition);
    }

    private void ReadSequenceData(DbDataReader reader, Schema schema)
    {
      var sequence = schema.CreateSequence(reader.GetString(1).Trim());
      var descriptor = sequence.SequenceDescriptor;
      descriptor.MinValue = 0;
      // TODO: Firebird quickfix, we must implement support for arbitrary incr. in comparer
      descriptor.Increment = 128;
    }

    private SqlValueType CreateValueType(IDataRecord row,
      int majorTypeIndex, int minorTypeIndex, int precisionIndex, int scaleIndex, int charLengthIndex)
    {
      var majorType = row.GetInt16(majorTypeIndex);
      var minorType = row.GetValue(minorTypeIndex) == DBNull.Value ? (short?) null : row.GetInt16(minorTypeIndex);
      var typeName = GetTypeName(majorType, minorType).Trim();

      if (typeName == "NUMERIC" || typeName == "DECIMAL") {
        var precision = Convert.ToInt32(row[precisionIndex]);
        var scale = Convert.ToInt32(row[scaleIndex]);
        return new SqlValueType(SqlType.Decimal, precision, scale);
      }
      if (typeName.StartsWith("TIMESTAMP")) {
        return new SqlValueType(SqlType.DateTime);
      }

      if (typeName == "VARCHAR" || typeName == "CHAR") {
        var length = Convert.ToInt32(row[charLengthIndex]);
        var sqlType = typeName.Length == 4 ? SqlType.Char : SqlType.VarChar;
        return new SqlValueType(sqlType, length);
      }

      if (typeName == "BLOB SUB TYPE 0") {
        return new SqlValueType(SqlType.VarCharMax);
      }

      if (typeName == "BLOB SUB TYPE 1") {
        return new SqlValueType(SqlType.VarBinaryMax);
      }

      var typeInfo = Driver.ServerInfo.DataTypes[typeName];
      return typeInfo != null
        ? new SqlValueType(typeInfo.Type)
        : new SqlValueType(typeName);
    }

    private static void CreateIndexBasedConstraint(
      Table table, string constraintName, string constraintType, List<TableColumn> columns)
    {
      switch (constraintType.Trim()) {
        case "PRIMARY KEY":
          _ = table.CreatePrimaryKey(constraintName, columns.ToArray());
          return;
        case "UNIQUE":
          _ = table.CreateUniqueConstraint(constraintName, columns.ToArray());
          return;
        default:
          throw new ArgumentOutOfRangeException(nameof(constraintType));
      }
    }

    private static bool ReadBool(IDataRecord row, int index)
    {
      var value = row.IsDBNull(index) ? (short) 0 : Convert.ToInt16(row.GetString(index) ?? "0");
      switch (value) {
        case 1:
          return true;
        case 0:
          return false;
        default:
          throw new ArgumentOutOfRangeException(string.Format(Strings.ExInvalidBooleanSmallintValue, value));
      }
    }

    private static string ReadStringOrNull(IDataRecord row, int index) =>
      row.IsDBNull(index) ? null : row.GetString(index).Trim();

    private static void ReadConstraintProperties(Constraint constraint,
      IDataRecord row, int isDeferrableIndex, int isInitiallyDeferredIndex)
    {
      constraint.IsDeferrable = ReadStringOrNull(row, isDeferrableIndex) == "YES";
      constraint.IsInitiallyDeferred = ReadStringOrNull(row, isInitiallyDeferredIndex) == "YES";
    }

    private static void ReadCascadeAction(ForeignKey foreignKey, IDataRecord row, int deleteRuleIndex)
    {
      var deleteRule = ReadStringOrNull(row, deleteRuleIndex);
      switch (deleteRule) {
        case "CASCADE":
          foreignKey.OnDelete = ReferentialAction.Cascade;
          return;
        case "SET NULL":
          foreignKey.OnDelete = ReferentialAction.SetNull;
          return;
        case "NO ACTION":
          foreignKey.OnDelete = ReferentialAction.NoAction;
          return;
        case "RESTRICT": // behaves like NO ACTION
          foreignKey.OnDelete = ReferentialAction.NoAction;
          return;
        case "SET DEFAULT":
          foreignKey.OnDelete = ReferentialAction.SetDefault;
          return;
      }
    }

    private static string GetTypeName(int majorTypeIdentifier, int? minorTypeIdentifier)
    {
      switch (majorTypeIdentifier) {
        case 7:
          return minorTypeIdentifier == 2
            ? "NUMERIC"
            : minorTypeIdentifier == 1
              ? "DECIMAL"
              : "SMALLINT";
        case 8:
          return minorTypeIdentifier == 2
            ? "NUMERIC"
            : minorTypeIdentifier == 1
              ? "DECIMAL"
              : "INTEGER";
        case 10:
          return "FLOAT";
        case 12:
          return "DATE";
        case 13:
          return "TIME";
        case 14:
          return "CHAR";
        case 16:
          return minorTypeIdentifier == 2
            ? "NUMERIC"
            : minorTypeIdentifier == 1
              ? "DECIMAL"
              : "BIGINT";
        case 27:
          return "DOUBLE PRECISION";
        case 29:
          return "TIMESTAMP WITH WITH TIME ZONE";
        case 35:
          return "TIMESTAMP";
        case 37:
          return "VARCHAR";
        case 261:
          return minorTypeIdentifier == 0
            ? "BLOB SUB TYPE 1"
            : minorTypeIdentifier == 1
              ? "BLOB SUB TYPE 0"
              : string.Empty;
        default:
          return string.Empty;
      }
    }

    protected override DbDataReader ExecuteReader(ISqlCompileUnit statement)
    {
      var commandText = Connection.Driver.Compile(statement).GetCommandText();
      // base.ExecuteReader(...) cannot be used because it disposes the DbCommand so it makes returned DbDataReader unusable
      return Connection.CreateCommand(commandText).ExecuteReader();
    }

    // Constructors

    public Extractor(SqlDriver driver)
      : base(driver)
    {
    }
  }

  internal partial class Extractor
  {
    protected virtual string GetExtractSchemasQuery()
    {
      return @"select " + Constants.DefaultSchemaName + @"from rdb$database";
    }

    protected virtual string GetExtractTablesQuery()
    {
      return @"
select cast(null as varchar(30)) as schema
      ,trim(rdb$relation_name) as table_name
      ,rdb$relation_type as table_type
from rdb$relations 
where rdb$relation_type in (0, 5, 4) and rdb$relation_name not starts with 'RDB$' and rdb$relation_name not starts with 'MON$'";
    }

    protected virtual string GetExtractTableColumnsQuery()
    {
      return @"
select   schema
        ,table_name
        ,ordinal_position
        ,column_name
        ,field_type
        ,column_subtype
        ,column_size
        ,numeric_precision
        ,-numeric_scale as numeric_scale
        ,character_max_length
        ,(1 - coalesce(column_nullable,0)) as column_nullable
        ,column_default
        ,relation_type
from     (select   cast(null as varchar(30)) as schema
                  ,trim(rfr.rdb$relation_name) as table_name
                  ,trim(rfr.rdb$field_name) as column_name
                  ,fld.rdb$field_sub_type as column_subtype
                  ,cast(fld.rdb$field_length as integer) as column_size
                  ,cast(fld.rdb$field_precision as integer) as numeric_precision
                  ,cast(fld.rdb$field_scale as integer) as numeric_scale
                  ,cast(fld.rdb$character_length as integer) as character_max_length
                  ,cast(fld.rdb$field_length as integer) as character_octet_length
                  ,rfr.rdb$field_position as ordinal_position
                  ,trim(rfr.rdb$field_source) as domain_name
                  ,trim(rfr.rdb$default_source) as column_default
                  ,trim(fld.rdb$computed_source) as computed_source
                  ,fld.rdb$dimensions as column_array
                  ,coalesce(fld.rdb$null_flag, rfr.rdb$null_flag) as column_nullable
                  ,0 as is_readonly
                  ,fld.rdb$field_type as field_type
                  ,trim(cs.rdb$character_set_name) as character_set_name
                  ,trim(coll.rdb$collation_name) as collation_name
                  ,trim(rfr.rdb$description) as description
                  ,cast(rr.rdb$relation_type as integer) as relation_type
          from     rdb$relations rr join rdb$relation_fields rfr on rfr.rdb$relation_name = rr.rdb$relation_name
                   left join rdb$fields fld on rfr.rdb$field_source = fld.rdb$field_name
                   left join rdb$character_sets cs
                      on cs.rdb$character_set_id = fld.rdb$character_set_id
                   left join rdb$collations coll
                      on (coll.rdb$collation_id = fld.rdb$collation_id
                      and coll.rdb$character_set_id = fld.rdb$character_set_id)
          where    rr.rdb$relation_type in (0, 4, 5) and rr.rdb$relation_name not starts with 'RDB$' and rr.rdb$relation_name not starts with 'MON$'
          order by table_name, ordinal_position)";
    }

    protected virtual string GetExtractViewsQuery()
    {
      return @"
select cast(null as varchar(30)) as schema
      ,trim(rdb$relation_name) as table_name
      ,rdb$view_source as view_source
from rdb$relations
where rdb$relation_type = 1";
    }

    protected virtual string GetExtractViewColumnsQuery()
    {
      return @"
select     cast(null as varchar(30)) as schema
          ,trim(rfr.rdb$relation_name) as table_name
          ,trim(rfr.rdb$field_name) as column_name
          ,rfr.rdb$field_position as ordinal_position
from       rdb$relations rr join rdb$relation_fields rfr on rfr.rdb$relation_name = rr.rdb$relation_name
where      rr.rdb$relation_type = 1
order by   rfr.rdb$relation_name, rfr.rdb$field_position";
    }

    protected virtual string GetExtractIndexesQuery()
    {
      return @"
select     cast(null as varchar(30)) as schema
          ,trim(ri.rdb$relation_name) as table_name
          ,trim(ri.rdb$index_name) as index_name
          ,ri.rdb$index_id as index_seq
          ,ri.rdb$index_type as descend
          ,ri.rdb$unique_flag as unique_flag
          ,trim(ris.rdb$field_name) as column_name
          ,ris.rdb$field_position as column_position
          ,ri.rdb$expression_source as expression_source
from       rdb$indices ri left join rdb$index_segments ris on ris.rdb$index_name = ri.rdb$index_name
where      ri.rdb$system_flag = 0
       and not exists
              (select   1
               from     rdb$relation_constraints rc
               where    rc.rdb$constraint_type in ('PRIMARY KEY', 'FOREIGN KEY')
                    and rc.rdb$relation_name = ri.rdb$relation_name
                    and rc.rdb$index_name = ri.rdb$index_name)
order by   ri.rdb$relation_name, ri.rdb$index_id, ris.rdb$field_position";
    }

    protected virtual string GetExtractForeignKeysQuery()
    {
      return @"
select     cast(null as varchar(30)) as schema
          ,trim(co.rdb$relation_name) as table_name
          ,trim(co.rdb$constraint_name) as constraint_name
          ,trim(co.rdb$deferrable) as is_deferrable
          ,trim(co.rdb$initially_deferred) as deferred
          ,trim(ref.rdb$delete_rule) as delete_rule
          ,trim(coidxseg.rdb$field_name) as column_name
          ,coidxseg.rdb$field_position as column_position
          ,cast(null as varchar(30)) as referenced_schema
          ,trim(refidx.rdb$relation_name) as referenced_table_name
          ,trim(refidxseg.rdb$field_name) as referenced_column_name
          ,trim(ref.rdb$match_option) as match_option
          ,trim(ref.rdb$update_rule) as update_rule
from       rdb$relation_constraints co join rdb$ref_constraints ref on co.rdb$constraint_name = ref.rdb$constraint_name
           join rdb$indices tempidx
              on co.rdb$index_name = tempidx.rdb$index_name
           join rdb$index_segments coidxseg
              on co.rdb$index_name = coidxseg.rdb$index_name
           join rdb$relation_constraints unqc
              on ref.rdb$const_name_uq = unqc.rdb$constraint_name and unqc.rdb$constraint_type in ('UNIQUE', 'PRIMARY KEY')
           join rdb$indices refidx
              on refidx.rdb$index_name = unqc.rdb$index_name and refidx.rdb$relation_name not starts with 'RDB$'
           join rdb$index_segments refidxseg
              on refidx.rdb$index_name = refidxseg.rdb$index_name
             and coidxseg.rdb$field_position = refidxseg.rdb$field_position
where      co.rdb$constraint_type = 'FOREIGN KEY'
order by   co.rdb$relation_name, co.rdb$constraint_name, coidxseg.rdb$field_position";
    }

    protected override string GetExtractUniqueAndPrimaryKeyConstraintsQuery()
    {
      return @"
select     cast(null as varchar(30)) as schema
          ,trim(rel.rdb$relation_name) as table_name
          ,trim(rel.rdb$constraint_name) as constraint_name
          ,trim(rel.rdb$constraint_type) constraint_type
          ,trim(seg.rdb$field_name) as column_name
          ,seg.rdb$field_position as column_position
from       rdb$relation_constraints rel
left join rdb$indices idx on rel.rdb$index_name = idx.rdb$index_name
left join rdb$index_segments seg on idx.rdb$index_name = seg.rdb$index_name
where      rel.rdb$constraint_type in ('PRIMARY KEY', 'UNIQUE')
           and rel.rdb$relation_name not starts with 'RDB$'
           and rel.rdb$relation_name not starts with 'MON$'
order by   rel.rdb$relation_name, rel.rdb$constraint_name, seg.rdb$field_position";
    }

    protected virtual string GetExtractCheckConstraintsQuery()
    {
      return @"
select     cast(null as varchar(30)) as schema
          ,trim(chktb.rdb$relation_name) as table_name
          ,trim(chktb.rdb$constraint_name) as constraint_name
          ,trim(trig.rdb$trigger_source) as check_clausule
from       rdb$relation_constraints chktb inner join rdb$check_constraints chk
              on (chktb.rdb$constraint_name = chk.rdb$constraint_name and chktb.rdb$constraint_type = 'CHECK')
           inner join rdb$triggers trig
              on chk.rdb$trigger_name = trig.rdb$trigger_name and trig.rdb$trigger_type = 1
order by   chktb.rdb$relation_name, chktb.rdb$constraint_name";
    }
  }
}
