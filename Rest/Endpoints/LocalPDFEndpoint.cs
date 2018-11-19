using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using DashShared;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using Microsoft.Data.Sqlite;

namespace Dash
{
    public class LocalPDFEndpoint
    {
        private readonly object _transactionMutex = new object();

        private const string FileName = "pdf.db";

        public SqliteConnection DataBaseConnection
        {
            get
            {
                // create a string to connect to the databse (this string can be parameterized using the builder)
                var connectionStringBuilder = new SqliteConnectionStringBuilder
                {
                    DataSource = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\" + FileName,
                };

                // instantiate the connection to the database and open it
                return new SqliteConnection(connectionStringBuilder.ConnectionString);
            }
        }

        public LocalPDFEndpoint()
        {
            //// create a string to connect to the databse (this string can be parameterized using the builder)
            //var connectionStringBuilder = new SqliteConnectionStringBuilder
            //{
            //    DataSource = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\" + FileName,
            //};

            //// instantiate the connection to the database and open it
            //_db = new SqliteConnection(connectionStringBuilder.ConnectionString);
            //_db.Open();
        }

        private bool SafeExecuteMutateQuery(IDbCommand command, string source)
        {
            //Try to perform the update. Catch any resulting SQL errors
            try
            {
                command.ExecuteNonQuery();
                command.Dispose();
            }
            catch (SqliteException ex)
            {
                Debug.WriteLine("SQL ERROR: LocalPDFEndpoint.cs, " + source);

                return false;
            }

            return true;
        }

        public void Close()
        {
            lock (_transactionMutex)
            {
                //_db.Close();
            }
        }

        public Task AddPdf(Uri pdf, List<int> pages, List<SelectableElement> selectableElements)
        {
            lock (_transactionMutex)
            {
                using (var db = DataBaseConnection)
                {
                    db.Open();
                    using (var transaction = db.BeginTransaction())
                    {
                        // set the database schema
                        var createFieldCommand = new SqliteCommand
                        {
                            //Creates a new database with the title "Fields" containing two TEXT attributes (columns) "id" and "field" with the respective default values of NOT NULL and ""
                            //The primary key, or the attribute for which each tuplet (entry, or row) must be different is set to "id". In English, it's the document id's that differentiate the entries. 
                            CommandText = @"
                            CREATE TABLE IF NOT EXISTS [" + pdf.Segments.Last() + @"] (
	                            `id`	INTEGER PRIMARY KEY AUTOINCREMENT,
                                `page`  INTEGER,
                                `index` INTEGER,
                                `data`  TEXT,
                                `left`  REAL,
                                `right` REAL,
                                `top`   REAL,
                                `bottom`    REAL
                            );",
                            Connection = db,
                            Transaction = transaction
                        };
                        //Create database;
                        createFieldCommand.ExecuteNonQuery();
                        createFieldCommand.Dispose();
                        ;

                        var createIndexCommand = new SqliteCommand
                        {
                            CommandText = @"
                            CREATE INDEX IF NOT EXISTS `page` ON [" + pdf.Segments.Last() + @"] (`page`);",
                            Connection = db,
                            Transaction = transaction
                        };
                        createIndexCommand.ExecuteNonQuery();
                        createIndexCommand.Dispose();

                        for (int i = 0, j = 0; i < pages.Count; i++)
                        {
                            for (; j < pages[i]; j++)
                            {
                                if (!AddItem(selectableElements[j], pdf.Segments.Last(), i, db, transaction)
                                    .IsCompletedSuccessfully)
                                {
                                    throw new InvalidOperationException("Error adding selectable element");
                                }
                            }
                        }

                        transaction.Commit();
                    }
                }
            }

            return Task.CompletedTask;
        }

        private Task AddItem(SelectableElement element, object pdf, object page, SqliteConnection db, SqliteTransaction transaction)
        {
            var addDocCommand = new SqliteCommand
            {
                //i.e. "In the database "Fields", insert a new field (filled with the serialized text of the received document) at/with the document id specified
                //if something already exists at the given document ID, replace it
                CommandText = @"INSERT OR REPLACE INTO [" + pdf +
                                  "] (`page`, `index`, `data`, `left`, `right`, `top`, `bottom`) VALUES (@page, @index, @data, @left, @right, @top, @bottom);",
                Connection = db,
                Transaction = transaction
            };
            addDocCommand.Parameters.AddWithValue("@page", page);
            addDocCommand.Parameters.AddWithValue("@index", element.Index);
            addDocCommand.Parameters.AddWithValue("@data", element.Contents);
            addDocCommand.Parameters.AddWithValue("@left", element.Bounds.Left);
            addDocCommand.Parameters.AddWithValue("@right", element.Bounds.Right);
            addDocCommand.Parameters.AddWithValue("@top", element.Bounds.Top);
            addDocCommand.Parameters.AddWithValue("@bottom", element.Bounds.Bottom);

            if (!SafeExecuteMutateQuery(addDocCommand, "AddDocument"))
                throw new InvalidOperationException("Error adding selectable element");
            addDocCommand.Dispose();

            return Task.CompletedTask;
        }

        public Task<bool> ContainsPDF(Uri pdf)
        {
            lock (_transactionMutex)
            {
                using (var db = DataBaseConnection)
                {
                    db.Open();
                    var containsPdfCommand = new SqliteCommand
                    {
                        CommandText = @"SELECT * FROM sqlite_master WHERE type = 'table' AND name = '" +
                                      pdf.Segments.Last() + "';",
                        Connection = db,
                    };

                    bool hasPdf;
                    try
                    {
                        var reader = containsPdfCommand.ExecuteReader();
                        hasPdf = reader.Read();
                        containsPdfCommand.Dispose();
                        reader.Close();
                        reader.Dispose();
                    }
                    catch (SqliteException)
                    {
                        Debug.WriteLine("LocalPDFEndpoint.cs, ContainsPDF");
                        return Task.FromException<bool>(new InvalidOperationException());
                    }

                    return Task.FromResult(hasPdf);
                }
            }
        }

        public Task<(List<SelectableElement>, List<int>)> GetSelectableElements(Uri pdfUri, int page = -1)
        {
            lock (_transactionMutex)
            {
                using (var db = DataBaseConnection)
                {
                    db.Open();
                    var getDocCommand = new SqliteCommand
                    {
                        CommandText = @"SELECT * FROM '" + pdfUri.Segments.Last() + "';",
                        Connection = db,
                    };
                    getDocCommand.CommandText += page == -1 ? ";" : " WHERE `page`=@page;";
                    if (page != -1)
                    {
                        getDocCommand.Parameters.AddWithValue("@page", page);
                    }

                    var (selectableElements, pages) = SafeExecuteAccessQuery(getDocCommand, "GetDocument");

                    if (selectableElements == null)
                    {
                        return Task.FromException<(List<SelectableElement>, List<int>)>(
                            new InvalidOperationException());
                    }

                    getDocCommand.Dispose();

                    return Task.FromResult((selectableElements, pages));
                }
            }
        }

        private (List<SelectableElement>, List<int>) SafeExecuteAccessQuery(IDbCommand command, string source)
        {
            //Try to perform the access/reading. Catch any resulting SQL errors
            try
            {
                using (command)
                {
                    return GetSelectables(command.ExecuteReader());
                }
            }
            catch (SqliteException)
            {
                Debug.WriteLine("SQL ERROR: LocalPDFEndpoint.cs, " + source);

                return (null, null);
            }
        }

        private static (List<SelectableElement>, List<int>) GetSelectables(IDataReader reader)
        {
            var selectables = new List<SelectableElement>();
            var pages = new List<int>();
            int page = 0;
            while (reader.Read())
            {
                if (reader.GetInt32(1) != page)
                {
                    page++;
                    pages.Add(selectables.Last().Index);
                }
                var index = reader.GetInt32(2);
                var data = reader.GetString(3);
                var left = reader.GetDouble(4);
                var right = reader.GetDouble(5);
                var top = reader.GetDouble(6);
                var bottom = reader.GetDouble(7);

                selectables.Add(new SelectableElement(index, data, new Rect(left, top, right - left, bottom - top), "Times New Roman", 14));
            }
            reader.Close();
            reader.Dispose();

            return (selectables, pages);
        }
    }
}
