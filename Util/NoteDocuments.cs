using DashShared;
using Dash.Controllers;
using System;
using System.Collections.Generic;

namespace Dash
{
    public abstract class NoteDocument
    {
        public DocumentController Document { get; set; }

        protected DocumentController Prototype;
        protected NoteDocument(string prototypeId)
        {
            Prototype = GetPrototype(prototypeId);
        }

        protected DocumentController GetPrototype(string prototypeId)
        {
            if (_prototypeDict.TryGetValue(DocumentType, out var proto))
            {
                return proto;
            }
            proto = RESTClient.Instance.Fields.GetController<DocumentController>(prototypeId);
            if (proto == null)
            {
                proto = createPrototype(prototypeId);
            }
            _prototypeDict[DocumentType] = proto;
            return proto;
        }

        private static Dictionary<DocumentType, DocumentController> _prototypeDict = new Dictionary<DocumentType, DocumentController>();

        protected abstract DocumentType DocumentType { get; }

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
            DocumentController dataDocument = Prototype.MakeDelegate();

            dataDocument.SetField(KeyStore.DataKey, controller, true);
            dataDocument.SetField<DateTimeController>(KeyStore.DateCreatedKey, DateTime.Now, true);
            dataDocument.SetField<DateTimeController>(KeyStore.DateModifiedKey, DateTime.Now, true);
            dataDocument.SetField<TextController>(KeyStore.VisibleTypeKey, dataDocument.DocumentType.Type, true);
            var author = MainPage.Instance.SettingsView.UserName;
            if (author != null)
            {
                dataDocument.SetField<TextController>(KeyStore.AuthorKey, author, true);
            }

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
