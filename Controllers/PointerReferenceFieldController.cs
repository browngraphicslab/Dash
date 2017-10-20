using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using DashShared.Models;

namespace Dash.Controllers
{
    class PointerReferenceFieldController : ReferenceFieldModelController
    {
        public ReferenceFieldModelController DocumentReference { get; set; }

        public PointerReferenceFieldController(ReferenceFieldModelController documentReference, KeyController key) : base(new PointerReferenceFieldModel(documentReference.Id, key.Id))
        {
            Init();
        }

        public PointerReferenceFieldController(PointerReferenceFieldModel pointerReferenceFieldModel) : base(pointerReferenceFieldModel)
        {
        }

        public override void Init()
        {
            DocumentReference =
                ContentController<FieldModel>.GetController<ReferenceFieldModelController>(
                    (Model as PointerReferenceFieldModel).ReferenceFieldModelId);
        }

        public override FieldModelController<ReferenceFieldModel> Copy()
        {
            return new PointerReferenceFieldController(DocumentReference, FieldKey);
        }

        public override DocumentController GetDocumentController(Context context)
        {
            return DocumentReference.DereferenceToRoot<DocumentFieldModelController>(context)?.Data;
        }

        public override FieldReference GetFieldReference()
        {
            return new DocumentPointerFieldReference(DocumentReference.GetFieldReference(), FieldKey);
        }

        public override string GetDocumentId(Context context)
        {
            return GetDocumentController(context).Id;
        }
    }
}
