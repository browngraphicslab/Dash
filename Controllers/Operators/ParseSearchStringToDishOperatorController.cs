using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using DashShared;


namespace Dash
{
    [OperatorType("parseSearchString")]
    public class ParseSearchStringToDishOperatorController : OperatorController
    {


        //Input keys
        public static readonly KeyController QueryKey = new KeyController("2569BB88-F1BD-4953-9403-00B1895109C6", "Query");

        //Output keys
        public static readonly KeyController ScriptKey = new KeyController("EE6A6F8A-D0EF-48FC-89FD-87EBB91F8C77", "Script");

        public ParseSearchStringToDishOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {

        }

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
        private static readonly KeyController TypeKey = new KeyController("91CBB332-871B-4289-B639-ABB4C93D755D", "Parse Search String");

        private string WrapSearchTermInFunction(string searchTerm)
        {
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

        private string WrapInParameterizedFunction(string funcName, string paramName)
        {
            //this returns a string that more closely follows function syntax
            //TODO check if func exists

            if (!DSL.FuncNameExists(funcName))
            {
                return OperatorScript.GetDishOperatorName<GetAllDocumentsWithKeyFieldValuesOperatorController>() + "(\"" + funcName + "\",\"" + paramName + "\")";
            }

            if (OperatorScript.GetFirstInputType(funcName) == DashShared.TypeInfo.Text)
            {
                return funcName + "(\"" + paramName + "\")";
            }

            return funcName + "(" + paramName + ")";
        }

        private string GetBasicSearchResultsFromSearchPart(string searchPart)
        {
            searchPart = searchPart?.ToLower() ?? " ";
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

        private int FindNextDivider(String inputString)
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
        private int FindEndParenthesis(String inputString)
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

        private String Parse(String inputString)
        {
            int dividerIndex = FindNextDivider(inputString);
            String searchTerm = inputString.Substring(0, dividerIndex);
            bool isNegated = searchTerm.StartsWith("!") ? true : false;
            String modifiedSearchTerm = searchTerm.TrimStart('!');
            String finalSearchTerm = modifiedSearchTerm.Replace("\"", "");

            String modInput = inputString.TrimStart('!');

            int endParenthesis = -2;

            // Making sure parenthesis doesn't clash with regex
            if ((modifiedSearchTerm.StartsWith("(") && !modifiedSearchTerm.EndsWith(")")) || 
                (isNegated && modifiedSearchTerm.StartsWith("(") && modifiedSearchTerm.EndsWith(")")))
            {
                endParenthesis = FindEndParenthesis(inputString);
            }

            
            String searchDict;
            if (endParenthesis > 0 || (inputString.StartsWith('(') && inputString.EndsWith(')') && (modInput.Contains(' ') || modInput.Contains('|'))))
            {
                String newInput = modInput.Substring(1, modInput.Length - 2);
                searchDict = Parse(newInput);
            } else {
                searchDict = WrapInDictifyFunc(GetBasicSearchResultsFromSearchPart(finalSearchTerm));
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
                String rest = inputString.Substring(dividerIndex + 1);

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
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, ScriptState state = null)
        {
            var inputString = ((inputs[QueryKey] as TextController)?.Data ?? "").Trim();
            outputs[ScriptKey] = new TextController(Parse(inputString));
        }
    }
}
