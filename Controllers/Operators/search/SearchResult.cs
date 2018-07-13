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
        public string TitleAppendix;

        public SearchResult() : this(null, "", "", 0) { }

        public SearchResult(FieldControllerBase doc)
        {
            ViewDocument = doc as DocumentController;
            DataDocument = doc as DocumentController;
            TitleAppendix = "";
            RelevantText = "";
            Rank = 1;
        }

        public SearchResult(DocumentNode node, string titleAppendix, string relevantText, int rank = 1)
        {
            Node = node;
            TitleAppendix = titleAppendix;
            RelevantText = relevantText;
            Rank = rank;

            ViewDocument = node.ViewDocument;
            DataDocument = node.DataDocument;
        }
    }
}