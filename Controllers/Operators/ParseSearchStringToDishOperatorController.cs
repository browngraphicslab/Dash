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
            if (searchPart.Contains(":"))
            {
                Debug.Assert(searchPart.Count(c => c == ':') == 1);//TODO handle the case of multiple ':'
                var parts = searchPart.Split(':').Select(s => s.Trim()).ToArray();
                return WrapInParameterizedFunction(parts[0], parts[1]);
            }
            else
            {
                return WrapSearchTermInFunction(searchPart);
            }
        }

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, ScriptState state = null)
        {
            //very simple for now, can only join with intersections and unions
            var inputString = ((inputs[QueryKey] as TextController)?.Data ?? "").Trim();

            var charSeq = new List<char>();
            for (int i = 0; i < inputString.Length; i++)
            {
                char divider = inputString[i];
                if (divider == ',' || divider == ' ')
                {
                    charSeq.Add(divider);
                }
            }
            char[] splitChars = { ' ', ',' };
            var parts = inputString.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1)
            {
                outputs[ScriptKey] = new TextController("");
                return;
            }

            var negateSeq = new List<bool>();
            for (int i = 0; i < parts.Length; i++)
            {
                var searchToken = parts[i];
                parts[i] = searchToken.Replace(@"\", @"\\");
                if (searchToken.StartsWith("!"))
                {
                    parts[i] = searchToken.Substring(1);
                    negateSeq.Add(true);
                } else
                {
                    negateSeq.Add(false);
                }
            }


            var searches = new Stack<string>(parts.Select(GetBasicSearchResultsFromSearchPart).Select(WrapInDictifyFunc));
            if (negateSeq.FirstOrDefault())
            {
                var search = searches.Pop();
                searches.Push(NegateSearch(search));
                negateSeq.RemoveAt(0);
            }
            while (searches.Count() > 1)
            {
                var search1 = searches.Pop();
                var search2 = searches.Pop();

                if (negateSeq.FirstOrDefault())
                {
                    searches.Push(NegateSearch(search2));
                    negateSeq.RemoveAt(0);
                }
                
                switch (charSeq.ElementAt(0))
                {
                    case ' ':
                        searches.Push(JoinTwoSearchesWithIntersection(search1, search2));
                        break;
                    case ',': 
                        searches.Push(JoinTwoSearchesWithUnion(search1, search2));
                        break;
                    default:
                        break;
                }
                charSeq.RemoveAt(0);
            }
            outputs[ScriptKey] = new TextController(searches.First());
        }
    }
}
