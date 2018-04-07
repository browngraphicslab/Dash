using System;
using System.Collections.Generic;
using System.Data.Common;
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
            var addDocCommand = new SqliteCommand
            {
                CommandText = @"INSERT INTO `Fields` VALUES (@id, @field);",
                Connection = _db,
            };
            addDocCommand.Parameters.AddWithValue("@id", newDocument.Id);
            addDocCommand.Parameters.AddWithValue("@field", JsonConvert.SerializeObject(newDocument));

            try
            {
                addDocCommand.ExecuteNonQuery();
                success?.Invoke(newDocument);
            }
            catch (SqliteException e)
            {
                error?.Invoke(e);
            }
        }

        public void UpdateDocument(FieldModel documentToUpdate, Action<FieldModel> success, Action<Exception> error)
        {
            var updateDocCommand =
                new SqliteCommand
                {
                    CommandText = @"UPDATE `Fields` SET `field`=@field WHERE `id`=@id;",
                    Connection = _db
                };

            updateDocCommand.Parameters.AddWithValue("@id", documentToUpdate.Id);
            updateDocCommand.Parameters.AddWithValue("@field", JsonConvert.SerializeObject(documentToUpdate));

            try
            {
                updateDocCommand.ExecuteNonQuery();
                success?.Invoke(documentToUpdate);
            }
            catch (SqliteException e)
            {
                error?.Invoke(e);
            }
        }

        public async Task GetDocument(string id, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public async Task GetDocuments(IEnumerable<string> ids, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public async Task GetDocuments<V>(IEnumerable<string> ids, Func<IEnumerable<V>, Task> success, Action<Exception> error) where V : EntityBase
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

        public async Task GetDocumentsByQuery(IQuery<FieldModel> query, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            var getAllDocsCommand = new SqliteCommand
            {
                CommandText = @"SELECT field from Fields",
                Connection = _db
            };

            var fieldModels = new List<FieldModel>();
            try
            {
                var reader = getAllDocsCommand.ExecuteReader();
                while (reader.Read())
                {
                    fieldModels.Add(reader.GetString(1).CreateObject<FieldModel>());
                }

                fieldModels = fieldModels.Where(query.Func).ToList();
            }
            catch (SqliteException e)
            {
                error?.Invoke(e);
            }

            //success?.Invoke(fieldModels);
        }

        public async Task GetDocumentsByQuery<V>(IQuery<FieldModel> query, Func<IEnumerable<V>, Task> success, Action<Exception> error) where V : EntityBase
        {
            var getAllDocsCommand = new SqliteCommand
            {
                CommandText = @"SELECT field from Fields",
                Connection = _db
            };

            var fieldModels = new List<FieldModel>();
            try
            {
                var reader = getAllDocsCommand.ExecuteReader();
                while (reader.Read())
                {
                    fieldModels.Add(reader.GetString(1).CreateObject<FieldModel>());
                }

                fieldModels = fieldModels.Where(query.Func).ToList();
            }
            catch (SqliteException e)
            {
                error?.Invoke(e);
            }

            success?.Invoke(fieldModels.OfType<V>());
        }

        public async Task Close()
        {
            _db.Close();
        }
    }
}
