using System.Diagnostics;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Class used for referencing a field on a document, if there is a delegate of the referenced document
    /// in the context, then it will reference the field found on that deepest delegate. If that delegate
    /// does not have the required field then it will by default return the first field it finds with the
    /// corresponding key on one of its prototypes. Thus this class should ideally be passed the highest
    /// possible prototype for the document that is being referenced.
    /// </summary>
    public class DocumentFieldReference : FieldReference
    {
        /// <summary>
        /// The unique id of the Document that is being referenced
        /// </summary>
        public string DocumentId { get; set; }

        /// <summary>
        /// Create a new reference to a field on a document. When this reference is resolved 
        /// it will return the corresponding field on the deepest delegate of this document
        /// in the passed in context.
        /// </summary>
        /// <param name="documentId"></param>
        /// <param name="fieldKey"></param>
        public DocumentFieldReference(string documentId, KeyController fieldKey) : base(fieldKey)
        {
            Debug.Assert(documentId != null);
            DocumentId = documentId;
        }

        /// <summary>
        /// Return the deepest delegate of the document uniquely identified by <see cref="DocumentId"/>
        /// that is in the context
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override DocumentController GetDocumentController(Context context)
        {
            string docId = DocumentId;
            if (context != null)
            {
                docId = context.GetDeepestDelegateOf(docId) ?? docId;
            }
            return ContentController<FieldModel>.GetController<DocumentController>(docId);
        }

        public override FieldReference Copy()
        {
            return new DocumentFieldReference(DocumentId, FieldKey);
        }

        public override IReference Resolve(Context context)
        {
            string docId = context.GetDeepestDelegateOf(DocumentId) ?? DocumentId;
            return new DocumentFieldReference(docId, FieldKey);
        }

        public override bool Equals(object obj)
        {
            var reference = obj as DocumentFieldReference;
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

        public override ReferenceController ToReferenceController()
        {
            return new DocumentReferenceController(DocumentId, FieldKey);
        }
    }
}
