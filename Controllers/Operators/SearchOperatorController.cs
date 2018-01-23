using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class SearchOperatorController : OperatorController
    {
        public SearchOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public SearchOperatorController() : base(new OperatorModel(OperatorType.Search))
        {
        }

        //Input keys
        public static readonly KeyController Text = new KeyController("69DDED67-894A-41F0-81B2-FF6A8357B0DA", "Search Text");
        public static readonly KeyController InputCollection = new KeyController("4ECAFE47-0E2D-4A04-B24D-42C6668A4962", "Input");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("7431D567-7582-477B-A372-5964C2D26AE6", "Results");

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [Text] = new IOInfo(TypeInfo.Text, true),
            [InputCollection] = new IOInfo(TypeInfo.List, true)
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultsKey] = TypeInfo.List,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs)
        {
            var searchText = (inputs[Text] as TextController)?.Data;
            var searchCollection = (inputs[InputCollection] as ListController<DocumentController>)?.TypedData;
            var searchResultDocs = MainSearchBox.SearchHelper.SearchOverCollectionList(searchText, searchCollection).Select(srvm => srvm.ViewDocument);
            outputs[ResultsKey] = new ListController<DocumentController>(searchResultDocs);
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
            return new SearchOperatorController();
        }
    }
}
