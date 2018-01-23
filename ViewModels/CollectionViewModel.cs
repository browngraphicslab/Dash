using System;
using System.Collections.Generic;
using System.Diagnostics;
using DashShared;
using Windows.Foundation;
using System.Linq;

namespace Dash
{
    public class CollectionViewModel : BaseCollectionViewModel
    {
        public ListController<DocumentController> CollectionController => _collectionRef.DereferenceToRoot<ListController<DocumentController>>(_context);

        public InkController InkController;

        public DocumentController ContainerDocument => _collectionRef.GetDocumentController(_context);

        private FieldReference _collectionRef;
        private Context _context;

        public CollectionViewModel(FieldReference refToCollection, bool isInInterfaceBuilder = false, Context context = null) : base(isInInterfaceBuilder)
        {
            Debug.Assert(refToCollection != null);
            _collectionRef = refToCollection;
            _context = context;
            AddViewModels(CollectionController.TypedData, context);

            var copiedContext = new Context(context);

            refToCollection.GetDocumentController(context).AddFieldUpdatedListener(refToCollection.FieldKey,
                delegate (FieldControllerBase sender, FieldUpdatedEventArgs args, Context context1)
                {
                    var dargs = (DocumentController.DocumentFieldUpdatedEventArgs)args;
                    var cargs = dargs.FieldArgs as ListController<DocumentController>.ListFieldUpdatedEventArgs;
                    if (cargs == null)
                    {
                        return;
                    }
                    if (args.Action == DocumentController.FieldUpdatedAction.Update)
                    {
                        UpdateViewModels(cargs, copiedContext);
                    }
                    else
                    {

                        var collectionFieldModelController = dargs.NewValue.DereferenceToRoot<ListController<DocumentController>>(context);
                        if (collectionFieldModelController == null) return;
                        var documents = collectionFieldModelController.GetElements();
                        DocumentViewModels.Clear();

                        AddViewModels(documents, context);
                    }
                });

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
                CollectionController.Remove(vmp.DocumentController);
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
            doc.CaptureNeighboringContext();

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
            CollectionController.Add(doc);
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
            CollectionController.Remove(document);
        }

        #endregion
    }
}
