using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DashShared;
using Microsoft.Data.Sqlite;

namespace Dash
{
    class Migrator
    {
        private SqliteConnection _db;
        public Migrator()
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\dash.db",
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
                Connection = _db,
                Transaction = transaction
            };


            var results = new List<KVP>();
            var fieldReader = fieldQuery.ExecuteReader();
            while (fieldReader.Read())
            {
                results.Add(new KVP(){key = fieldReader.GetString(0), value = fieldReader.GetString(1)});
            }
            fieldReader.Close();

            var opRegex =
                new Regex(
                    "\"Type\":{\"\\$type\":\"DashShared\\.KeyModel, DashShared\",\"Name\":\".*\",\"id\":(?'id'\"[0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12})\"}");
            var keyRegex = new Regex("^{\"\\$type\":\"DashShared\\.KeyModel, DashShared\",\"Name\":\"(?'name'.*)\",\"id\":\"[0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12}\"}");
            var keys = new List<(KVP, string name)>();
            foreach (var result in results)
            {
                if (result.value.Contains("OperatorModel"))
                {
                    result.value = opRegex.Replace(result.value, match => "\"TypeId\":" + match.Groups["id"].Value.ToUpper() + "\"");
                }

                var keyMatch = keyRegex.Match(result.value);
                if (keyMatch.Success)
                {
                    keys.Add((result, keyMatch.Groups["name"].Value));
                }
            }

            var updateKeysCommand = new SqliteCommand()
            {
                CommandText = @"
                    UPDATE `Fields` SET `id`=@newId WHERE `id`=@id;",
                Connection = _db,
                Transaction = transaction
            };

            var keyMap = new Dictionary<string, string>();
            foreach (var (key, name) in keys)
            {
                var actualName = name;
                if (actualName == "_DocumentContext")
                {
                    actualName = "DocumentContext";
                }
                var oldId = key.key;
                var newId = UtilShared.GetDeterministicGuid(actualName).ToString().ToUpper();
                keyMap[oldId] = newId;
                key.key = newId;
                updateKeysCommand.Parameters.Clear();
                updateKeysCommand.Parameters.AddWithValue("@newId", newId);
                updateKeysCommand.Parameters.AddWithValue("@id", oldId);
                updateKeysCommand.ExecuteNonQuery();
            }

            foreach (var result in results)
            {
                foreach (var keyMapping in keyMap)
                {
                    result.value = result.value.Replace(keyMapping.Key, keyMapping.Value);
                }
            }

            var updateCommand = new SqliteCommand()
            {
                CommandText = @"
                    UPDATE `Fields` SET `field`=@field WHERE `id`=@id;",
                Connection = _db,
                Transaction = transaction
            };
            foreach (var result in results)
            {
                updateCommand.Parameters.Clear();
                updateCommand.Parameters.AddWithValue("@field", result.value);
                updateCommand.Parameters.AddWithValue("@id", result.key);
                updateCommand.ExecuteNonQuery();
            }

            transaction.Commit();

            _db.Close();
            return true;
        }
    }
}
