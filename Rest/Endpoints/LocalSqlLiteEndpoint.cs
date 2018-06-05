using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace Dash
{
    public class LocalSqlLiteEndpoint : IModelEndpoint<FieldModel>
    {
        /// <summary>
        /// Connection to the sqlite database
        /// </summary>
        private SqliteConnection _db;


        public LocalSqlLiteEndpoint()
        {
            // create a string to connect to the databse (this string can be parameterized using the builder)
            var connectionStringBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\" + "dash.db",
            };

            // instantiate the connection to the database and open it
            _db = new SqliteConnection(connectionStringBuilder.ConnectionString);
            _db.Open();

            // set the database schema
            var createFieldCommand = new SqliteCommand
            {
                CommandText = @"
                CREATE TABLE IF NOT EXISTS `Fields` (
	                `id`	TEXT NOT NULL,
	                `field`	TEXT DEFAULT """",
	                PRIMARY KEY(`id`)
                );",
                Connection = _db
            };
            createFieldCommand.ExecuteNonQuery();
        }

        public void AddDocument(FieldModel newDocument, Action<FieldModel> success, Action<Exception> error)
        {
            var watch = Stopwatch.StartNew();

            var addDocCommand = new SqliteCommand
            {
                CommandText = @"INSERT OR REPLACE INTO `Fields` VALUES (@id, @field);",
                //CommandText = @"INSERT INTO `Fields` VALUES (@id, @field);",
                Connection = _db,
            };
            addDocCommand.Parameters.AddWithValue("@id", newDocument.Id);
            var serialize = newDocument.Serialize();
            addDocCommand.Parameters.AddWithValue("@field", serialize);

            try
            {
                addDocCommand.ExecuteNonQuery();
            }
            catch (SqliteException e)
            {
                error?.Invoke(e);
                return;
            }

            watch.Stop();
            Debug.WriteLine($"Add Doc: {watch.ElapsedMilliseconds}");

            success?.Invoke(newDocument);
        }

        public void UpdateDocument(FieldModel documentToUpdate, Action<FieldModel> success, Action<Exception> error)
        {
            var watch = Stopwatch.StartNew();

            var updateDocCommand =
                new SqliteCommand
                {
                    CommandText = @"UPDATE `Fields` SET `field`=@field WHERE `id`=@id;",
                    Connection = _db
                };

            updateDocCommand.Parameters.AddWithValue("@id", documentToUpdate.Id);
            var serialize = documentToUpdate.Serialize();
            updateDocCommand.Parameters.AddWithValue("@field", serialize);

            try
            {
                updateDocCommand.ExecuteNonQuery();
            }
            catch (SqliteException e)
            {
                error?.Invoke(e);
                return;
            }

            watch.Stop();
            Debug.WriteLine($"Update Doc: {watch.ElapsedMilliseconds}");
            //Debug.WriteLine(serialize);

            success?.Invoke(documentToUpdate);

        }

        public async Task GetDocument(string id, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            var watch = Stopwatch.StartNew();

            List<FieldModel> fieldModels;

            var getDocCommand = new SqliteCommand
            {
                CommandText = @"SELECT field from Fields WHERE `id`=@id;",
                Connection = _db
            };
            getDocCommand.Parameters.AddWithValue("@id", id);

            try
            {
                var reader = getDocCommand.ExecuteReader();
                fieldModels = GetFieldModels(reader);
            }
            catch (SqliteException e)
            {
                error?.Invoke(e);
                return;
            }


            watch.Stop();
            Debug.WriteLine($"GetDocument: {watch.ElapsedMilliseconds}");

            success?.Invoke(new RestRequestReturnArgs(fieldModels));
        }

        private List<FieldModel> GetFieldModels(SqliteDataReader reader)
        {
            var fieldModels = new List<FieldModel>();
            while (reader.Read())
            {
                var fm = reader.GetString(0).CreateObject<FieldModel>();
                fieldModels.Add(fm);
            }

            return fieldModels;
        }

        public async Task GetDocuments(IEnumerable<string> ids, Func<RestRequestReturnArgs, Task> success,
            Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public async Task GetDocuments<V>(IEnumerable<string> ids, Func<IEnumerable<V>, Task> success,
            Action<Exception> error) where V : EntityBase
        {
            throw new NotImplementedException();
        }

        public void DeleteDocument(FieldModel document, Action success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public void DeleteAllDocuments(Action success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public async Task GetDocumentsByQuery(IQuery<FieldModel> query, Func<RestRequestReturnArgs, Task> success,
            Action<Exception> error)
        {
            var watch = Stopwatch.StartNew();

            var getAllDocsCommand = new SqliteCommand
            {
                CommandText = @"SELECT field from Fields",
                Connection = _db
            };

            List<FieldModel> fieldModels;
            try
            {
                var reader = getAllDocsCommand.ExecuteReader();
                fieldModels = GetFieldModels(reader);

                fieldModels = fieldModels.Where(query.Func).ToList();
            }
            catch (SqliteException e)
            {
                error?.Invoke(e);
                return;
            }

            watch.Stop();
            Debug.WriteLine($"GetDocumentsByQuery: {watch.ElapsedMilliseconds}");

            success?.Invoke(new RestRequestReturnArgs(fieldModels));
        }

        public async Task GetDocumentsByQuery<V>(IQuery<FieldModel> query, Func<IEnumerable<V>, Task> success,
            Action<Exception> error) where V : EntityBase
        {
            var watch = Stopwatch.StartNew();

            var getAllDocsCommand = new SqliteCommand
            {
                CommandText = @"SELECT field from Fields",
                Connection = _db
            };

            List<FieldModel> fieldModels;
            try
            {
                var reader = getAllDocsCommand.ExecuteReader();
                fieldModels = GetFieldModels(reader);

                fieldModels = fieldModels.Where(query.Func).ToList();
            }
            catch (SqliteException e)
            {
                error?.Invoke(e);
                return;
            }

            watch.Stop();
            Debug.WriteLine($"GetDocumentsByQuery<V>s: {watch.ElapsedMilliseconds}");

            success?.Invoke(fieldModels.OfType<V>());
        }

        public async Task Close()
        {
            _db.Close();
        }

        public void HasDocument(FieldModel model, Action<bool> success, Action<Exception> error)
        {
            var watch = Stopwatch.StartNew();

            var hasDocCommand =
                new SqliteCommand
                {
                    CommandText = @"SELECT EXISTS (SELECT `id` FROM `Fields` WHERE `id`=@id LIMIT 1);",
                    Connection = _db
                };

            hasDocCommand.Parameters.AddWithValue("@id", model.Id);

            var hasDoc = false;

            try
            {
                var reader = hasDocCommand.ExecuteReader();
                reader.Read();
                hasDoc = reader.GetBoolean(0);
            }
            catch (SqliteException e)
            {
                error?.Invoke(e);
                return;
            }


            watch.Stop();
            Debug.WriteLine($"Update Doc: {watch.ElapsedMilliseconds}");

            success?.Invoke(hasDoc);
        }

        public bool CheckAllDocuments(IEnumerable<FieldModel> documents)
        {
            var watch = Stopwatch.StartNew();

            foreach (var doc in documents)
            {

                List<FieldModel> fieldModels;

                var getDocCommand = new SqliteCommand
                {
                    CommandText = @"SELECT field from Fields WHERE `id`=@id;",
                    Connection = _db
                };
                getDocCommand.Parameters.AddWithValue("@id", doc.Id);

                try
                {
                    var reader = getDocCommand.ExecuteReader();
                    fieldModels = GetFieldModels(reader);
                }
                catch (SqliteException e)
                {
                    throw;
                }

                if (!fieldModels.Any())
                {
                    Debug.Assert(false);
                    return false;
                }

                foreach (var fieldModel in fieldModels)
                {
                    if (!doc.Equals(fieldModel))
                    {
                        Debug.Assert(false);
                        return false;
                    }
                }
            }

            watch.Stop();
            Debug.WriteLine($"CheckDocs: {watch.ElapsedMilliseconds}");

            return true;

        }

        public Dictionary<string, string> GetBackups()
        {
            return new Dictionary<string, string>();
        }
    }

}
