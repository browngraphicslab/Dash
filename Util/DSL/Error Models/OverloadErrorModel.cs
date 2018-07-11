using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}