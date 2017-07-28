using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class DocumentPointerFieldReference : FieldReference
    {
        public FieldReference DocumentReference { get; set; }

        public DocumentPointerFieldReference(FieldReference documentReference, Key fieldKey) : base(fieldKey)
        {
            DocumentReference = documentReference;
        }

        public override DocumentController GetDocumentController(Context context)
        {
            return DocumentReference.DereferenceToRoot<DocumentFieldModelController>(context)?.Data;
        }

        public override FieldReference Resolve(Context context)
        {
            string docId = context.GetDeepestDelegateOf(GetDocumentController(context).GetId());
            return new DocumentFieldReference(docId, FieldKey);
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
    }
}
