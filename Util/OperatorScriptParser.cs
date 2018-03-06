using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class OperatorScriptParser
    {
        private static List<KeyValuePair<char, char>> EncapsulatingCharacterPairs = new List<KeyValuePair<char, char>>()
        {
            new KeyValuePair<char, char>('\'','\''),
            new KeyValuePair<char, char>('"','"'),
            new KeyValuePair<char, char>('(', ')')
        };

        private static char FunctionOpeningCharacter = '(';

        private static OperatorScript os = OperatorScript.Instance;

        public static void TEST()
        {
            var parts1 = ParseToOuterFunctionParts(@"search(term:'trent')");
            Debug.Assert(parts1.Equals(new FunctionParts("search", new Dictionary<string, string>(){{"term","'trent'"}})));

            var parts2 = ParseToOuterFunctionParts(@"search(term:'trent', test:'tyler')");
            Debug.Assert(parts2.Equals(new FunctionParts("search", new Dictionary<string, string>() { { "term", "'trent'" }, { "test", "'tyler'" } })));

            var parts3 = ParseToOuterFunctionParts(" search (term:'trent', hello:', \" world')");
            Debug.Assert(parts3.Equals(new FunctionParts("search", new Dictionary<string, string>() { { "term", "'trent'" }, { "hello", "', \" world'" } })));

            TestString(@"'hello'","hello");
            TestString("'hello \" world,,,'", "hello \" world,,,");
            TestNumber("2", 2);
            TestNumber("add(A:20,B:25)", 45);
            TestNumber("add(A:20,B:add(A:25,B:30))", 75);
            TestNumber("add(A: add(B: 1, A: 2), B: add(A: 25, B: 30))", 58);
            TestNumber("mult (A: add(B: 1, A: 2), B: add(A: 25, B: 30))", 3 * (25 + 30));
            TestNumber("div (A: add(B: 20, A: 10), B: add(A: 1, B: 2))", 10);
            TestNumber("mult(A:5,B:div(A:1,B:2.5))",2);
            TestNumber("exec(Script:'mult(A:5,B:div(A:1,B:2.5))')", 2);
            TestNumber("exec(Script:'exec(Script:'mult(A:5,B:div(A:1,B:2.5))')')", 2);
            TestNumber("add(A:exec(Script:'exec(Script:'mult(A:5,B:div(A:1,B:2.5))')'),B:5)", 7);

            var testResults = Interpret("exec(Script:parseSearchString(Query:'cat dog'))");



            //Testing unnamed params
            TestNumber("add(20,B:25)", 45);
            TestNumber("add(20,25)", 45);
            TestNumber("add(exec('exec('mult(5,div(A:1,B:2.5))')'),5)", 7);
            TestNumber("add(exec('exec('mult(5,div(A:1,2.5))')'),5)", 7);
            TestNumber("add(exec('exec('mult(5,div(1,B:2.5))')'),5)", 7);

            var parts4 = ParseToOuterFunctionParts(@"search('trent')");
            Debug.Assert(parts4.Equals(new FunctionParts("search", new Dictionary<string, string>() { { "Term", "'trent'" } })));

        }

        private static void TestNumber(string script, double correctValue)
        {
            var number = Interpret(script);
            var num = (double)number.GetValue(null);
            Debug.Assert(num == correctValue);
        }

        private static void TestString(string script, string correctValue)
        {
            var number1 = Interpret(script);
            Debug.Assert(number1.GetValue(null).Equals(correctValue));
        }


        public static FieldControllerBase Interpret(string script)
        {
            try
            {
                var se = ParseToExpression(script);
                return se.Execute();
            }
            catch (ScriptException scriptException)
            {
                throw new InvalidDishScriptException(script, scriptException.Error, scriptException);
            }
        }

        private static ScriptExpression ParseToExpression(string script)
        {
            ScriptExpression toReturn;
            if (script.Length == 0)
            {
                throw new ScriptException(new EmptyScriptErrorModel());
            }
            switch (script[0])
            {
                case '\'':
                case '\"':
                    toReturn = ParseString(script);
                    break;
                default:
                    double number;
                    if (Double.TryParse(script, out number))//TODO optimize this
                    {
                        toReturn = ParseNumber(number);
                        break;
                    }

                    toReturn = ParseFunction(script);
                    break;
            }

            if (toReturn == null)
            {
                
            }

            return toReturn;
        }

        private static ScriptExpression ParseString(string s)
        {
            if (s.Length < 2)
            {
                throw new ScriptException(new InvalidStringScriptErrorModel(s)); 
            }
            if (s[0] == '\'' && s[s.Length - 1] == '\'' ||
                s[0] == '\"' && s[s.Length - 1] == '\"')
            {
                return new LiteralExpression(new TextController(s.Substring(1, s.Length - 2)));
            }
            //TODO Make sure there aren't multiple quotes
            return new LiteralExpression(new TextController(s));//TODO
        }

        private static ScriptExpression ParseNumber(double number)
        {
            return new LiteralExpression(new NumberController(number));
        }

        private static ScriptExpression ParseFunction(string func)
        {
            var parts = ParseToOuterFunctionParts(func);
            var parameters = new Dictionary<KeyController, ScriptExpression>();

            var keys = OperatorScript.GetKeyControllersForFunction(parts.FunctionName);
            var keyDict = OperatorScript.GetKeyControllerDictionaryForFunction(parts.FunctionName);

            if (keys == null)
            {
                throw new ScriptException(new FunctionNotFoundScriptErrorModel(parts.FunctionName));
            }

            foreach (var parameter in parts.FunctionParameters)
            {
                if (!keys.ContainsKey(parameter.Key))
                {
                    //handles the case where the function doesn't contain the specified key.
                    //Maybe generalize and look for very similarly named keys, and then Throw an exception if it still cant be found.
                    throw new ScriptException(new InvalidParameterScriptErrorModel(parameter.Key));
                }
                var keyController = keys[parameter.Key];
                parameters.Add(keyController, ParseToExpression(parameter.Value));
            }

            foreach (var kvp in keyDict)
            {
                if (kvp.Value.IsRequired && !parameters.ContainsKey(kvp.Key))
                {
                    throw new ScriptException(new MissingParameterScriptErrorModel(func, kvp.Key.Name));
                }
            }

            return new FunctionExpression(parts.FunctionName, parameters);
        }

        private static FunctionParts ParseToOuterFunctionParts(string script)
        {
            var parts = new FunctionParts();
            int parametersStartIndex = 0;
            foreach(char c in script)
            {
                parametersStartIndex++;
                if (c == FunctionOpeningCharacter)
                {
                    break;
                }
                parts.FunctionName += c;
            }
            parts.FunctionName = parts.FunctionName.Trim();

            int parametersEndIndex = 1;
            while (script.Length - parametersEndIndex - parametersStartIndex > 0 && script[script.Length - parametersEndIndex] != ')')
            {
                parametersEndIndex++;
            }

            if (script.Length < parametersEndIndex + parametersStartIndex)
            {
                throw new ScriptException(new FunctionCallMissingScriptErrorModel(script));
            }

            parts.FunctionParameters = ParseOutFunctionParameters(script.Substring(parametersStartIndex, script.Length - parametersEndIndex - parametersStartIndex), parts.FunctionName);

            return parts;
        }

        /// <summary>
        /// This function should take in only the stringified list of parameters.  
        /// For instance, if the original script was 'Search(term : "hello")'
        /// Then this function should only be PASSED the string 'term : "hello"'.
        /// </summary>
        /// <param name="innerFunctionParameters"></param>
        /// <returns></returns>
        private static Dictionary<string, string> ParseOutFunctionParameters(string innerFunctionParameters, string functionName)
        {
            var toReturn = new Dictionary<string, string>();

            var functionKeys = OperatorScript.GetOrderedKeyControllersForFunction(functionName);

            if (functionKeys == null)
            {
                throw new ScriptException(new FunctionNotFoundScriptErrorModel(functionName));
            }

            int parameterIndex = -1;

            bool inValue = false;
            char closingChar = ' ';
            int startIndex = 0;
            KeyValuePair<string, string> kvp;
            for (int i = 0; i < innerFunctionParameters.Length; ++i)
            {
                char c = innerFunctionParameters[i];
                if (inValue)
                {
                    if (c == closingChar)
                    {
                        inValue = false;
                    }
                    continue;
                }
                foreach (var encapsulatingCharacterPair in EncapsulatingCharacterPairs)
                {
                    if (c == encapsulatingCharacterPair.Key)
                    {
                        inValue = true;
                        closingChar = encapsulatingCharacterPair.Value;
                    }
                }
                if (c == ',')
                {
                    kvp = ParseKeyValue(innerFunctionParameters.Substring(startIndex, i - startIndex), functionKeys, ++parameterIndex, functionName);
                    if (toReturn.ContainsKey(kvp.Key))
                    {
                        throw new ScriptException(new ParameterProvidedMultipleTimesScriptErrorModel(functionName, kvp.Key));
                    }
                    toReturn[kvp.Key.Trim()] = kvp.Value.Trim();
                    startIndex = i + 1;
                }
            }
            kvp = ParseKeyValue(innerFunctionParameters.Substring(startIndex), functionKeys, ++parameterIndex, functionName);
            if (toReturn.ContainsKey(kvp.Key))
            {
                throw new ScriptException(new ParameterProvidedMultipleTimesScriptErrorModel(functionName, kvp.Key));
            }
            toReturn[kvp.Key.Trim()] = kvp.Value.Trim();

            return toReturn;
        }

        private static KeyValuePair<string, string> ParseKeyValue(string s, List<KeyController> functionKeys, int parameterIndex, string functionName)
        {
            int index = s.IndexOf(':');

            KeyValuePair<string, string> kvp;

            bool hasProvidedParamName = (index != -1);//TODO im not certain that this is foolproof 
            if (hasProvidedParamName)
            {
                var funcOpeningCharIndex = s.IndexOf(FunctionOpeningCharacter);
                if (funcOpeningCharIndex != -1 && funcOpeningCharIndex < index)
                {
                    hasProvidedParamName = false;
                }
            }

            if (hasProvidedParamName)
            {
                kvp = new KeyValuePair<string, string>(s.Substring(0, index).Trim(), s.Substring(index + 1).Trim());
            }
            else
            {
                if (parameterIndex >= functionKeys.Count)
                {
                    throw new ScriptException(new TooManyParametersGivenScriptErrorModel(functionName, s));
                }

                //This could also be bad if we aren't requireing either all named or all unnamed params
                kvp = new KeyValuePair<string, string>(functionKeys[parameterIndex].Name, s.Trim());
            }

            return kvp;
        }

        public class FunctionParts
        {
            public FunctionParts() { }

            public FunctionParts(string functionName, Dictionary<string, string> parameters)
            {
                FunctionName = functionName;
                FunctionParameters = parameters;
            }
            public string FunctionName { get; set; }
            public Dictionary<string, string> FunctionParameters { get; set; }

            public override bool Equals(object obj)
            {
                var parts = obj as FunctionParts;
                if (parts == null)
                {
                    return false;
                }
                return parts.FunctionName == FunctionName &&
                           FunctionParameters.All(i => parts.FunctionParameters.ContainsKey(i.Key) && parts.FunctionParameters[i.Key] == i.Value) &&
                           parts.FunctionParameters.Count == FunctionParameters.Count;
            }
        }


        private abstract class ScriptExpression
        {
            public abstract FieldControllerBase Execute();
        }

        private class LiteralExpression : ScriptExpression
        {
            private FieldControllerBase field;

            public LiteralExpression(FieldControllerBase field)
            {
                this.field = field;
            }

            public override FieldControllerBase Execute()
            {
                return field;
            }
        }

        private class FunctionExpression : ScriptExpression
        {
            private string opName;
            private Dictionary<KeyController, ScriptExpression> parameters;

            public FunctionExpression(string opName, Dictionary<KeyController, ScriptExpression> parameters)
            {
                this.opName = opName;
                this.parameters = parameters;
            }
            public override FieldControllerBase Execute()
            {
                var outputs = new Dictionary<KeyController, FieldControllerBase>();

                var inputs = new Dictionary<KeyController, FieldControllerBase>();
                foreach (var parameter in parameters)
                {
                    inputs.Add(parameter.Key, parameter.Value.Execute());
                }


                return OperatorScript.Run(opName, inputs);
            }
        }

        public abstract class ScriptErrorModel : EntityBase
        {
            public string ExtraInfo { get; set; }
        }

        public class ScriptException : Exception
        {
            public ScriptException(ScriptErrorModel error)
            {
                Error = error;
            }
            public ScriptErrorModel Error { get; }
        }

        public class InvalidDishScriptException : Exception
        {
            public InvalidDishScriptException(string script, ScriptErrorModel scriptErrorModel, ScriptException innerScriptException =  null)
            {
                Script = script;
                ScriptErrorModel = scriptErrorModel;
                InnerScriptException = innerScriptException;
            }

            public string Script { get; private set; }
            public ScriptException InnerScriptException { get; }
            public ScriptErrorModel ScriptErrorModel { get; private set; }
        }

        public class InvalidParameterScriptErrorModel : ScriptErrorModel
        {
            public InvalidParameterScriptErrorModel(string parameterName)
            {
                ParameterName = parameterName;
            }
            public string ParameterName { get; }
        }

        public class ParameterProvidedMultipleTimesScriptErrorModel : ScriptErrorModel
        {
            public ParameterProvidedMultipleTimesScriptErrorModel(string functionName, string parameterName)
            {
                ParameterName = parameterName;
                FunctionName = functionName;
            }
            public string ParameterName { get; }
            public string FunctionName { get; }
        }

        public class FunctionNotFoundScriptErrorModel : ScriptErrorModel
        {
            public FunctionNotFoundScriptErrorModel(string functionName)
            {
                FunctionName = functionName;
            }
            public string FunctionName { get; }
        }

        public class FunctionCallMissingScriptErrorModel : ScriptErrorModel
        {
            public FunctionCallMissingScriptErrorModel(string attemptedFunction)
            {
                AttemptedFunction = attemptedFunction;
            }
            public string AttemptedFunction { get; }
        }


        public class InvalidStringScriptErrorModel : ScriptErrorModel
        {
            public InvalidStringScriptErrorModel(string attemptedString)
            {
                AttemptedString = attemptedString;
            }
            public string AttemptedString { get; }
        }

        public class EmptyScriptErrorModel : ScriptErrorModel
        {
            public EmptyScriptErrorModel()
            {
                ExtraInfo = ExtraInfo ?? "";
                ExtraInfo += "The script was a blank space";
            }
        }

        public class TooManyParametersGivenScriptErrorModel : ScriptErrorModel
        {
            public TooManyParametersGivenScriptErrorModel(string functionName, string paramValue)
            {
                FunctionName = functionName;
                ParameterValue = paramValue;
            }
            public string FunctionName { get; }
            public string ParameterValue{ get; }
        }

        public class MissingParameterScriptErrorModel : ScriptErrorModel
        {
            public MissingParameterScriptErrorModel(string functionName, string missingParam)
            {
                FunctionName = functionName;
                MissingParameter = missingParam;
            }
            public string FunctionName { get; }
            public string MissingParameter { get; }
        }
    }
}
