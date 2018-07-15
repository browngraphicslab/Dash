// ReSharper disable once CheckNamespace

using DashShared;

namespace Dash
{
    internal class DuplicateVariableDeclarationErrorModel : ScriptExecutionErrorModel
    {
        private readonly string _variableName;
        private readonly FieldControllerBase _value;

        public DuplicateVariableDeclarationErrorModel(string variableName, FieldControllerBase value)
        {
            _variableName = variableName;
            _value = value;
        }

        public override string GetHelpfulString()
        {
            return $" Exception:\n            AttemptedDuplicateVariableDeclaration\n      Feedback:\n            Variable {_variableName} already exists with value {_value}. Update current assignment rather than re-declaring.";
        }

        public override DocumentController BuildErrorDoc()
        {
            var errorDoc = new DocumentController();

            string title = "Duplicate Variable Declaration Exception";

            errorDoc.DocumentType = DashConstants.TypeStore.ErrorType;
            errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            errorDoc.SetField<TextController>(KeyStore.ExceptionKey, GetHelpfulString(), true);
            errorDoc.SetField<TextController>(KeyStore.ReceivedKey, _variableName, true);
            //errorDoc.SetField(KeyStore.ExpectedKey, Expected(), true);
            //errorDoc.SetField<TextController>(KeyStore.FeedbackKey, Feedback(), true);

            return errorDoc;
        }

        //private string Received()
        //{
        //    string receivedTypes = string.Join(", ", _typeInfo);
        //    string receivedExpr = receivedTypes == "" ? "None" : receivedTypes;
        //    return $"({receivedExpr})";
        //}
    }
}