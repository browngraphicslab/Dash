using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Microsoft.Data.Sqlite;
using Timer = System.Threading.Timer;

namespace Dash
{
    public class LocalSqliteEndpoint : CachedEndpoint
    {
        /// <summary>
        /// Connection to the sqlite database
        /// </summary>
        private SqliteConnection _db;
        private SqliteTransaction _currentTransaction;
        private readonly System.Timers.Timer _backupTimer, _saveTimer, _cleanupTimer;
        private int _numBackups = DashConstants.DefaultNumBackups;
        public bool NewChangesToBackup { get; set; }

        private readonly Mutex _transactionMutex = new Mutex();

        private const string FileName = "dash.db";

        #region DATABASE INITIALIZED IN CONSTRUCTOR

        public LocalSqliteEndpoint()
        {
            // create a string to connect to the databse (this string can be parameterized using the builder)
            var connectionStringBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\" + FileName,
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
            _currentTransaction = _db.BeginTransaction();

            _saveTimer = new System.Timers.Timer(DashConstants.MillisecondBetweenLocalSave);
            _saveTimer.Elapsed += Timer_Elapsed;
            _saveTimer.Start();
            //Application.Current.Suspending += (sender, args) => { _currentTransaction.Commit(); };
            //Application.Current.Resuming += (sender, o) => { _currentTransaction = _db.BeginTransaction(); };

            _backupTimer = new System.Timers.Timer(DashConstants.DefaultBackupInterval * 1000);
            _backupTimer.Elapsed += (sender, args) => { CopyAsBackup(); };
            _backupTimer.Start();

            //_cleanupTimer = new System.Timers.Timer(30 * 1000);
            //_cleanupTimer.Elapsed += (sender, args) => CleanupDocuments();
            //_cleanupTimer.Start();

            NewChangesToBackup = false;
        }

        private async void CleanupDocuments()
        {
            var fields = new HashSet<FieldModel>();
            await DeleteDocumentsExcept(fields);
        }

        public override void SetBackupInterval(int millis) { _backupTimer.Interval = millis; }

        public override void SetNumBackups(int numBackups) { }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _transactionMutex.WaitOne();
            _currentTransaction.Commit();
            _currentTransaction = _db.BeginTransaction();
            _transactionMutex.ReleaseMutex();
        }

        private void CopyAsBackup()
        {
            _numBackups = MainPage.Instance.GetSettingsView.NumBackups;
            var dbPath = ApplicationData.Current.LocalFolder.Path + "\\" + FileName;
            if (!NewChangesToBackup || !File.Exists(dbPath)) return;

            var backupPath = dbPath + ".bak";

            for (var i = _numBackups - 1; i >= 1; i--)
            {
                var source = backupPath + i;
                var destination = backupPath + (i + 1);
                if (File.Exists(source)) { File.Copy(source, destination, true); }
            }
            File.Copy(dbPath, backupPath + 1, true);
        }

        #endregion

        #region DATABASE MUTATORS


        public override Task AddDocument(FieldModel newDocument)
        {
            var watch = Stopwatch.StartNew();

            _transactionMutex.WaitOne();
            var addDocCommand = new SqliteCommand
            {
                //i.e. "In the database "Fields", insert a new field (filled with the serialized text of the received document) at/with the document id specified
                //if something already exists at the given document ID, replace it
                CommandText = @"INSERT OR REPLACE INTO `Fields` VALUES (@id, @field);",
                Connection = _db,
                Transaction = _currentTransaction
            };
            addDocCommand.Parameters.AddWithValue("@id", newDocument.Id);
            addDocCommand.Parameters.AddWithValue("@field", newDocument.Serialize());
            watch.Stop();

            if (!SafeExecuteMutateQuery(addDocCommand, "AddDocument", watch.ElapsedMilliseconds)) throw new InvalidOperationException("Error adding document");

            NewChangesToBackup = true;
            return Task.CompletedTask;
        }

        public override Task UpdateDocument(FieldModel documentToUpdate)
        {
            var watch = Stopwatch.StartNew();

            _transactionMutex.WaitOne();
            var updateDocCommand =
                new SqliteCommand
                {
                    //i.e. "Set the field attribute in the database to the field serialization (below) only for the tuplet with the document ID (also specified below)"
                    CommandText = @"UPDATE `Fields` SET `field`=@field WHERE `id`=@id;",
                    Connection = _db,
                    Transaction = _currentTransaction
                };
            updateDocCommand.Parameters.AddWithValue("@id", documentToUpdate.Id);
            updateDocCommand.Parameters.AddWithValue("@field", documentToUpdate.Serialize());
            watch.Stop();

            if (!SafeExecuteMutateQuery(updateDocCommand, "UpdateDocument", watch.ElapsedMilliseconds)) return Task.FromException(new InvalidOperationException());

            NewChangesToBackup = true;

            return Task.CompletedTask;
        }

        public override Task DeleteDocument(FieldModel documentToDelete)
        {
            var watch = Stopwatch.StartNew();

            _transactionMutex.WaitOne();
            var deleteDocCommand = new SqliteCommand
            {
                //i.e. "Delete the tuplet from "Fields" that has the specified document id"
                CommandText = @"DELETE FROM `Fields` WHERE `id`=@id;",
                Connection = _db,
                Transaction = _currentTransaction
            };
            deleteDocCommand.Parameters.AddWithValue("@id", documentToDelete.Id);
            watch.Stop();

            if (!SafeExecuteMutateQuery(deleteDocCommand, "DeleteDocument", watch.ElapsedMilliseconds)) return Task.FromException(new InvalidOperationException());

            NewChangesToBackup = true;
            return Task.CompletedTask;
        }

        public override Task DeleteDocuments(IEnumerable<FieldModel> documents)
        {
            var watch = Stopwatch.StartNew();

            var fieldModels = documents.ToList();
            var tempParams = new string[fieldModels.Count];

            for (var i = 0; i < fieldModels.Count; ++i) { tempParams[i] = "@param" + i; }

            _transactionMutex.WaitOne();
            var deleteDocsCommand = new SqliteCommand
            {
                //i.e. "In "Fields", return the field contents at the specified document ids"
                CommandText = @"DELETE FROM `Fields` WHERE `id` IN (" + string.Join(", ", tempParams) + ");",
                Connection = _db,
                Transaction = _currentTransaction
            };

            for (var i = 0; i < fieldModels.Count; ++i) { deleteDocsCommand.Parameters.AddWithValue(tempParams[i], fieldModels[i]); }

            if (!SafeExecuteMutateQuery(deleteDocsCommand, "DeleteDocument", watch.ElapsedMilliseconds)) return Task.FromException(new InvalidOperationException());
            return Task.CompletedTask;
        }

        public Task DeleteDocumentsExcept(IEnumerable<FieldModel> documents)
        {
            var watch = Stopwatch.StartNew();

            var ids = new List<string>();
            _transactionMutex.WaitOne();
            var selectAllDocs = new SqliteCommand
            {
                CommandText = @"SELECT `id` FROM `Fields`",
                Connection = _db,
                Transaction = _currentTransaction
            };
            var enumerable = documents.ToList();
            try
            {
                var reader = selectAllDocs.ExecuteReader();
                while (reader.Read())
                {
                    ids.Add(reader.GetString(0));
                }
                reader.Close();
                ids = ids.Except(enumerable.Select(fm => fm.Id)).ToList();
            }
            catch (SqliteException)
            {
                _transactionMutex.ReleaseMutex();
                return Task.FromException(new InvalidOperationException());
            }

            var deleteDoc = new SqliteCommand
            {
                CommandText = @"DELETE FROM `Fields` WHERE `id` = @param",
                Connection = _db,
                Transaction = _currentTransaction
            };
            var param = new SqliteParameter("@param", SqliteType.Text);
            deleteDoc.Parameters.Add(param);
            foreach (var id in ids)
            {
                param.Value = id;
                try
                {
                    deleteDoc.ExecuteNonQuery();
                }
                catch (SqliteException)
                {
                    _transactionMutex.ReleaseMutex();
                    return Task.FromException(new InvalidOperationException());
                }
            }

            _transactionMutex.ReleaseMutex();

            //var tempParams = new string[ids.Count];

            //for (var i = 0; i < ids.Count; ++i) { tempParams[i] = "@param" + i; }

            //_transactionMutex.WaitOne();
            //var deleteDocsCommand = new SqliteCommand
            //{
            //    //i.e. "In "Fields", return the field contents at the specified document ids"
            //    CommandText = @"DELETE FROM `Fields` WHERE `id` IN (" + string.Join(", ", tempParams) + ");",
            //    Connection = _db,
            //    Transaction = _currentTransaction
            //};

            //for (var i = 0; i < ids.Count; ++i) { deleteDocsCommand.Parameters.AddWithValue(tempParams[i], ids[i]); }

            //if (!SafeExecuteMutateQuery(deleteDocsCommand, error, "DeleteDocument", watch.ElapsedMilliseconds)) return;
            Debug.WriteLine($"Delete Documents Except took {watch.ElapsedMilliseconds} ms");
            return Task.CompletedTask;
        }

        public override Task DeleteAllDocuments()
        {
            var watch = Stopwatch.StartNew();

            _transactionMutex.WaitOne();
            var deleteAllCommand = new SqliteCommand
            {
                //i.e. "Delete all tuplets (rows) from the database. In essence, clear the database"
                CommandText = @"DELETE FROM `Fields`;",
                Connection = _db,
                Transaction = _currentTransaction
            };
            watch.Stop();

            if (!SafeExecuteMutateQuery(deleteAllCommand, "DeleteAllDocuments", watch.ElapsedMilliseconds)) { return Task.FromException(new InvalidOperationException()); }

            NewChangesToBackup = true;
            return Task.CompletedTask;
        }

        #endregion

        #region DATABASE ACCESSORS

        public override Task<FieldModel> GetDocument(string id)
        {
            var watch = Stopwatch.StartNew();

            _transactionMutex.WaitOne();
            var getDocCommand = new SqliteCommand
            {
                //i.e. "In "Fields", return the field contents at the specified document id"
                CommandText = @"SELECT `field` FROM `Fields` WHERE `id`=@id;",
                Connection = _db,
                Transaction = _currentTransaction
            };
            getDocCommand.Parameters.AddWithValue("@id", id);
            watch.Stop();

            var fieldModels = SafeExecuteAccessQuery(getDocCommand, "GetDocument", watch.ElapsedMilliseconds);
            if (fieldModels == null) return Task.FromException<FieldModel>(new InvalidOperationException());

            return Task.FromResult(fieldModels.FirstOrDefault());
        }

        public override Task<List<FieldModel>> GetDocuments(IEnumerable<string> ids)
        {
            var watch = Stopwatch.StartNew();

            var enumerable = ids as string[] ?? ids.ToArray();
            var tempParams = new string[enumerable.Length];

            for (var i = 0; i < enumerable.Length; ++i) { tempParams[i] = "@param" + i; }

            _transactionMutex.WaitOne();
            //IN (" + string.Join(',', temp) + "
            var getDocCommand = new SqliteCommand
            {
                //i.e. "In "Fields", return the field contents at the specified document ids"
                CommandText = @"SELECT `field` from `Fields` WHERE `id` in (" + string.Join(", ", tempParams) + ");",
                Connection = _db,
                Transaction = _currentTransaction
            };

            for (var i = 0; i < enumerable.Length; ++i) { getDocCommand.Parameters.AddWithValue(tempParams[i], enumerable[i]); }

            watch.Stop();

            var fieldModels = SafeExecuteAccessQuery(getDocCommand, "GetDocumentsssss", watch.ElapsedMilliseconds);
            if (fieldModels == null) return Task.FromException<List<FieldModel>>(new InvalidOperationException());

            return Task.FromResult(fieldModels.ToList());
        }

        public override async Task<List<V>> GetDocuments<V>(IEnumerable<string> ids)
        {
            return (await GetDocuments(ids)).OfType<V>().ToList();
        }

        public override Task<List<FieldModel>> GetDocumentsByQuery(IQuery<FieldModel> query)
        {
            var watch = Stopwatch.StartNew();

            _transactionMutex.WaitOne();
            var getAllDocsCommand = new SqliteCommand
            {
                CommandText = @"SELECT `field` FROM `Fields`;",
                Connection = _db,
                Transaction = _currentTransaction
            };
            watch.Stop();

            List<FieldModel> fieldModels;
            try
            {
                fieldModels = GetFieldModels(getAllDocsCommand.ExecuteReader());
                fieldModels = fieldModels.Where(query.Func).ToList();
            }
            catch (SqliteException)
            {
                Debug.WriteLine(
                    $"LocalSqliteEndpoint.cs, GetDocumentsByQuery @ Time Elapsed = {watch.ElapsedMilliseconds}");
                return Task.FromException<List<FieldModel>>(new InvalidOperationException());
            }
            finally
            {
                _transactionMutex.ReleaseMutex();
            }

            return Task.FromResult(fieldModels);
        }

        public override async Task<List<V>> GetDocumentsByQuery<V>(IQuery<FieldModel> query)
        {
            return (await GetDocumentsByQuery(query)).OfType<V>().ToList();
        }

        public override Task<bool> HasDocument(FieldModel model)
        {
            var watch = Stopwatch.StartNew();

            _transactionMutex.WaitOne();
            var hasDocCommand = new SqliteCommand
            {
                CommandText = @"SELECT EXISTS (SELECT `id` FROM `Fields` WHERE `id`=@id LIMIT 1);",
                Connection = _db,
                Transaction = _currentTransaction
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
            catch (SqliteException)
            {
                Debug.WriteLine($"LocalSqliteEndpoint.cs, HasDocument @ Time Elapsed = {watch.ElapsedMilliseconds}");
                return Task.FromException<bool>(new InvalidOperationException());
            }
            finally
            {
                _transactionMutex.ReleaseMutex();
            }

            return Task.FromResult(hasDoc);
        }

        public override bool CheckAllDocuments(IEnumerable<FieldModel> documents)
        {
            var watch = Stopwatch.StartNew();

            foreach (var doc in documents)
            {

                List<FieldModel> fieldModels;

                _transactionMutex.WaitOne();
                var getDocCommand = new SqliteCommand
                {
                    CommandText = @"SELECT `field` from `Fields` WHERE `id`=@id;",
                    Connection = _db,
                    Transaction = _currentTransaction
                };
                getDocCommand.Parameters.AddWithValue("@id", doc.Id);
                watch.Stop();

                try
                {
                    fieldModels = GetFieldModels(getDocCommand.ExecuteReader());
                }
                catch (SqliteException)
                {
                    Debug.WriteLine(
                        $"LocalSqliteEndpoint.cs, CheckAllDocuments @ Time Elapsed = {watch.ElapsedMilliseconds}");
                    throw;
                }
                finally
                {
                    _transactionMutex.ReleaseMutex();
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

        public override Task Close()
        {
            _saveTimer.Stop();
            _transactionMutex.WaitOne();
            CleanupDocuments();
            _currentTransaction?.Commit();
            _currentTransaction = null;
            _db.Close();
            _transactionMutex.ReleaseMutex();
            return Task.CompletedTask;
        }

        public override Dictionary<string, string> GetBackups() { return new Dictionary<string, string>(); }

        private bool SafeExecuteMutateQuery(IDbCommand command, string source, long elapsedTime)
        {
            //Try to perform the update. Catch any resulting SQL errors
            try
            {
                command.ExecuteNonQuery();
            }
            catch (SqliteException)
            {
                Debug.WriteLine("SQL ERROR: LocalSqliteEndpoint.cs, " + source + $" @ Time Elapsed = {elapsedTime}");

                return false;
            }
            finally
            {
                _transactionMutex.ReleaseMutex();
            }

            return true;
        }

        private IEnumerable<FieldModel> SafeExecuteAccessQuery(IDbCommand command, string source, long elapsedTime)
        {
            //Try to perform the access/reading. Catch any resulting SQL errors
            try
            {
                return GetFieldModels(command.ExecuteReader());
            }
            catch (SqliteException)
            {
                Debug.WriteLine("SQL ERROR: LocalSqliteEndpoint.cs, " + source + $" @ Time Elapsed = {elapsedTime}");

                return null;
            }
            finally
            {
                _transactionMutex.ReleaseMutex();
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
