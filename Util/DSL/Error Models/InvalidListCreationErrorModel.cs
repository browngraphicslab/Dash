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

        public override DocumentController BuildErrorDoc()
        {
            var errorDoc = new DocumentController();

            string title = "Invalid List Creation";

            errorDoc.DocumentType = DashConstants.TypeStore.ErrorType;
            errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            errorDoc.SetField<TextController>(KeyStore.ExceptionKey, GetHelpfulString(), true);
            errorDoc.SetField<TextController>(KeyStore.ReceivedKey, Received(), true);
            //errorDoc.SetField(KeyStore.ExpectedKey, Expected(), true);
            //errorDoc.SetField<TextController>(KeyStore.FeedbackKey, Feedback(), true);

            return errorDoc;
        }

        private string Received()
        {
            string receivedTypes = string.Join(", ", _typeInfo);
            string receivedExpr = receivedTypes == "" ? "None" : receivedTypes;
            return $"({receivedExpr})";
        }
    }
}