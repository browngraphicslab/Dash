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

        public override DocumentController GetDocumentController(Context context)
        {
            return DocReference.DereferenceToRoot<DocumentFieldModelController>(context)?.Data;
        }

        public override ReferenceFieldModelController Resolve(Context context)
        {
            string docId = context.GetDeepestDelegateOf(GetDocumentController(context).GetId());
            return new DocumentReferenceController(docId, FieldKey);
        }

        public DocumentPointerReferenceFieldModel DocumentPointerReferenceFieldModel => FieldModel as DocumentPointerReferenceFieldModel;
        
        public override FieldModelController Copy()
        {
            return new DocumentPointerReferenceController(DocReference.Copy<ReferenceFieldModelController>(), FieldKey);
        }
    }
}
