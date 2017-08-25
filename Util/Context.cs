﻿using System;
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

        public HashSet<DocumentController> DocContextList { get { return _documentContextList; } }

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

        public bool IsApplicableTo(DocumentController doc)
        {
            var prototype = GetDeepestDelegateOf((doc.GetPrototype() ?? doc).GetId());
            return DocContextList.Contains(doc) || // document is explicitly in the context 
                   (prototype == doc.GetId() ||  doc.IsDelegateOf(prototype)); // document is equal to or a delegate of the deepest delegate of its prototype in the context
         }

        public bool IsContextCompatible(Context context)
        {
            bool valid2 = context.DocContextList.Where((doc) => !IsApplicableTo(doc)).Count() == 0;// see if context is valid for every document in the context
            
            if (valid2)
                foreach (var dcb in DocContextList) // if it is, then everything in the context must ...??
                {
                    var prototype = dcb.GetPrototype() ?? dcb;
                    if (!context.DocContextList.Contains(dcb) && context.GetDeepestDelegateOf(prototype.GetId()) != dcb.GetId())
                        valid2 = false;
                }
            return valid2;
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

        public string GetDeepestDelegateOf(string referenceDocId)
        {
            foreach (var doc in _documentContextList.Reverse())
                if (doc.IsDelegateOf(referenceDocId))
                    referenceDocId = doc.GetId();
            return referenceDocId;
        }

        public static Context SafeInit(Context context)
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
