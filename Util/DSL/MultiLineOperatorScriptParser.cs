using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zu.TypeScript;
using Zu.TypeScript.TsParser;
using Zu.TypeScript.TsTypes;

namespace Dash
{
    public class MultiLineOperatorScriptParser
    {
        private enum LineType
        {
            Let,
            Return
        }

        public static char LineDelimiterCharacter = ';';
        public static char VariableSettingCharacter = '=';
        public static void TEST()
        {
            //TestParse("a = 1; b = {test}; {hello}", "let(a,1,let(b,{test},{hello}))");

            TestNumber("a = 1; add(3,add(a, 6))", 10);
            TestNumber("a = 1; b = 9; add(3,add(a, b))", 13);
            TestNumber("a = 1; b = 9; add(3,add(a, 9))", 13);
            TestNumber("a = 1; b = 9; add(3,add(a, 9)); add(3,add(a, 9))", 13);

            TestNumber("a = add(1,9); " +
                       "b=mult(a,1.5); " +
                       "add(a,mult(b, 0))", 10);

            TestNumber("a = 7", 7);
            TestNumber("a = 7;", 7);

            TestNumber("a = 7; b = 9", 9);
            TestNumber("a = 7; b = 9;", 9);
            TestNumber("a = 7; b = 9;;;;;", 9);
        }


        private static void TestNumber(string script, double correctValue)
        {
            var number = Interpret(script);
            var num = (double)number.GetValue(null);
            Debug.Assert(num.Equals(correctValue));
        }

        private static void TestString(string script, string correctValue)
        {
            var s = Interpret(script);
            Debug.Assert(s.GetValue(null).Equals(correctValue));
        }

        private static void TestParse(string multiLine, string singleLine)
        {
            var s = ParseMultiLineToSingleLine(multiLine);
            Debug.Assert(s.Equals(singleLine));
        }

        public static string ParseMultiLineToSingleLine(string multiLineScript)
        {
            if (string.IsNullOrWhiteSpace(multiLineScript))
            {
                return multiLineScript;
            }


            multiLineScript = multiLineScript?.Trim();
            while (multiLineScript[multiLineScript.Length - 1] == LineDelimiterCharacter)
            {
                multiLineScript = multiLineScript.Remove(multiLineScript.Length - 1);
            }
            var allEncapsulatingCharacters = OperatorScriptParser.EncapsulatingCharacterPairsIgnoringInternals.Concat(OperatorScriptParser.EncapsulatingCharacterPairsTrackingInternals).ToArray();
            var ignoreValueClosingChars = new HashSet<char>(OperatorScriptParser.EncapsulatingCharacterPairsIgnoringInternals.Select(i => i.Value));

            var lines = new List<ScriptLine>();

            var closingCharacters = new Stack<char>();
            int startIndex = 0;
            for (int i = 0; i < multiLineScript.Length; ++i)
            {
                char c = multiLineScript[i];

                if (!closingCharacters.Any() || !ignoreValueClosingChars.Contains(closingCharacters.Peek()))
                {
                    foreach (var encapsulatingCharacterPair in allEncapsulatingCharacters)
                    {
                        if (c == encapsulatingCharacterPair.Key)
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


                if (c == LineDelimiterCharacter)
                {

                    lines.Add(ParseLine(multiLineScript.Substring(startIndex, i - startIndex)));
                    startIndex = i + 1;
                }
            }

            lines.Add(ParseLine(multiLineScript.Substring(startIndex)));

            return ConcatMultipleLines(lines.ToArray());
        }

        private static string ConcatMultipleLines(ScriptLine[] lines)
        {

            string script = OperatorScriptParser.StringOpeningCharacters[0].ToString() + OperatorScriptParser.StringClosingCharacters[0];

            int i = lines.Length - 1;
            while (i >= 0)
            {
                var line = lines[i];
                if(line.Type == LineType.Return)
                {
                    if (i == lines.Length - 1)
                    {
                        script = line.GetLine();
                    }
                    lines[i] = new ScriptLetLine("___", line.GetLine());
                    continue;
                }
                else
                {
                    if (i == lines.Length - 1) //if this is the last line but also is a let line,
                    {
                        script = (line as ScriptLetLine).GetDishScript((line as ScriptLetLine).GetVariableName());
                    }
                    else
                    {
                        script = (line as ScriptLetLine).GetDishScript(script);
                    }
                }

                i--;
            }
            return script;
        }

        private static FieldControllerBase Interpret(string multiLineScript, ScriptState state = null)
        {
            var parsed = ParseMultiLineToSingleLine(multiLineScript);
            return OperatorScriptParser.Interpret(parsed, state);
        }

        private static ScriptLine ParseLine(string line)
        {
            Debug.Assert(line != null);

            try
            {
                var parser = new TypeScriptAST(line + ";");
                var descendants = parser.GetDescendants().ToArray();
                var statements = descendants.OfType<ExpressionStatement>().ToArray();
                var binaryExpressions = descendants.OfType<BinaryExpression>().ToArray();

                var isBinaryExpression = binaryExpressions.Any();

                if (isBinaryExpression)
                {
                    Debug.Assert(binaryExpressions.Length == 1, "Not an issue, just wanted to see if this is possible.  Can delete thiis assert if it is getting annoying");
                    var expr = binaryExpressions[0];

                    var equals = expr.Children.OfType<Token>().Where(i => i.Kind == SyntaxKind.EqualsToken).ToArray();

                    if (equals.Any())
                    {
                        var equalToken = equals[0];

                        var first = line.substring(0, equalToken.NodeStart);
                        var second = line.substring(equalToken.End.Value);

                        if (DSL.IsValidScript(first))
                        {
                            var letLine = new ScriptLetLine(first.Trim(), second.Trim());
                            return letLine;
                        }


                    }

                }

                line = line?.Trim();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Typescript parsing error");
            }


            var allEncapsulatingCharacters = OperatorScriptParser.EncapsulatingCharacterPairsIgnoringInternals.Concat(OperatorScriptParser.EncapsulatingCharacterPairsTrackingInternals).ToArray();
            var ignoreValueClosingChars = new HashSet<char>(OperatorScriptParser.EncapsulatingCharacterPairsIgnoringInternals.Select(i => i.Value));

            var allCharsToReject = allEncapsulatingCharacters.SelectMany(i => new List<char>(){i.Key, i.Value}).Concat(ignoreValueClosingChars).ToHashSet();

            for (int i = 0; i < line.Length; ++i)
            {
                var c = line[i];
                if (allCharsToReject.Contains(c))
                {
                    break;
                }
                if (c.Equals(VariableSettingCharacter))
                {
                    if (line.Length > i + 1)
                    {
                        return new ScriptLetLine(line.Substring(0, i).Trim(), line.Substring(i + 1).Trim());
                    }
                    return new ScriptLetLine(line.Substring(0, i).Trim(), "{}");
                }
            }
            return new ScriptReturnLine(line);
        }

        private abstract class ScriptLine
        {
            private string _line;
            public ScriptLine(string line)
            {
                _line = line;
            }
            public abstract LineType Type { get; }

            public string GetLine()
            {
                return _line;
            }
        }

        private class ScriptReturnLine : ScriptLine
        {
            public ScriptReturnLine(string line) : base(line){}
            public override LineType Type { get { return LineType.Return; } }
        }

        private class ScriptLetLine : ScriptLine
        {
            private string _variableName;
            private string _valueExpression;
            public ScriptLetLine(string variableName, string valueExpression) : base(variableName + VariableSettingCharacter + valueExpression)
            {
                _variableName = variableName;
                _valueExpression = valueExpression;
            }

            public string GetDishScript(string continuedExpression)
            {
                return DSL.GetFuncName<LetOperatorController>()
                       + OperatorScriptParser.FunctionOpeningCharacter
                       + _variableName
                       + OperatorScriptParser.ParameterDelimiterCharacter
                       + _valueExpression
                       + OperatorScriptParser.ParameterDelimiterCharacter
                       + continuedExpression
                       + OperatorScriptParser.FunctionClosingCharacter;
            }

            public string GetVariableName()
            {
                return _variableName;
            }

            public override LineType Type { get{return LineType.Let;} }
        }
    }
}
