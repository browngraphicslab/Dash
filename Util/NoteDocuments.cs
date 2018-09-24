using DashShared;
using Dash.Controllers;
using System;

namespace Dash
{
    public abstract class NoteDocument
    {
        public DocumentController Document { get; set; }

        public NoteDocument(string prototypeID)
        {
            _prototype = ContentController<FieldModel>.GetController<DocumentController>(prototypeID);
            if (_prototype == null)
            {
                _prototype = createPrototype(prototypeID);
            }
        }
        protected DocumentController _prototype;

        /// <summary>
        /// creates the prototype data document 
        /// </summary>
        /// <param name="prototypeID"></param>
        /// <returns></returns>
        protected abstract DocumentController createPrototype(string prototypeID);

        /// <summary>
        /// Returns a reference to the data document's Data field
        /// </summary>
        /// <param name="dataDoc"></param>
        /// <returns></returns>
        protected DocumentReferenceController getDataReference(DocumentController dataDoc)
        {
            return new DocumentReferenceController(dataDoc, KeyStore.DataKey);
        }

        /// <summary>
        /// Makes a delegate of the note classes generic prototype data object and assigns its Data to the passed controller
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        protected DocumentController makeDataDelegate(FieldControllerBase controller)
        {
            DocumentController dataDocument = _prototype.MakeDelegate();

            dataDocument.SetField(KeyStore.DataKey, controller, true);
            dataDocument.SetField<DateTimeController>(KeyStore.DateCreatedKey, DateTime.Now, true);
            dataDocument.SetField<DateTimeController>(KeyStore.DateModifiedKey, DateTime.Now, true);
            dataDocument.SetField<TextController>(KeyStore.VisibleTypeKey, dataDocument.DocumentType.Type, true);
            dataDocument.SetField<TextController>(KeyStore.AuthorKey, MainPage.Instance.GetSettingsView.UserName, true);

            return dataDocument;
        }

        /// <summary>
        /// Initializes the data fields common to all note documents, including: Title, DocumentContext, and Data
        /// </summary>
        /// <param name="layout"></param>
        /// <param name="dataDocument"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        protected DocumentController initSharedLayout(DocumentController layout, DocumentController dataDocument, string title = null)
        {
            if (!string.IsNullOrEmpty(title))
                dataDocument.SetTitle(title);
            layout.SetField(KeyStore.DocumentContextKey, dataDocument, true);
            var docContextReference = new DocumentReferenceController(layout, KeyStore.DocumentContextKey);
            layout.SetField(KeyStore.DataKey, new PointerReferenceController(docContextReference, KeyStore.DataKey), true);
            layout.SetField(KeyStore.TitleKey, new PointerReferenceController(docContextReference, KeyStore.TitleKey), true);
            return layout;
        }
    }
}
