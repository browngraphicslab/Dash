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
        private ListController<DocumentController> _collectionFieldModelController;

        public InkController InkFieldModelController;

        public CollectionViewModel(FieldControllerBase collection = null, bool isInInterfaceBuilder = false, Context context = null) : base(isInInterfaceBuilder)
        {
            Debug.Assert(collection != null);
            _collectionFieldModelController = collection.DereferenceToRoot<ListController<DocumentController>>(context);
            AddViewModels(_collectionFieldModelController.TypedData, context);

            var copiedContext = new Context(context);
            OutputKey = KeyStore.CollectionOutputKey;

            if (collection is ReferenceController)
            {
                var reference = collection as ReferenceController;
                _collectionKey = reference.FieldKey;
                reference.GetDocumentController(context).AddFieldUpdatedListener(reference.FieldKey,
                    delegate (FieldControllerBase sender, FieldUpdatedEventArgs args, Context context1)
                    {
                        var dargs = (DocumentController.DocumentFieldUpdatedEventArgs) args;
                        var cargs = dargs.FieldArgs as ListController<DocumentController>.ListFieldUpdatedEventArgs;
                        if (cargs != null && args.Action == DocumentController.FieldUpdatedAction.Update)
                        {
                            UpdateViewModels(cargs, copiedContext);
                        }
                        else
                        {

                            _collectionFieldModelController = dargs.NewValue.DereferenceToRoot<ListController<DocumentController>>(context);
                            if (_collectionFieldModelController == null) return;
                            var documents = _collectionFieldModelController.GetElements();
                            DocumentViewModels.Clear();
                            AddViewModels(documents, context);
                            //TODO tfs: I don't think we actually want to do this...
                            //bool newDoc = DocumentViewModels.Count != documents.Count;
                            //if (!newDoc)
                            //    foreach (var d in DocumentViewModels.Select((v) => v.DocumentController))
                            //        if (!documents.Contains(d))
                            //        {
                            //            newDoc = true;
                            //            break;
                            //        }
                            //if (newDoc)
                            //{
                            //    if (args.Action == DocumentController.FieldUpdatedAction.Update)
                            //        DocumentViewModels.Clear();
                            //    if (cargs == null)
                            //        cargs = new ListController<DocumentController>.CollectionFieldUpdatedEventArgs(ListController<DocumentController>.CollectionFieldUpdatedEventArgs.CollectionChangedAction.Add, documents);
                            //    UpdateViewModels(cargs, copiedContext);
                            //}
                        }
                    });
            }
            else
            {
                collection.FieldModelUpdated += delegate (FieldControllerBase sender, FieldUpdatedEventArgs args, Context context1)
                {
                    UpdateViewModels(args as ListController<DocumentController>.ListFieldUpdatedEventArgs,
                        copiedContext);
                };
            }
            CellSize = 250; // TODO figure out where this should be set
          //  OutputKey = KeyStore.CollectionOutputKey;  // bcz: this wasn't working -- can't assume the collection is backed by a document with a CollectionOutputKey.  
        }

        public KeyController _collectionKey = null; // bcz: hack for now.  need to properly be able to set the output collection key from a collection view
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
                _collectionFieldModelController.Remove(vmp.DocumentController);
            }
        }

        #endregion

        #region DocumentModel and DocumentViewModel Data Changes

        private void UpdateViewModels(ListController<DocumentController>.ListFieldUpdatedEventArgs args, Context c)
        {
            switch (args.ListAction)
            {
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add:
                    AddViewModels(args.ChangedDocuments, c);
                    break;
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Clear:
                    DocumentViewModels.Clear();
                    break;
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Remove:
                    RemoveViewModels(args.ChangedDocuments);
                    break;
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Replace:
                    DocumentViewModels.Clear();
                    AddDocuments(args.ChangedDocuments, c);
                    break;
            }
        }

        private void AddViewModels(List<DocumentController> documents, Context c)
        {
            foreach (var documentController in documents)
            {
                var documentViewModel = new DocumentViewModel(documentController, IsInInterfaceBuilder, c);
                documentViewModel.IsDraggerVisible = this.IsSelected;
                DocumentViewModels.Add(documentViewModel);
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
            if (doc.DocumentType.Equals(DashConstants.TypeStore.CollectionDocument))
            {
                var coll = doc.GetDereferencedField<ListController<DocumentController>>(CollectionKey, context);
                if (coll.Data.Contains(doc))
                    return;
            }

            if (context != null && context.DocContextList.Contains(doc))
            {
                return;
            }

            // just update the collection, the colllection will update our view automatically
            _collectionFieldModelController.Add(doc);
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
            _collectionFieldModelController.Remove(document);
        }

        #endregion
    }
}
