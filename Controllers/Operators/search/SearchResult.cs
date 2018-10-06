using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class SearchResult
    {
        public DocumentNode Node;
        public DocumentController ViewDocument;
        public DocumentController DataDocument;
        public int Rank;
        public List<string> RelevantText;
        public List<string> FormattedKeyRef;
        public List<Search.SearchTerm> RtfHighlight { get; set; }

        //public SearchResult(FieldControllerBase doc)
        //{
        //    ViewDocument = doc as DocumentController;
        //    DataDocument = doc as DocumentController;
        //    TitleAppendix = "";
        //    RelevantText = "";
        //    Rank = 1;
        //}

        public SearchResult(DocumentController doc, List<string> formattedKeyRef, List<string> relevantText,
            int rank = 1)
        {
            FormattedKeyRef = formattedKeyRef;
            RelevantText = relevantText;
            Rank = rank;

            ViewDocument = doc;
            DataDocument = doc.GetDataDocument();
            RtfHighlight = new List<Search.SearchTerm>();

        }

        public SearchResult(DocumentNode node, List<string> formattedKeyRef, List<string> relevantText, int rank = 1)
        {
            Node = node;
            FormattedKeyRef = formattedKeyRef;
            RelevantText = relevantText;
            Rank = rank;

            ViewDocument = node.ViewDocument;
            DataDocument = node.DataDocument;
            RtfHighlight = new List<Search.SearchTerm>();
        }

        public SearchResult AddRtfTerm(Search.SearchTerm term)
        {
            RtfHighlight.Add(term);
            return this;
        }
    }
}
