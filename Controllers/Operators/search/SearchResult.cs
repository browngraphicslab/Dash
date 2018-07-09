// ReSharper disable once CheckNamespace
namespace Dash
{
    public class SearchResult
    {
        public DocumentController Document;
        public int Rank;
        public string RelevantText;

        public SearchResult() : this("", 0, null)
        {
        }

        public SearchResult(string relevantText, DocumentController document)
        {
            RelevantText = relevantText;
            Document = document;
            ComputeAndSetRank();
        }

        public SearchResult(string relevantText, int rank, DocumentController document)
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