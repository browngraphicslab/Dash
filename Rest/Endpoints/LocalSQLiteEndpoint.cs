using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DashShared;
using Microsoft.Data.Sqlite;

namespace Dash
{
    public class LocalSqliteEndpoint : IModelEndpoint<FieldModel>
    {
        /// <summary>
        /// Connection to the sqlite database
        /// </summary>
        private SqliteConnection _db;

        #region DATABASE INITIALIZED IN CONSTRUCTOR

        public LocalSqliteEndpoint()
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
                //Creates a new database with the title "Fields" containing two TEXT attributes (columns) "id" and "field" with the respective default values of NOT NULL and ""
                //The primary key, or the attribute for which each tuplet (entry, or row) must be different is set to "id". In English, it's the document id's that differentiate the entries. 
                CommandText = @"
                CREATE TABLE IF NOT EXISTS `Fields` (
	                `id`	TEXT NOT NULL,
	                `field`	TEXT DEFAULT """",
	                PRIMARY KEY(`id`)
                );",
                Connection = _db
            };
            //Create database
            createFieldCommand.ExecuteNonQuery();
        }

        #endregion

        #region DATABASE MUTATORS

        public void AddDocument(FieldModel newDocument, Action<FieldModel> success, Action<Exception> error)
        {
            var watch = Stopwatch.StartNew();

            var addDocCommand = new SqliteCommand
            {
                //i.e. "In the database "Fields", insert a new field (filled with the serialized text of the received document) at/with the document id specified
                //if something already exists at the given document ID, replace it
                CommandText = @"INSERT OR REPLACE INTO `Fields` VALUES (@id, @field);",
                Connection = _db,
            };
            addDocCommand.Parameters.AddWithValue("@id", newDocument.Id);
            addDocCommand.Parameters.AddWithValue("@field", newDocument.Serialize());
            watch.Stop();

            if (!SafeExecuteMutateQuery(addDocCommand, error, "AddDocument", watch.ElapsedMilliseconds)) return;

            success?.Invoke(newDocument);
        }

        public void UpdateDocument(FieldModel documentToUpdate, Action<FieldModel> success, Action<Exception> error)
        {
            var watch = Stopwatch.StartNew();

            var updateDocCommand =
                new SqliteCommand
                {
                    //i.e. "Set the field attribute in the database to the field serialization (below) only for the tuplet with the document ID (also specified below)"
                    CommandText = @"UPDATE `Fields` SET `field`=@field WHERE `id`=@id;",
                    Connection = _db
                };
            updateDocCommand.Parameters.AddWithValue("@id", documentToUpdate.Id);
            updateDocCommand.Parameters.AddWithValue("@field", documentToUpdate.Serialize());
            watch.Stop();

            if (!SafeExecuteMutateQuery(updateDocCommand, error, "UpdateDocument", watch.ElapsedMilliseconds)) return;

            success?.Invoke(documentToUpdate);
        }

        public void DeleteDocument(FieldModel documentToDelete, Action success, Action<Exception> error)
        {
            var watch = Stopwatch.StartNew();

            var deleteDocCommand = new SqliteCommand
            {
                //i.e. "Delete the tuplet from "Fields" that has the specified document id"
                CommandText = @"DELETE FROM `Fields` WHERE `id`=@id;",
                Connection = _db
            };
            deleteDocCommand.Parameters.AddWithValue("@id", documentToDelete.Id);
            watch.Stop();

            if (!SafeExecuteMutateQuery(deleteDocCommand, error, "DeleteDocument", watch.ElapsedMilliseconds)) return;

            success?.Invoke();
        }

        public void DeleteAllDocuments(Action success, Action<Exception> error)
        {
            var watch = Stopwatch.StartNew();

            var deleteAllCommand = new SqliteCommand
            {
                //i.e. "Delete all tuplets (rows) from the database. In essence, clear the database"
                CommandText = @"DELETE FROM `Fields`;",
                Connection = _db
            };
            watch.Stop();

            if (!SafeExecuteMutateQuery(deleteAllCommand, error, "DeleteAllDocuments", watch.ElapsedMilliseconds)) return;

            success?.Invoke();
        }

        #endregion

        #region DATABASE ACCESSORS

        public async Task GetDocument(string id, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            var watch = Stopwatch.StartNew();

            var getDocCommand = new SqliteCommand
            {
                //i.e. "In "Fields", return the field contents at the specified document id"
                CommandText = @"SELECT `field` FROM `Fields` WHERE `id`=@id;",
                Connection = _db
            };
            getDocCommand.Parameters.AddWithValue("@id", id);
            watch.Stop();

            var fieldModels = SafeExecuteAccessQuery(getDocCommand, error, "GetDocument", watch.ElapsedMilliseconds);
            if (fieldModels == null) return;

            success?.Invoke(new RestRequestReturnArgs(fieldModels));
        }

        public async Task GetDocuments(IEnumerable<string> ids, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            var watch = Stopwatch.StartNew();

            var enumerable = ids as string[] ?? ids.ToArray();
            var tempParams = new string[enumerable.Length];
            
            for (var i = 0; i < enumerable.Length; ++i) { tempParams[i] = "@param" + i; }

            //IN (" + string.Join(',', temp) + "
            var getDocCommand = new SqliteCommand
            {
                //i.e. "In "Fields", return the field contents at the specified document ids"
                CommandText = @"SELECT `field` from `Fields` WHERE `id` in (" + string.Join(", ", tempParams) + ");",
                Connection = _db
            };

            for (var i = 0; i < enumerable.Length; ++i) { getDocCommand.Parameters.AddWithValue(tempParams[i], enumerable[i]); }

            watch.Stop();

            var fieldModels = SafeExecuteAccessQuery(getDocCommand, error, "GetDocumentsssss", watch.ElapsedMilliseconds);
            if (fieldModels == null) return;

            success?.Invoke(new RestRequestReturnArgs(fieldModels));
        }

        public async Task GetDocuments<V>(IEnumerable<string> ids, Func<IEnumerable<V>, Task> success, Action<Exception> error) where V : EntityBase
        {
            throw new NotImplementedException();
        }

        public async Task GetDocumentsByQuery(IQuery<FieldModel> query, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            var watch = Stopwatch.StartNew();

            var getAllDocsCommand = new SqliteCommand
            {
                CommandText = @"SELECT `field` FROM `Fields`;",
                Connection = _db
            };
            watch.Stop();

            List<FieldModel> fieldModels;
            try
            {
                fieldModels = GetFieldModels(getAllDocsCommand.ExecuteReader());
                fieldModels = fieldModels.Where(query.Func).ToList();
            }
            catch (SqliteException e)
            {
                Debug.WriteLine($"LocalSqliteEndpoint.cs, GetDocumentsByQuery @ Time Elapsed = {watch.ElapsedMilliseconds}");
                error?.Invoke(e);
                return;
            }

            success?.Invoke(new RestRequestReturnArgs(fieldModels));
        }

        public async Task GetDocumentsByQuery<V>(IQuery<FieldModel> query, Func<IEnumerable<V>, Task> success, Action<Exception> error) where V : EntityBase
        {
            var watch = Stopwatch.StartNew();

            var getAllDocsCommand = new SqliteCommand
            {
                CommandText = @"SELECT field from Fields",
                Connection = _db
            };
            watch.Stop();

            List<FieldModel> fieldModels;
            try
            {
                fieldModels = GetFieldModels(getAllDocsCommand.ExecuteReader());
                fieldModels = fieldModels.Where(query.Func).ToList();
            }
            catch (SqliteException e)
            {
                Debug.WriteLine($"LocalSqliteEndpoint.cs, GetDocumentsByQuery<V> (1) @ Time Elapsed = {watch.ElapsedMilliseconds}");
                error?.Invoke(e);
                return;
            }

            success?.Invoke(fieldModels.OfType<V>());
        }

        public void HasDocument(FieldModel model, Action<bool> success, Action<Exception> error)
        {
            var watch = Stopwatch.StartNew();

            var hasDocCommand = new SqliteCommand
            {
                CommandText = @"SELECT EXISTS (SELECT `id` FROM `Fields` WHERE `id`=@id LIMIT 1);",
                Connection = _db
            };
            hasDocCommand.Parameters.AddWithValue("@id", model.Id);
            watch.Stop();

            bool hasDoc;

            try
            {
                var reader = hasDocCommand.ExecuteReader();
                reader.Read();
                hasDoc = reader.GetBoolean(0);
                reader.Close();
            }
            catch (SqliteException e)
            {
                Debug.WriteLine($"LocalSqliteEndpoint.cs, HasDocument @ Time Elapsed = {watch.ElapsedMilliseconds}");
                error?.Invoke(e);
                return;
            }

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
                    CommandText = @"SELECT `field` from `Fields` WHERE `id`=@id;",
                    Connection = _db
                };
                getDocCommand.Parameters.AddWithValue("@id", doc.Id);
                watch.Stop();

                try
                {
                    fieldModels = GetFieldModels(getDocCommand.ExecuteReader());
                }
                catch (SqliteException e)
                {
                    Debug.WriteLine($"LocalSqliteEndpoint.cs, CheckAllDocuments @ Time Elapsed = {watch.ElapsedMilliseconds}");
                    throw;
                }

                if (!fieldModels.Any())
                {
                    //Debug.Assert(false);
                    return false;
                }

                foreach (var fieldModel in fieldModels)
                {
                    if (!doc.Equals(fieldModel))
                    {
                        //Debug.Assert(false);
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion

        #region CONVENIENCE AND HELPER METHODS

        public async Task Close() { _db.Close(); }

        public Dictionary<string, string> GetBackups() { return new Dictionary<string, string>(); }

        private bool SafeExecuteMutateQuery(IDbCommand command, Action<Exception> error, string source, long elapsedTime)
        {
            //Try to perform the update. Catch any resulting SQL errors
            try
            {
                command.ExecuteNonQuery();
            }
            catch (SqliteException e)
            {
                Debug.WriteLine("SQL ERROR: LocalSqliteEndpoint.cs, " + source + $" @ Time Elapsed = {elapsedTime}");
                error?.Invoke(e);
                return false;
            }

            return true;
        }

        private IEnumerable<FieldModel> SafeExecuteAccessQuery(IDbCommand command, Action<Exception> error, string source, long elapsedTime)
        {
            //Try to perform the access/reading. Catch any resulting SQL errors
            try
            {
                return GetFieldModels(command.ExecuteReader());
            }
            catch (SqliteException e)
            {
                Debug.WriteLine("SQL ERROR: LocalSqliteEndpoint.cs, " + source + $" @ Time Elapsed = {elapsedTime}");
                error?.Invoke(e);
                return null;
            }
        }

        private static List<FieldModel> GetFieldModels(IDataReader reader)
        {
            var fieldModels = new List<FieldModel>();
            while (reader.Read())
            {
                var fm = reader.GetString(0).CreateObject<FieldModel>();
                fieldModels.Add(fm);
            }
            reader.Close();

            return fieldModels;
        }

        #endregion
    }

}
