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

        public override IReference Resolve(Context context)
        {
            var docController = GetDocumentController(context)?.GetId();
            Debug.Assert(docController != null);
            string docId = context.GetDeepestDelegateOf(docController) ?? docController;
            return new DocumentFieldReference(docId, FieldKey);
        }
        public override FieldReference Copy()
        {
            return new DocumentPointerFieldReference(DocumentReference.Copy(), FieldKey);
        }
        public override bool Equals(object obj)
        {
            if (obj is DocumentPointerFieldReference reference)
            {
                return base.Equals(reference) && reference.DocumentReference.Equals(DocumentReference);
            }

            return false;

        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ DocumentReference.GetHashCode();
        }

        public override ReferenceController ToReferenceController()
        {
            return new PointerReferenceController(DocumentReference.ToReferenceController(), FieldKey);
        }
    }
}
