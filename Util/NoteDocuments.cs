﻿using DashShared;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Dash.Controllers;
using System;

namespace Dash
{
    public abstract class NoteDocument {
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
            return new DocumentReferenceController(dataDoc.Id, KeyStore.DataKey);
        }

        /// <summary>
        /// Makes a delegate of the note classes generic prototype data object and assigns its Data to the passed controller
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        protected DocumentController makeDataDelegate(FieldControllerBase controller)
        {
            var dataDocument = _prototype.MakeDelegate();
            var mapping = new Dictionary<FieldControllerBase, FieldControllerBase>();
            mapping.Add(_prototype, dataDocument);
            dataDocument.MapDocuments(mapping);
            dataDocument.SetField(KeyStore.DataKey, controller, true);
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
                dataDocument.SetField(KeyStore.TitleKey, new TextController(title), true);
            layout.SetField(KeyStore.DocumentContextKey, dataDocument, true);
            layout.SetField(KeyStore.DataKey,  new PointerReferenceController(new DocumentReferenceController(layout.Id, KeyStore.DocumentContextKey), KeyStore.DataKey), true);
            // layout.SetField(KeyStore.DataKey,  new DocumentReferenceController(dataDocument.Id, KeyStore.DataKey), true);
            layout.SetField(KeyStore.TitleKey, new DocumentReferenceController(dataDocument.Id, KeyStore.TitleKey), true);
            return layout;
        }
    }
}
