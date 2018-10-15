using System;
using System.Collections.Generic;
using DashShared;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.text)]
    public sealed class PdfDocTextOperatorController : OperatorController
    {
        public PdfDocTextOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public PdfDocTextOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("DocumentTextPDF", new Guid("A0E5EC85-8B9A-4B06-B355-66869F3A4486"));

        //Input keys
        public static readonly KeyController DocumentKey = KeyController.Get("Document");

        //Output keys
        public static readonly KeyController DocumentTextKey = KeyController.Get("DocumentTextPDF");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(DocumentKey, new IOInfo(TypeInfo.Document, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [DocumentTextKey] = TypeInfo.Text,
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var doc = inputs[DocumentKey] as DocumentController;

            if (doc == null)
            {
                outputs[DocumentTextKey] = new TextController("Invalid document input...");
                return Task.CompletedTask;
            }

            outputs[DocumentTextKey] = new TextController(doc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null).Data ?? "");
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController() => new RegexOperatorController(OperatorFieldModel);
    }
}
