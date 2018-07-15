// ReSharper disable once CheckNamespace
namespace Dash
{
    public class ImageCreationFailureErrorModel : ScriptExecutionErrorModel
    {
        private readonly string _url;

        public ImageCreationFailureErrorModel(string url) => _url = url;

        public override string GetHelpfulString()
        {
            return
                $" Exception:\n            ImageCreationFailure\n      Feedback:\n            <Unable to create an image using {_url}>." +
                "\n            The resource might be invalid or protected. Consider making your addition with the menu toolbar.";
        }

    }
}