using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;
using System;
using Windows.UI.Xaml.Data;
using Dash.Converters;
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
            if (data != null)
            {
                //var collectionController = data.DereferenceToRoot<ListController<DocumentController>>(context);
                //Debug.Assert(collectionController != null);
                var collectionViewModel = new CollectionViewModel(docController, KeyStore.DataKey)
                {
                    InkController = docController.GetField(KeyStore.InkDataKey) as InkController
                };

                var view = new CollectionView(collectionViewModel);

                SetupBindings(view, docController, context);

                void docContextChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context c)
                {
                    collectionViewModel.SetCollectionRef(docController, KeyStore.DataKey);
                }

                view.Loaded += (sender, args) =>
                {
                    docController.AddFieldUpdatedListener(KeyStore.DocumentContextKey, docContextChanged);
                };
                view.Unloaded += (sender, args) =>
                {
                    docController.RemoveFieldUpdatedListener(KeyStore.DocumentContextKey, docContextChanged);
                };


                return view;
            }

            return null;
        }

        protected static void BindWidth(FrameworkElement element, DocumentController docController, Context context)
        {
            FieldBinding<NumberController> binding = new FieldBinding<NumberController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = KeyStore.WidthFieldKey,
                Context = context
            };

            element.AddFieldBinding(FrameworkElement.WidthProperty, binding);
        }

        protected static void BindHeight(FrameworkElement element, DocumentController docController, Context context)
        {
            FieldBinding<NumberController> binding = new FieldBinding<NumberController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = KeyStore.HeightFieldKey,
                Context = context
            };

            element.AddFieldBinding(FrameworkElement.HeightProperty, binding);
        }

        protected static void BindPosition(FrameworkElement element, DocumentController docController, Context context)
        {
            FieldBinding<PointController> binding = new FieldBinding<PointController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = KeyStore.PositionFieldKey,
                Context = context,
                Converter = new PointToTranslateTransformConverter()
            };

            element.AddFieldBinding(UIElement.RenderTransformProperty, binding);
        }

        protected static void BindHorizontalAlignment(FrameworkElement element, DocumentController docController,
            Context context)
        {
            var binding = new FieldBinding<TextController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = KeyStore.HorizontalAlignmentKey,
                Converter = new StringToEnumConverter<HorizontalAlignment>(),
                Context = context
            };

            element.AddFieldBinding(FrameworkElement.HorizontalAlignmentProperty, binding);
        }

        protected static void BindVerticalAlignment(FrameworkElement element, DocumentController docController,
            Context context)
        {
            var binding = new FieldBinding<TextController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = KeyStore.VerticalAlignmentKey,
                Converter = new StringToEnumConverter<VerticalAlignment>(),
                Context = context
            };

            element.AddFieldBinding(FrameworkElement.VerticalAlignmentProperty, binding);
        }
    }
}