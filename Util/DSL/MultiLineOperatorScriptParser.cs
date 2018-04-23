using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            TestParse("a = 1; b = {test}; {hello}", "let(a,1,let(b,{test},{hello}))");

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
            Debug.Assert(multiLineScript != null);
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

            for (int i = lines.Length -1; i >= 0; i--)
            {
                var line = lines[i];
                if(line.Type == LineType.Return)
                {
                    if (i == lines.Length - 1)
                    {
                        script = line.GetLine();
                    }
                    continue;
                }
                else
                {
                    if (i == lines.Length - 1)
                    {
                        script = (line as ScriptLetLine).GetDishScript((line as ScriptLetLine).GetVariableName());
                    }
                    else
                    {
                        script = (line as ScriptLetLine).GetDishScript(script);
                    }
                }
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
            line = line?.Trim();
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
