using DashShared;
using System;
using System.Collections.Generic;

namespace Dash.Controllers
{
    class PointerReferenceController : ReferenceController
    {
        public ReferenceController DocumentReference { get; private set; }

        DocumentController _lastDoc = null;
        void fieldUpdatedHandler(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
        {
            DisposeField();
            Init();
        }

        public PointerReferenceController(ReferenceController documentReference, KeyController key) : base(new PointerReferenceModel(documentReference.Id, key.Id))
        {
            SaveOnServer();
            Init();
        }
        public PointerReferenceController(PointerReferenceModel pointerReferenceFieldModel) : base(pointerReferenceFieldModel)
        {
        }

        public override void Init()
        {
            DocumentReference =
                ContentController<FieldModel>.GetController<ReferenceController>(
                    (Model as PointerReferenceModel).ReferenceFieldModelId);
            DocumentReference?.Init();
            base.Init();
            _lastDoc = DocumentReference?.GetDocumentController(null);
           _lastDoc?.AddFieldUpdatedListener(DocumentReference.FieldKey, fieldUpdatedHandler);
        }

        public override void DisposeField()
        {
             base.DisposeField();
            _lastDoc.RemoveFieldUpdatedListener(DocumentReference.FieldKey, fieldUpdatedHandler);
        }

        public override FieldControllerBase Copy()
        {
            return new PointerReferenceController(DocumentReference.Copy() as ReferenceController, FieldKey);
        }

        public override DocumentController GetDocumentController(Context context)
        {
            return DocumentReference?.DereferenceToRoot<DocumentController>(context);
        }

        public override FieldReference GetFieldReference()
        {
            return new DocumentPointerFieldReference(DocumentReference.GetFieldReference(), FieldKey);
        }

        public override FieldControllerBase CopyIfMapped(Dictionary<FieldControllerBase, FieldControllerBase> mapping)
        {
            if (mapping.ContainsKey(DocumentReference.GetDocumentController(null)))
            {
                return new PointerReferenceController(new DocumentReferenceController(mapping[DocumentReference.GetDocumentController(null)] as DocumentController, DocumentReference.FieldKey), FieldKey);
            }
            return null;
        }
    }
}
