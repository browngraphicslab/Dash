using Dash.Controllers;
using System.Diagnostics;

namespace Dash
{
    public class DocumentPointerFieldReference : FieldReference
    {
        public FieldReference DocumentReference { get; set; }

        public DocumentPointerFieldReference(FieldReference documentReference, KeyController fieldKey) : base(fieldKey)
        {
            DocumentReference = documentReference;
        }

        public override DocumentController GetDocumentController(Context context)
        {
            return DocumentReference.DereferenceToRoot<DocumentController>(context);
        }

        public override FieldReference Resolve(Context context)
        {
            var docController = GetDocumentController(context);
            Debug.Assert(docController != null);
            var doc = context.GetDeepestDelegateOf(docController) ?? docController;
            return new DocumentFieldReference(doc, FieldKey);
        }
        public override FieldReference Copy()
        {
            return new DocumentPointerFieldReference(DocumentReference.Copy(), FieldKey);
        }
        public override bool Equals(object obj)
        {
            DocumentPointerFieldReference reference = obj as DocumentPointerFieldReference;
            if (reference == null)
            {
                return false;
            }

            return base.Equals(reference) && reference.DocumentReference.Equals(DocumentReference);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ DocumentReference.GetHashCode();
        }

        public override ReferenceController GetReferenceController()
        {
            return new PointerReferenceController(DocumentReference.GetReferenceController(), FieldKey);
        }
    }
}
