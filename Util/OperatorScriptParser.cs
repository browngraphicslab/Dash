using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var se = ParseToExpression(script);
            return se.Execute();
        }

        private static ScriptExpression ParseToExpression(string script)
        {
            switch (script[0])
            {
                case '\'':
                case '\"':
                    return ParseString(script);
                default:
                    double number;
                    if (Double.TryParse(script, out number))
                    {
                        return ParseNumber(number);
                    }

                    return ParseFunction(script);
            }
        }

        private static ScriptExpression ParseString(string s)
        {
            if (s.Length < 2)
            {
                return null;
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

            if (keys == null)
            {
                //TODO handle the case when the function cant be found.  Throw an exception
            }

            if (parameters.Count != keys.Count)
            {
                //TODO handle the case where in input parameter count doesn't match the given parameters count
            }

            foreach (var parameter in parts.FunctionParameters)
            {
                if (!keys.ContainsKey(parameter.Key))
                {
                    //TODO handle the case where the function doesn't contain the specified key.
                    //Maybe generalize and look for very similarly named keys, and then Throw an exception if it still cant be found.
                }
                var keyController = keys[parameter.Key];
                parameters.Add(keyController, ParseToExpression(parameter.Value));
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
                if (c == '(')
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

            parts.FunctionParameters = ParseOutFunctionParameters(script.Substring(parametersStartIndex, script.Length - parametersEndIndex - parametersStartIndex));

            return parts;
        }

        /// <summary>
        /// This function should take in only the stringified list of parameters.  
        /// For instance, if the original script was 'Search(term : "hello")'
        /// Then this function should only be PASSED the string 'term : "hello"'.
        /// </summary>
        /// <param name="innerFunctionParameters"></param>
        /// <returns></returns>
        private static Dictionary<string, string> ParseOutFunctionParameters(string innerFunctionParameters)
        {
            var toReturn = new Dictionary<string, string>();

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
                    kvp = ParseKeyValue(innerFunctionParameters.Substring(startIndex, i - startIndex));
                    if (toReturn.ContainsKey(kvp.Key))
                    {
                        //TODO ignore or throw exception
                    }
                    toReturn[kvp.Key.Trim()] = kvp.Value.Trim();
                    startIndex = i + 1;
                }
            }
            kvp = ParseKeyValue(innerFunctionParameters.Substring(startIndex));
            if (toReturn.ContainsKey(kvp.Key))
            {
                //TODO ignore or throw exception
            }
            toReturn[kvp.Key.Trim()] = kvp.Value.Trim();

            return toReturn;
        }

        private static KeyValuePair<string, string> ParseKeyValue(string s)
        {
            int index = s.IndexOf(':');
            if (index == -1)
            {
                //TODO throw exception
            }
            return new KeyValuePair<string, string>(s.Substring(0, index), s.Substring(index + 1));
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
                var inputs = parameters.ToDictionary(kv => kv.Key, kv => kv.Value.Execute());

                return OperatorScript.Run(opName, inputs);
            }
        }
    }
}
