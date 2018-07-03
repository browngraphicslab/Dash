// ReSharper disable once CheckNamespace
namespace Dash
{
    public class IndexOutOfBoundsErrorModel : ScriptExecutionErrorModel
    {
        private readonly int _index;
        private readonly int _listCount;

        public IndexOutOfBoundsErrorModel(int index, int listCount)
        {
            _index = index;
            _listCount = listCount;
        }

        public override string GetHelpfulString()
        {
            var range = _listCount == 1 ? "Only valid index is [0]." : $"Valid indices range from 0 to {_listCount - 1}.";
            return $" Exception:\n            IndexOutOfBounds\n      Feedback:\n            Cannot access index {_index}. {range}";
        }
    }
}