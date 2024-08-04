using System;

namespace DatabaseSchemaReader.DataSchema
{
    /// <summary>
    /// Represents a trigger in the database.
    /// </summary>
    [Serializable]
    public partial class DatabaseTrigger : NamedSchemaObject<DatabaseTrigger>
    {
        string _tableSchemaOwner = null;
        /// <summary>
        /// Gets or sets the table's schema owner.
        /// </summary>
        /// <value>
        /// The table's schema owner.
        /// </value>
        /// <remarks>
        /// Not all database's support the ability for a trigger to be in a different schema than the table it is attached to.
        /// Oracle certainly does support this.
        /// 
        /// If not defind this property will return the value of the SchemaOwner property
        /// </remarks>
        public string TableSchemaOwner { 
            get
            {
                if (string.IsNullOrEmpty(this._tableSchemaOwner))
                {
                    return this.SchemaOwner;
                }
                else
                {
                    return this._tableSchemaOwner;
                }
            } 

            set
            {
                this._tableSchemaOwner = value;
            } 
        }

        /// <summary>
        /// Gets or sets the name of the table.
        /// </summary>
        /// <value>
        /// The name of the table.
        /// </value>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the trigger body.
        /// </summary>
        /// <value>
        /// The trigger body.
        /// </value>
        public string TriggerBody { get; set; }

        /// <summary>
        /// Gets or sets the trigger event (INSERT, UPDATE, DELETE or a combination of these)
        /// </summary>
        /// <value>
        /// The trigger event.
        /// </value>
        public string TriggerEvent { get; set; }

        /// <summary>
        /// Gets or sets the trigger type.
        /// </summary>
        /// <value>
        /// The trigger type.
        /// </value>
        /// <remarks>
        /// In oracle, one of BEFORE STATEMENT, BEFORE EACH ROW, AFTER STATEMENT, AFTER EACH ROW, INSTEAD OF, COMPOUND
        /// In SqlServer, our custom SQL uses AFTER and INSTEAD OF
        /// </remarks>
        public string TriggerType { get; set; }

        /// <summary>
        /// Gets or sets the trigger Enabled status.
        /// </summary>
        /// <value>
        /// If True the trigger is enabled.
        /// </value>
        /// <remarks>
        /// Defaults to true incase the database does not have the concept of a disabled trigger
        /// </remarks>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (string.Equals(SchemaOwner, TableSchemaOwner, StringComparison.OrdinalIgnoreCase))
                return Name + " on " + TableName;
            else
                return String.Format("{0}.{1} on {2}.{3}", SchemaOwner, Name, TableSchemaOwner, TableName);
        }
    }
}
