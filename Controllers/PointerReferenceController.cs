using System;
using DashShared;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dash.Controllers.Operators;

namespace Dash
{
    public class PointerReferenceController : ReferenceController
    {
        public ReferenceController DocumentReference { get; private set; }

        public PointerReferenceController(ReferenceController documentReference, KeyController key) : base(new PointerReferenceModel(documentReference.Id, key.Id))
        {
            FieldKey = key;
            DocumentReference = documentReference;
            DocumentReference.FieldModelUpdated += DocumentReferenceOnFieldModelUpdated;
            DocumentChanged();
            SaveOnServer();
        }

        public static PointerReferenceController CreateFromServer(PointerReferenceModel model)
        {
            var prc = new PointerReferenceController(model);
            return prc;
        }

        private PointerReferenceController(PointerReferenceModel pointerReferenceFieldModel) : base(pointerReferenceFieldModel)
        {
            _initialized = false;
        }

        private bool _initialized = true;
        public override async Task InitializeAsync()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            DocumentReference = await RESTClient.Instance.Fields.GetControllerAsync<ReferenceController>(
                    (Model as PointerReferenceModel).ReferenceFieldModelId);
            await base.InitializeAsync();
            DocumentReference.FieldModelUpdated += DocumentReferenceOnFieldModelUpdated;
            DocumentChanged();
        }

        private void DocumentReferenceOnFieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            DocumentChanged();
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return DSL.GetFuncName<PointerReferenceOperator>() +
                   $"({DocumentReference.ToScriptString(thisDoc)}, {FieldKey.ToScriptString(thisDoc)})";
        }

        public override FieldControllerBase Copy() => new PointerReferenceController(DocumentReference.Copy() as ReferenceController, FieldKey);

        public override DocumentController GetDocumentController(Context context) => DocumentReference?.DereferenceToRoot<DocumentController>(context);

        public override FieldReference GetFieldReference() => new DocumentPointerFieldReference(DocumentReference.GetFieldReference(), FieldKey);

        public override FieldControllerBase GetDocumentReference() => DocumentReference;

        public override string ToString() => $"pRef({DocumentReference}, {FieldKey})";

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
