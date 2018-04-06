namespace Dash
{
    public class SearchResultViewModel
    {

        public SearchResultViewModel(string title, string contextualText, string id, DocumentController viewDoc, DocumentController documentCollectionController, bool isLikelyUsefulContextText = false)
        {
            ContextualText = contextualText;
            Title = title;
            Id = id;
            ViewDocument = viewDoc;
            DocumentCollection = documentCollectionController;
            IsLikelyUsefulContextText = isLikelyUsefulContextText;
        }

        public string Title { get; private set; }
        public string Id { get; private set; }
        public string ContextualText { get; set; }
        public DocumentController ViewDocument { get; }
        public DocumentController DocumentCollection { get; }
        public bool IsLikelyUsefulContextText { get; }

    }
}
