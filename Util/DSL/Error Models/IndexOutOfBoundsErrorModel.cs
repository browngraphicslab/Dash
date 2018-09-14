// ReSharper disable once CheckNamespace

using DashShared;

namespace Dash
{
    public class IndexOutOfBoundsErrorModel : ScriptExecutionErrorModel
    {
        private DocumentController _errorDoc;
        private readonly int _index;
        private readonly int _listCount;
        private readonly int _range;

        public IndexOutOfBoundsErrorModel(int index, int listCount, int range = 1)
        {
            _index = index;
            _listCount = listCount;
            _range = range;
        }

        public override string GetHelpfulString() => "IndexOutOfBoundsException";

        public override DocumentController BuildErrorDoc()
        {
            _errorDoc = new DocumentController();

            const string title = "IndexOutOfBoundsException";

            _errorDoc.DocumentType = DashConstants.TypeStore.ErrorType;
            _errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            _errorDoc.SetField<TextController>(KeyStore.ExceptionKey, Exception(), true);
            _errorDoc.SetField<TextController>(KeyStore.FeedbackKey, Feedback(), true);

            return _errorDoc;
        }

        private string Exception()
        {
            string access = _range == 1 ? $"index {_index}" : _index + _range - 1 == _listCount ? $"index {_listCount}" : $"indices {_listCount} through {_index + _range - 1}";
            string valid = _listCount == 1 ? "Only valid index is [0]." : _listCount == 0 ? "List is empty and cannot be indexed." : $"Valid indices range from 0 to {_listCount - 1}.";
            return $"Cannot access {access}. {valid}";
        }

        private static string Feedback() => "Confirm that you are indexing the proper list, and that its length exceeds the index by at least one.";

    }
}