using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Dash.Util
{
    class Migrator
    {
        private SqliteConnection _db;
        public Migrator()
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\" + FileName,
            };

            // instantiate the connection to the database and open it
            _db = new SqliteConnection(connectionStringBuilder.ConnectionString);
            _db.Open();
        }

        private class KVP
        {
            public string key, value;
        }

        public bool MigrateOperatorModels()
        {
            var transaction = _db.BeginTransaction();
            var fieldQuery = new SqliteCommand()
            {
                CommandText = @"
                    SELECT id, field FROM `Fields`;",
                Connection = _db
            };


            var results = new List<KVP>();
            var fieldReader = fieldQuery.ExecuteReader();
            while (fieldReader.Read())
            {
                results.Add(new KVP(){key = fieldReader.GetString(0), value = fieldReader.GetString(1)});
            }
            fieldReader.Close();

            foreach (var result in results)
            {
                result.value = result.value.Replace()
            }

            transaction.Commit();

            return true;
        }
    }
}
