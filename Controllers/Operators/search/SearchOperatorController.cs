using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.search)]
    public class SearchOperatorController : OperatorController
    {
        public SearchOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public SearchOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Search", "EA5FD353-F99A-4F99-B0BC-5D2C88A51019");

        public override Func<ReferenceController, CourtesyDocument> LayoutFunc { get; } = rfmc => new SearchOperatorBox(rfmc);

        //Input keys
        public static readonly KeyController TextKey = new KeyController("Term");
        public static readonly KeyController InputCollection = new KeyController("InputCollection");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("Results");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(TextKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(InputCollection, new IOInfo(TypeInfo.List, false)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultsKey] = TypeInfo.List,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)

        {

            //var searchText = inputs.ContainsKey(TextKey) ? (inputs[TextKey] as TextController)?.Data?.ToLower() : null;
            //var searchCollection = inputs.ContainsKey(InputCollection) ? (inputs[InputCollection] as ListController<DocumentController>)?.TypedData : null;
            //var searchResultDocs = (MainSearchBox.SearchHelper.SearchOverCollectionList(searchText, searchCollection)?.Select(srvm => srvm.ViewDocument) ?? new DocumentController[]{}).ToArray();
            //outputs[ResultsKey] = new ListController<DocumentController>(searchResultDocs);

            var searchText = inputs.ContainsKey(TextKey) ? (inputs[TextKey] as TextController)?.Data : null;
            //search all docs for searchText and get results (list of doc controller)
            var searchResultDocs = Search.SearchByQuery(searchText).Select(res => res.ViewDocument).ToArray();
            outputs[ResultsKey] = new ListController<DocumentController>(searchResultDocs);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new SearchOperatorController();
        }
    }
}
