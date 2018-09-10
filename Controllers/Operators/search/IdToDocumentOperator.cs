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

        private static readonly KeyController TypeKey = new KeyController("Id to Document", new Guid("8B10EEF4-9B0A-4015-A8A6-4DE189D9F70B"));

        //Input keys
        public static readonly KeyController IdKey = new KeyController("Text");

        //Output keys
        public static readonly KeyController DocKey = new KeyController("Document");

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
                outputs[DocKey] = Search.SearchIndividualById(id);
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

