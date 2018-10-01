using DashShared;
using System.Collections.Generic;
using Dash.Controllers.Operators;
using Microsoft.Office.Interop.Word;

namespace Dash
{
    public class PointerReferenceController : ReferenceController
    {
        public ReferenceController DocumentReference { get; private set; }

        DocumentController _lastDoc = null;
        void fieldUpdatedHandler(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
        {
            DisposeField();
            Init();
            DocFieldUpdated(null, args, context);
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
            base.Init();
            _lastDoc = DocumentReference?.GetDocumentController(null);
           _lastDoc?.AddFieldUpdatedListener(DocumentReference.FieldKey, fieldUpdatedHandler);
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return DSL.GetFuncName<PointerReferenceOperator>() +
                   $"({DocumentReference.ToScriptString(thisDoc)}, {FieldKey.ToScriptString(thisDoc)})";
        }

        public override void DisposeField()
        {
             base.DisposeField();
            _lastDoc.RemoveFieldUpdatedListener(DocumentReference.FieldKey, fieldUpdatedHandler);
        }

        public override FieldControllerBase Copy() => new PointerReferenceController(DocumentReference.Copy() as ReferenceController, FieldKey);

        public override DocumentController GetDocumentController(Context context) => DocumentReference?.DereferenceToRoot<DocumentController>(context);

        public override FieldReference GetFieldReference() => new DocumentPointerFieldReference(DocumentReference.GetFieldReference(), FieldKey);

        public override FieldControllerBase GetDocumentReference() => DocumentReference;

        public override string ToString() => $"pRef[{DocumentReference}, {FieldKey}]";

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
