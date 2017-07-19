using DashShared;

namespace Dash
{
    public class DocumentReferenceController : ReferenceFieldModelController
    {
        public string DocId => DocumentReferenceFieldModel.DocId;

        public DocumentReferenceController(string docId, Key key) : base(new DocumentReferenceFieldModel(docId, key))
        {
        }

        public DocumentReferenceFieldModel DocumentReferenceFieldModel => FieldModel as DocumentReferenceFieldModel;

        public override DocumentController GetDocumentController(Context context)
        {
            string docId = DocId;
            if (context != null)
            {
                docId = context.GetDeepestDelegateOf(DocId);
            }
            return ContentController.GetController<DocumentController>(docId);
        }

        public override ReferenceFieldModelController Resolve(Context context)
        {
            string docId = context.GetDeepestDelegateOf(DocId);
            return new DocumentReferenceController(docId, FieldKey);
        }

        public override FieldModelController Copy()
        {
            return new DocumentReferenceController(DocId, FieldKey);
        }
    }
}
