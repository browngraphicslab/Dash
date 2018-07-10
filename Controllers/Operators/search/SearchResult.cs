// ReSharper disable once CheckNamespace
namespace Dash
{
    public class SearchResult
    {
        public DocumentNode Node;
        public DocumentController ViewDocument;
        public DocumentController DataDocument;
        public int Rank;
        public string RelevantText;

        public SearchResult() : this(null, "", 0) { }

        public SearchResult(DocumentNode node, string relevantText, int rank = 1)
        {
            Node = node;
            RelevantText = relevantText;
            Rank = rank;

            ViewDocument = node.ViewDocument;
            DataDocument = node.DataDocument;
        }
    }
}