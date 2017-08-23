using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class DocumentAppendOperatorController : OperatorFieldModelController
    {
        public static readonly KeyController InputDocumentKey = new KeyController("F7CE7746-EDBA-4DAD-8D75-BEAEAC491B28", "Input Document");
        public static readonly KeyController FieldKey = new KeyController("DC93BDC1-A354-4CAA-8F04-E6EA20F7E030", "Input Field");

        public static readonly KeyController OutputDocumentKey = new KeyController("114C5C68-7A02-491D-8B52-43A27EC63BE4", "OutputDocument");

        public DocumentAppendOperatorController() : base(new OperatorFieldModel("DocumentConcat"))
        {
        }
        public DocumentAppendOperatorController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldModelController Copy()
        {
            return new DocumentAppendOperatorController();
        }
        public override object GetValue()
        {
            throw new System.NotImplementedException();
        }
        public override void SetValue(object value)
        {
            throw new System.NotImplementedException();
        }

        public override ObservableDictionary<KeyController, TypeInfo> Inputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [InputDocumentKey] = TypeInfo.Document,
            [FieldKey] = TypeInfo.Any
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputDocumentKey] = TypeInfo.Document
        };

        public override void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
        {
            DocumentController doc = ((DocumentFieldModelController) inputs[InputDocumentKey]).Data;
            FieldModelController field = inputs[FieldKey];

            var del = doc.MakeDelegate();
            del.SetField(new KeyController(Guid.NewGuid().ToString(), "Concat output"), field, true);

            outputs[OutputDocumentKey] = new DocumentFieldModelController(del);
        }
    }
}
