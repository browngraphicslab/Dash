using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Dash.Converters;
using Windows.UI.Xaml.Data;
using static Dash.DocumentController;
using System.Diagnostics;

namespace Dash
{
    public class DocumentCollectionFieldModelController : FieldModelController
    {
        public class CollectionFieldUpdatedEventArgs : FieldUpdatedEventArgs
        {
            public enum CollectionChangedAction
            {
                Add,
                Remove,
                Replace,
                Clear
            }

            public readonly CollectionChangedAction CollectionAction;
            public readonly List<DocumentController> ChangedDocuments;

            private CollectionFieldUpdatedEventArgs() : base(TypeInfo.Collection, DocumentController.FieldUpdatedAction.Update)
            {
            }

            public CollectionFieldUpdatedEventArgs(CollectionChangedAction action) : this()
            {
                if (action != CollectionChangedAction.Clear)
                {
                    throw new ArgumentException();
                }
                CollectionAction = action;
                ChangedDocuments = null;
            }
            public CollectionFieldUpdatedEventArgs(CollectionChangedAction action, List<DocumentController> changedDocuments) : this()
            {
                CollectionAction = action;
                ChangedDocuments = changedDocuments;
            }
        }

        /// <summary>
        /// Key for collection data
        /// TODO This might be better in a different class
        /// </summary>
        public static KeyController CollectionKey = new KeyController("7AE0CB96-7EF0-4A3E-AFC8-0700BB553CE2", "Collection");
        public override object GetValue(Context context)
        {
            return GetDocuments();
        }
        public override bool SetValue(object value)
        {
            if (!(value is List<DocumentController>))
                return false;

            SetDocuments(value as List<DocumentController>);
            return true;
        }
        public List<DocumentController> Data
        {
            get { return _documents; }
            set
            {
                if (_documents != null)
                    foreach (var docController in _documents)
                        docController.DocumentFieldUpdated -= ContainedDocumentFieldUpdated;
                foreach (var docController in value)
                    docController.DocumentFieldUpdated += ContainedDocumentFieldUpdated;
                if (SetProperty(ref _documents, value))
                {
                    OnFieldModelUpdated(null);
                    // update local
                    // update server
                }
            }
        }

        /// <summary>
        ///     A wrapper for <see cref="DocumentCollectionFieldModel.Data" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        private List<DocumentController> _documents;

        public DocumentCollectionFieldModelController() : this(new List<DocumentController>())
        {
        }

        public DocumentCollectionFieldModelController(IEnumerable<DocumentController> documents) : base(new DocumentCollectionFieldModel(documents.Select(doc => doc.DocumentModel.Id)))
        {
            Data = documents.ToList();
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
            if (_documents.Contains(docController))
                return;
            docController.DocumentFieldUpdated += ContainedDocumentFieldUpdated;
            _documents.Add(docController);
            DocumentCollectionFieldModel.Data.Add(docController.GetId());
            OnFieldModelUpdated(new CollectionFieldUpdatedEventArgs(CollectionFieldUpdatedEventArgs.CollectionChangedAction.Add, new List<DocumentController> { docController }));
        }

        public delegate void ContainedDocumentFieldUpdatedHandler(DocumentCollectionFieldModelController collection, DocumentController doucment, DocumentFieldUpdatedEventArgs args);
        public event ContainedDocumentFieldUpdatedHandler ContainedDocumentFieldUpdatedEvent;

        void ContainedDocumentFieldUpdated(DocumentController sender, DocumentFieldUpdatedEventArgs args)
        {
            if (ContainedDocumentFieldUpdatedEvent != null)
                ContainedDocumentFieldUpdatedEvent(this, sender, args);
        }

        public void RemoveDocument(DocumentController doc)
        {
            doc.DocumentFieldUpdated -= ContainedDocumentFieldUpdated;
            var isDocInList = _documents.Remove(doc);
            DocumentCollectionFieldModel.Data.Remove(doc.GetId());
            if (isDocInList)
                OnFieldModelUpdated(new CollectionFieldUpdatedEventArgs(CollectionFieldUpdatedEventArgs.CollectionChangedAction.Remove, new List<DocumentController> { doc }));
        }

        public void SetDocuments(List<DocumentController> docControllers)
        {
            foreach (var docController in Data)
                docController.DocumentFieldUpdated -= ContainedDocumentFieldUpdated;

            _documents = new List<DocumentController>(docControllers);
            DocumentCollectionFieldModel.Data = _documents.Select(d => d.GetId()).ToList();

            foreach (var docController in Data)
                docController.DocumentFieldUpdated += ContainedDocumentFieldUpdated;

            OnFieldModelUpdated(new CollectionFieldUpdatedEventArgs(CollectionFieldUpdatedEventArgs.CollectionChangedAction.Replace, new List<DocumentController>(docControllers)));
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

        public override FieldModelController GetDefaultController()
        {
            return new DocumentCollectionFieldModelController(new List<DocumentController>());
        }
        
        public override FieldModelController Copy()
        {
            return new DocumentCollectionFieldModelController(new List<DocumentController>(_documents));
        }
    }
}