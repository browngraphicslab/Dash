using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("in")]
    public class GetDocumentsInCollectionOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController TextKey = new KeyController("57EB542F-D77C-41A3-975C-1E9A571BBD01","Term");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("054D019F-665B-4053-9A24-1EE45245920A", "Results");


        public GetDocumentsInCollectionOperatorController() : base(new OperatorModel(OperatorType.GetDocumentsInCollection))
        {
        }

        public GetDocumentsInCollectionOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
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


        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(TextKey, new IOInfo(TypeInfo.Text, true)),
        };


        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultsKey] = TypeInfo.List,
        };


        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            var searchTerm = inputs[TextKey] as TextController;
            if (searchTerm != null && searchTerm.Data != null)
            {
                var term = searchTerm.Data.ToLower();
                var tree = DocumentTree.MainPageTree;
                var allResults = DSL.Interpret(OperatorScript.GetDishOperatorName<SearchOperatorController>() + "({ })") as ListController<DocumentController>;
                var final = allResults.TypedData.Where(doc => doc.GetField<TextController>(KeyStore.SearchResultDocumentOutline.SearchResultIdKey) != null &&
                                                  tree.GetNodeFromViewId(doc.GetField<TextController>(KeyStore.SearchResultDocumentOutline.SearchResultIdKey).Data).Parents.Any(
                                                      p => p?.DataDocument?.GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data?.ToLower()?.Contains(term) == true));
                outputs[ResultsKey] = new ListController<DocumentController>(final);
            }
        }
    }
}
