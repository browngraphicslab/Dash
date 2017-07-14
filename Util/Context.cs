using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class Context
    {
        private readonly List<DocumentController> _documentContextList;

        private readonly Dictionary<ReferenceFieldModelController, FieldModelController> _data;

        public List<DocumentController> DocContextList { get { return _documentContextList; } }
        public Context()
        {
            _documentContextList = new List<DocumentController>();
            _data = new Dictionary<ReferenceFieldModelController, FieldModelController>();
        }

        public Context(Context copyFrom)
        {
            _documentContextList = new List<DocumentController>(copyFrom._documentContextList);
            _data = new Dictionary<ReferenceFieldModelController, FieldModelController>(copyFrom._data);
        }

        public void AddDocumentContext(DocumentController document)
        {
            _documentContextList.Add(document);
        }

        public void AddData(ReferenceFieldModelController reference, FieldModelController data)
        {
            _data[reference] = data;
        }

        public bool TryDereferenceToRoot(ReferenceFieldModelController reference, out FieldModelController data)
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
            foreach (var doc in _documentContextList)
                if (doc.IsDelegateOf(referenceDocId))
                    referenceDocId = doc.GetId();
            return referenceDocId;
        }

        public void PrintContextList()
        {
            Debug.WriteLine(string.Join(", ",_documentContextList.Select(dc => dc.GetId())));
        }
    }
}
