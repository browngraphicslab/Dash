using DashShared;

namespace Dash
{
    public class DocumentPointerReferenceController : ReferenceFieldModelController
    {
        public ReferenceFieldModelController DocReference { get; }

        public DocumentPointerReferenceController(ReferenceFieldModelController docReference, Key key) : base(new DocumentPointerReferenceFieldModel(docReference.ReferenceFieldModel, key))
        {
            DocReference = docReference;
        }

        public override DocumentController GetDocumentController(Context context = null)
        {
            return DocReference.DereferenceToRoot<DocumentFieldModelController>(context)?.Data;
        }

        public DocumentPointerReferenceFieldModel DocumentPointerReferenceFieldModel => FieldModel as DocumentPointerReferenceFieldModel;
    }
}
