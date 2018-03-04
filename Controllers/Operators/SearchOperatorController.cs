using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("search")]
    public class SearchOperatorController : OperatorController
    {
        public SearchOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public SearchOperatorController() : base(new OperatorModel(OperatorType.Search))
        {
        }

        //Input keys
        public static readonly KeyController TextKey = new KeyController("69DDED67-894A-41F0-81B2-FF6A8357B0DA", /*"Search Text"*/"Term");
        public static readonly KeyController InputCollection = new KeyController("4ECAFE47-0E2D-4A04-B24D-42C6668A4962", /*"Input"*/"InputCollection");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("7431D567-7582-477B-A372-5964C2D26AE6", /*"Results"*/"Results");

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [TextKey] = new IOInfo(TypeInfo.Text, true),
            [InputCollection] = new IOInfo(TypeInfo.List, false)
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultsKey] = TypeInfo.List,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {

            var searchText = inputs.ContainsKey(TextKey) ? (inputs[TextKey] as TextController)?.Data?.ToLower() : null;
            var searchCollection = inputs.ContainsKey(InputCollection) ? (inputs[InputCollection] as ListController<DocumentController>)?.TypedData : null;
            var searchResultDocs = (MainSearchBox.SearchHelper.SearchOverCollectionList(searchText, searchCollection)?.Select(srvm => srvm.ViewDocument) ?? new DocumentController[]{}).ToArray();
            outputs[ResultsKey] = new ListController<DocumentController>(searchResultDocs);
        }

        public override bool SetValue(object value)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(Context context)
        {
            return this;
        }

        public override FieldModelController<OperatorModel> Copy()
        {
            return new SearchOperatorController();
        }
    }
}
