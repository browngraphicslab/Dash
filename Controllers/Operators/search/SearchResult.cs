using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class SearchResult
    {
        public DocumentNode Node;
        public DocumentController ViewDocument;
        public DocumentController DataDocument;
        public string Path;
        public int Rank;
        public List<string> RelevantText;
        public List<string> FormattedKeyRef;
        public List<Search.SearchTerm> RtfHighlight { get; set; }

        public SearchResult(DocumentController doc, List<string> formattedKeyRef, List<string> relevantText, string path,
            int rank = 1)
        {
            FormattedKeyRef = formattedKeyRef;
            RelevantText = relevantText;
            Rank = rank;
            Path = path;

            ViewDocument = doc;
            DataDocument = doc.GetDataDocument();
            RtfHighlight = new List<Search.SearchTerm>();

        }

        public SearchResult(DocumentNode node, List<string> formattedKeyRef, List<string> relevantText, string path, int rank = 1)
        {
            Node = node;
            FormattedKeyRef = formattedKeyRef.ToList();
            RelevantText = relevantText.ToList();
            Rank = rank;
            Path = path;

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
