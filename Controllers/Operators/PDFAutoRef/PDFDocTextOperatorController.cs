using System;
using System.Collections.Generic;
using DashShared;
using System.Collections.ObjectModel;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.text)]
    public sealed class PdfDocTextOperatorController : OperatorController
    {
        public PdfDocTextOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public PdfDocTextOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("DocumentTextPDF", new Guid("A0E5EC85-8B9A-4B06-B355-66869F3A4486"));

        //Input keys
        public static readonly KeyController DocumentKey = new KeyController("Document");

        //Output keys
        public static readonly KeyController DocumentTextKey = new KeyController("DocumentTextPDF");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(DocumentKey, new IOInfo(TypeInfo.Document, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [DocumentTextKey] = TypeInfo.Text,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var doc = inputs[DocumentKey] as DocumentController;

            if (doc == null)
            {
                outputs[DocumentTextKey] = new TextController("Invalid document input...");
                return;
            }

            outputs[DocumentTextKey] = new TextController(doc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null).Data ?? "");
        }

        public override FieldControllerBase GetDefaultController() => new RegexOperatorController(OperatorFieldModel);
    }
}
