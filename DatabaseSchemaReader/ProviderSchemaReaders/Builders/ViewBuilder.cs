using DatabaseSchemaReader.DataSchema;
using DatabaseSchemaReader.Filters;
using DatabaseSchemaReader.ProviderSchemaReaders.Adapters;
using DatabaseSchemaReader.ProviderSchemaReaders.ResultModels;
using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable once RedundantUsingDirective
using System.Threading;

namespace DatabaseSchemaReader.ProviderSchemaReaders.Builders
{
    class ViewBuilder
    {
        private readonly ReaderAdapter _readerAdapter;
        private readonly Exclusions _exclusions;

        public event EventHandler<ReaderEventArgs> ReaderProgress;

        public ViewBuilder(ReaderAdapter readerAdapter, Exclusions exclusions)
        {
            _readerAdapter = readerAdapter;
            _exclusions = exclusions;
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

        public IList<DatabaseView> Execute(CancellationToken ct, DatabaseViewComponentType components)
        {
            if (ct.IsCancellationRequested) return new List<DatabaseView>();
            RaiseReadingProgress(SchemaObjectType.Views);
            var views = _readerAdapter.Views(null);

            if (ct.IsCancellationRequested) return views;
            ReaderEventArgs.RaiseEvent(ReaderProgress, this, ProgressType.Processing, SchemaObjectType.Views);
            var viewFilter = _exclusions.ViewFilter;
            if (viewFilter != null)
            {
                views = views.Where(t => !viewFilter.Exclude(t.Name)).ToList();
            }

            if (ct.IsCancellationRequested) return views;
            var sources = components.IsSet(DatabaseViewComponentType.Source) ? _readerAdapter.ViewSources(null) : new List<ProcedureSource>();
            if (components.IsSet(DatabaseViewComponentType.Source) && sources.Count > 0)
            {
                foreach (var view in views)
                {
                    var owner = view.SchemaOwner;
                    var name = view.Name;
                    var src = sources.FirstOrDefault(x => x.Name == name && x.SchemaOwner == owner);
                    if (src != null) view.Sql = src.Text;
                }
            }

            if (ct.IsCancellationRequested) return views;
            var indexes = components.IsSet(DatabaseViewComponentType.Indexes) ? _readerAdapter.ViewIndexes(null) : new List<DatabaseIndex>();

            //get full datatables for all tables, to minimize database calls
            if (ct.IsCancellationRequested) return views;
            RaiseReadingProgress(SchemaObjectType.ViewColumns);

            var viewColumns = components.IsSet(DatabaseViewComponentType.Columns) ? _readerAdapter.ViewColumns(null) : new List<DatabaseColumn>();
            var count = views.Count;
            for (var index = 0; index < count; index++)
            {
                if (ct.IsCancellationRequested) return views;
                DatabaseView v = views[index];
                ReaderEventArgs.RaiseEvent(ReaderProgress, this, ProgressType.Processing, SchemaObjectType.ViewColumns, v.Name, index, count);
                if (components.IsSet(DatabaseViewComponentType.Columns))
                {
                    IEnumerable<DatabaseColumn> cols;
                    if (viewColumns.Count == 0)
                    {
                        cols = _readerAdapter.ViewColumns(v.Name);
                    }
                    else
                    {
                        cols = viewColumns.Where(x => x.TableName == v.Name && x.SchemaOwner == v.SchemaOwner);
                    }
                    v.Columns.AddRange(cols);
                }
                if (components.IsSet(DatabaseViewComponentType.Indexes))
                {
                    v.Indexes = indexes.Where(x => x.TableName == v.Name && x.SchemaOwner == v.SchemaOwner).ToList();
                }
            }

            if (components.IsSet(DatabaseViewComponentType.Triggers))
            {
                var triggers = _readerAdapter.Triggers(null);
                foreach (var view in views)
                {
                    UpdateTriggers(view, triggers);
                }
            }

            return views;
        }

        private void UpdateTriggers(DatabaseView view, IList<DatabaseTrigger> triggers)
        {
            var viewTriggers = triggers.Where(x => x.SchemaOwner == view.SchemaOwner &&
                                                   x.TableName == view.Name);
            view.Triggers.Clear();
            view.Triggers.AddRange(viewTriggers);
        }
    }
}