using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash.Models;
using DashShared;

namespace Dash
{
    public class DocumentReferenceController : ReferenceFieldModelController
    {
        public string DocId => DocumentReferenceFieldModel.DocId;

        public DocumentReferenceController(string docId, Key key) : base(new DocumentReferenceFieldModel(docId, key))
        {
        }

        public DocumentReferenceFieldModel DocumentReferenceFieldModel => FieldModel as DocumentReferenceFieldModel;

        public override DocumentController GetDocumentController()
        {
            return ContentController.GetController<DocumentController>(DocId);
        }
    }
}
