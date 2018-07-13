using System;
using System.Collections.Generic;
using System.Linq;
using DashShared;

namespace Dash
{
    public class OverloadErrorModel : ScriptExecutionErrorModel
    {
        private readonly bool _ambiguous;
        private readonly string _functionName;
        private readonly List<TypeInfo> _givenParamTypes;
        private readonly List<string> _candidateParamTypes;
        private readonly List<int> _validParamCounts;

        private DocumentController _errorDoc;

        public OverloadErrorModel(bool ambiguous, string functionName, List<TypeInfo> givenParamTypes, List<string> candidateParamTypes, List<int> validParamCounts)
        {
            _ambiguous = ambiguous;
            _functionName = functionName;
            _givenParamTypes = givenParamTypes;
            _candidateParamTypes = candidateParamTypes;
            _validParamCounts = validParamCounts;
            _validParamCounts.Sort();
        }

        public override string GetHelpfulString() => "";

        public override DocumentController BuildErrorDoc()
        {
            _errorDoc = new DocumentController();

            string title = _ambiguous ? "AmbiguousOverloadException" : "InvalidOverloadException";

            _errorDoc.DocumentType = DashConstants.TypeStore.ErrorType;
            _errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            _errorDoc.SetField<TextController>(KeyStore.ExceptionKey, Exception(), true);
            _errorDoc.SetField<TextController>(KeyStore.ReceivedKey, Received(), true);
            _errorDoc.SetField(KeyStore.ExpectedKey, Expected(), true);
            _errorDoc.SetField<TextController>(KeyStore.FeedbackKey, Feedback(), true);

            return _errorDoc;
        }

        private string Exception()
        {
            string exception = _ambiguous
                ? $"Ambiguous call to function {_functionName}(). Multiple valid overloads exist."
                : $"No valid overloads exist for function {_functionName}().";
            return exception;
        }

        private string Received()
        {
            string receivedTypes = string.Join(", ", _givenParamTypes);
            string receivedExpr = receivedTypes == "" ? "None" : receivedTypes;
            return $"({receivedExpr})";
        }

        private ListController<TextController> Expected()
        {
            var expected = new ListController<TextController>();
            bool validNumParams = _validParamCounts.Contains(_givenParamTypes.Count);
            string paramExpr = _givenParamTypes.Count > 0 ? _givenParamTypes.Count == 1 ? "supports 1 parameter" : $"supports { _givenParamTypes.Count} parameters" : "is parameterless";
            if (!_ambiguous && !validNumParams) expected.Add(new TextController($"No implementation of {_functionName}() {paramExpr}. Instead, try..."));
            expected.AddRange(_candidateParamTypes.Select(p => new TextController(p)));
            return expected;
        }

        private string Feedback()
        {
            string fullFunction = $"{_functionName}(<{_givenParamTypes.Count}>)";
            string ambiguousFeedback = $"Consider changing input types or removing \n conflicting implementations of {fullFunction}";

            string paramCountAsString = $"{string.Join(", ", _validParamCounts)}";
            if (_validParamCounts.Count > 1)
            {
                char last = paramCountAsString[paramCountAsString.Length - 1];
                paramCountAsString = paramCountAsString.TrimEnd().Substring(0, paramCountAsString.Length - 3) + " or " + last;
            }

            bool validNumParams = _validParamCounts.Contains(_givenParamTypes.Count);
            string suffix = _validParamCounts.Count > 1 ? "s" : _validParamCounts[0] == 1 ? "" : "s";
            string invalidFeedback = validNumParams ? $"Inputs for {fullFunction} must satisfy one of the expected configurations listed above" : $"{_functionName}() must recieve {paramCountAsString} input{suffix} of proper type";

            string feedback = _ambiguous ? ambiguousFeedback : invalidFeedback;
            return feedback;
        }
    }
}