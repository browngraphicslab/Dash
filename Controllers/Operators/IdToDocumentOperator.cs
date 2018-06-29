using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.id_to_document)]
    class IdToDocumentOperator : OperatorController
    {
        public IdToDocumentOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public IdToDocumentOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey =
    new KeyController("8B10EEF4-9B0A-4015-A8A6-4DE189D9F70B", "Id to Document");

        //Input keys
        public static readonly KeyController IdKey = new KeyController("5F8A64B9-410F-4543-85B9-C986E47C5DF8", "Text");

        //Output keys
        public static readonly KeyController DocKey = new KeyController("3AB1D330-6137-4254-A18E-3E157BF54BE6", "Document");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } =
            new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
            {
                new KeyValuePair<KeyController, IOInfo>(IdKey, new IOInfo(TypeInfo.Text, true))
            };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, TypeInfo>
            {
                [DocKey] = TypeInfo.Document,
            };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var textController = inputs[IdKey] as TextController;
            var id = textController.Data;
            //var docFromResult = tree.GetNodeFromViewId(
            //    result.GetField<TextController>(KeyStore.SearchResultDocumentOutline.SearchResultIdKey).Data);
   
            
            try
            {
                outputs[DocKey] = ContentController<FieldModel>.GetController<DocumentController>(id);
            }
            catch (Exception e)
            {
                return;
            }
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new IdToDocumentOperator();
        }
    }

}

