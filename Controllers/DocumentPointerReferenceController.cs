using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash.Models;
using DashShared;

namespace Dash
{
    public class DocumentPointerReferenceController : ReferenceFieldModelController
    {
        public ReferenceFieldModelController DocReference { get; }

        public DocumentPointerReferenceController(ReferenceFieldModelController docReference, Key key) : base(new DocumentPointerReferenceFieldModel(docReference.ReferenceFieldModel, key))
        {
            DocReference = docReference;
        }

        public override DocumentController GetDocumentController()
        {
            return DocReference.DereferenceToRoot<DocumentFieldModelController>()?.Data;
        }

        public DocumentPointerReferenceFieldModel DocumentPointerReferenceFieldModel => FieldModel as DocumentPointerReferenceFieldModel;
    }
}
