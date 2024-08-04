using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using DatabaseSchemaReader.DataSchema;
using DatabaseSchemaReader.ProviderSchemaReaders.ConnectionContext;

namespace DatabaseSchemaReader.ProviderSchemaReaders.Databases.Oracle
{
    class Triggers : OracleSqlExecuter<DatabaseTrigger>
    {
        private readonly string _tableName;
        public Triggers(int? commandTimeout, string owner, string tableName) : base(commandTimeout, owner)
        {
            _tableName = tableName;
            Sql = @"SELECT OWNER,
  TRIGGER_NAME,
  TABLE_OWNER,
  TABLE_NAME,
  TRIGGER_BODY,
  TRIGGERING_EVENT,
  TRIGGER_TYPE,
  CASE STATUS WHEN 'DISABLED' THEN 'true' ELSE 'false' END as IS_DISABLED
FROM ALL_TRIGGERS
WHERE (TABLE_NAME = :tableName OR :tableName IS NULL) AND 
(OWNER = :schemaOwner OR :schemaOwner IS NULL) AND 
TRIGGER_NAME NOT IN ( SELECT object_name FROM USER_RECYCLEBIN ) 
";

        }

        protected override void AddParameters(DbCommand command)
        {
            EnsureOracleBindByName(command);
            AddDbParameter(command, "schemaOwner", Owner);
            AddDbParameter(command, "tableName", _tableName);
        }

        protected override void Mapper(IDataRecord record)
        {
            var trigger = new DatabaseTrigger
            {
                Name = record.GetString("TRIGGER_NAME"),
                SchemaOwner = record.GetString("OWNER"),
                TableSchemaOwner = record.GetString("TABLE_OWNER"),
                TableName = record.GetString("TABLE_NAME"),
                TriggerBody = record.GetString("TRIGGER_BODY"),
                TriggerType = record.GetString("TRIGGER_TYPE"),
                TriggerEvent = record.GetString("TRIGGERING_EVENT"),
                Enabled = record.GetBoolean("IS_DISABLED")
            };
            Result.Add(trigger);
        }

        public IList<DatabaseTrigger> Execute(IConnectionAdapter connectionAdapter)
        {
            ExecuteDbReader(connectionAdapter);
            return Result;
        }
    }
}
