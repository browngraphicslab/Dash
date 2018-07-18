using System;
using DashShared;

namespace Dash
{

    public abstract class ScriptExecutionErrorModel : ScriptErrorModel
    {
        public Exception InnerException { get; set; }

        public DocumentController GetErrorDoc() => BuildErrorDoc();

        public virtual DocumentController BuildErrorDoc()
        {
            var errorDoc = new DocumentController();

            string title = DashConstants.TypeStore.ErrorType.ToString();

            errorDoc.DocumentType = DashConstants.TypeStore.ErrorType;
            errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            errorDoc.SetField<TextController>(KeyStore.ExceptionKey, GetHelpfulString(), true);
            //errorDoc.SetField<TextController>(KeyStore.ReceivedKey, _variableName, true);
            //errorDoc.SetField(KeyStore.ExpectedKey, Expected(), true);
            //errorDoc.SetField<TextController>(KeyStore.FeedbackKey, Feedback(), true);

            return errorDoc;
        }
    }

}
