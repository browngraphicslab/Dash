using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.data_doc, Op.Name.data_document)]
    public class GetDataDocumentOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController InputDocumentKey = new KeyController("InputDocument");

        //Output keys
        public static readonly KeyController ResultDataDocumentKey = new KeyController("ResultDataDocument");

        public GetDataDocumentOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public GetDataDocumentOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }


        public override FieldControllerBase GetDefaultController()
        {
            return new SimplifiedSearchOperatorController();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(InputDocumentKey, new IOInfo(TypeInfo.Document, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultDataDocumentKey] = TypeInfo.Document
        };

        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey =
            new KeyController("Get Data Document", "420D6ED9-F09E-4912-B106-576567E00C83");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var inputDocument = inputs[InputDocumentKey] as DocumentController;
            if (inputDocument != null)
            {
                outputs[ResultDataDocumentKey] = inputDocument.GetDataDocument();
            }

            else
            {
                outputs[ResultDataDocumentKey] = new DocumentController();
            }
        }
    }
}

