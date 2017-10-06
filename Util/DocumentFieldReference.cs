using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash.Controllers;
using DashShared;

namespace Dash
{
    public class DocumentFieldReference : FieldReference
    {
        public string DocumentId { get; set; }

        public DocumentFieldReference(string documentId, KeyController fieldKey) : base(fieldKey)
        {
            DocumentId = documentId;
        }

        public override DocumentController GetDocumentController(Context context)
        {
            string docId = DocumentId;
            if (context != null)
            {
                docId = context.GetDeepestDelegateOf(docId) ?? docId;
            }
            return ContentController<DocumentModel>.GetController<DocumentController>(docId);
        }

        public override FieldReference Copy()
        {
            return new DocumentFieldReference(DocumentId, FieldKey);
        }

        public override FieldReference Resolve(Context context)
        {
            string docId = context.GetDeepestDelegateOf(DocumentId) ?? DocumentId;
            return new DocumentFieldReference(docId, FieldKey);
        }

        public override bool Equals(object obj)
        {
            DocumentFieldReference reference = obj as DocumentFieldReference;
            if (reference == null)
            {
                return false;
            }

            return base.Equals(reference) && DocumentId.Equals(reference.DocumentId);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ DocumentId.GetHashCode();
        }

        public override ReferenceFieldModelController GetReferenceController()
        {
            return new DocumentReferenceFieldController(DocumentId, FieldKey);
        }
    }
}
