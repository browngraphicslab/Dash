﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using DashShared;

namespace Dash
{
    public class DocumentReferenceController : ReferenceController
    {
        private DocumentController _documentController;

        public DocumentController DocumentController
        {
            get => _documentController;
            set
            {
                var oldDoc = _documentController;
                _documentController = value;
                ((DocumentReferenceModel) Model).DocumentId = value.Id;
                UndoCommand command = new UndoCommand(() => DocumentController = value,
                    () => DocumentController = oldDoc);
                ReferenceField(value);
                UpdateOnServer(command);
                ReleaseField(oldDoc);
                DocumentChanged();
            }
        }

        public DocumentReferenceController(DocumentController doc, KeyController key, bool copyOnWrite = false) : base(new DocumentReferenceModel(doc.Id, key.Id, copyOnWrite))
        {
            Debug.Assert(doc != null);
            Debug.Assert(key != null);
            _documentController = doc;
            FieldKey = key;
        }

        public static DocumentReferenceController CreateFromServer(DocumentReferenceModel model)
        {
            var drc = new DocumentReferenceController(model);
            return drc;
        }
        private DocumentReferenceController(DocumentReferenceModel documentReferenceFieldModel) : base(documentReferenceFieldModel)
        {
            _initialized = false;
            Debug.Assert(documentReferenceFieldModel?.DocumentId != null);
        }

        private bool _initialized = true;
        public override async Task InitializeAsync()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            await base.InitializeAsync();
            _documentController = await RESTClient.Instance.Fields.GetControllerAsync<DocumentController>((Model as DocumentReferenceModel).DocumentId);
            Debug.Assert(_documentController != null);
        }

        public override FieldControllerBase Copy()
        {
            return new DocumentReferenceController(DocumentController, FieldKey);
        }

        public override DocumentController GetDocumentController(Context context)
        {
            var deepestDelegate = context?.GetDeepestDelegateOf(DocumentController) ?? DocumentController;
            return deepestDelegate;
        }

        public override FieldReference GetFieldReference()
        {
            return new DocumentFieldReference(DocumentController, FieldKey);
        }

        public override string ToString() => $"dRef({DocumentController}, {FieldKey})";

        public override FieldControllerBase GetDocumentReference() => DocumentController;

        public override FieldControllerBase CopyIfMapped(Dictionary<FieldControllerBase, FieldControllerBase> mapping)
        {
            if (mapping.ContainsKey(GetDocumentController(null)))
            {
                return new DocumentReferenceController(mapping[GetDocumentController(null)] as DocumentController, FieldKey);
            }
            return null;
        }

        public override TypeInfo TypeInfo => TypeInfo.DocumentReference;

        public override string ToScriptString(DocumentController thisDoc)
        {
            var funcString = DSL.GetFuncName<DocumentReferenceOperator>();
            string DocAndKeyToString(DocumentController doc, KeyController key)
            {
                return funcString + $"({doc.ToScriptString(thisDoc)}, {key.ToScriptString(thisDoc)})";
            }
            var ops = DocumentController.GetField<ListController<OperatorController>>(KeyStore.OperatorKey);
            if (ops != null)
            {
                var op = ops.FirstOrDefault(o => o.Outputs.ContainsKey(FieldKey));
                if (op != null)
                {
                    //return DSL.GetFuncName(op) + "(" + string.Join(", ", op.Inputs.Select(kvp => DocAndKeyToString(DocumentController, kvp.Key))) + ")";
                    return DSL.GetFuncName(op) + "(" + string.Join(", ", op.Inputs.Select(kvp => $"this.{kvp.Key.Name}")) + ")";
                }
            }
            if (thisDoc == DocumentController)
            {
                return $"this.{FieldKey}";
            }

            return DocAndKeyToString(DocumentController, FieldKey);
        }

        protected override IEnumerable<FieldControllerBase> GetReferencedFields()
        {
            yield return DocumentController;
            yield return FieldKey;
        }

        protected override void RefInit()
        {
            base.RefInit();
            ReferenceField(DocumentController);
        }

        protected override void RefDestroy()
        {
            base.RefDestroy();
            ReleaseField(DocumentController);
        }
    }
}
