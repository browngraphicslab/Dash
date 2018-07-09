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

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var keyQuery = (inputs[KeyQueryKey] as TextController)?.Data?.ToLower();
            var toReturn = new ListController<DocumentController>();
            if (!string.IsNullOrEmpty(keyQuery))
            {
                var negateCategory = keyQuery.StartsWith('!');
                keyQuery = keyQuery.TrimStart('!');

                var valueQuery = (inputs[ValueQueryKey] as TextController)?.Data?.ToLower() ?? "";

                var tree = DocumentTree.MainPageTree;

                var positive = new List<DocumentNode>();
                var negative = new List<DocumentNode>();

                foreach (var node in tree)
                {
                    foreach (var field in node.DataDocument.EnumFields())
                    {
                        var keyOrValueMatch = field.Key.Name.ToLower().Contains(keyQuery) && field.Value.SearchForString(valueQuery).StringFound;
                        if (keyOrValueMatch) positive.Add(node);
                        else negative.Add(node);
                    }
                }

                var finalResults = (negateCategory ? negative.Select(d => d.ViewDocument) : positive.Select(d => d.ViewDocument)).ToArray();

                //TODO FURTHER modify the helpful text of these docs so the text is more helpful

                var found = $"Found the specified key/value: {keyQuery}/{valueQuery} ";
                var absent = $"Didn't contain the specified negated key/value: {keyQuery}/{valueQuery} ";

                foreach (var resultDoc in finalResults)
                {
                    resultDoc.GetField<TextController>(KeyStore.SearchResultDocumentOutline.SearchResultHelpTextKey).Data = !negateCategory ? found : absent;
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
