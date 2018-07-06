// ReSharper disable once CheckNamespace
namespace Dash
{
    public class IndexOutOfBoundsErrorModel : ScriptExecutionErrorModel
    {
        private readonly int _index;
        private readonly int _listCount;
        private readonly int _range;

        public IndexOutOfBoundsErrorModel(int index, int listCount, int range = 1)
        {
            _index = index;
            _listCount = listCount;
            _range = range;
        }

        public override string GetHelpfulString()
        {
            var access = _range == 1 ? $"index {_index}" : _index + _range - 1 == _listCount ? $"index {_listCount}" : $"indices {_listCount} through {_index + _range - 1}"; 
            var valid = _listCount == 1 ? "Only valid index is [0]." : $"Valid indices range from 0 to {_listCount - 1}.";
            return $" Exception:\n            IndexOutOfBounds\n      Feedback:\n            Cannot access {access}. {valid}";
        }
    }
}