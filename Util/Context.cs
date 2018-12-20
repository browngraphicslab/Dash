using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Dash
{
    public class Context
    {
        private readonly LinkedList<DocumentController> _documentContextList;

        public LinkedList<DocumentController> DocContextList => _documentContextList;
        
        public Context(DocumentController initialContext)
        {
            _documentContextList = new LinkedList<DocumentController>();
            _documentContextList.AddLast(initialContext);
        }

        public Context(Context copyFrom)
        {
            _documentContextList = new LinkedList<DocumentController>(copyFrom?._documentContextList.ToArray() ?? new DocumentController[] { });
        }

        /// <summary>
        /// Tests if the deepest delegate of the base prototype of the document is an ancestor the document.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public bool ContainsAncestorOf(DocumentController doc)
        {
            if (DocContextList.Contains(doc))
            {
                return true;
            }
            var deepestRelative = GetDeepestDelegateOf(doc.GetAllPrototypes().First());
            return doc.IsDelegateOf(deepestRelative); 
        }
        

        public FieldControllerBase Dereference(ReferenceController reference)
        {
            return reference.GetFieldReference().Dereference(this);
        }

        public FieldControllerBase DereferenceToRoot(ReferenceController reference)
        {
            return reference.GetFieldReference().DereferenceToRoot(this);
        }

        public void AddDocumentContext(DocumentController document)
        {
            Debug.Assert(document != null);
            _documentContextList.AddFirst(document);
        }

        /// <summary>
        /// Returns the id of the deepest delegate of the document associated with the passed in id.
        /// Returns the passed in id if there is no deeper delegate
        /// </summary>
        /// <param name="referenceDoc"></param>
        /// <returns></returns>
        public DocumentController GetDeepestDelegateOf(DocumentController referenceDoc)
        {
            // flag to say if we found a delegate
            var found = false;    
            foreach (var doc in _documentContextList)
            {
                // set the flag if we find a delegate or the document with the passed in id
                if (doc.Equals(referenceDoc) || doc.IsDelegateOf(referenceDoc))
                {
                    found = true;
                    referenceDoc = doc;
                }
            }

            return found ? referenceDoc : null;
        }
    }
}
