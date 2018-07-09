// ReSharper disable once CheckNamespace
namespace Dash
{
    public class SearchResult
    {
        public DocumentController Document;
        public int Rank;
        public string RelevantText;

        public SearchResult() : this(null, "", 0)
        {
        }

        public SearchResult(string relevantText, DocumentController document)
        {
            RelevantText = relevantText;
            Document = document;
            ComputeAndSetRank();
        }

        public SearchResult(DocumentController document, string relevantText, int rank)
        {
            RelevantText = relevantText;
            Rank = rank;
            Document = document;
        }

        private void ComputeAndSetRank()
        {
            //TODO: determine frequency of text in all fields of document controller
            Rank = 0;
        }
    }
}