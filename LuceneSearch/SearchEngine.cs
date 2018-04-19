using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Version = Lucene.Net.Util.Version;

namespace Dash
{
    public class SearchEngine : IDisposable
    {
        private Directory _directory;
        private Analyzer _analyzer;
        private IndexWriter _indexWriter;
        private IndexSearcher _indexSearcher;
        private bool _isBuilt; //TODO: temporary

        struct SearchResult
        {
            private string id;
            private float score;
            private string context;
        }
        public SearchEngine()
        {
            //Initialize the Directory and the IndexWriter (Read more: http://codeclimber.net.nz/archive/2009/08/31/lucenenet-the-main-concepts/)
            _isBuilt = false;
            _directory = FSDirectory.Open(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\LuceneIndex");
            _analyzer = new StandardAnalyzer(Version.LUCENE_30);
            _indexWriter = new IndexWriter(_directory, _analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
            _indexWriter.DeleteAll(); //Comment out for persistence
            AddDocuments(ContentController<FieldModel>.GetControllers<DocumentController>());
            _indexSearcher = new IndexSearcher(_directory);

        }

        public void AddDocuments(IEnumerable<DocumentController> documents)
        {
            foreach (var documentController in documents)
            {
                string documentId = "1"; //TODO: use actual document controller
                string documentText = "the mitochondria is the powerhouse of the cell";
                Document doc = new Document();
                doc.Add(new Field("id", documentId, Field.Store.YES, Field.Index.NO));
                doc.Add(new Field("postBody", documentText, Field.Store.YES, Field.Index.ANALYZED)); //TODO: double check that the Field.Index.ANALYZED is what we want.
                _indexWriter.AddDocument(doc);
            }
        }
        public void AddInitialDocumentsToWriter()
        {
            string documentId = "1"; //TODO: use actual document controller
            string documentText = "the mitochondria is the powerhouse of the cell";
            Document doc = new Document();
            doc.Add(new Field("id", documentId, Field.Store.YES, Field.Index.NO));
            doc.Add(new Field("postBody", documentText, Field.Store.YES, Field.Index.ANALYZED)); //TODO: double check that the Field.Index.ANALYZED is what we want.
            _indexWriter.AddDocument(doc);
            _isBuilt = true;
            OptimizeAndCloseWriter(true, true, true);
        }

        private void OptimizeAndCloseWriter(bool triggerMerge, bool flushDocStores, bool flushDeletes)
        {
            /*
             * And when you are done with adding all the documents you need, you might call the Optimize method “priming the index for the fastest available search”,
             * and later either Flush to commit all the updates to the Directory or, if you don’t need to add to the index any more, call the Close method to flush and
             * then close all the files in the Directory.
             */
            _indexWriter.Optimize();
            //Close the writer
            _indexWriter.Flush(triggerMerge, flushDocStores, flushDeletes);
            _indexWriter.Dispose();
        }

        public void Dispose()
        {
            _indexWriter.Dispose();
            _indexSearcher.Dispose();

        }

        public TopDocs Search(string searchString)
        {
            
            Debug.Assert(!string.IsNullOrEmpty(searchString));
            
            QueryParser parser = new QueryParser(Version.LUCENE_30, "postBody", _analyzer);
            Query query = parser.Parse(searchString);



            TopDocs topDocs = _indexSearcher.Search(query, 10); //TODO: investigate other options
            for (int i = 0; i < topDocs.ScoreDocs.Length; i++)
            {
                var topDoc = topDocs.ScoreDocs[i];
                int docId = topDoc.Doc;
                float score = topDoc.Score;
                Console.WriteLine("Result num {0}, score {1}", i + 1, score);
                //_directory.
                var doc = _indexSearcher.Doc(docId);
                Console.WriteLine("ID: {0}", _indexSearcher.Doc(docId));
                //Console.WriteLine("Text found: {0}" + Environment.NewLine, doc.Get("postBody"));

            }

            return topDocs;

        }
    }
}