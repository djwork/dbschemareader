using System;

namespace DatabaseSchemaReader.DataSchema
{
    /// <summary>
    /// View sub compnents (columns, constraints, triggers, etc..)
    /// </summary>
    [Flags]
    public enum DatabaseViewComponentType
    {
        /// <summary>
        /// No View Component Types
        /// </summary>
        None = 0,

        /// <summary>
        /// View Columns
        /// </summary>
        Columns = 1 << 0,        
        /// <summary>
        /// View Check Constraints
        /// </summary>
        Source = 1 << 1,
        /// <summary>
        /// View Indexes
        /// </summary>
        Indexes = 1 << 2,
        /// <summary>
        /// View Triggers
        /// </summary>
        Triggers = 1 << 3,

        /// <summary>
        /// All View Component Types
        /// </summary>
        All = ~(~0 << 4)
    }

    /// <summary>
    /// DatabaseViewComponentType Extension Helper
    /// </summary>
    public static class DatabaseViewComponentTypeExtensionHelper
    {
        /// <summary>
        /// Because .net 3.5 doesn't support Flags.HasFlag we need to provide this extension helper method
        /// </summary>
        /// <param name="self"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public static bool IsSet(this DatabaseViewComponentType self, DatabaseViewComponentType flag)
        {
            return (self & flag) == flag;
        }
    }
}