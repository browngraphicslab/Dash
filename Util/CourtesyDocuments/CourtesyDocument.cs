using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Converters;
using DashShared;
using Windows.UI.Xaml.Controls.Primitives;
using Dash.Views;
using TextWrapping = Windows.UI.Xaml.TextWrapping;

namespace Dash
{
    /// <summary>
    /// This class provides base functionality for creating and providing layouts to documents which contain data
    /// </summary>
    public abstract class CourtesyDocument
    {

        protected abstract DocumentController GetLayoutPrototype();

        public virtual DocumentController Document { get; set; }

        protected abstract DocumentController InstantiatePrototypeLayout();

        protected static FieldModelController GetDereferencedDataFieldModelController(DocumentController docController, Context context, FieldModelController defaultFieldModelController, out ReferenceFieldModelController refToData)
        {
            refToData = docController.GetField(DashConstants.KeyStore.DataKey) as ReferenceFieldModelController;
            Debug.Assert(refToData != null);
            var fieldModelController = refToData.DereferenceToRoot(context);

            // bcz: think this through better:
            //   -- the idea is that we're referencing a field that doesn't exist.  Instead of throwing an error, we can
            //      create the field with a default value.  The question is where in the 'context' should we set it?  I think
            //      we want to follow the reference to its end, adding fields along the way ... this just follows the reference one level.
            if (fieldModelController == null)
            {
                var parent = refToData.GetDocumentController(context);
                Debug.Assert(parent != null);
                parent.SetField((refToData as ReferenceFieldModelController).ReferenceFieldModel.FieldKey, defaultFieldModelController, true);
                fieldModelController = refToData.DereferenceToRoot(context);
            }
            return fieldModelController;
        }

        /// <summary>
        /// Sets the active layout on the <paramref name="dataDocument"/> to the passed in <paramref name="layoutDoc"/>
        /// </summary>
        protected static void SetLayoutForDocument(DocumentController dataDocument, DocumentController layoutDoc, bool forceMask, bool addToLayoutList)
        {
            dataDocument.SetActiveLayout(layoutDoc, forceMask: forceMask, addToLayoutList: addToLayoutList);
        }

        [Deprecated("Use alternate DefaultLayoutFields", DeprecationType.Deprecate, 1)]
        protected static Dictionary<Key, FieldModelController> DefaultLayoutFields(double x, double y, double w, double h,
            FieldModelController data)
        {
            return DefaultLayoutFields(new Point(x, y), new Size(w, h), data);
        }

        protected static Dictionary<Key, FieldModelController> DefaultLayoutFields(Point pos, Size size, FieldModelController data = null)
        {
            // assign the default fields
            var fields = new Dictionary<Key, FieldModelController>
            {
                [DashConstants.KeyStore.WidthFieldKey] = new NumberFieldModelController(size.Width),
                [DashConstants.KeyStore.HeightFieldKey] = new NumberFieldModelController(size.Height),
                [DashConstants.KeyStore.PositionFieldKey] = new PointFieldModelController(pos),
                [DashConstants.KeyStore.ScaleAmountFieldKey] = new PointFieldModelController(1, 1),
                [DashConstants.KeyStore.ScaleCenterFieldKey] = new PointFieldModelController(0, 0)
            };

            if (data != null)
                fields.Add(DashConstants.KeyStore.DataKey, data);
            return fields;
        }

        public virtual FrameworkElement makeView(DocumentController docController,
            Context context, bool isInterfaceBuilderLayout = false)
        {
            return new Grid();
        }

        #region Bindings

        /// <summary>
        /// Adds bindings needed to create links between renderable fields on collections.
        /// </summary>
        protected static void BindOperationInteractions(FrameworkElement renderElement, ReferenceFieldModelController fieldModelController)
        {
            renderElement.ManipulationMode = ManipulationModes.All;
            renderElement.ManipulationStarted += delegate (object sender, ManipulationStartedRoutedEventArgs args)
            {
                var view = renderElement.GetFirstAncestorOfType<CollectionView>();
                if (view == null) return; // we can't always assume we're on a collection
                    if (view.CanLink)
                {
                    args.Complete();
                    view.CanLink = false; // essential s.t. drag events don't get overriden
                    }
            };
            renderElement.IsHoldingEnabled = true; // turn on holding

            // must hold on element first to fetch link node
            renderElement.Holding += delegate (object sender, HoldingRoutedEventArgs args)
            {
                var view = renderElement.GetFirstAncestorOfType<CollectionView>();
                if (view == null) return; // we can't always assume we're on a collection
                    view.CanLink = true;
                if (view.CurrentView is CollectionFreeformView)
                    (view.CurrentView as CollectionFreeformView).StartDrag(new OperatorView.IOReference(fieldModelController, true, view.PointerArgs, renderElement,
                        renderElement.GetFirstAncestorOfType<DocumentView>()));

            };
            renderElement.PointerPressed += delegate (object sender, PointerRoutedEventArgs args)
            {
                var view = renderElement.GetFirstAncestorOfType<CollectionView>();
                if (view == null) return; // we can't always assume we're on a collection
                    view.PointerArgs = args;
                if (args.GetCurrentPoint(view).Properties.IsLeftButtonPressed)
                {

                }
                else if (args.GetCurrentPoint(view).Properties.IsRightButtonPressed)
                {
                    view.CanLink = true;
                    if (view.CurrentView is CollectionFreeformView)
                        (view.CurrentView as CollectionFreeformView).StartDrag(new OperatorView.IOReference(fieldModelController, true, args, renderElement,
                            renderElement.GetFirstAncestorOfType<DocumentView>()));
                }
            };
            renderElement.PointerReleased += delegate (object sender, PointerRoutedEventArgs args)
            {
                var view = renderElement.GetFirstAncestorOfType<CollectionView>();
                if (view == null) return; // we can't always assume we're on a collection
                    view.CanLink = false;

                args.Handled = true;
                (view.CurrentView as CollectionFreeformView)?.EndDrag(
                    new OperatorView.IOReference(fieldModelController, false, args, renderElement,
                        renderElement.GetFirstAncestorOfType<DocumentView>()));

            };
        }

        /// <summary>
        /// Adds a binding from the passed in <see cref="renderElement"/> to the passed in <see cref="NumberFieldModelController"/>
        /// <exception cref="ArgumentNullException">Throws an exception if the passed in <see cref="NumberFieldModelController"/> is null</exception>
        /// </summary>
        protected static void BindHeight(FrameworkElement renderElement,
            NumberFieldModelController heightController)
        {
            if (heightController == null) throw new ArgumentNullException(nameof(heightController));
            var heightBinding = new Binding
            {
                Source = heightController,
                Path = new PropertyPath(nameof(heightController.Data)),
                Mode = BindingMode.TwoWay
            };
            renderElement.SetBinding(FrameworkElement.HeightProperty, heightBinding);
        }

        /// <summary>
        /// Adds a binding from the passed in <see cref="renderElement"/> to the passed in <see cref="NumberFieldModelController"/>
        /// <exception cref="ArgumentNullException">Throws an exception if the passed in <see cref="NumberFieldModelController"/> is null</exception>
        /// </summary>
        protected static void BindWidth(FrameworkElement renderElement, NumberFieldModelController widthController)
        {
            if (widthController == null) throw new ArgumentNullException(nameof(widthController));
            var widthBinding = new Binding
            {
                Source = widthController,
                Path = new PropertyPath(nameof(widthController.Data)),
                Mode = BindingMode.TwoWay
            };
            renderElement.SetBinding(FrameworkElement.WidthProperty, widthBinding);
        }

        /// <summary>
        /// Adds a binding from the passed in <see cref="renderElement"/> to the passed in <see cref="PointFieldModelController"/>
        /// <exception cref="ArgumentNullException">Throws an exception if the passed in <see cref="PointFieldModelController"/> is null</exception>
        /// </summary>
        public static void BindTranslation(FrameworkElement renderElement,
            PointFieldModelController translateController)
        {
            if (translateController == null) throw new ArgumentNullException(nameof(translateController));
            var translateBinding = new Binding
            {
                Source = translateController,
                Path = new PropertyPath(nameof(translateController.Data)),
                Mode = BindingMode.TwoWay,
                Converter = new PointToTranslateTransformConverter()
            };
            renderElement.SetBinding(UIElement.RenderTransformProperty, translateBinding);
        }

        #endregion

        #region GettersAndSetters

        protected static NumberFieldModelController GetHeightField(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(DashConstants.KeyStore.HeightFieldKey)
                .DereferenceToRoot<NumberFieldModelController>(context);
        }

        protected static NumberFieldModelController GetWidthField(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(DashConstants.KeyStore.WidthFieldKey)
                .DereferenceToRoot<NumberFieldModelController>(context);
        }

        protected static PointFieldModelController GetPositionField(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(DashConstants.KeyStore.PositionFieldKey)
                .DereferenceToRoot<PointFieldModelController>(context);
        }

        #endregion
    }
}