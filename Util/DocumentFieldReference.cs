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
        public DocumentController DocumentController { get; set; }

        /// <summary>
        /// Create a new reference to a field on a document. When this reference is resolved 
        /// it will return the corresponding field on the deepest delegate of this document
        /// in the passed in context.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="fieldKey"></param>
        public DocumentFieldReference(DocumentController document, KeyController fieldKey) : base(fieldKey)
        {
            Debug.Assert(document != null);
            DocumentController = document;
        }

        /// <summary>
        /// Return the deepest delegate of the document uniquely identified by <see cref="DocumentId"/>
        /// that is in the context
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override DocumentController GetDocumentController(Context context)
        {
            return DocumentController;
            return context?.GetDeepestDelegateOf(DocumentController) ?? DocumentController;
        }

        public override FieldReference Copy()
        {
            return new DocumentFieldReference(DocumentController, FieldKey);
        }

        public override FieldReference Resolve(Context context)
        {
            DocumentController doc = context.GetDeepestDelegateOf(DocumentController) ?? DocumentController;
            return new DocumentFieldReference(doc, FieldKey);
        }

        public override bool Equals(object obj)
        {
            var reference = obj as DocumentFieldReference;
            if (reference == null)
            {
                return false;
            }

            return base.Equals(reference) && DocumentController.Equals(reference.DocumentController);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ DocumentController.GetHashCode();
        }

        public override ReferenceController GetReferenceController()
        {
            return new DocumentReferenceController(DocumentController, FieldKey);
        }
    }
}
