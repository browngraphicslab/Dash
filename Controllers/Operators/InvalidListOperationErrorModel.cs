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

        public override string GetHelpfulString()
        {
            var feedback = "";
            switch (_operation)
            {
                case OpError.AppendType:
                    feedback = $"Cannot add an element of type {_typeA} to a list of type {_typeB}.";
                    break;
                case OpError.ConcatType:
                    feedback = $"Cannot add the contents of a list of type {_typeB} to a list of type {_typeA}.";
                    break;
                case OpError.ZipType:
                    feedback = $"Cannot zip lists with types {_typeA} and {_typeB}, respectively.";
                    break;
                case OpError.ZipLength:
                    feedback = $"Cannot zip lists with lengths {_lengthA} and {_lengthB}, respectively.";
                    break;
            }
            return $" Exception:\n            InvalidListOperation: {_operation}\n      Feedback:\n            {feedback}";
        }

        public override DocumentController GetErrorDoc() => new DocumentController();
    }
}