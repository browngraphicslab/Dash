using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;

namespace Dash
{
    public class DocumentCollectionFieldModelController : FieldModelController
    {
        public delegate void DocumentsChangedHandler(IEnumerable<DocumentController> currentDocuments);

        public event DocumentsChangedHandler OnDocumentsChanged;

        /// <summary>
        /// Key for collection data
        /// TODO This might be better in a different class
        /// </summary>
        public static Key CollectionKey = new Key("7AE0CB96-7EF0-4A3E-AFC8-0700BB553CE2", "Collection");


        /// <summary>
        ///     A wrapper for <see cref="DocumentCollectionFieldModel.Data" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        private List<DocumentController> _documents;

        public DocumentCollectionFieldModelController(IEnumerable<DocumentController> documents) :base(new DocumentCollectionFieldModel(documents.Select(doc => doc.DocumentModel.Id)))
        {
            _documents = documents.ToList();
        }

        /// <summary>
        ///     The <see cref="DocumentCollectionFieldModel" /> associated with this
        ///     <see cref="DocumentCollectionFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public DocumentCollectionFieldModel DocumentCollectionFieldModel => FieldModel as DocumentCollectionFieldModel;

        public override TypeInfo TypeInfo => TypeInfo.Collection;

        /// <summary>
        /// Adds a single document to the collection.
        /// </summary>
        /// <param name="docController"></param>
        public void AddDocument(DocumentController docController)
        {
            _documents.Add(docController);
            DocumentCollectionFieldModel.Data = _documents.Select(d => d.GetId());

            FireFieldModelUpdated();
            OnDocumentsChanged?.Invoke(GetDocuments());
        }


        public void RemoveDocument(DocumentController doc) {
            _documents.Remove(doc);
            DocumentCollectionFieldModel.Data = _documents.Select(d => d.GetId());
            FireFieldModelUpdated();
        }

        public void SetDocuments(List<DocumentController> docControllers)
        {
            _documents = docControllers;
            DocumentCollectionFieldModel.Data = _documents.Select(d => d.GetId());

            FireFieldModelUpdated();
            OnDocumentsChanged?.Invoke(GetDocuments());

        }

        /// <summary>
        /// YOU CANNOT ADD DOCUMENTS TO THIS LIST
        /// </summary>
        /// <returns></returns>
        public List<DocumentController> GetDocuments()
        {
            // since we want people to set the documents through methods lets just pass a copy of the model's list
            return _documents.ToList();
        }

        protected override void UpdateValue(FieldModelController fieldModel)
        {
            SetDocuments((fieldModel as DocumentCollectionFieldModelController)._documents);
        }

        public override FrameworkElement GetTableCellView()
        {
            return GetTableCellViewOfScrollableText(BindTextOrSetOnce);
        }

        public override FieldModelController GetDefaultController()
        {
            return new DocumentCollectionFieldModelController(new List<DocumentController>());
        }

        private void BindTextOrSetOnce(TextBlock textBlock)
        {
            textBlock.Text = "A Collection of Documents";
        }


        public override FieldModelController Copy()
        {
            return new DocumentCollectionFieldModelController(new List<DocumentController>(_documents));
        }
    }
}