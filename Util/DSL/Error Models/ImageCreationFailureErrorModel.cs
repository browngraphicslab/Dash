// ReSharper disable once CheckNamespace

using DashShared;

namespace Dash
{
    public class ImageCreationFailureErrorModel : ScriptExecutionErrorModel
    {
        private DocumentController _errorDoc;
        private readonly string _url;

        public ImageCreationFailureErrorModel(string url) => _url = url;

        public override string GetHelpfulString() => "ImageCreationFailureException";

        public override DocumentController BuildErrorDoc()
        {
            _errorDoc = new DocumentController();

            const string title = "ImageCreationFailureException";

            _errorDoc.DocumentType = DashConstants.TypeStore.ErrorType;
            _errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            _errorDoc.SetField<TextController>(KeyStore.ExceptionKey, Exception(), true);
            _errorDoc.SetField<TextController>(KeyStore.FeedbackKey, Feedback(), true);

            return _errorDoc;
        }

        private string Exception() => $"Unable to create an image using {_url}.";

        private static string Feedback() => "The resource might be invalid or protected. Consider making your addition with the menu toolbar.";

    }
}