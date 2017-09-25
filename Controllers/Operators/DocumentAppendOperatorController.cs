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
        public static readonly KeyControllerBase InputDocumentKey = new KeyControllerBase("F7CE7746-EDBA-4DAD-8D75-BEAEAC491B28", "Input Document");
        public static readonly KeyControllerBase FieldKey = new KeyControllerBase("DC93BDC1-A354-4CAA-8F04-E6EA20F7E030", "Input Field");

        public static readonly KeyControllerBase OutputDocumentKey = new KeyControllerBase("114C5C68-7A02-491D-8B52-43A27EC63BE4", "OutputDocument");

        public DocumentAppendOperatorController() : base(new OperatorFieldModel("DocumentConcat"))
        {
        }
        public DocumentAppendOperatorController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldModelController<OperatorFieldModel> Copy()
        {
            return new DocumentAppendOperatorController();
        }
        public override object GetValue(Context context)
        {
            throw new NotImplementedException();
        }
        public override bool SetValue(object value)
        {
            return false;
        }

        public override ObservableDictionary<KeyControllerBase, IOInfo> Inputs { get; } = new ObservableDictionary<KeyControllerBase, IOInfo>
        {
            [InputDocumentKey] = new IOInfo(TypeInfo.Document, true),
            [FieldKey] = new IOInfo(TypeInfo.Any, true)
        };

        public override ObservableDictionary<KeyControllerBase, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyControllerBase, TypeInfo>
        {
            [OutputDocumentKey] = TypeInfo.Document
        };

        public override void Execute(Dictionary<KeyControllerBase, FieldControllerBase> inputs, Dictionary<KeyControllerBase, FieldControllerBase> outputs)
        {
            DocumentController doc = ((DocumentFieldModelController) inputs[InputDocumentKey]).Data;
            FieldControllerBase field = inputs[FieldKey];

            var del = doc.MakeDelegate();
            del.SetField(new KeyControllerBase(Guid.NewGuid().ToString(), "Concat output"), field, true);

            outputs[OutputDocumentKey] = new DocumentFieldModelController(del);
        }
    }
}
