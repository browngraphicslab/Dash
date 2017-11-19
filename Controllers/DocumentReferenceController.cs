﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using DashShared.Models;

namespace Dash
{
    public class DocumentReferenceController : ReferenceController
    {
        public string DocumentId
        {
            get { return (Model as DocumentReferenceModel)?.DocumentId; } 
            set { (Model as DocumentReferenceModel).DocumentId = value; }
        }

        public DocumentReferenceController(string docId, KeyController key) : base(new DocumentReferenceModel(docId, key.Id))
        {
            Debug.Assert(docId != null);
            Debug.Assert(key != null);
            //DocumentId = docId;
            Init();
        }

        public DocumentReferenceController(DocumentReferenceModel documentReferenceFieldModel) : base(documentReferenceFieldModel)
        {
            Debug.Assert(documentReferenceFieldModel?.DocumentId != null);
            Debug.Assert(DocumentId != null);
        }

        public void ChangeFieldDoc(string docId)
        {
            var docController = GetDocumentController(null);
            docController.RemoveFieldUpdatedListener(FieldKey, DocFieldUpdated);
                DocumentId = docId;
            var docController2 = GetDocumentController(null);
            docController2.AddFieldUpdatedListener(FieldKey, DocFieldUpdated);
            UpdateOnServer();
        }

        public override FieldModelController<ReferenceModel> Copy()
        {
            return new DocumentReferenceController(DocumentId, FieldKey);
        }

        public override DocumentController GetDocumentController(Context context)
        {
            var deepestDelegateID = context?.GetDeepestDelegateOf(DocumentId) ?? DocumentId;
            return ContentController<FieldModel>.GetController<DocumentController>(deepestDelegateID);
        }

        public override FieldReference GetFieldReference()
        {
            return new DocumentFieldReference(DocumentId, FieldKey);
        }

        public override string GetDocumentId(Context context)
        {
            return DocumentId;
        }
    }
}
