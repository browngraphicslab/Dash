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
            bool inParen = inputString.Trim('!').StartsWith("(");
            bool inQuote = false;
            int len = inputString.Length;
            for (int i = 0; i < len; i++)
            {
                char curChar = inputString[i];
                if (inParen)
                {
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
                    else if (!inQuote && (curChar == ' ' || curChar == ','))
                    {
                        return i;
                    }
                }
            }
            return len;
        }

        private int FindEndParenthesis(String inputString)
        {
            int len = inputString.Length;
            for (int i = 0; i < len; i++)
            {
                // Making sure to ignore parenthesis captured within quotation marks 
                if (inputString[i] == ')' && (i == len-1 || inputString[i+1] == ' ' || inputString[i+1] == ','))
                {
                    return i;
                }
            }
            return -1;
        }

        private String Parse(String inputString)
        {
            int dividerIndex = FindNextDivider(inputString);
            String searchTerm = inputString.Substring(0, dividerIndex);
            bool isNegated = searchTerm.StartsWith("!") ? true : false;
            String modifiedSearchTerm = isNegated ? searchTerm.Substring(1) : searchTerm;
            String finalSearchTerm = modifiedSearchTerm.Replace("\"", "");

            int endParenthesis = -1;

            // Making sure parenthesis doesn't clash with regex
            if (modifiedSearchTerm.StartsWith("(") && !modifiedSearchTerm.EndsWith(")"))
            {
                endParenthesis = FindEndParenthesis(inputString);
            }

            // Using modifiedSearchTerm since we still want quotes inside, while giving the user option to negate the whole grouping
            String searchDict;
            if (endParenthesis >= 0)
            {
                if (isNegated)
                {
                    String newInput = inputString.Substring(1); 
                }
                searchDict = Parse()
            } else {
                searchDict = WrapInDictifyFunc(GetBasicSearchResultsFromSearchPart(finalSearchTerm));
            }

            if (isNegated)
                searchDict = NegateSearch(searchDict);


            int len = inputString.Length;
            if (dividerIndex == len || endParenthesis == len - 1)
            {
                return searchDict;
            } else
            {
                char divider = inputString[dividerIndex];
                String rest = inputString.Substring(dividerIndex + 1);

                if (divider == ' ')
                {
                    return JoinTwoSearchesWithIntersection(searchDict, Parse(rest));
                } else if (divider == ',')
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
