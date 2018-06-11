using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DashShared;

namespace Dash
{
    [OperatorType("search")]
    public class SearchOperatorController : OperatorController
    {
        public SearchOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public SearchOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("EA5FD353-F99A-4F99-B0BC-5D2C88A51019", "Search");

        public override Func<ReferenceController, CourtesyDocument> LayoutFunc { get; } = rfmc => new SearchOperatorBox(rfmc);

        //Input keys
        public static readonly KeyController TextKey = new KeyController("69DDED67-894A-41F0-81B2-FF6A8357B0DA", /*"Search Text"*/"Term");
        public static readonly KeyController InputCollection = new KeyController("4ECAFE47-0E2D-4A04-B24D-42C6668A4962", /*"Input"*/"InputCollection");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("7431D567-7582-477B-A372-5964C2D26AE6", /*"Results"*/"Results");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(TextKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(InputCollection, new IOInfo(TypeInfo.List, false)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultsKey] = TypeInfo.List,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, ScriptState state = null)
        {

            //var searchText = inputs.ContainsKey(TextKey) ? (inputs[TextKey] as TextController)?.Data?.ToLower() : null;
            //var searchCollection = inputs.ContainsKey(InputCollection) ? (inputs[InputCollection] as ListController<DocumentController>)?.TypedData : null;
            //var searchResultDocs = (MainSearchBox.SearchHelper.SearchOverCollectionList(searchText, searchCollection)?.Select(srvm => srvm.ViewDocument) ?? new DocumentController[]{}).ToArray();
            //outputs[ResultsKey] = new ListController<DocumentController>(searchResultDocs);

            var searchText = inputs.ContainsKey(TextKey) ? (inputs[TextKey] as TextController)?.Data?.ToLower() : null;
            //search all docs for searchText and get results (list of doc controller)
            var searchResultDocs = (MainSearchBox.SearchHelper.SearchAllDocumentsForSingleTerm(searchText) ?? new DocumentController[] { }).ToArray();
            outputs[ResultsKey] = new ListController<DocumentController>(searchResultDocs);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new SearchOperatorController();
        }
    }
}
