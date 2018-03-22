using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public ParseSearchStringToDishOperatorController() : base(new OperatorModel(OperatorType.ParseSearchStringToDish))
        {

        }

        public ParseSearchStringToDishOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override bool SetValue(object value)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(Context context)
        {
            throw new NotImplementedException();
        }

        public override FieldModelController<OperatorModel> Copy()
        {
            throw new NotImplementedException();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(QueryKey, new IOInfo(TypeInfo.Text, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ScriptKey] = TypeInfo.Text
        };

        private string WrapSearchTermInFunction(string searchTerm)
        {
            return OperatorScript.GetDishOperatorName<PutSearchResultsIntoDictionaryOperatorController>() + "("+OperatorScript.GetDishOperatorName<SearchOperatorController>()+"({" + searchTerm + "}))";
        }

        private string JoinTwoSearchesWithUnion(string search1, string search2)
        {
            //TODO not have the function name and paremter name strings be hardcoded here
            return "unionByValue(A:" +search1+",B:"+search2+")";
        }

        private string JoinTwoSearchesWithIntersection(string search1, string search2)
        {
            return OperatorScript.GetDishOperatorName<IntersectSearchOperator>() + "(" + search1 + "," + search2 + ")";
        }

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            //very simple for now, can only join with intersections
            var inputString = ((inputs[QueryKey] as TextController)?.Data ?? "").Trim();
            var parts = inputString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1)
            {
                outputs[ScriptKey] = new TextController("");
                return;
            }

            var searches = new Stack<string>(parts.Select(WrapSearchTermInFunction));
            while (searches.Count() > 1)
            {
                var search1 = searches.Pop();
                var search2 = searches.Pop();
                searches.Push(JoinTwoSearchesWithIntersection(search1, search2));
            }
            outputs[ScriptKey] = new TextController(searches.First());
        }
    }
}
