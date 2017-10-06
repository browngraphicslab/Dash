using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Controllers
{
    class DocumentReferenceFieldController : ReferenceFieldModelController
    {
        public string DocumentId
        {
            get { return (Model as DocumentReferenceFieldModel)?.DocumentId; } 
            set { (Model as DocumentReferenceFieldModel).DocumentId = value; }
        }

        public DocumentReferenceFieldController(string docId, KeyController key) : base(new DocumentReferenceFieldModel(docId, key.Id), key)
        {
            Debug.Assert(docId != null);
            //DocumentId = docId;
        }

        public DocumentReferenceFieldController(DocumentReferenceFieldModel documentReferenceFieldModel) : base(documentReferenceFieldModel, ContentController<KeyModel>.GetController<KeyController>(documentReferenceFieldModel.KeyId))
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
        }

        public override FieldModelController<ReferenceFieldModel> Copy()
        {
            return new DocumentReferenceFieldController(DocumentId, FieldKey);
        }

        public override DocumentController GetDocumentController(Context context)
        {
            return ContentController<DocumentModel>.GetController<DocumentController>(DocumentId);
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
