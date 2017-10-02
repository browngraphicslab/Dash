using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Controllers
{
    class DocumentReferenceFieldController : ReferenceFieldModelController
    {
        public string DocumentId { get; set; }

        public DocumentReferenceFieldController(string docId, KeyController key) : base(new DocumentReferenceFieldModel(docId, key.Id), key)
        {
            DocumentId = docId;
        }

        public DocumentReferenceFieldController(DocumentReferenceFieldModel documentReferenceFieldModel) : base(documentReferenceFieldModel, ContentController<KeyModel>.GetController<KeyController>(documentReferenceFieldModel.KeyId))
        {
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
