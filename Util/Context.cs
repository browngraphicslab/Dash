using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DashShared;

namespace Dash
{
    public class Context
    {
        private readonly LinkedList<DocumentController> _documentContextList;

        private readonly Dictionary<FieldReference, FieldControllerBase> _data;

        public LinkedList<DocumentController> DocContextList => _documentContextList;

        /// <summary>
        /// Create a new context with no initial values
        /// </summary>
        public Context()
        {
            _documentContextList = new LinkedList<DocumentController>();
            _data = new Dictionary<FieldReference, FieldControllerBase>();
        }

        public Context(DocumentController initialContext)
        {
            _documentContextList = new LinkedList<DocumentController>();
            _documentContextList.AddLast(initialContext);
            _data = new Dictionary<FieldReference, FieldControllerBase>();
        }

        public Context(Context copyFrom)
        {
            if (copyFrom == null)
            {
                _documentContextList = new LinkedList<DocumentController>();
                _data = new Dictionary<FieldReference, FieldControllerBase>();
            }
            else
            {
                _documentContextList = new LinkedList<DocumentController>(copyFrom._documentContextList);
                _data = new Dictionary<FieldReference, FieldControllerBase>(copyFrom._data);
            }
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

        /// <summary>
        /// Loops through every document in the set:
        ///    if the document has no delegates within the set, then
        ///        returns false if the deepest relative of the document in the context is a prototype or delegate of the document
        /// </summary>
        /// <param name="docSet"></param>
        /// <returns></returns>
        //public bool IsCompatibleWith(Context c)
        //{
        //    if (c == null)
        //    {
        //        return true;
        //    }
        //    var docSetList = new List<DocumentController>(c.DocContextList);
        //    for (int i = 0; i < docSetList.Count; i++)
        //    {
        //        var dcb = docSetList[i];
        //        var dcbPrototype = dcb.GetAllPrototypes().First();
        //        bool skip = false;
        //        for (int j = i+1; j < docSetList.Count && !skip; j++)
        //            if (docSetList[j].GetAllPrototypes().First().Equals(dcbPrototype))
        //            {
        //                skip = true;
        //            }
        //        if (!skip)
        //        {
        //            var deepestRelative = GetDeepestDelegateOf(dcbPrototype.GetId());
        //            if (deepestRelative != null && deepestRelative != dcb.GetId())
        //                return false;
        //        }
        //    }
        //    return true;
        //}

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
            _documentContextList.AddFirst(document);
        }

        //public void AddData(ReferenceFieldModelController reference, FieldControllerBase data)
        //{
        //    _data[reference.FieldReference] = data;
        //}

        public void AddData(FieldReference reference, FieldControllerBase data)
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

        public bool TryDereferenceToRoot(FieldReference reference, out FieldControllerBase data)
        {
            reference = reference.Resolve(this);
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
