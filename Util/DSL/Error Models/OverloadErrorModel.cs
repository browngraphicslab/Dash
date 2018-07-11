using System;
using System.Collections.Generic;
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

        public override string GetHelpfulString()
        {
            var fullFunction = $"{_functionName}(<{_givenParamTypes.Count}>)";
            var paramCountAsString = $"{string.Join(", ", _validParamCounts)}";
            if (_validParamCounts.Count > 1)
            {
                var last = paramCountAsString[paramCountAsString.Length - 1];
                paramCountAsString = paramCountAsString.TrimEnd().Substring(0, paramCountAsString.Length - 3) + " or " + last;
            }

            var validNumParams = _validParamCounts.Contains(_givenParamTypes.Count);

            var suffix = _validParamCounts.Count > 1 ? "s" : _validParamCounts[0] == 1 ? "" : "s"; 
            var invalidFeedback = validNumParams ? $"Inputs for {fullFunction} must satisfy one of the expected configurations listed above" : $"{_functionName}() must recieve {paramCountAsString} input{suffix} of proper type";
            var ambiguousFeedback = $"Consider changing input types or removing \n conflicting implementations of {fullFunction}";

            var paramExpr = _givenParamTypes.Count > 0 ? _givenParamTypes.Count == 1 ? "supports 1 parameter" : $"supports { _givenParamTypes.Count} parameters" : "is parameterless";
            var invalidParamNum = validNumParams ? "" : $"\n            No implementation of {_functionName}() {paramExpr}. Instead, try...";
            var receivedTypes = string.Join(", ", _givenParamTypes);
            var receivedExpr = receivedTypes == "" ? "None" : receivedTypes;
            var invalidTypeBreakdown = $"\n Received: \n ({receivedExpr})\n Expected:" + invalidParamNum;
            var ambiguousTypeBreakdown = $"\n Received: \n ({receivedExpr})\n Ambiguity:";
            foreach (var paramList in _candidateParamTypes)
            {
                invalidTypeBreakdown += paramList;
                ambiguousTypeBreakdown += paramList;
            }
            return _ambiguous ? $"Exception:\n Ambiguous call to function {_functionName}().\n Multiple valid overloads exist." + ambiguousTypeBreakdown + $"\n Feedback:\n {ambiguousFeedback}." : 
                $"Exception:\n No valid overloads exist for function {_functionName}()" + invalidTypeBreakdown + $"\n Feedback:\n {invalidFeedback}.";
        }

        public override DocumentController GetErrorDoc() => BuildErrorDoc();

        public DocumentController BuildErrorDoc()
        {
            string title = _ambiguous ? "AmbiguousOverloadException" : "InvalidOverloadException";
            _errorDoc = new DocumentController();
            _errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            _errorDoc.SetField<ListController<TextController>>(KeyStore.ExceptionKey, Exception(), true);
            _errorDoc.SetField<ListController<TextController>>(KeyStore.ReceivedKey, Received(), true);
            _errorDoc.SetField<ListController<TextController>>(KeyStore.ExpectedKey, Expected(), true);
            _errorDoc.SetField<ListController<TextController>>(KeyStore.FeedbackKey, Feedback(), true);
            return _errorDoc;
        }

        private ListController<TextController> Exception()
        {
            string exception = _ambiguous
                ? $"Ambiguous call to function {_functionName}(). Multiple valid overloads exist."
                : $"No valid overloads exist for function {_functionName}().";
            return new ListController<TextController>(new TextController(exception));
        }

        private ListController<TextController> Received()
        {
            string receivedTypes = string.Join(", ", _givenParamTypes);
            string receivedExpr = receivedTypes == "" ? "None" : receivedTypes;
            return new ListController<TextController>(new TextController($"({receivedExpr})"));
        }

        private ListController<TextController> Expected()
        {
            bool validNumParams = _validParamCounts.Contains(_givenParamTypes.Count);
            string paramExpr = _givenParamTypes.Count > 0 ? _givenParamTypes.Count == 1 ? "supports 1 parameter" : $"supports { _givenParamTypes.Count} parameters" : "is parameterless";
            string invalidParamNum = validNumParams ? "" : $"No implementation of {_functionName}() {paramExpr}. Instead, try...";
            if (!_ambiguous) _candidateParamTypes.Insert(0, invalidParamNum);
            return new ListController<TextController>(new ListModel(_candidateParamTypes, TypeInfo.Text));
        }

        private ListController<TextController> Feedback()
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
            return new ListController<TextController>(new TextController(feedback));
        }
    }
}