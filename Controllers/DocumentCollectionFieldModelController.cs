using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Dash.Converters;
using Windows.UI.Xaml.Data;
using static Dash.DocumentController;
using System.Diagnostics;
using Dash.Controllers;
using DashShared.Models;

namespace Dash
{
    public class DocumentCollectionFieldModelController : FieldModelController<DocumentCollectionFieldModel>
    {
        /// <summary>
        ///     Key for collection data
        ///     TODO This might be better in a different class
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
                if (_documents != value)
                {
                    _documents = value;
                    // update 
                    OnFieldModelUpdated(null);

                    // Update server
                    UpdateOnServer();
                }
            }
        }

        /// <summary>
        ///     A wrapper for <see cref="DocumentCollectionFieldModel.Data" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        private List<DocumentController> _documents = new List<DocumentController>();

        public DocumentCollectionFieldModelController() : this(new List<DocumentController>())
        {
            
        }

        public DocumentCollectionFieldModelController(DocumentCollectionFieldModel model) : base(model)
        {
            
        }

        public DocumentCollectionFieldModelController(IEnumerable<DocumentController> documents) : base(new DocumentCollectionFieldModel(documents.Select(doc => doc.Model.Id)))
        {
            Init();
        }

        public override void Init()
        {
            AddDocuments((Model as DocumentCollectionFieldModel).Data.Select(i => ContentController<DocumentModel>.GetController<DocumentController>(i)).ToList());
        }

        /// <summary>
        ///     The <see cref="DocumentCollectionFieldModel" /> associated with this
        ///     <see cref="DocumentCollectionFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public DocumentCollectionFieldModel DocumentCollectionFieldModel => Model as DocumentCollectionFieldModel;

        public override TypeInfo TypeInfo => TypeInfo.Collection;

        /*
        public static async Task<DocumentCollectionFieldModelController> CreateFromServer(
            DocumentCollectionFieldModel docCollectionFieldModel)
        {
            var localController = ContentController<FieldModel>.GetController<DocumentCollectionFieldModelController>(docCollectionFieldModel.Id);
            if (localController != null)
            {
                return localController;
            }

            List<DocumentController> docControllerList = new List<DocumentController>();
            Debug.WriteLine("started get documents");
            await RESTClient.Instance.Documents.GetDocuments(docCollectionFieldModel.Data, async docmodelDtos =>
            {
                foreach (var dto in docmodelDtos)
                {
                    docControllerList.Add(new DocumentController(dto));
                }
                Debug.WriteLine("done with get documents");

            }, exception => throw exception);
            return new DocumentCollectionFieldModelController(docControllerList, docCollectionFieldModel);
        }
        */

        public int Count => _documents.Count;

        /// <summary>
        ///     Adds a single document to the collection.
        /// </summary>
        /// <param name="docController"></param>
        public void AddDocuments(IEnumerable<DocumentController> docControllers)
        {
            foreach (var docController in docControllers)
            {
                AddDocument(docController);
            }
        }

        /// <summary>
        ///     Adds a single document to the collection.
        /// </summary>
        /// <param name="docController"></param>
        public void AddDocument(DocumentController docController)
        {
            if (docController == null)
                return;
            // if the document is already in the collection don't readd it
            if (_documents.Contains(docController))
            {
                Debug.Assert(DocumentCollectionFieldModel.Data.Contains(docController.GetId()));
                return;
            }

            docController.DocumentFieldUpdated += ContainedDocumentFieldUpdated;
            docController.DocumentDeleted += DocController_DocumentDeleted;

            _documents.Add(docController); // update the controller

            // update the model
            if (!DocumentCollectionFieldModel.Data.Contains(docController.GetId()))
            {
                DocumentCollectionFieldModel.Data.Add(docController.GetId());

                UpdateOnServer();
            }

            //TODO only fire this once for add documents
            OnFieldModelUpdated(new CollectionFieldUpdatedEventArgs(
                CollectionFieldUpdatedEventArgs.CollectionChangedAction.Add,
                new List<DocumentController> {docController}));
        }

        private void DocController_DocumentDeleted(object sender, EventArgs e)
        {
            this.RemoveDocument(sender as DocumentController);
        }

        void ContainedDocumentFieldUpdated(DocumentController sender, DocumentFieldUpdatedEventArgs args)
        {
            var keylist = sender.GetDereferencedField<ListController<KeyController>>(KeyStore.PrimaryKeyKey, new Context(sender))?.Data;
            if (keylist != null && keylist.Contains(args.Reference.FieldKey.Id))
                OnFieldModelUpdated(args.FieldArgs);
        }

        public void RemoveDocument(DocumentController doc)
        {
            doc.DocumentFieldUpdated -= ContainedDocumentFieldUpdated;
            doc.DocumentDeleted -= DocController_DocumentDeleted;

            var isDocInList = _documents.Remove(doc);
            DocumentCollectionFieldModel.Data.Remove(doc.GetId());
            if (isDocInList)
                OnFieldModelUpdated(new CollectionFieldUpdatedEventArgs(
                    CollectionFieldUpdatedEventArgs.CollectionChangedAction.Remove,
                    new List<DocumentController> {doc}));
        }

        public void SetDocuments(List<DocumentController> docControllers)
        {
            //TODO make sure the server is getting notified here or at a lower level
            foreach (var docController in Data)
            {
                docController.DocumentFieldUpdated -= ContainedDocumentFieldUpdated;
                docController.DocumentDeleted -= DocController_DocumentDeleted;
            }

            _documents = new List<DocumentController>(docControllers);
            DocumentCollectionFieldModel.Data = _documents.Select(d => d.GetId()).ToList();


            foreach (var docController in Data)
            {
                docController.DocumentFieldUpdated += ContainedDocumentFieldUpdated;
                docController.DocumentDeleted += DocController_DocumentDeleted;
            }

            OnFieldModelUpdated(new CollectionFieldUpdatedEventArgs(
                CollectionFieldUpdatedEventArgs.CollectionChangedAction.Replace,
                new List<DocumentController>(docControllers)));
        }

        /// <summary>
        ///     YOU CANNOT ADD DOCUMENTS TO THIS LIST
        /// </summary>
        /// <returns></returns>
        public List<DocumentController> GetDocuments()
        {
            // since we want people to set the documents through methods lets just pass a copy of the model's list
            return _documents.ToList();
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new DocumentCollectionFieldModelController(new List<DocumentController>());
        }

        public override FieldModelController<DocumentCollectionFieldModel> Copy()
        {
            return new DocumentCollectionFieldModelController(new List<DocumentController>(_documents));
        }


        public override void MakeAllViewUI(DocumentController container, KeyController kc, Context context, Panel sp,
            string id, bool isInterfaceBuilder = false)
        {
            var rfmc = new DocumentReferenceFieldController(id, kc);
            var vm = new CollectionViewModel(container, rfmc, isInterfaceBuilder, context);
            var viewType = container.GetActiveLayout()?.Data?.GetDereferencedField<TextFieldModelController>(KeyStore.CollectionViewTypeKey, null)?.Data ??  CollectionView.CollectionViewType.Grid.ToString();
            var colView = new CollectionView(vm, (CollectionView.CollectionViewType)Enum.Parse(typeof(CollectionView.CollectionViewType), viewType));
            sp.Children.Add(colView);
            colView.TryBindToParentDocumentSize();
        }


        public class CollectionFieldUpdatedEventArgs : FieldUpdatedEventArgs
        {
            public enum CollectionChangedAction
            {
                Add,
                Remove,
                Replace,
                Clear
            }

            public readonly List<DocumentController> ChangedDocuments;

            public readonly CollectionChangedAction CollectionAction;

            private CollectionFieldUpdatedEventArgs() : base(TypeInfo.Collection,
                DocumentController.FieldUpdatedAction.Update)
            {
            }

            public CollectionFieldUpdatedEventArgs(CollectionChangedAction action) : this()
            {
                if (action != CollectionChangedAction.Clear)
                    throw new ArgumentException();
                CollectionAction = action;
                ChangedDocuments = null;
            }

            public CollectionFieldUpdatedEventArgs(CollectionChangedAction action,
                List<DocumentController> changedDocuments) : this()
            {
                CollectionAction = action;
                ChangedDocuments = changedDocuments;
            }
        }
    }
}