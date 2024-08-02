using System;

namespace DatabaseSchemaReader.DataSchema
{
    /// <summary>
    /// Table sub compnents (columns, views, triggers, etc..)
    /// </summary>
    [Flags]
    public enum DatabaseTableComponentType
    {
        /// <summary>
        /// No Table Component Types
        /// </summary>
        None = 0,

        /// <summary>
        /// Table Columns
        /// </summary>
        Columns = 1 << 0,
        /// <summary>
        /// Table Identity Columns
        /// </summary>
        ColumnsIdentity = 1 << 1,
        /// <summary>
        /// Table Computed Columns
        /// </summary>
        ColumnsComputed = 1 << 2,
        /// <summary>
        /// All Table Column Types
        /// </summary>
        ColumnsAll = Columns | ColumnsIdentity | ColumnsComputed,
        /// <summary>
        /// Table Check Constraints
        /// </summary>
        ConstraintsCheck = 1 << 3,
        /// <summary>
        /// Table Default Constraints
        /// </summary>
        ConstraintsDefault = 1 << 4,
        /// <summary>
        /// Table Primary Key Constraints
        /// </summary>
        ConstraintsPrimaryKey = 1 << 5,
        /// <summary>
        /// Table Unique Key Constraints
        /// </summary>
        ConstraintsUniqueKey = 1 << 6,
        /// <summary>
        /// Table Foreign Key Constraints
        /// </summary>
        ConstraintsForeignKey = 1 << 7,
        /// <summary>
        /// All Table Constraint Types
        /// </summary>
        ConstraintsAll = ConstraintsCheck | ConstraintsDefault | ConstraintsPrimaryKey | ConstraintsUniqueKey | ConstraintsForeignKey,
        /// <summary>
        /// Table Indexes
        /// </summary>
        Indexes = 1 << 8,
        /// <summary>
        /// Table Triggers
        /// </summary>
        Triggers = 1 << 9,
        /// <summary>
        /// Table Descriptions
        /// </summary>
        DescriptionsTable = 1 << 10,
        /// <summary>
        /// Table Column Descriptions
        /// </summary>
        DescriptionsColumn = 1 << 11,
        /// <summary>
        /// All Table Description Types
        /// </summary>
        DescriptionsAll = DescriptionsTable | DescriptionsColumn,

        /// <summary>
        /// All Table Component Types
        /// </summary>
        All = ~(~0 << 12)
    }

    /// <summary>
    /// DatabaseTableComponentType Extension Helper
    /// </summary>
    public static class DatabaseTableComponentTypeExtensionHelper
    {
        /// <summary>
        /// Because .net 3.5 doesn't support Flags.HasFlag we need to provide this extension helper method
        /// </summary>
        /// <param name="self"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public static bool IsSet(this DatabaseTableComponentType self, DatabaseTableComponentType flag)
        {
            return (self & flag) == flag;
        }
    }
}