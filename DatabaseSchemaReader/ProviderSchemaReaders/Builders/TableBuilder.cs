using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
// ReSharper disable once RedundantUsingDirective 
using System.Threading;
using DatabaseSchemaReader.DataSchema;
using DatabaseSchemaReader.ProviderSchemaReaders.Adapters;

namespace DatabaseSchemaReader.ProviderSchemaReaders.Builders
{
    class TableBuilder
    {
        private readonly ReaderAdapter _readerAdapter;

        public event EventHandler<ReaderEventArgs> ReaderProgress;

        public TableBuilder(ReaderAdapter readerAdapter)
        {
            _readerAdapter = readerAdapter;
        }

        protected void RaiseReadingProgress(SchemaObjectType schemaObjectType)
        {
            ReaderEventArgs.RaiseEvent(ReaderProgress, this, ProgressType.ReadingSchema, schemaObjectType);
        }

        protected void RaiseProgress(ProgressType progressType,
            SchemaObjectType schemaObjectType,
            string name, int? index, int? count)
        {
            ReaderEventArgs.RaiseEvent(ReaderProgress, this, progressType, schemaObjectType,
    name, index, count);
        }

        private IList<DatabaseTable> EmptyList()
        {
            return new List<DatabaseTable>();
        }

        public DatabaseTable Execute(CancellationToken ct, string tableName)
        {
            if (ct.IsCancellationRequested) return null;

            var tables = _readerAdapter.Tables(tableName);
            if (tables.Count == 0)
            {
                return null;
            }

            tableName = tables.FirstOrDefault()?.Name ?? tableName;

            if (string.IsNullOrEmpty(_readerAdapter.Parameters.Owner))
            {
                var owner = tables[0].SchemaOwner;
                Trace.WriteLine("Using first schema " + owner);
                _readerAdapter.Parameters.Owner = owner;
            }
            var columns = _readerAdapter.Columns(tableName);
            var identityColumns = _readerAdapter.IdentityColumns(tableName);
            var checkConstraints = _readerAdapter.CheckConstraints(tableName);
            var pks = _readerAdapter.PrimaryKeys(tableName);
            var uks = _readerAdapter.UniqueKeys(tableName);
            var fks = _readerAdapter.ForeignKeys(tableName);
            var dfs = _readerAdapter.DefaultConstraints(tableName);
            var triggers = _readerAdapter.Triggers(tableName);
            var tableDescs = _readerAdapter.TableDescriptions(tableName);
            var colDescs = _readerAdapter.ColumnDescriptions(tableName);
            var computed = _readerAdapter.ComputedColumns(tableName);

            var indexes = MergeIndexColumns(_readerAdapter.Indexes(null), _readerAdapter.IndexColumns(null));
            FillOutForeignKey(fks, indexes);
            var noIndexes = (indexes.Count == 0); //we may not be able to get any indexes without a tableName
            if (noIndexes)
            {
                indexes.Clear();
                indexes = MergeIndexColumns(_readerAdapter.Indexes(tableName), _readerAdapter.IndexColumns(tableName));
            }
            if (columns.Count == 0) return null;

            var table = new DatabaseTable
            {
                SchemaOwner = _readerAdapter.Parameters.Owner,
                Name = tableName
            };
            table.Columns.AddRange(columns);
            UpdateCheckConstraints(table, checkConstraints);
            UpdateIdentities(table.Columns, identityColumns);
            UpdateComputed(table.Columns, computed);
            UpdateConstraints(table, pks, ConstraintType.PrimaryKey);
            UpdateConstraints(table, uks, ConstraintType.UniqueKey);
            UpdateConstraints(table, fks, ConstraintType.ForeignKey);
            UpdateConstraints(table, dfs, ConstraintType.Default);
            TableIndexMerger.UpdateIndexes(table, indexes);
            UpdateTriggers(table, triggers);
            UpdateTableDescriptions(table, tableDescs);
            UpdateColumnDescriptions(table, colDescs);
            _readerAdapter.PostProcessing(table);
            return table;
        }

        public IList<DatabaseTable> Execute(CancellationToken ct, DatabaseTableComponentType components)
        {
            if (ct.IsCancellationRequested) return EmptyList();

            var tables = _readerAdapter.Tables(null);

            if (ct.IsCancellationRequested) return tables;

            var columns = components.IsSet(DatabaseTableComponentType.Columns) ? _readerAdapter.Columns(null) : new List<DatabaseColumn>();
            var identityColumns = components.IsSet(DatabaseTableComponentType.ColumnsIdentity) ? _readerAdapter.IdentityColumns(null) : new List<DatabaseColumn>();
            var checkConstraints = components.IsSet(DatabaseTableComponentType.ConstraintsCheck) ? _readerAdapter.CheckConstraints(null) : new List<DatabaseConstraint>();
            var pks = components.IsSet(DatabaseTableComponentType.ConstraintsPrimaryKey) ? _readerAdapter.PrimaryKeys(null) : new List<DatabaseConstraint>();
            var uks = components.IsSet(DatabaseTableComponentType.ConstraintsUniqueKey) ? _readerAdapter.UniqueKeys(null) : new List<DatabaseConstraint>();
            var fks = components.IsSet(DatabaseTableComponentType.ConstraintsForeignKey) ? _readerAdapter.ForeignKeys(null) : new List<DatabaseConstraint>();

            var dfs = components.IsSet(DatabaseTableComponentType.ConstraintsDefault) ? _readerAdapter.DefaultConstraints(null) : new List<DatabaseConstraint>();
            var triggers = components.IsSet(DatabaseTableComponentType.Triggers) ? _readerAdapter.Triggers(null) : new List<DatabaseTrigger>();
            var tableDescs = components.IsSet(DatabaseTableComponentType.DescriptionsTable) ? _readerAdapter.TableDescriptions(null) : new List<DatabaseTable>();
            var colDescs = components.IsSet(DatabaseTableComponentType.DescriptionsColumn) ? _readerAdapter.ColumnDescriptions(null) : new List<DatabaseTable>();
            var computed = components.IsSet(DatabaseTableComponentType.ColumnsComputed) ? _readerAdapter.ComputedColumns(null) : new List<DatabaseColumn>();
            var indexes = components.IsSet(DatabaseTableComponentType.Indexes) ? MergeIndexColumns(_readerAdapter.Indexes(null), _readerAdapter.IndexColumns(null)) : ((IList<DatabaseIndex>)new List<DatabaseIndex>());

            var noIndexes = (indexes.Count == 0); //we may not be able to get any indexes without a tableName
            if (components.IsSet(DatabaseTableComponentType.Indexes) && components.IsSet(DatabaseTableComponentType.ConstraintsForeignKey)) 
                FillOutForeignKey(fks, indexes);
            
            var tableFilter = _readerAdapter.Parameters.Exclusions.TableFilter;
            if (tableFilter != null)
            {
                tables = tables.Where(t => !tableFilter.Exclude(t.Name)).ToList();
            }

            int tablesCount = tables.Count;
            for (var i = 0; i < tablesCount; i++)
            {
                var table = tables[i];
                var tableName = table.Name;
                var schemaName = table.SchemaOwner;

                if (ct.IsCancellationRequested) return tables;
                RaiseProgress(ProgressType.Processing, SchemaObjectType.Tables,
                    tableName, i, tablesCount);
                IEnumerable<DatabaseColumn> tableCols = Enumerable.Empty<DatabaseColumn>();
                if (components.IsSet(DatabaseTableComponentType.Columns))
                {
                    if (columns.Count == 0)
                    {
                        tableCols = _readerAdapter.Columns(tableName);
                    }
                    else
                    {
                        tableCols =
                           columns.Where(x => string.Equals(x.TableName, tableName, StringComparison.OrdinalIgnoreCase)
                                              && string.Equals(x.SchemaOwner, schemaName, StringComparison.OrdinalIgnoreCase));
                    }
                }
                table.Columns.AddRange(tableCols);
                UpdateIdentities(table.Columns, identityColumns);
                UpdateCheckConstraints(table, checkConstraints);
                UpdateComputed(table.Columns, computed);
                UpdateConstraints(table, pks, ConstraintType.PrimaryKey);
                UpdateConstraints(table, uks, ConstraintType.UniqueKey);
                UpdateConstraints(table, fks, ConstraintType.ForeignKey);
                UpdateConstraints(table, dfs, ConstraintType.Default);
                if (components.IsSet(DatabaseTableComponentType.Indexes) && noIndexes)
                {
                    indexes.Clear();
                    indexes = MergeIndexColumns(_readerAdapter.Indexes(tableName), _readerAdapter.IndexColumns(tableName));
                }
                TableIndexMerger.UpdateIndexes(table, indexes);
                UpdateTriggers(table, triggers);
                UpdateTableDescriptions(table, tableDescs);
                UpdateColumnDescriptions(table, colDescs);
                _readerAdapter.PostProcessing(table);
            }

            return tables;
        }

        private static void FillOutForeignKey(IList<DatabaseConstraint> fks, IList<DatabaseIndex> indexes)
        {
            foreach (var fk in fks.Where(f =>
                !string.IsNullOrEmpty(f.RefersToConstraint) && string.IsNullOrEmpty(f.RefersToTable)))
            {
                var constraint = indexes.FirstOrDefault(i => i.Name == fk.RefersToConstraint);
                if (constraint == null) continue;
                fk.RefersToTable = constraint.TableName;
                fk.RefersToSchema = constraint.SchemaOwner;
            }
        }

        private void UpdateTableDescriptions(DatabaseTable table, IList<DatabaseTable> descriptions)
        {
            var tableDesc = descriptions.FirstOrDefault(x => x.SchemaOwner == table.SchemaOwner &&
                                                x.Name == table.Name);
            if (tableDesc == null) return;
            table.Description = tableDesc.Description;
        }

        private void UpdateColumnDescriptions(DatabaseTable table, IList<DatabaseTable> descriptions)
        {
            var tableDesc = descriptions.FirstOrDefault(x => x.SchemaOwner == table.SchemaOwner &&
                                                x.Name == table.Name);
            if (tableDesc == null) return;
            foreach (var column in tableDesc.Columns)
            {
                var col = table.Columns.Find(x => x.Name == column.Name);
                if (col != null)
                {
                    col.Description = column.Description;
                }
            }
        }

        private void UpdateTriggers(DatabaseTable table, IList<DatabaseTrigger> triggers)
        {
            var tableTriggers = triggers.Where(x => x.SchemaOwner == table.SchemaOwner &&
                                                x.TableName == table.Name);
            table.Triggers.Clear();
            table.Triggers.AddRange(tableTriggers);
        }



        private IList<DatabaseIndex> MergeIndexColumns(IList<DatabaseIndex> indexes, IList<DatabaseIndex> indexColumns)
        {
            if (indexes == null || indexes.Count == 0) return indexColumns;
            foreach (var indexColumn in indexColumns)
            {
                var index = indexes.FirstOrDefault(f =>
                                            f.Name == indexColumn.Name &&
                                            f.SchemaOwner == indexColumn.SchemaOwner &&
                                            f.TableName.Equals(indexColumn.TableName, StringComparison.OrdinalIgnoreCase));
                if (index == null)
                {
                    index = indexColumn;
                    indexes.Add(index);
                    continue;
                }
                //copy the index columns across
                index.Columns.AddRange(indexColumn.Columns);
            }
            return indexes;
        }

        private void UpdateConstraints(DatabaseTable table, IList<DatabaseConstraint> constraints, ConstraintType constraintType)
        {
            var keys = constraints.Where(x => x.SchemaOwner == table.SchemaOwner &&
                                                x.TableName == table.Name &&
                                                x.ConstraintType == constraintType);
            table.AddConstraints(keys);
        }

        private void UpdateCheckConstraints(DatabaseTable table, IList<DatabaseConstraint> constraints)
        {
            var checks = constraints.Where(x => x.SchemaOwner == table.SchemaOwner &&
                                                x.TableName == table.Name);
            table.AddConstraints(checks);
        }

        private void UpdateIdentities(IList<DatabaseColumn> list, IList<DatabaseColumn> ids)
        {
            foreach (var id in ids)
            {
                var column = list.FirstOrDefault(x => x.SchemaOwner == id.SchemaOwner &&
                                                      x.TableName == id.TableName &&
                                                      x.Name == id.Name);
                if (column == null) continue;
                column.IdentityDefinition = new DatabaseColumnIdentity
                {
                    IdentitySeed = id.IdentityDefinition.IdentitySeed,
                    IdentityIncrement = id.IdentityDefinition.IdentityIncrement,
                };
            }
        }

        private void UpdateComputed(IList<DatabaseColumn> list, IList<DatabaseColumn> computeds)
        {
            foreach (var computed in computeds)
            {
                var column = list.FirstOrDefault(x => x.SchemaOwner == computed.SchemaOwner &&
                                                      x.TableName == computed.TableName &&
                                                      x.Name == computed.Name);
                if (column == null) continue;
                column.ComputedDefinition = computed.ComputedDefinition;
            }
        }
    }
}