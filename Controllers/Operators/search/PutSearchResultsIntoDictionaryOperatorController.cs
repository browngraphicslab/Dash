using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using DashShared.Models;

namespace Dash
{
    [OperatorType("putSearchResultsIntoDictionary")]
    public class PutSearchResultsIntoDictionaryOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController ListKey = new KeyController("1EB7C160-A53C-4497-9BC5-212E30594C1C", "ListResults");

        //Output keys
        public static readonly KeyController DictionaryResultsKey = new KeyController("57014D2A-E4DA-461C-8064-C9E3EF215A9A", "DictionaryResults");

        public PutSearchResultsIntoDictionaryOperatorController() : base(new OperatorModel(OperatorType.PutSearchResultsIntoDictionary))
        {
        }
        public PutSearchResultsIntoDictionaryOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
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
            new KeyValuePair<KeyController, IOInfo>(ListKey, new IOInfo(TypeInfo.List, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [DictionaryResultsKey] = TypeInfo.Document
        };
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            var list = (inputs[ListKey] as BaseListController).Data;
            var output = new DocumentController();
            foreach (var searchResultDoc in list)
            {
                var resultDocument = searchResultDoc as DocumentController;
                if (resultDocument != null)
                {
                    var resultDocId = resultDocument.GetField<TextController>(KeyStore.SearchResultDocumentOutline.SearchResultIdKey);
                    var resultHash = UtilShared.GetDeterministicGuid(resultDocId.Data);
                    var keyController = ContentController<FieldModel>.GetController<KeyController>(resultHash) ?? new KeyController(resultHash);
                    if (output.GetField(keyController) == null)
                    {
                        output.SetField(keyController, new ListController<DocumentController>(), true);
                    }
                    output.GetField<ListController<DocumentController>>(keyController).Add(resultDocument);
                }
            }

            outputs[DictionaryResultsKey] = output;
        }
    }
}
