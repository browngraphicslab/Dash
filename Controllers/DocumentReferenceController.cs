using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class
        DocumentReferenceController : ReferenceController
    {
        private DocumentController _documentController;

        public DocumentController DocumentController
        {
            get => _documentController;
            set
            {
                _documentController = value;
                (Model as DocumentReferenceModel).DocumentId = value.Id;
                UpdateOnServer(null);//TODO Add FieldUpdate and undo
            }
        }

        public DocumentReferenceController(DocumentController doc, KeyController key, bool copyOnWrite = false) : base(new DocumentReferenceModel(doc.Id, key.Id, copyOnWrite))
        {
            Debug.Assert(doc != null);
            Debug.Assert(key != null);
            _documentController = doc;
            FieldKey = key;
            SaveOnServer();
        }

        public static DocumentReferenceController CreateFromServer(DocumentReferenceModel model)
        {
            DocumentReferenceController drc = new DocumentReferenceController(model);
            return drc;
        }
        private DocumentReferenceController(DocumentReferenceModel documentReferenceFieldModel) : base(documentReferenceFieldModel)
        {
            Debug.Assert(documentReferenceFieldModel?.DocumentId != null);
        }

        public override async Task InitializeAsync()
        {
            _documentController = await RESTClient.Instance.Fields.GetControllerAsync<DocumentController>((Model as DocumentReferenceModel).DocumentId);
            await base.InitializeAsync();
        }

        public void ChangeFieldDoc(DocumentController doc, bool withUndo = true)
        {
            DocumentController oldDoc = DocumentController;
            UndoCommand newEvent = new UndoCommand(() => ChangeFieldDoc(DocumentController, false), () => ChangeFieldDoc(oldDoc, false));

            //docController for old DocumentId
            var docController = GetDocumentController(null);
            docController.RemoveFieldUpdatedListener(FieldKey, DocFieldUpdated);
            DocumentController = doc;
            //docController for given DocumentId
            var docController2 = GetDocumentController(null);
            docController2.AddFieldUpdatedListener(FieldKey, DocFieldUpdated);

            UpdateOnServer(withUndo ? newEvent : null);
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


        public override string ToString() => $"dRef[{DocumentController}, {FieldKey}]";

        public override FieldControllerBase GetDocumentReference() => DocumentController;

        public override FieldControllerBase CopyIfMapped(Dictionary<FieldControllerBase, FieldControllerBase> mapping)
        {
            if (mapping.ContainsKey(GetDocumentController(null)))
            {
                return new DocumentReferenceController(mapping[GetDocumentController(null)] as DocumentController, FieldKey);
            }
            return null;
        }
    }
}
