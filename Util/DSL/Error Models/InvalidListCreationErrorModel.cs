using DashShared;

namespace Dash
{
    public class InvalidListCreationErrorModel : ScriptExecutionErrorModel
    {
        private readonly TypeInfo _typeInfo;

        public InvalidListCreationErrorModel(TypeInfo typeInfo)
        {
            _typeInfo = typeInfo;
        }

        public override string GetHelpfulString() => $"Creating a list with a source of type {_typeInfo} currently not supported.";
    }
}