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
using TextWrapping = Windows.UI.Xaml.TextWrapping;

namespace Dash
{
    /// <summary>
    /// This class provides base functionality for creating and providing layouts to documents which contain data
    /// </summary>
    public abstract class CourtesyDocument
    {
        public static readonly KeyController HorizontalAlignmentKey = new KeyController("B43231DA-5A22-45A3-8476-005A62396686", "Horizontal Alignment");
        public static readonly KeyController VerticalAlignmentKey = new KeyController("227B9887-BC09-40E4-A3F0-AD204D00E48D", "Vertical Alignment");

        public static readonly KeyController GridRowKey = new KeyController("FC447698-1C96-4014-94A5-845D411C1CD1", "Grid.Row");
        public static readonly KeyController GridColumnKey = new KeyController("E6663AA3-26E1-48D1-8A95-768EC0CFD4BC", "Grid.Column");
        public static readonly KeyController GridRowSpanKey = new KeyController("3F305CD6-343E-4155-AFEB-5530E499727C", "Grid.RowSpan");
        public static readonly KeyController GridColumnSpanKey = new KeyController("C0A16508-76AF-42B5-A3D7-D693FDD5AA84", "Grid.ColumnSpan");

        protected abstract DocumentController GetLayoutPrototype();

        public virtual DocumentController Document { get; set; }

        protected abstract DocumentController InstantiatePrototypeLayout();

        /// <summary>
        /// Fully dereference the field associated with the data key in the passed in docController
        /// //TODO explain default field model controller idea here once it is written
        /// </summary>
        /// <returns></returns>
        protected static FieldModelController GetDereferencedDataFieldModelController(DocumentController docController, Context context, FieldModelController defaultFieldModelController, out ReferenceFieldModelController refToData)
        {
            refToData = docController.GetField(KeyStore.DataKey) as ReferenceFieldModelController;
            if (refToData != null)
            {
                var fieldModelController = refToData.DereferenceToRoot(context);

                // bcz: think this through better:
                //   -- the idea is that we're referencing a field that doesn't exist.  Instead of throwing an error, we can
                //      create the field with a default value.  The question is where in the 'context' should we set it?  I think
                //      we want to follow the reference to its end, adding fields along the way ... this just follows the reference one level.
                if (fieldModelController == null)
                {
                    var parent = refToData.GetDocumentController(context);
                    Debug.Assert(parent != null);
                    parent.SetField((refToData as ReferenceFieldModelController).FieldKey, defaultFieldModelController, true);
                    fieldModelController = refToData.DereferenceToRoot(context);
                }
                return fieldModelController;
            }
            return null;
        }

        /// <summary>
        /// Sets the active layout on the <paramref name="dataDocument"/> to the passed in <paramref name="layoutDoc"/>
        /// </summary>
        protected static void SetLayoutForDocument(DocumentController dataDocument, DocumentController layoutDoc, bool forceMask, bool addToLayoutList)
        {
            dataDocument.SetActiveLayout(layoutDoc, forceMask: forceMask, addToLayoutList: addToLayoutList);
        }

        protected delegate void BindingDelegate<in T>(T element, DocumentController controller, Context c);

        private static int loaded = 0, unloaded = 0;

        

        protected static void AddBinding<T>(T element, DocumentController docController, KeyController k, Context context,
            BindingDelegate<T> bindingDelegate) where T : FrameworkElement
        {
            DocumentController.OnDocumentFieldUpdatedHandler handler = (sender, args) =>
            {
                if (args.Action == DocumentController.FieldUpdatedAction.Update) return;
                bindingDelegate(element, sender, args.Context); //TODO Should be context or args.Context?
            };

            AddHandlers(element, docController, k, context, bindingDelegate, handler);
        }

        protected static void AddHandlers<T>(T element, DocumentController docController, KeyController k, Context context,
            BindingDelegate<T> bindingDelegate, DocumentController.OnDocumentFieldUpdatedHandler handler) where T : FrameworkElement
        {
            element.Loaded += delegate
            {
                bindingDelegate(element, docController, context);
                docController.AddFieldUpdatedListener(k, handler);
            };
            element.Unloaded += delegate
            {
                docController.RemoveFieldUpdatedListener(k, handler);
            };
        }

        protected static void BindWidth(FrameworkElement element, DocumentController docController, Context context)
        {
            FieldBinding<NumberFieldModelController> binding = new FieldBinding<NumberFieldModelController>()
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
            FieldBinding<NumberFieldModelController> binding = new FieldBinding<NumberFieldModelController>()
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
            var positionFmc = docController.GetPositionField(context);
            Binding positionBinding = new Binding
            {
                Source = positionFmc,
                Path = new PropertyPath(nameof(positionFmc.Data)),
                Mode = BindingMode.TwoWay,
                Converter = new PointToTranslateTransformConverter()
            };
            element.SetBinding(UIElement.RenderTransformProperty, positionBinding);
        }

        protected static void BindHorizontalAlignment(FrameworkElement element, DocumentController docController,
            Context context)
        {
            var binding = new FieldBinding<TextFieldModelController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = HorizontalAlignmentKey,
                Converter = new StringToEnumConverter<HorizontalAlignment>(),
                Context = context
            };

            element.AddFieldBinding(FrameworkElement.HorizontalAlignmentProperty, binding);
        }

        protected static void BindVerticalAlignment(FrameworkElement element, DocumentController docController,
            Context context)
        {
            var binding = new FieldBinding<TextFieldModelController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = VerticalAlignmentKey,
                Converter = new StringToEnumConverter<VerticalAlignment>(),
                Context = context
            };

            element.AddFieldBinding(FrameworkElement.VerticalAlignmentProperty, binding);
        }

        protected static void BindGridRow(FrameworkElement element, DocumentController docController, Context context)
        {
            FieldBinding<NumberFieldModelController> binding = new FieldBinding<NumberFieldModelController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = GridRowKey,
                Context = context
            };

            element.AddFieldBinding(Grid.RowProperty, binding);
        }

        protected static void BindGridColumn(FrameworkElement element, DocumentController docController, Context context)
        {
            FieldBinding<NumberFieldModelController> binding = new FieldBinding<NumberFieldModelController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = GridColumnKey,
                Context = context
            };

            element.AddFieldBinding(Grid.ColumnProperty, binding);
        }

        protected static void BindGridRowSpan(FrameworkElement element, DocumentController docController, Context context)
        {
            FieldBinding<NumberFieldModelController> binding = new FieldBinding<NumberFieldModelController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = GridRowSpanKey,
                Context = context
            };

            element.AddFieldBinding(Grid.RowSpanProperty, binding);
        }

        protected static void BindGridColumnSpan(FrameworkElement element, DocumentController docController, Context context)
        {
            FieldBinding<NumberFieldModelController> binding = new FieldBinding<NumberFieldModelController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = GridColumnSpanKey,
                Context = context
            };

            element.AddFieldBinding(Grid.ColumnSpanProperty, binding);
        }

        protected static void SetupBindings(FrameworkElement element, DocumentController docController, Context context)
        {
            //Set width and height
            BindWidth(element, docController, context);
            BindHeight(element, docController, context);

            //Set alignments
            BindHorizontalAlignment(element, docController, context);
            BindVerticalAlignment(element, docController, context);

            //Set column, row, and span
            BindGridRow(element, docController, context);
            BindGridColumn(element, docController, context);
            BindGridRowSpan(element, docController, context);
            BindGridColumnSpan(element, docController, context);
        }
        
        public static Dictionary<KeyController, FieldModelController> DefaultLayoutFields(Point pos, Size size, FieldModelController data = null)
        {
            // assign the default fields
            var fields = new Dictionary<KeyController, FieldModelController>
            {
                [KeyStore.WidthFieldKey] = new NumberFieldModelController(size.Width),
                [KeyStore.HeightFieldKey] = new NumberFieldModelController(size.Height),
                [KeyStore.PositionFieldKey] = new PointFieldModelController(pos),
                [KeyStore.ScaleAmountFieldKey] = new PointFieldModelController(1, 1),
                [KeyStore.ScaleCenterFieldKey] = new PointFieldModelController(0, 0),
                [HorizontalAlignmentKey] = new TextFieldModelController(HorizontalAlignment.Stretch.ToString()),
                [VerticalAlignmentKey] = new TextFieldModelController(VerticalAlignment.Top.ToString())
            };

            if (data != null)
                fields.Add(KeyStore.DataKey, data);
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
        protected static void BindOperationInteractions(FrameworkElement renderElement, FieldReference reference, KeyController fieldKey, FieldModelController fmController)
        {
            //TODO If we allow fields in documents to change type, caputuring/using fmController.TypeInfo for drag events won't necesarilly always be correct
            renderElement.ManipulationMode = ManipulationModes.All;
            renderElement.ManipulationStarted += delegate (object sender, ManipulationStartedRoutedEventArgs args)
            {
                var view = renderElement.GetFirstAncestorOfType<ICollectionView>();
                var freeform = view as CollectionFreeformView;
                if (view == null) return; // we can't always assume we're on a collection
                if (freeform != null && freeform.CanLink)
                {
                    args.Complete(); // This was stopping manipulations from happening on the first try? 
                    //view.CanLink = false; // essential that this is false s.t. drag events don't get overriden
                }
            };
            renderElement.IsHoldingEnabled = true; // turn on holding

            // must hold on element first to fetch link node
            renderElement.Holding += delegate (object sender, HoldingRoutedEventArgs args)
            {
                var view = renderElement.GetFirstAncestorOfType<ICollectionView>();
                var freeform = view as CollectionFreeformView;
                if (view == null) return; // we can't always assume we're on a collection
                if (freeform != null) freeform.CanLink = true;
                freeform?.StartDrag(new IOReference(reference, true, fmController.TypeInfo, freeform.PointerArgs, renderElement,
                    renderElement.GetFirstAncestorOfType<DocumentView>()));
            };
            renderElement.PointerPressed += delegate (object sender, PointerRoutedEventArgs args)
            {
                var view = renderElement.GetFirstAncestorOfType<ICollectionView>();
                var freeform = view as CollectionFreeformView;
                if (view == null) return; // we can't always assume we're on a collection
                if (freeform != null) freeform.PointerArgs = args;
                args.Handled = true;
                if (args.GetCurrentPoint(freeform).Properties.IsRightButtonPressed)
                {
                    if (freeform != null) freeform.CanLink = true;
                    freeform?.StartDrag(new IOReference(reference, true, fmController.TypeInfo, args, renderElement,
                        renderElement.GetFirstAncestorOfType<DocumentView>()));
                }
            };
            renderElement.PointerReleased += delegate (object sender, PointerRoutedEventArgs args)
            {
                var view = renderElement.GetFirstAncestorOfType<ICollectionView>();
                var freeform = view as CollectionFreeformView;
                if (view == null) return; // we can't always assume we're on a collection
                if (freeform != null) freeform.CanLink = false;

                args.Handled = true;
                freeform?.EndDrag(new IOReference(reference, false, fmController.TypeInfo, args, renderElement,
                        renderElement.GetFirstAncestorOfType<DocumentView>()), false);

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
            return docController.GetField(KeyStore.HeightFieldKey)
                .DereferenceToRoot<NumberFieldModelController>(context);
        }

        protected static NumberFieldModelController GetWidthField(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(KeyStore.WidthFieldKey)
                .DereferenceToRoot<NumberFieldModelController>(context);
        }

        protected static PointFieldModelController GetPositionField(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(KeyStore.PositionFieldKey)
                .DereferenceToRoot<PointFieldModelController>(context);
        }

        #endregion
    }

    public static class CourtesyDocumentExtensions
    {
        public static void SetHorizontalAlignment(this DocumentController document, HorizontalAlignment alignment)
        {
            document.SetField(CourtesyDocument.HorizontalAlignmentKey, new TextFieldModelController(alignment.ToString()), true);
        }


        public static HorizontalAlignment GetHorizontalAlignment(this DocumentController document)
        {
            var horizontalAlignmentController =
                document.GetField(CourtesyDocument.HorizontalAlignmentKey) as TextFieldModelController;
            if (horizontalAlignmentController == null)
            {
                return HorizontalAlignment.Stretch;
            }
            return (HorizontalAlignment)Enum.Parse(typeof(HorizontalAlignment), horizontalAlignmentController?.Data);
        }

        public static void SetVerticalAlignment(this DocumentController document, VerticalAlignment alignment)
        {
            var currentHeight = document.GetHeightField().Data;
            document.SetField(CourtesyDocument.VerticalAlignmentKey, new TextFieldModelController(alignment.ToString()), true);
            document.SetHeight(currentHeight);
        }

        public static VerticalAlignment GetVerticalAlignment(this DocumentController document)
        {
            var verticalAlignmentController =
                document.GetField(CourtesyDocument.VerticalAlignmentKey) as TextFieldModelController;
            if (verticalAlignmentController == null)
            {
                return VerticalAlignment.Stretch;
            }
            return (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment), verticalAlignmentController?.Data);
        }

        public static void SetWidth(this DocumentController document, double width)
        {
            document.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(width), true);
        }

        public static void SetHeight(this DocumentController document, double height)
        {
            document.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(height), true);
        }

        public static void SetGridRow(this DocumentController document, int row)
        {
            document.SetField(CourtesyDocument.GridRowKey, new NumberFieldModelController(row), true);
        }

        public static void SetGridColumn(this DocumentController document, int column)
        {
            document.SetField(CourtesyDocument.GridColumnKey, new NumberFieldModelController(column), true);
        }

        public static void SetGridRowSpan(this DocumentController document, int rowSpan)
        {
            document.SetField(CourtesyDocument.GridRowSpanKey, new NumberFieldModelController(rowSpan), true);
        }

        public static void SetGridColumnSpan(this DocumentController document, int columnSpan)
        {
            document.SetField(CourtesyDocument.GridColumnSpanKey, new NumberFieldModelController(columnSpan), true);
        }
    }
}