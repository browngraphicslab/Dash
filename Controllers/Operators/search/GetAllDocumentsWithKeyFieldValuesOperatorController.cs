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
    /// operator to get all documents with a given field and value of that field
    /// </summary>
    [OperatorType(Op.Name.key_field_query, Op.Name.kv)]
    public sealed class GetAllDocumentsWithKeyFieldValuesOperatorController : OperatorController
    {

        //Input keys
        public static readonly KeyController KeyQueryKey = new KeyController("KeyQuery");
        public static readonly KeyController ValueQueryKey = new KeyController("ValueQuery");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("Results");

        public GetAllDocumentsWithKeyFieldValuesOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public GetAllDocumentsWithKeyFieldValuesOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(KeyQueryKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(ValueQueryKey, new IOInfo(TypeInfo.Text, true)),
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } =
        new ObservableDictionary<KeyController, DashShared.TypeInfo>()
        {
            [ResultsKey] = TypeInfo.List
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Key Field Query", "DAB89167-7D62-4EE5-9DCF-D3E0A4ED72F9");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var keyQuery = (inputs[KeyQueryKey] as TextController)?.Data?.ToLower();
            var toReturn = new ListController<DocumentController>();
            if (!string.IsNullOrEmpty(keyQuery))
            {
                var negateCategory = keyQuery.StartsWith('!');
                keyQuery = keyQuery.TrimStart('!');

                var valueQuery = (inputs[ValueQueryKey] as TextController)?.Data?.ToLower() ?? "";

                var tree = DocumentTree.MainPageTree;
                var allResults = DSL.Interpret(OperatorScript.GetDishOperatorName<SearchOperatorController>() + "(\" \")") as ListController<DocumentController>;

                Debug.Assert(allResults != null);
                var stringContainResults = allResults.TypedData.Where(doc => tree.GetNodeFromViewId(doc.GetField<TextController>(KeyStore.SearchResultDocumentOutline.SearchResultIdKey).Data).DataDocument.EnumFields().Any(f => f.Key.Name.ToLower().Contains(keyQuery) && f.Value.SearchForString(valueQuery).StringFound));

                var finalResults = (negateCategory ? allResults.TypedData.Except(stringContainResults) : stringContainResults).ToArray();

                //TODO FURTHER modify the helpful text of these docs so the text is more helpful

                foreach (var resultDoc in finalResults)
                {
                    if (!negateCategory)
                    {
                        resultDoc.GetField<TextController>(KeyStore.SearchResultDocumentOutline.SearchResultHelpTextKey).Data = $"Found the specified key/value: {keyQuery}/{valueQuery} ";
                    }
                    else
                    {
                        resultDoc.GetField<TextController>(KeyStore.SearchResultDocumentOutline.SearchResultHelpTextKey).Data = $"Didn't contain the specified negated key/value: {keyQuery}/{valueQuery} ";
                    }
                }


                toReturn.AddRange(finalResults);
            }
            outputs[ResultsKey] = toReturn;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new GetAllDocumentsWithKeyFieldValuesOperatorController();
        }
    }
}
