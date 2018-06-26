﻿using System.Collections.Generic;
using System.Diagnostics;
using DashShared;

namespace Dash
{
    public class DocumentReferenceController : ReferenceController
    {
        public string DocumentId
        {
            get { return (Model as DocumentReferenceModel)?.DocumentId; } 
            set { (Model as DocumentReferenceModel).DocumentId = value; }
        }

        public DocumentReferenceController(string docId, KeyController key, bool copyOnWrite=false) : base(new DocumentReferenceModel(docId, key.Id, copyOnWrite))
        {
            Debug.Assert(docId != null);
            Debug.Assert(key != null);
            //DocumentId = docId;
            SaveOnServer();
            Init();
        }

        public DocumentReferenceController(DocumentReferenceModel documentReferenceFieldModel) : base(documentReferenceFieldModel)
        {
            Debug.Assert(documentReferenceFieldModel?.DocumentId != null);
            Debug.Assert(DocumentId != null);
        }

        public void ChangeFieldDoc(string docId, bool withUndo = true)
        {
            string oldId = DocumentId;
            UndoCommand newEvent = new UndoCommand(() => ChangeFieldDoc(docId, false), () => ChangeFieldDoc(oldId, false));

            //docController for old DocumentId
            var docController = GetDocumentController(null);
            docController.RemoveFieldUpdatedListener(FieldKey, DocFieldUpdated);
                DocumentId = docId;
            //docController for given DocumentId
            var docController2 = GetDocumentController(null);
            docController2.AddFieldUpdatedListener(FieldKey, DocFieldUpdated);

            UpdateOnServer(withUndo ? newEvent : null);
        }

        public override FieldControllerBase Copy()
        {
            return new DocumentReferenceController(DocumentId, FieldKey);
        }

        public override DocumentController GetDocumentController(Context context)
        {
            var deepestDelegateID = context?.GetDeepestDelegateOf(DocumentId) ?? DocumentId;
            return ContentController<FieldModel>.GetController<DocumentController>(deepestDelegateID);
        }

        public override ReferenceController Resolve(Context c)
        {
            var del = c.GetDeepestDelegateOf(DocumentId);
            if (del == DocumentId)
            {
                return this;
            }
            return new DocumentReferenceController(del, FieldKey);
        }

        // todo: more meaningful tostring here
        public override string ToString()
        {
            return "Reference";
        }

        public override string GetDocumentId(Context context)
        {
            return DocumentId;
        }
        public override FieldControllerBase CopyIfMapped(Dictionary<FieldControllerBase, FieldControllerBase> mapping)
        {
            if (mapping.ContainsKey(GetDocumentController(null)))
            {
                return new DocumentReferenceController(mapping[GetDocumentController(null)].Id, FieldKey);
            }
            return null;
        }
    }
}
