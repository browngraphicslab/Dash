using DashShared;

namespace Dash.Controllers
{
    class PointerReferenceController : ReferenceController
    {
        public ReferenceController DocumentReference { get; set; }

        public PointerReferenceController(ReferenceController documentReference, KeyController key) : base(new PointerReferenceModel(documentReference.Id, key.Id))
        {
            Init();
        }

        public PointerReferenceController(PointerReferenceModel pointerReferenceFieldModel) : base(pointerReferenceFieldModel)
        {
        }

        public override void Init()
        {
            DocumentReference =
                ContentController<FieldModel>.GetController<ReferenceController>(
                    (Model as PointerReferenceModel).ReferenceFieldModelId);
        }

        public override FieldModelController<ReferenceModel> Copy()
        {
            return new PointerReferenceController(DocumentReference, FieldKey);
        }

        public override DocumentController GetDocumentController(Context context)
        {
            return DocumentReference.DereferenceToRoot<DocumentController>(context);
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
