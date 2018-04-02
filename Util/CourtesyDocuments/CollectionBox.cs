using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Dash;
using DashShared;
using System;
using DashShared.Models;

namespace Dash
{
    /// <summary>
    /// The data field for a collection is a document collection field model controller
    /// </summary>
    public class CollectionBox : CourtesyDocument
    {
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

            Document = GetLayoutPrototype().MakeDelegate();
            Document.SetFields(fields, true);
            
        }

        protected override DocumentController GetLayoutPrototype()
        {
            var prototype = ContentController<FieldModel>.GetController<DocumentController>(PrototypeId);
            if (prototype == null)
            {
                prototype = InstantiatePrototypeLayout();
            }
            return prototype;
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            var docController = new ListController<DocumentController>(new List<DocumentController>());
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), docController);
            fields[KeyStore.IconTypeFieldKey] = new NumberController((int)IconTypeEnum.Collection); // TODO factor out into SetIconField() method in base class
            fields[KeyStore.AbstractInterfaceKey] = new TextController("CollectionBox Layout");

            return new DocumentController(fields, DashConstants.TypeStore.CollectionBoxType, PrototypeId);
        }

        public override FrameworkElement makeView(DocumentController docController, Context context)
        {
            return MakeView(docController, context);
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
            
            return view;
        }
    }
}