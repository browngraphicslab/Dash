using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DashShared;

namespace Dash
{
    public class OperatorScriptParser
    {
        public static char FunctionOpeningCharacter = '(';
        public static char FunctionClosingCharacter = ')';

        public static char[] StringOpeningCharacters = {'{', '<'};
        public static char[] StringClosingCharacters = { '}', '>'};

        public static char ParameterDelimiterCharacter = ',';

        public static char FieldAccessorCharacter = '.';

        public static char[] AllowedVariableNameCharacters = new[]{'_'};

        public static List<KeyValuePair<char, char>> EncapsulatingCharacterPairsIgnoringInternals = new List<KeyValuePair<char, char>>();

        private static bool TEST_STRING_TO_REF = false;

        private static HashSet<string> _currentScriptExecutions = new HashSet<string>();

        static OperatorScriptParser()
        {
            for (int i = 0; i < StringOpeningCharacters.Length; ++i)
            {
                EncapsulatingCharacterPairsIgnoringInternals.Add(new KeyValuePair<char, char>(StringOpeningCharacters[i], StringClosingCharacters[i]));
            }
        }

        public static List<KeyValuePair<char, char>> EncapsulatingCharacterPairsTrackingInternals = new List<KeyValuePair<char, char>>()
        {
            new KeyValuePair<char, char>('[',']'),
            new KeyValuePair<char, char>(FunctionOpeningCharacter, FunctionClosingCharacter)
        };


        private static OperatorScript os = OperatorScript.Instance;

        public static void TEST()
        {
            for (int i = 0; i < StringOpeningCharacters.Length; i++)
            {
                char o = StringOpeningCharacters[i];
                char c = StringClosingCharacters[i];
                var parts1 = ParseToOuterFunctionParts($"search(term:{o}trent{c})");
                Debug.Assert(parts1.Equals(new FunctionParts("search", new Dictionary<string, string>() {{"term", $"{o}trent{c}" } })));

                var parts2 = ParseToOuterFunctionParts($"search(term:{o}trent{c}, test:{o}tyler{c})");
                Debug.Assert(parts2.Equals(new FunctionParts("search", new Dictionary<string, string>() {{"term", $"{o}trent{c}" }, {"test", $"{o}tyler{c}" }})));

                var parts3 = ParseToOuterFunctionParts($" search (term:{o}trent{c}, hello:{o}, {o} world{c})");
                Debug.Assert(parts3.Equals(new FunctionParts("search", new Dictionary<string, string>() {{"term", $"{o}trent{c}" }, {"hello", $"{o}, {o} world{c}" }})));

                TestString($"{o}hello{c}", "hello");
                TestString($"{o}hello {c} world,,,{c}", $"hello {c} world,,,");
                TestNumber("2", 2);
                TestNumber("add(A:20,B:25)", 45);
                TestNumber("add(A:20,B:add(A:25,B:30))", 75);
                TestNumber("add(A: add(B: 1, A: 2), B: add(A: 25, B: 30))", 58);
                TestNumber("mult (A: add(B: 1, A: 2), B: add(A: 25, B: 30))", 3 * (25 + 30));
                TestNumber("div (A: add(B: 20, A: 10), B: add(A: 1, B: 2))", 10);
                TestNumber("mult(A:5,B:div(A:1,B:2.5))", 2);
                TestNumber($"exec(Script:{o}mult(A:5,B:div(A:1,B:2.5)){c})", 2);
                TestNumber($"exec(Script:{o}exec(Script:{o}mult(A:5,B:div(A:1,B:2.5)){c}){c})", 2);
                TestNumber($"add(A:exec(Script:{o}exec(Script:{o}mult(A:5,B:div(A:1,B:2.5)){c}){c}),B:5)", 7);

                var testResults = Interpret($"exec(Script:parseSearchString(Query:{o}cat dog{c}))");//Shouldn't throw an error



                //Testing unnamed params
                TestNumber("add(20,B:25)", 45);
                TestNumber("add(20,25)", 45);
                TestNumber($"add(exec({o}exec({o}mult(5,div(A:1,B:2.5)){c}){c}),5)", 7);
                TestNumber($"add(exec({o}exec({o}mult(5,div(A:1,2.5)){c}){c}),5)", 7);
                TestNumber($"add(exec({o}exec({o}mult(5,div(1,B:2.5)){c}){c}),5)", 7);

                TestNumber($"add(exec({o}div(add(5,9),4){c}),4)", 7.5f); //TODO 

                var parts4 = ParseToOuterFunctionParts($"search({o}trent{c})");
                Debug.Assert(parts4.Equals(new FunctionParts("search", new Dictionary<string, string>() {{"Term", $"{o}trent{c}" } })));


                //Testing parse to strign wtihout string notation

                TestString($"{o}hello{c}", "hello");
                TestString($"{o}[\"'({c}", "[\"'(");
                TestString($"{o}[\"{{'({c}", "[\"{'(");
                TestString(o + "{{" + c, "{{");

                var parts5 = ParseToOuterFunctionParts($"search({o}trent{c})");
                Debug.Assert(parts5.Equals(new FunctionParts("search", new Dictionary<string, string>() { { "Term", $"{o}trent{c}" } })));

                var testResults2 = Interpret($"search({o}hello{c})"); //Shouldn't throw an error

                TestNumber($"let(x, 6, add(x,7))", 13);
                TestNumber($"let(x, 6, add(x,let(x, 3, add(x,2))))", 11);
                TestNumber($"let(x, 6, add(let(x, 3, add(x,2)), x))", 11);
            }

        }

        private static void TestNumber(string script, double correctValue)
        {
            var number = Interpret(script);
            var num = (double)number.GetValue(null);
            Debug.Assert(num.Equals(correctValue));


            if (TEST_STRING_TO_REF)
            {
                var asRef = DSL.GetOperatorControllerForScript(script);
                var toString = DSL.GetScriptForOperatorTree(asRef);
                for (int i = 0; i < (new Random()).Next(10); i++)
                {
                    asRef = DSL.GetOperatorControllerForScript(toString);
                    toString = DSL.GetScriptForOperatorTree(asRef);
                }
                var number2 = (double) Interpret(toString).GetValue(null);
                Debug.Assert(number2.Equals(num));
            }
        }

        private static void TestString(string script, string correctValue)
        {
            var s = Interpret(script);
            Debug.Assert(s.GetValue(null).Equals(correctValue));

            if (TEST_STRING_TO_REF)
            {
                var asRef = DSL.GetOperatorControllerForScript(script);
                var toString = DSL.GetScriptForOperatorTree(asRef);
                for (int i = 0; i < (new Random()).Next(10); i++)
                {
                    asRef = DSL.GetOperatorControllerForScript(toString);
                    toString = DSL.GetScriptForOperatorTree(asRef);
                }
                var s2 = (string) (Interpret(toString).GetValue(null));
                Debug.Assert(s2.Equals(s.GetValue(null)));
            }
        }

        /// <summary>
        /// Method to call to execute a string as a Dish Script and return the FieldController return value.
        /// This method should throw exceptions if the string is not a valid script.
        /// If an InvalidDishScriptException is throw, the exception.ScriptErrorModel SHOULD be a helpful error message
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public static FieldControllerBase Interpret(string script, ScriptState state = null)
        {
            var hash = script;//DashShared.UtilShared.GetDeterministicGuid(script);

            if (_currentScriptExecutions.Contains(hash))
            {
                return new TextController(script);
            }

            _currentScriptExecutions.Add(hash);
            try
            {
                var se = ParseToExpression(script);
                var exec = se.Execute(state ?? new ScriptState());
                return exec;
            }
            catch (ScriptException scriptException)
            {
                throw new InvalidDishScriptException(script, scriptException.Error, scriptException);
            }
            finally
            {
                _currentScriptExecutions.Remove(hash);
            }
        }

        /// <summary>
        /// Method to call to get an operator controller that represents the script called
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public static FieldControllerBase GetOperatorControllerForScript(string script, ScriptState state = null)
        {
            try
            {
                var se = ParseToExpression(script);
                return se.CreateReference(state ?? new ScriptState());

            }
            catch (ScriptException scriptException)
            {
                throw new InvalidDishScriptException(script, scriptException.Error, scriptException);
            }
        }

        public static string GetScriptForOperatorTree(ReferenceController operatorReference, Context context = null)
        {
            var doc = operatorReference.GetDocumentController(context);
            var op = doc.GetDereferencedField<OperatorController>(KeyStore.OperatorKey, context);

            var funcName = op.GetDishName();
            var script = funcName + FunctionOpeningCharacter;
            var middle = new List<string>();
            foreach (var inputKey in OperatorScript.GetOrderedKeyControllersForFunction(funcName))
            {
                Debug.Assert(doc.GetField(inputKey) != null);
                middle.Add(inputKey.Name + ":" + DSL.GetScriptForOperatorTree(doc.GetField(inputKey)));
            }
            return script + string.Join(ParameterDelimiterCharacter, middle)+FunctionClosingCharacter;
        }

        /// <summary>
        /// Public method to call to COMPILE but not Execute a Dish script.  
        /// This will return the helpful error message of the invalid script, or NULL if the script compiled correctly.
        /// 
        /// This is slightly faster than actually executing a script so if you are repeatedly checking the validity of a Dish script without needing the return value, call this.
        /// 
        /// AS YOU SHOULD KNOW, JUST BECAUSE IT WILL COMPILE DOESN'T MEAN IT WILL RETURN A VALID VALUE WHEN EXECUTED.   
        /// For instance: add(5, 'hello world') will compile but obviously not return a valid value.
        /// </summary>
        /// <param name="script"></param>
        public static string GetScriptError(string script)
        {
            try
            {
                ParseToExpression(script);
                return null;
            }
            catch (ScriptException scriptException)
            {
                return scriptException.Error.GetHelpfulString();
            }
        }

        private static ScriptExpression ParseToExpression(string script)
        {
            script = script?.Trim(' ');
            ScriptExpression toReturn;
            if (script.Length == 0)
            {
                throw new ScriptException(new EmptyScriptErrorModel());
            }

            if (StringOpeningCharacters.Contains(script[0]))
            {
                toReturn = ParseString(script);
            }
            else
            {
                double number;
                if (Double.TryParse(script, out number)) //TODO optimize this
                {
                    toReturn = ParseNumber(number);
                }
                else
                {
                    toReturn = IsFunction(script) ? ParseFunction(script) : ParseToVariable(script);
                }
            }
            
            Debug.Assert(toReturn != null);

            return toReturn;
        }

        private static bool IsFunction(string script)
        {
            return char.IsLetter(script[0]) && script.Any(i => i.Equals(FunctionOpeningCharacter));
        }

        private static ScriptExpression ParseToVariable(string variableName)
        {
            var fieldAccessCharIndex = variableName.LastIndexOf(FieldAccessorCharacter);
            if (fieldAccessCharIndex >= 1) //this if statement checks for dot notation of fields
            {
                if (fieldAccessCharIndex.Equals(variableName.Length - 1))
                {
                    throw new ScriptException(new InvalidDotNotationScriptErrorModel(variableName));
                }
                if (variableName.Skip(fieldAccessCharIndex + 1).All(IsAllowedNameCharacter))//if all the chars after the '.' are chars or digits, 
                {
                    var newString = DSL.GetFuncName<GetFieldOperatorController>() +
                                    FunctionOpeningCharacter +
                                    variableName.Substring(0, fieldAccessCharIndex) +
                                    ParameterDelimiterCharacter +
                                    StringOpeningCharacters[0] +
                                    variableName.Substring(fieldAccessCharIndex + 1) +
                                    StringClosingCharacters[0] +
                                    FunctionClosingCharacter;
                    return ParseToExpression(newString);
                }
            }

            //TODO maybe require the variable name to be of certain format? (no spaces, no special chars, etc)
            return new VariableExpression(variableName);

        }
        private static bool IsAllowedNameCharacter(char c)
        {
            return char.IsLetterOrDigit(c) || AllowedVariableNameCharacters.Contains(c);
        }

        private static ScriptExpression ParseString(string s)
        {
            if (s.Length < 2)
            {
                throw new ScriptException(new InvalidStringScriptErrorModel(s)); 
            }
            for (int i = 0; i < StringOpeningCharacters.Length; i++)//this for loop finds string literals only
            {
                if (s[0] == StringOpeningCharacters[i] && s[s.Length - 1] == StringClosingCharacters[i])
                {
                    return new LiteralExpression(new TextController(s.Substring(1, s.Length - 2)));
                }
            }

            if (ContentController<FieldModel>.HasController(s)) //checks to see if this is defining a field ID
            {
                return new LiteralExpression(ContentController<FieldModel>.GetController(s) as FieldControllerBase);
            }

            //TODO Make sure there aren't multiple quotes
            return new LiteralExpression(new TextController(s)); //otherwise defaults to returnig as existing string
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
                if (parameters.ContainsKey(kvp.Key) && !kvp.Value.Type.HasFlag(parameters[kvp.Key].Type))
                {
                    //TODO Trent
                    //throw new ScriptException(new ...);
                }
            }

            if (parts.FunctionName.Equals(DSL.GetFuncName<LetOperatorController>()))
            {
                return new LetExpression(
                    (parameters[LetOperatorController.VariableNameKey] as VariableExpression).GetVariableName(), 
                    parameters[LetOperatorController.VariableValueKey], 
                    parameters[LetOperatorController.ContinuedExpressionKey]);
            }

            return new FunctionExpression(parts.FunctionName, parameters);
        }

        private static FunctionParts ParseToOuterFunctionParts(string script)
        {
            var fieldAccessCharIndex = script.LastIndexOf(FieldAccessorCharacter);
            if (fieldAccessCharIndex > 1 && script[fieldAccessCharIndex - 1] == FunctionClosingCharacter) //this if statement checks for dot notation of fields
            {
                if (fieldAccessCharIndex.Equals(script.Length - 1))
                {
                    throw new ScriptException(new InvalidDotNotationScriptErrorModel(script));
                }
                if (script.Skip(fieldAccessCharIndex + 1).All(IsAllowedNameCharacter))//if all the chars after the '.' are chars or digits, 
                {
                    return ParseToOuterFunctionParts(DSL.GetFuncName<GetFieldOperatorController>()+
                                                     FunctionOpeningCharacter+
                                                     script.Substring(0, fieldAccessCharIndex)+
                                                     ParameterDelimiterCharacter+
                                                     StringOpeningCharacters[0] +
                                                     script.Substring(fieldAccessCharIndex + 1) +
                                                     StringClosingCharacters[0] +
                                                     FunctionClosingCharacter);
                }
            }
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
            while (script.Length - parametersEndIndex - parametersStartIndex > 0 && script[script.Length - parametersEndIndex] != FunctionClosingCharacter)
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

            var allEncapsulatingCharacters = EncapsulatingCharacterPairsIgnoringInternals.Concat(EncapsulatingCharacterPairsTrackingInternals).ToArray();
            HashSet<char> ignoreValueClosingChars = new HashSet<char>(EncapsulatingCharacterPairsIgnoringInternals.Select(i => i.Value));

            int parameterIndex = -1;

            Stack<char> closingCharacters = new Stack<char>();
            int startIndex = 0;
            KeyValuePair<string, string> kvp;
            for (int i = 0; i < innerFunctionParameters.Length; ++i)
            {
                char c = innerFunctionParameters[i];
                var currentlyInString = closingCharacters.Any() && ignoreValueClosingChars.Contains(closingCharacters.Peek());
                if (!closingCharacters.Any() || !currentlyInString)
                {
                    foreach (var encapsulatingCharacterPair in allEncapsulatingCharacters)
                    {
                        if (c == encapsulatingCharacterPair.Key)
                        {
                            closingCharacters.Push(encapsulatingCharacterPair.Value);
                        }
                    }
                }

                if (currentlyInString)
                {
                    foreach (var encapsulatingCharacterPair in allEncapsulatingCharacters)
                    {
                        if (c == encapsulatingCharacterPair.Key && ignoreValueClosingChars.Contains(encapsulatingCharacterPair.Value))
                        {
                            closingCharacters.Push(encapsulatingCharacterPair.Value);
                        }
                    }
                }

                if (closingCharacters.Any())
                {
                    if (c == closingCharacters.Peek())
                    {
                        closingCharacters.Pop();
                    }
                    continue;
                }


                if (c == ParameterDelimiterCharacter)
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

            if (kvp.Key != null)
            {
                if (toReturn.ContainsKey(kvp.Key))
                {
                    throw new ScriptException(new ParameterProvidedMultipleTimesScriptErrorModel(functionName, kvp.Key));
                }
                toReturn[kvp.Key.Trim()] = kvp.Value.Trim();
            }

            return toReturn;
        }

        private static KeyValuePair<string, string> ParseKeyValue(string s, List<KeyController> functionKeys, int parameterIndex, string functionName)
        {
            s = s.Trim();
            if (string.IsNullOrWhiteSpace(s))
            {
                return new KeyValuePair<string, string>();
            }
            int index = s.IndexOf(':');

            KeyValuePair<string, string> kvp;

            bool hasProvidedParamName = (index != -1);
            var isStringLiteral = StringOpeningCharacters.Contains(s[0]) && StringClosingCharacters[StringOpeningCharacters.IndexOf(s[0])] == s.Last();
            hasProvidedParamName &= !isStringLiteral;
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
    }
}
