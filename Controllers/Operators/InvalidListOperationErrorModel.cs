using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash { 

    public class InvalidListOperationErrorModel : ScriptExecutionErrorModel
    {
        public enum OpError
        {
            AppendType,
            ConcatType,
            ZipType,
            ZipLength,
        }

        private DocumentController _errorDoc;
        private readonly TypeInfo _typeA;
        private readonly TypeInfo? _typeB;
        private readonly OpError _operation;
        private readonly int _lengthA;
        private readonly int _lengthB;

        public InvalidListOperationErrorModel(TypeInfo typeA, TypeInfo? typeB, OpError operation, int lengthA = 0, int lengthB = 0)
        {
            _typeA = typeA;
            _typeB = typeB;
            _operation = operation;
            _lengthA = lengthA;
            _lengthB = lengthB;
        }

        public override string GetHelpfulString() => "InvalidListOperationException";

        public override DocumentController BuildErrorDoc()
        {
            _errorDoc = new DocumentController();

            string title = $"InvalidListOperationException : {_operation}";

            _errorDoc.DocumentType = DashConstants.TypeStore.ErrorType;
            _errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            _errorDoc.SetField<TextController>(KeyStore.ExceptionKey, Exception(), true);
            _errorDoc.SetField<TextController>(KeyStore.FeedbackKey, Feedback(), true);

            return _errorDoc;
        }

        private string Exception()
        {
            var exception = "";

            switch (_operation)
            {
                case OpError.AppendType:
                    exception = $"Cannot add an element of type {_typeA} to a list of type {_typeB}.";
                    break;
                case OpError.ConcatType:
                    exception = $"Cannot add the contents of a list of type {_typeB} to a list of type {_typeA}.";
                    break;
                case OpError.ZipType:
                    exception = $"Cannot zip lists with types {_typeA} and {_typeB}, respectively.";
                    break;
                case OpError.ZipLength:
                    exception = $"Cannot zip lists with lengths {_lengthA} and {_lengthB}, respectively.";
                    break;
            }

            return exception;
        }

        private static string Feedback() => "Ensure lists are of the same length, and that all elements and lists are of the appropriate type.";
    }
}