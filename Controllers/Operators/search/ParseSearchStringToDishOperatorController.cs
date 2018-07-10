using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using DashShared;


namespace Dash
{
    [OperatorType(Op.Name.parse_search_string)]
    public class ParseSearchStringToDishOperatorController : OperatorController
    {


        //Input keys
        public static readonly KeyController QueryKey = new KeyController("Query");

        //Output keys
        public static readonly KeyController ScriptKey = new KeyController("Script");

        public ParseSearchStringToDishOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public ParseSearchStringToDishOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new ParseSearchStringToDishOperatorController();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(QueryKey, new IOInfo(TypeInfo.Text, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ScriptKey] = TypeInfo.Text
        };

        public override KeyController OperatorType { get; }
        private static readonly KeyController TypeKey = new KeyController("Parse Search String", "91CBB332-871B-4289-B639-ABB4C93D755D");

        private string WrapSearchTermInFunction(string searchTerm)
        {
            searchTerm = searchTerm.Replace(@"\", @"\\");
            return OperatorScript.GetDishOperatorName<SearchOperatorController>()+ "(\"" + searchTerm + "\")";
        }

        private string WrapInDictifyFunc(string resultsString)
        {
            return OperatorScript.GetDishOperatorName<PutSearchResultsIntoDictionaryOperatorController>() + "("+resultsString+")";
        }

        private string JoinTwoSearchesWithUnion(string search1, string search2)
        {
            return OperatorScript.GetDishOperatorName<UnionSearchOperator>() + "(" + search1 + "," + search2 + ")";
        }

        private string JoinTwoSearchesWithIntersection(string search1, string search2)
        {
            return OperatorScript.GetDishOperatorName<IntersectSearchOperator>() + "(" + search1 + "," + search2 + ")";
        }

        private string NegateSearch(string search)
        {
            return OperatorScript.GetDishOperatorName<NegationSearchOperator>() + "(" + search + ")";
        }

        private string WrapInParameterizedFunction(string name, string paramName)
        {
            //this returns a string that more closely follows function syntax
            //TODO check if func exists

            if (!DSL.FuncNameExists(name))
            {
                return OperatorScript.GetDishOperatorName<GetAllDocumentsWithKeyFieldValuesOperatorController>() + "(\"" + name + "\",\"" + paramName + "\")";
            }

            if (OperatorScript.GetFirstInputType(Op.Parse(name)) == DashShared.TypeInfo.Text)
            {
                return name + "(\"" + paramName + "\")";
            }

            return name + "(" + paramName + ")";
        }

        private string GetBasicSearchResultsFromSearchPart(string searchPart)
        {
            searchPart = searchPart ?? " ";
            //if the part is a quote, it ignores the colon
            if (searchPart.Contains(":") && searchPart[0] != '"')
            {
                //   Debug.Assert(searchPart.Count(c => c == ':') == 1);//TODO handle the case of multiple ':'

                //splits after first colon
                var parts = searchPart.Split(':', 2).Select(s => s.Trim()).ToArray();
                //created a key field query function with both parts as parameters if parts[0] isn't a function name

                return WrapInParameterizedFunction(parts[0], parts[1]);
            }
            else
            {
                return WrapSearchTermInFunction(searchPart);
            }
        }

        private int FindNextDivider(string inputString)
        {
            bool inParen = false;
            int parenCounter = 0;
            if (inputString.TrimStart('!').StartsWith("("))
            {
                inParen = true;
            }

            bool inQuote = false;
            int len = inputString.Length;
            for (int i = 0; i < len; i++)
            {
                // if it starts with quotes, ignore parenthesis, if it starts with parenthesis, ignore quotes
                char curChar = inputString[i];
                if (curChar == '"')
                {
                    if (inQuote && !inParen)
                    {
                        inQuote = false;
                    }
                    else
                    {
                        inQuote = true;
                    }

                }
                else if (!inQuote && curChar == '(')
                {
                    inParen = true;
                    parenCounter += 1;
                }
                else if (!inQuote && inParen && curChar == ')')
                {
                    parenCounter -= 1;
                    if (parenCounter == 0)
                    {
                        inParen = false;
                    }
                }
                else if (!inQuote && !inParen && (curChar == ' ' || curChar == '|'))
                {
                    return i;
                }
            }
            return len;
        }

        // Assumes that the inputString starts with "(" or "!("
        private int FindEndParenthesis(string inputString)
        {
            int parenCounter = 0;
            bool inQuote = false;
            int len = inputString.Length;
            for (int i = 0; i < len; i++)
            {
                char curChar = inputString[i];
                if (curChar == '"')
                {
                    if (inQuote)
                    {
                        inQuote = false;
                    }
                    else
                    {
                        inQuote = true;
                    }

                }
                else if (!inQuote && curChar == '(')
                {
                    parenCounter += 1;
                }
                else if (!inQuote && curChar == ')')
                {
                    parenCounter -= 1;
                    if (parenCounter == 0)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private string SelectivelyReplace(string inputString, string toReplace, string toIgnore, string replaceWith)
        {
            int len = inputString.Length;
            int rep1 = toReplace.Length;
            int rep2 = toIgnore.Length;
            int repW1 = replaceWith.Length;

            for (int i = 0; i < len - (rep1 - 1); i++)
            {   
                if (len - i > rep2 - 1 && inputString.Substring(i, rep2).Equals(toIgnore)) {
                    i += rep2 - 1;
                }
                else if (inputString.Substring(i, rep1).Equals(toReplace))
                {
                    inputString = inputString.Remove(i, rep1).Insert(i, replaceWith);
                    i += repW1 - 1;

                }
            }
            return inputString;
        }

        /// <summary>
        /// Re-adds escaped quotes so that they don't interfere with operator call
        /// </summary>
        private string EscapeQuotes(string functionString)
        {
            int len = functionString.Length;
            if (len < 3)
            {
                return functionString;
            }
            else
            {
                for (int i = 1; i < len - 1; i++)
                {
                    if (functionString.Substring(i, 1).Equals("\""))
                    {
                        //if (!(functionString.Substring(i - 1, 2).Equals("(\"") || functionString.Substring(i, 2).Equals("\")")))
                        //{
                            functionString = functionString.Insert(i, "\\");
                            i += 1;
                            len += 1;
                        //}
                    }
                }
                return functionString;
            }
        }

        private string Parse(string inputString)
        {
            int dividerIndex = FindNextDivider(inputString);
            string searchTerm = inputString.Substring(0, dividerIndex);
            bool isNegated = searchTerm.StartsWith("!") ? true : false;
            string modifiedSearchTerm = searchTerm.TrimStart('!');

            if (modifiedSearchTerm.StartsWith('"') && modifiedSearchTerm.EndsWith('"'))
            {
                modifiedSearchTerm = modifiedSearchTerm.Substring(1, modifiedSearchTerm.Length - 2);
            }

            String finalSearchTerm = SelectivelyReplace(modifiedSearchTerm, "\"", "\\\"", "");
            finalSearchTerm = SelectivelyReplace(finalSearchTerm, "\\n", "\\\\n", "\n");
            finalSearchTerm = SelectivelyReplace(finalSearchTerm, "\\t", "\\\\t", "\t");
            finalSearchTerm = SelectivelyReplace(finalSearchTerm, "\\r", "\\\\r", "\r");

            string modInput = inputString.TrimStart('!');

            int endParenthesis = -2;

            // Making sure parenthesis doesn't clash with regex
            if ((modifiedSearchTerm.StartsWith("(") && !modifiedSearchTerm.EndsWith(")")) || 
                (isNegated && modifiedSearchTerm.StartsWith("(") && modifiedSearchTerm.EndsWith(")")))
            {
                endParenthesis = FindEndParenthesis(inputString);
            }

            
            string searchDict;
            if (endParenthesis > 0 || (inputString.StartsWith('(') && inputString.EndsWith(')') && (modInput.Contains(' ') || modInput.Contains('|'))))
            {
                string newInput = modInput.Substring(1, modInput.Length - 2);
                searchDict = Parse(newInput);
            } else {
                searchDict = GetBasicSearchResultsFromSearchPart(finalSearchTerm);
            }

            if (isNegated)
                searchDict = NegateSearch(searchDict);


            int len = inputString.Length;

            // Debugging check - make sure that Dash doesn't crash with open parenthesis input - if user types in something like "(fafe afeef",
            // it doesn't necessarily have to show anything unless its in quotes, but it should at least not crash
            if (dividerIndex == len)
            {
                return searchDict;
            } else
            {
                char divider = inputString[dividerIndex];
                string rest = inputString.Substring(dividerIndex + 1);

                if (divider == ' ')
                {
                    return JoinTwoSearchesWithIntersection(searchDict, Parse(rest));
                } else if (divider == '|')
                {
                    return JoinTwoSearchesWithUnion(searchDict, Parse(rest));
                } else
                {
                    throw new Exception("Unknown Divider");
                }

            }
        }

        /// <summary>
        /// Right now, we can join with intersections and unions, and negate searches
        /// </summary>
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var inputString = ((inputs[QueryKey] as TextController)?.Data ?? "").Trim();
            string functionString = Parse(inputString);
            functionString = functionString.Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", "\\r"); ;
            outputs[ScriptKey] = new TextController(functionString);
        }
    }
}
