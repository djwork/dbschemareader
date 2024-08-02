using DatabaseSchemaReader.DataSchema;
using DatabaseSchemaReader.ProviderSchemaReaders.ConnectionContext;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace DatabaseSchemaReader.ProviderSchemaReaders.Databases.Oracle
{
    class Schemas : SqlExecuter<DatabaseDbSchema>
    {
        public Schemas(int? commandTimeout) : base(commandTimeout, null)
        {
            //Sql = @"SELECT USERNAME AS name FROM ALL_USERS ORDER BY USERNAME";          //Returns all users reguardless of if they are actual owners of database objects         
            //Sql = @"SELECT DISTINCT OWNER AS name FROM ALL_OBJECTS ORDER BY OWNER";     //Returns distinct list of all owners of database objects
            Sql = @"SELECT DISTINCT OWNER AS name FROM DBA_OBJECTS ORDER BY OWNER";     //Returns distinct list of all owners of database objects but about 20x faster than using ALL_OBJECTS (which makes sense when you compare the DDLs of the views)
            //The other difference between ALL_OBJECTS and DBA_OBJECTS is that ALL_OBJECTS has builtin permissions checking to only show the objects that the current connecting user has permissions to see
            //See: https://asktom.oracle.com/ords/f?p=100:11:0::::P11_QUESTION_ID:9287207731148
        }

        protected override void AddParameters(DbCommand command)
        {
        }

        protected override void Mapper(IDataRecord record)
        {
            var name = record.GetString("name");
            var schema = new DatabaseDbSchema
            {
                Name = name,
            };
            Result.Add(schema);
        }

        public IList<DatabaseDbSchema> Execute(IConnectionAdapter connectionAdapter)
        {
            ExecuteDbReader(connectionAdapter);
            return Result;
        }
    }
}