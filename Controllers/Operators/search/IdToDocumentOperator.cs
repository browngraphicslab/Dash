using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
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

        private static readonly KeyController TypeKey = KeyController.Get("Id to Document");

        //Input keys
        public static readonly KeyController IdKey = KeyController.Get("Text");

        //Output keys
        public static readonly KeyController DocKey = KeyController.Get("Document");

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

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
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
            catch (Exception)
            {
            }
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new IdToDocumentOperator();
        }
    }

}

