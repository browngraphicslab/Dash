using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    /// <summary>
    /// operator to get all documents before a given time
    /// </summary>
    [OperatorType("alias")]
    public class GetAllDocsByAlias : OperatorController
    {
        //Input keys
        public static readonly KeyController IdKey = new KeyController("53A28F84-359F-4E51-B1FA-87E8C09B8485", "Id");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("6EADF73F-B86E-45F3-A3AB-2F5963AC58EC", "Results");

        public GetAllDocsByAlias() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }
        public GetAllDocsByAlias(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(IdKey, new IOInfo(TypeInfo.Text, true)),
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, DashShared.TypeInfo>()
            {
                [ResultsKey] = TypeInfo.List
            };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("DB543B75-15D3-467A-A9DE-9F262F496C25", "Alias");


        /// <summary>
        /// Searches through all documents in the dash view and compares their data documents to find aliases
        /// </summary>
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, ScriptState state = null)
        {
            var toReturn = new ListController<DocumentController>();

            String id = (inputs[IdKey] as TextController)?.Data;

            var doc = ContentController<FieldModel>.GetController<DocumentController>(id);

            if (!string.IsNullOrEmpty(id)) {
                var allResults =
                DSL.Interpret(OperatorScript.GetDishOperatorName<SearchOperatorController>() + "(\" \")") as
                ListController<DocumentController>;

                Debug.Assert(allResults != null);

                var data = allResults.TypedData;

                var tree = DocumentTree.MainPageTree;

                for (int i = 0; i < data.Count; i++)
                {
                    var result = data[i];
                    var docFromResult = tree.GetNodeFromViewId(result.GetField<TextController>(KeyStore.SearchResultDocumentOutline.SearchResultIdKey).Data);
                    if (doc.GetDataDocument() == docFromResult.DataDocument)
                    {
                        toReturn.Add(data[i]);
                    }
                }
            }

            outputs[ResultsKey] = toReturn;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new GetAllDocsByAlias();
        }
    }
}
