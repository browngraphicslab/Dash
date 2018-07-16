namespace Dash
{
    public class SearchResultViewModel
    {
        public string Title { get; private set; }
        public string ContextualText { get; set; }
        public DocumentController ViewDocument { get; }
        public DocumentController DocumentCollection { get; set; }
        public bool IsLikelyUsefulContextText { get; }

        public SearchResultViewModel(string title, string contextualText, DocumentController viewDoc, DocumentController documentCollectionController, bool isLikelyUsefulContextText = false)
        {
            ContextualText = contextualText;
            Title = title;
            ViewDocument = viewDoc;
            DocumentCollection = documentCollectionController;
            IsLikelyUsefulContextText = isLikelyUsefulContextText;
        }
    }
}
