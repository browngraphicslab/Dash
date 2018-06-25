using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    public class DocumentAppendOperatorController : OperatorController
    {
        public static readonly KeyController InputDocumentKey = new KeyController("F7CE7746-EDBA-4DAD-8D75-BEAEAC491B28", "Input Document");
        public static readonly KeyController FieldKey = new KeyController("DC93BDC1-A354-4CAA-8F04-E6EA20F7E030", "Input Field");

        public static readonly KeyController OutputDocumentKey = new KeyController("114C5C68-7A02-491D-8B52-43A27EC63BE4", "OutputDocument");

        public DocumentAppendOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }
        public DocumentAppendOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("4DAD9DE7-DAF8-4EB6-8EA4-8AA5F8D00121", "Document Append");

        public override FieldControllerBase GetDefaultController()
        {
            return new DocumentAppendOperatorController();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputDocumentKey, new IOInfo(TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(FieldKey, new IOInfo(TypeInfo.Any, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputDocumentKey] = TypeInfo.Document
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            DocumentController doc = (DocumentController) inputs[InputDocumentKey];
            FieldControllerBase field = inputs[FieldKey];

            var del = doc.MakeDelegate();
            del.SetField(new KeyController(Guid.NewGuid().ToString(), "Concat output"), field, true);

            outputs[OutputDocumentKey] = del;
        }
    }
}
