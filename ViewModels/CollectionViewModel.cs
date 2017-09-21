using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DashShared;
using Windows.Foundation;
using Visibility = Windows.UI.Xaml.Visibility;
using System.Linq;

namespace Dash
{
    public class CollectionViewModel : BaseCollectionViewModel
    {
        private DocumentCollectionFieldModelController _collectionFieldModelController;

        public InkFieldModelController InkFieldModelController;

        public CollectionViewModel(FieldModelController collection = null, bool isInInterfaceBuilder = false, Context context = null) : base(isInInterfaceBuilder)
        {
            if (collection == null) return;
            _collectionFieldModelController = collection.DereferenceToRoot<DocumentCollectionFieldModelController>(context);
            AddViewModels(_collectionFieldModelController.Data, context);

            var copiedContext = new Context(context);

            if (collection is ReferenceFieldModelController)
            {
                var reference = collection as ReferenceFieldModelController;
                _collectionKey = reference.FieldKey;
                reference.GetDocumentController(context).AddFieldUpdatedListener(reference.FieldKey,
                    delegate (DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
                    {
                        var cargs = args.FieldArgs as DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs;
                        if (cargs != null && args.Action == DocumentController.FieldUpdatedAction.Update)
                        {
                            UpdateViewModels(cargs, copiedContext);
                        }
                        else
                        {
                            _collectionFieldModelController = args.NewValue.DereferenceToRoot<DocumentCollectionFieldModelController>(args.Context);
                            if (_collectionFieldModelController == null) return;
                            var documents = _collectionFieldModelController.GetDocuments();
                            bool newDoc = DocumentViewModels.Count != documents.Count;
                            if (!newDoc)
                                foreach (var d in DocumentViewModels.Select((v) => v.DocumentController))
                                    if (!documents.Contains(d))
                                    {
                                        newDoc = true;
                                        break;
                                    }
                            if (newDoc)
                            {
                                if (cargs == null)
                                    cargs = new DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs(DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs.CollectionChangedAction.Add, documents);
                                UpdateViewModels(cargs, copiedContext);
                            }
                        }
                    });
            }
            else
            {
                collection.FieldModelUpdated += delegate (FieldModelController sender, FieldUpdatedEventArgs args, Context context1)
                {
                    UpdateViewModels(args as DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs,
                        copiedContext);
                };
            }
            CellSize = 250; // TODO figure out where this should be set
        }

        private KeyController _collectionKey = null;
        public override KeyController CollectionKey => _collectionKey ?? base.CollectionKey;


        #region Event Handlers

        /// <summary>
        /// Deletes all of the Documents selected in the CollectionView by removing their DocumentViewModels from the data binding source. 
        /// **Note that this removes the DocumentModel as well, and any other associated DocumentViewModels.
        /// </summary>
        /// <param name="sender">The "Delete" menu option</param>
        /// <param name="e"></param>
        public void DeleteSelected_Tapped()
        {
            var itemsToDelete = SelectionGroup.ToList();
            SelectionGroup.Clear();
            foreach (var vmp in itemsToDelete)
            {
                _collectionFieldModelController.RemoveDocument(vmp.DocumentController);
            }
        }

        #endregion

        #region DocumentModel and DocumentViewModel Data Changes

        private void UpdateViewModels(DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs args, Context c)
        {
            switch (args.CollectionAction)
            {
                case DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs.CollectionChangedAction.Add:
                    AddViewModels(args.ChangedDocuments, c);
                    break;
                case DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs.CollectionChangedAction.Clear:
                    DocumentViewModels.Clear();
                    break;
                case DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs.CollectionChangedAction.Remove:
                    RemoveViewModels(args.ChangedDocuments);
                    break;
                case DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs.CollectionChangedAction.Replace:
                    DocumentViewModels.Clear();
                    AddDocuments(args.ChangedDocuments, c);
                    break;
            }
        }

        private void AddViewModels(List<DocumentController> documents, Context c)
        {
            foreach (var documentController in documents)
            {
                DocumentViewModels.Add(new DocumentViewModel(documentController, IsInInterfaceBuilder, c));
            }
        }

        private void RemoveViewModels(List<DocumentController> documents)
        {
            var ids = documents.Select(doc => doc.GetId());
            var vms = DocumentViewModels.Where(vm => ids.Contains(vm.DocumentController.GetId())).ToList();
            foreach (var vm in vms)
            {
                DocumentViewModels.Remove(vm);
            }
        }

        public override void AddDocuments(List<DocumentController> documents, Context context)
        {
            foreach (var doc in documents)
            {
                AddDocument(doc, context);
            }
        }

        public override void AddDocument(DocumentController doc, Context context)
        {
            if (doc.DocumentType == DashConstants.DocumentTypeStore.CollectionDocument)
            {
                var coll = doc.GetDereferencedField<DocumentCollectionFieldModelController>(CollectionKey, context);
                if (coll.Data.Contains(doc))
                    return;
            }

            if (context != null && context.DocContextList.Contains(doc))
            {
                return;
            }

            // just update the collection, the colllection will update our view automatically
            _collectionFieldModelController.AddDocument(doc);
        }

        public override void RemoveDocuments(List<DocumentController> documents)
        {
            foreach (var doc in documents)
            {
                RemoveDocument(doc);
            }
        }

        public override void RemoveDocument(DocumentController document)
        {
            // just update the collection, the colllection will update our view automatically
            _collectionFieldModelController.RemoveDocument(document);
        }

        #endregion
    }
}
