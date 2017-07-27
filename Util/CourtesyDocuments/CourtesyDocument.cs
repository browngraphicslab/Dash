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
        public static readonly Key HorizontalAlignmentKey = new Key("B43231DA-5A22-45A3-8476-005A62396686", "Horizontal Alignment");
        public static readonly Key VerticalAlignmentKey = new Key("227B9887-BC09-40E4-A3F0-AD204D00E48D", "Vertical Alignment");

        public static readonly Key GridRowKey = new Key("FC447698-1C96-4014-94A5-845D411C1CD1", "Grid.Row");
        public static readonly Key GridColumnKey = new Key("E6663AA3-26E1-48D1-8A95-768EC0CFD4BC", "Grid.Column");
        public static readonly Key GridRowSpanKey = new Key("3F305CD6-343E-4155-AFEB-5530E499727C", "Grid.RowSpan");
        public static readonly Key GridColumnSpanKey = new Key("C0A16508-76AF-42B5-A3D7-D693FDD5AA84", "Grid.ColumnSpan");

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
                parent.SetField((refToData as ReferenceFieldModelController).FieldKey, defaultFieldModelController, true);
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

        protected delegate void BindingDelegate<in T>(T element, DocumentController controller, Context c);

        protected static void AddBinding<T>(T element, DocumentController docController, Key k, Context context,
            BindingDelegate<T> bindingDelegate)
        {
            bindingDelegate.Invoke(element, docController, context);
            docController.AddFieldUpdatedListener(k,
                delegate (DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
                {
                    if (args.Action == DocumentController.FieldUpdatedAction.Update)
                    {
                        return;
                    }
                    bindingDelegate(element, sender, args.Context);//TODO Should be context or args.Context?
                });
        }

        protected static void BindWidth(FrameworkElement element, DocumentController docController, Context context)
        {
            var widthFmc = docController.GetWidthField(context);
            Binding widthBinding = new Binding
            {
                Source = widthFmc,
                Path = new PropertyPath(nameof(widthFmc.Data)),
                Mode = BindingMode.TwoWay
            };
            element.SetBinding(FrameworkElement.WidthProperty, widthBinding);
        }

        protected static void BindHeight(FrameworkElement element, DocumentController docController, Context context)
        {
            var heightFmc = docController.GetHeightField(context);
            Binding heightBinding = new Binding
            {
                Source = heightFmc,
                Path = new PropertyPath(nameof(heightFmc.Data)),
                Mode = BindingMode.TwoWay
            };
            element.SetBinding(FrameworkElement.HeightProperty, heightBinding);
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
            var horizontalAlignmentFmc = docController.GetField(HorizontalAlignmentKey) as TextFieldModelController;
            if (horizontalAlignmentFmc == null)
            {
                return;
            }
            Binding binding = new Binding
            {
                Source = horizontalAlignmentFmc,
                Path = new PropertyPath(nameof(horizontalAlignmentFmc.Data)),
                Converter = new StringToEnumConverter<HorizontalAlignment>()
            };
            element.SetBinding(FrameworkElement.HorizontalAlignmentProperty, binding);
        }

        protected static void BindVerticalAlignment(FrameworkElement element, DocumentController docController,
            Context context)
        {
            var verticalAlignmentFmc = docController.GetField(VerticalAlignmentKey) as TextFieldModelController;
            if (verticalAlignmentFmc == null)
            {
                return;
            }
            Binding binding = new Binding
            {
                Source = verticalAlignmentFmc,
                Path = new PropertyPath(nameof(verticalAlignmentFmc.Data)),
                Converter = new StringToEnumConverter<VerticalAlignment>()
            };
            element.SetBinding(FrameworkElement.VerticalAlignmentProperty, binding);
        }

        protected static void BindGridRow(FrameworkElement element, DocumentController docController, Context context)
        {
            var gridRowFmc = docController.GetField(GridRowKey) as NumberFieldModelController;
            if (gridRowFmc == null)
            {
                return;
            }
            Binding binding = new Binding
            {
                Source = gridRowFmc,
                Path = new PropertyPath(nameof(gridRowFmc.Data))
            };
            element.SetBinding(Grid.RowProperty, binding);
        }

        protected static void BindGridColumn(FrameworkElement element, DocumentController docController, Context context)
        {
            var gridColumnFmc = docController.GetField(GridColumnKey) as NumberFieldModelController;
            if (gridColumnFmc == null)
            {
                return;
            }
            Binding binding = new Binding
            {
                Source = gridColumnFmc,
                Path = new PropertyPath(nameof(gridColumnFmc.Data))
            };
            element.SetBinding(Grid.ColumnProperty, binding);
        }

        protected static void BindGridRowSpan(FrameworkElement element, DocumentController docController, Context context)
        {
            var gridRowSpanFmc = docController.GetField(GridRowSpanKey) as NumberFieldModelController;
            if (gridRowSpanFmc == null)
            {
                return;
            }
            Binding binding = new Binding
            {
                Source = gridRowSpanFmc,
                Path = new PropertyPath(nameof(gridRowSpanFmc.Data))
            };
            element.SetBinding(Grid.RowSpanProperty, binding);
        }

        protected static void BindGridColumnSpan(FrameworkElement element, DocumentController docController, Context context)
        {
            var gridColumnSpanFmc = docController.GetField(GridColumnKey) as NumberFieldModelController;
            if (gridColumnSpanFmc == null)
            {
                return;
            }
            Binding binding = new Binding
            {
                Source = gridColumnSpanFmc,
                Path = new PropertyPath(nameof(gridColumnSpanFmc.Data))
            };
            element.SetBinding(Grid.ColumnSpanProperty, binding);
        }

        protected static void SetupBindings(FrameworkElement element, DocumentController docController, Context context)
        {
            //Set width and height
            AddBinding(element, docController, DashConstants.KeyStore.WidthFieldKey, context, BindWidth);
            AddBinding(element, docController, DashConstants.KeyStore.HeightFieldKey, context, BindHeight);

            //Set alignments
            AddBinding(element, docController, HorizontalAlignmentKey, context, BindHorizontalAlignment);
            AddBinding(element, docController, VerticalAlignmentKey, context, BindVerticalAlignment);

            //Set column, row, and span
            AddBinding(element, docController, GridRowKey, context, BindGridRow);
            AddBinding(element, docController, GridColumnKey, context, BindGridColumn);
            AddBinding(element, docController, GridRowSpanKey, context, BindGridRowSpan);
            AddBinding(element, docController, GridColumnKey, context, BindGridColumnSpan);
        }

        [Deprecated("Use alternate DefaultLayoutFields", DeprecationType.Deprecate, 1)]
        protected static Dictionary<Key, FieldModelController> DefaultLayoutFields(double x, double y, double w, double h,
            FieldModelController data)
        {
            return DefaultLayoutFields(new Point(x, y), new Size(w, h), data);
        }

        public static Dictionary<Key, FieldModelController> DefaultLayoutFields(Point pos, Size size, FieldModelController data = null)
        {
            // assign the default fields
            var fields = new Dictionary<Key, FieldModelController>
            {
                [DashConstants.KeyStore.WidthFieldKey] = new NumberFieldModelController(size.Width),
                [DashConstants.KeyStore.HeightFieldKey] = new NumberFieldModelController(size.Height),
                [DashConstants.KeyStore.PositionFieldKey] = new PointFieldModelController(pos),
                [DashConstants.KeyStore.ScaleAmountFieldKey] = new PointFieldModelController(1, 1),
                [DashConstants.KeyStore.ScaleCenterFieldKey] = new PointFieldModelController(0, 0),
                [HorizontalAlignmentKey] = new TextFieldModelController(HorizontalAlignment.Left.ToString()),
                [VerticalAlignmentKey] = new TextFieldModelController(VerticalAlignment.Top.ToString())
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
        protected static void BindOperationInteractions(FrameworkElement renderElement, FieldReference reference, Key fieldKey, FieldModelController fmController)
        {
            renderElement.ManipulationMode = ManipulationModes.All;
            renderElement.ManipulationDelta += (s, e) => { e.Handled = true; }; // this breaks interaction 
            renderElement.ManipulationStarted += delegate(object sender, ManipulationStartedRoutedEventArgs args)
            {
                var view = renderElement.GetFirstAncestorOfType<CollectionView>();
                if (view == null) return; // we can't always assume we're on a collection
                if (view.CanLink)
                {
                    //args.Complete(); -- This was stopping manipulations from happening on the first try? 
                    view.CanLink = false; // essential that this is false s.t. drag events don't get overriden
                }
            };
            renderElement.IsHoldingEnabled = true; // turn on holding

            // must hold on element first to fetch link node
            renderElement.Holding += delegate(object sender, HoldingRoutedEventArgs args)
            {
                var view = renderElement.GetFirstAncestorOfType<CollectionView>();
                if (view == null) return; // we can't always assume we're on a collection
                    view.CanLink = true;
                if (view.CurrentView is CollectionFreeformView)
                    (view.CurrentView as CollectionFreeformView).StartDrag(new OperatorView.IOReference(fieldKey, fmController, reference, true, view.PointerArgs, renderElement,
                        renderElement.GetFirstAncestorOfType<DocumentView>()));
            };
            renderElement.PointerPressed += delegate (object sender, PointerRoutedEventArgs args)
            {
                var view = renderElement.GetFirstAncestorOfType<CollectionView>();
                if (view == null) return; // we can't always assume we're on a collection
                    view.PointerArgs = args;
                args.Handled = true;
                if (args.GetCurrentPoint(view).Properties.IsLeftButtonPressed)
                {

                }
                else if (args.GetCurrentPoint(view).Properties.IsRightButtonPressed)
                {
                    view.CanLink = true;
                    if (view.CurrentView is CollectionFreeformView)
                        (view.CurrentView as CollectionFreeformView).StartDrag(new OperatorView.IOReference(fieldKey, fmController, reference, true, args, renderElement,
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
                    new OperatorView.IOReference(fieldKey, fmController, reference, false, args, renderElement,
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
            return (HorizontalAlignment) Enum.Parse(typeof(HorizontalAlignment), horizontalAlignmentController?.Data);
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
            document.SetField(DashConstants.KeyStore.WidthFieldKey, new NumberFieldModelController(width), true);
        }

        public static void SetHeight(this DocumentController document, double height)
        {
            document.SetField(DashConstants.KeyStore.HeightFieldKey, new NumberFieldModelController(height), true);
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