using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Dash.Converters;
using DashShared;

namespace Dash
{
    /// <summary>
    /// This class provides base functionality for creating and providing layouts to documents which contain data
    /// </summary>
    public abstract class CourtesyDocument
    {
        public static readonly KeyController GridRowKey = new KeyController("FC447698-1C96-4014-94A5-845D411C1CD1", "Grid.Row");
        public static readonly KeyController GridColumnKey = new KeyController("E6663AA3-26E1-48D1-8A95-768EC0CFD4BC", "Grid.Column");
        public static readonly KeyController GridRowSpanKey = new KeyController("3F305CD6-343E-4155-AFEB-5530E499727C", "Grid.RowSpan");
        public static readonly KeyController GridColumnSpanKey = new KeyController("C0A16508-76AF-42B5-A3D7-D693FDD5AA84", "Grid.ColumnSpan");

        protected DocumentController GetLayoutPrototype(DocumentType documentType, string prototypeId, string abstractInterface)
        {

            return ContentController<FieldModel>.GetController<DocumentController>(prototypeId) ??
                   InstantiatePrototypeLayout(documentType, abstractInterface, prototypeId);
        }

        public virtual DocumentController Document { get; set; }

        protected DocumentController InstantiatePrototypeLayout(DocumentType documentType, string abstractInterface, string prototypeId)
        {
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN));
            fields.Add(KeyStore.AbstractInterfaceKey, new TextController(abstractInterface));
            return new DocumentController(fields, documentType, prototypeId);
        }

    /// <summary>
    /// Fully dereference the field associated with the data key in the passed in docController
    /// //TODO explain default field model controller idea here once it is written
    /// </summary>
    /// <returns></returns>
    protected static FieldControllerBase GetDereferencedDataFieldModelController(DocumentController docController, Context context, FieldControllerBase defaultFieldModelController, out ReferenceController refToData)
        {
            refToData = docController.GetField(KeyStore.DataKey) as ReferenceController;
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
                    parent.SetField((refToData as ReferenceController).FieldKey, defaultFieldModelController, true);
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


        [Obsolete("Use FieldBindings and AddFieldBinding instead")]
        protected static void AddBinding<T>(T element, DocumentController docController, KeyController k, Context context,
            BindingDelegate<T> bindingDelegate) where T : FrameworkElement
        {
            FieldControllerBase.FieldUpdatedHandler handler = (sender, args, c) =>
            {
                if (args.Action == DocumentController.FieldUpdatedAction.Update) return;
                bindingDelegate(element, (DocumentController)sender, c); //TODO Should be context or args.Context?
            };

            AddHandlers(element, docController, k, context, bindingDelegate, handler);
        }

        protected static void AddHandlers<T>(T element, DocumentController docController, KeyController k, Context context,
            BindingDelegate<T> bindingDelegate, FieldControllerBase.FieldUpdatedHandler handler) where T : FrameworkElement
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

        protected static void BindGridRow(FrameworkElement element, DocumentController docController, Context context)
        {
            FieldBinding<NumberController> binding = new FieldBinding<NumberController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = GridRowKey,
                FallbackValue = 0,
                Context = context
            };

            element.AddFieldBinding(Grid.RowProperty, binding);
        }

        protected static void BindGridColumn(FrameworkElement element, DocumentController docController, Context context)
        {
            FieldBinding<NumberController> binding = new FieldBinding<NumberController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = GridColumnKey,
                FallbackValue = 0,
                Context = context
            };

            element.AddFieldBinding(Grid.ColumnProperty, binding);
        }

        protected static void BindGridRowSpan(FrameworkElement element, DocumentController docController, Context context)
        {
            FieldBinding<NumberController> binding = new FieldBinding<NumberController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = GridRowSpanKey,
                FallbackValue = 1,
                Context = context
            };

            element.AddFieldBinding(Grid.RowSpanProperty, binding);
        }

        protected static void BindGridColumnSpan(FrameworkElement element, DocumentController docController, Context context)
        {
            FieldBinding<NumberController> binding = new FieldBinding<NumberController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = GridColumnSpanKey,
                FallbackValue = 1,
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

        /// <summary>
        /// Adds the default fields necessary for rendering a layout, such as height, width, things for the render transform.
        /// this should be one of the first places you look if something isn't being rendered properly
        /// 
        /// <para>
        /// Takes in a FieldController <paramref name="data"/> which is stored in the <see cref="KeyStore.DataKey"/> and is
        /// usually a reference to the data which this layout is supposed to render
        /// </para>
        /// <returns></returns>
        public static Dictionary<KeyController, FieldControllerBase> DefaultLayoutFields(Point pos, Size size, FieldControllerBase data = null)
        {
            // assign the default fields
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.WidthFieldKey] = new NumberController(size.Width),
                [KeyStore.HeightFieldKey] = new NumberController(size.Height),
                [KeyStore.PositionFieldKey] = new PointController(pos),
                [KeyStore.ScaleAmountFieldKey] = new PointController(1, 1),
                [KeyStore.HorizontalAlignmentKey] = new TextController(HorizontalAlignment.Stretch.ToString()),
                [KeyStore.VerticalAlignmentKey] = new TextController(VerticalAlignment.Stretch.ToString()),
                [KeyStore.ActualSizeKey] = new PointController(double.NaN, double.NaN),
                
            };
            if (data != null)
                fields.Add(KeyStore.DataKey, data);

            return fields;
        }

        public void SetupDocument(DocumentType documentType, string prototypeId, string abstractInterface, IEnumerable<KeyValuePair<KeyController, FieldControllerBase>> fields)
        {
            Document = GetLayoutPrototype(documentType, prototypeId, abstractInterface).MakeDelegate();
            Document.SetFields(fields, true);
        }

        #region GettersAndSetters

        protected static NumberController GetHeightField(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(KeyStore.HeightFieldKey)
                .DereferenceToRoot<NumberController>(context);
        }

        protected static NumberController GetWidthField(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(KeyStore.WidthFieldKey)
                .DereferenceToRoot<NumberController>(context);
        }

        protected static PointController GetPositionField(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(KeyStore.PositionFieldKey)
                .DereferenceToRoot<PointController>(context);
        }

        #endregion
    }

    public static class CourtesyDocumentExtensions
    {
        public static void SetHorizontalAlignment(this DocumentController document, HorizontalAlignment alignment)
        {
            document.SetField(KeyStore.HorizontalAlignmentKey, new TextController(alignment.ToString()), true);
        }


        public static HorizontalAlignment GetHorizontalAlignment(this DocumentController document)
        {
            var horizontalAlignmentController =
                document.GetField(KeyStore.HorizontalAlignmentKey) as TextController;
            if (horizontalAlignmentController == null)
            {
                return HorizontalAlignment.Stretch;
            }
            return (HorizontalAlignment)Enum.Parse(typeof(HorizontalAlignment), horizontalAlignmentController?.Data);
        }

        public static void SetVerticalAlignment(this DocumentController document, VerticalAlignment alignment)
        {
            var currentHeight = document.GetHeightField().Data;
            document.SetField(KeyStore.VerticalAlignmentKey, new TextController(alignment.ToString()), true);
            document.SetHeight(currentHeight);
        }

        public static VerticalAlignment GetVerticalAlignment(this DocumentController document)
        {
            var verticalAlignmentController =
                document.GetField(KeyStore.VerticalAlignmentKey) as TextController;
            if (verticalAlignmentController == null)
            {
                return VerticalAlignment.Stretch;
            }
            return (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment), verticalAlignmentController?.Data);
        }

        public static void SetWidth(this DocumentController document, double width)
        {
            document.SetField(KeyStore.WidthFieldKey, new NumberController(width), true);
        }

        public static void SetHeight(this DocumentController document, double height)
        {
            document.SetField(KeyStore.HeightFieldKey, new NumberController(height), true);
        }

        public static void SetGridRow(this DocumentController document, int row)
        {
            document.SetField(CourtesyDocument.GridRowKey, new NumberController(row), true);
        }

        public static void SetGridColumn(this DocumentController document, int column)
        {
            document.SetField(CourtesyDocument.GridColumnKey, new NumberController(column), true);
        }

        public static void SetGridRowSpan(this DocumentController document, int rowSpan)
        {
            document.SetField(CourtesyDocument.GridRowSpanKey, new NumberController(rowSpan), true);
        }

        public static void SetGridColumnSpan(this DocumentController document, int columnSpan)
        {
            document.SetField(CourtesyDocument.GridColumnSpanKey, new NumberController(columnSpan), true);
        }
    }
}