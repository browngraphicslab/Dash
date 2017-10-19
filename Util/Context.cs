using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class Context
    {
        private readonly HashSet<DocumentController> _documentContextList;

        private readonly Dictionary<FieldReference, FieldModelController> _data;

        public HashSet<DocumentController> DocContextList => _documentContextList;

        /// <summary>
        /// Create a new context with no initial values
        /// </summary>
        public Context()
        {
            _documentContextList = new HashSet<DocumentController>();
            _data = new Dictionary<FieldReference, FieldModelController>();
        }

        public Context(DocumentController initialContext)
        {
            _documentContextList = new HashSet<DocumentController>{initialContext};
            _data = new Dictionary<FieldReference, FieldModelController>();
        }

        public Context(Context copyFrom)
        {
            if (copyFrom == null)
            {
                _documentContextList = new HashSet<DocumentController>();
                _data = new Dictionary<FieldReference, FieldModelController>();
            }
            else
            {
                _documentContextList = new HashSet<DocumentController>(copyFrom._documentContextList);
                _data = new Dictionary<FieldReference, FieldModelController>(copyFrom._data);
            }
        }

        /// <summary>
        /// Tests if the deepest delegate of the base prototype of the document is an ancestor the document.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public bool ContainsAncestorOf(DocumentController doc)
        {
            var deepestRelative = GetDeepestDelegateOf(doc.GetAllPrototypes().First().GetId());
            return doc.IsDelegateOf(deepestRelative); 
        }

        /// <summary>
        /// Loops through every document in the set:
        ///    if the document has no delegates within the set, then
        ///        returns false if the deepest relative of the document in the context is a prototype or delegate of the document
        /// </summary>
        /// <param name="docSet"></param>
        /// <returns></returns>
        public bool IsCompatibleWith(HashSet<DocumentController> docSet)
        {
            var docSetList = new List<DocumentController>(docSet);
            for (int i = 0; i < docSetList.Count; i++)
            {
                var dcb = docSetList[i];
                var dcbPrototype = dcb.GetAllPrototypes().First();
                bool skip = false;
                for (int j = i+1; j < docSet.Count && !skip; j++)
                    if (docSetList[j].GetAllPrototypes().First().Equals(dcbPrototype))
                    {
                        skip = true;
                    }
                if (!skip)
                {
                    var deepestRelative = GetDeepestDelegateOf(dcbPrototype.GetId());
                    if (deepestRelative != null && deepestRelative != dcb.GetId())
                        return false;
                }
            }
            return true;
        }

        public void AddDocumentContext(DocumentController document)
        {
            _documentContextList.Add(document);
        }

        public void AddData(ReferenceFieldModelController reference, FieldModelController data)
        {
            _data[reference.FieldReference] = data;
        }

        public void AddData(FieldReference reference, FieldModelController data)
        {
            _data[reference] = data;
        }

        public bool ContainsDataKey(KeyController key)
        {
            foreach (var d in _data)
                if (d.Key.FieldKey == key)
                    return true;
            return false;
        }

        public bool TryDereferenceToRoot(FieldReference reference, out FieldModelController data)
        {
            if (_data.ContainsKey(reference))
            {
                data = _data[reference];
                return true;
            }
            data = null;
            return false;
        }

        /// <summary>
        /// Returns the id of the deepest delegate of the document associated with the passed in id.
        /// Returns the passed in id if there is no deeper delegate
        /// </summary>
        /// <param name="referenceDocId"></param>
        /// <returns></returns>
        public string GetDeepestDelegateOf(string referenceDocId)
        {
            Debug.Assert(ContentController.GetController<DocumentController>(referenceDocId) != null, "the passed in documentId is not actually associated with any document in the system!");

            // flag to say if we found a delegate
            var found = false;    
            foreach (var doc in _documentContextList)
            {
                // set the flag if we find a delegate or the document with the passed in id
                if (doc.GetId() == referenceDocId || doc.IsDelegateOf(referenceDocId))
                {
                    found = true;
                    referenceDocId = doc.GetId();
                }
            }

            return found ? referenceDocId : null;
        }

        /// <summary>
        /// Initializes a new context if the passed in <paramref name="context"/> is not null.
        /// otherwise returns the passed in <paramref name="context"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Context InitIfNotNull(Context context)
        {
            return context ?? new Context();
        }

        public static Context SafeInitAndAddDocument(Context context, DocumentController doc)
        {
            var newcontext = new Context(context);
            newcontext.AddDocumentContext(doc);
            return newcontext;
        }
    }
}
