using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;
using System;
using static Dash.DocumentController;

namespace Dash
{
    /// <summary>
    /// The data field for a collection is a document collection field model controller
    /// </summary>
    public class CollectionBox : CourtesyDocument
    {
        public static DocumentType DocumentType = DashConstants.TypeStore.CollectionBoxType;
        private static string PrototypeId = "E1F828EA-D44D-4C3C-BE22-9AAF369C3F19";


        /// <summary>
        /// If the view type is unassigned this is the default view displayed to the user
        /// </summary>
        private static readonly string DefaultCollectionView = CollectionView.CollectionViewType.Grid.ToString();
        public CollectionBox(FieldControllerBase refToCollection, double x = 0, double y = 0, double w = double.NaN, double h = double.NaN, CollectionView.CollectionViewType viewType = CollectionView.CollectionViewType.Freeform)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToCollection);
            fields[KeyStore.CollectionViewTypeKey] = new TextController(viewType.ToString());
            fields[KeyStore.InkDataKey] = new InkController();
            fields[KeyStore.IconTypeFieldKey] = new NumberController((int)IconTypeEnum.Collection); // TODO factor out into SetIconField() method in base class

            SetupDocument(DocumentType, PrototypeId, "Collection Box Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {

            // get a collection and collection view model from the data
            var data = docController.GetField(KeyStore.DataKey);
            var collectionController = data.DereferenceToRoot<ListController<DocumentController>>(context);
            Debug.Assert(collectionController != null);
            var collectionViewModel = new CollectionViewModel(new DocumentFieldReference(docController.Id, KeyStore.DataKey), context)
            { InkController = docController.GetField(KeyStore.InkDataKey) as InkController};

            // set the view type (i.e. list, grid, freeform)
            var typeString = (docController.GetField(KeyStore.CollectionViewTypeKey) as TextController)?.Data ?? DefaultCollectionView;
            var viewType   = (CollectionView.CollectionViewType) Enum.Parse(typeof(CollectionView.CollectionViewType), typeString);
            var view       = new CollectionView(collectionViewModel,  viewType);

            SetupBindings(view, docController, context);

            void docContextChanged(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
            {
                var newDocContext = (args as DocumentFieldUpdatedEventArgs).NewValue as DocumentController;
                var newContext = new Context(c);
                foreach (var ctxt in context.DocContextList)
                    newContext.AddDocumentContext(ctxt);
                newContext.AddDocumentContext(newDocContext);
                collectionViewModel.SetCollectionRef(new DocumentFieldReference(docController.Id, KeyStore.DataKey), newContext);
            }
            docController.AddFieldUpdatedListener(KeyStore.DocumentContextKey, docContextChanged);
            
            return view;
        }
    }
}