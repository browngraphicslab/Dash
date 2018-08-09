using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI;
using Dash.Converters;
using DashShared;

namespace Dash
{
    /// <summary>
    /// This class provides base functionality for creating and providing layouts to documents which contain data
    /// </summary>
    public abstract class CourtesyDocument
    {
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
            DocumentController.DocumentUpdatedHandler handler = (sender, args, c) =>
            {
                if (args.Action == DocumentController.FieldUpdatedAction.Update) return;
                bindingDelegate(element, sender, c); //TODO Should be context or args.Context?
            };

            AddHandlers(element, docController, k, context, bindingDelegate, handler);
        }

        protected static void AddHandlers<T>(T element, DocumentController docController, KeyController k, Context context,
            BindingDelegate<T> bindingDelegate, DocumentController.DocumentUpdatedHandler handler) where T : FrameworkElement
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

        protected static void SetupBindings(FrameworkElement element, DocumentController docController, Context context)
        {
            //Set width and height
            BindWidth(element, docController, context);
            BindHeight(element, docController, context);

            //Set alignments
            BindHorizontalAlignment(element, docController, context);
            BindVerticalAlignment(element, docController, context);
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

        protected static PointController  GetPositionField(DocumentController docController, Context context)
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
            document.SetField<TextController>(KeyStore.HorizontalAlignmentKey, alignment.ToString(), true);
        }
        public static HorizontalAlignment GetHorizontalAlignment(this DocumentController document)
        {
            var data = document.GetField<TextController>(KeyStore.HorizontalAlignmentKey)?.Data;
            return data == null ? HorizontalAlignment.Stretch : Enum.Parse<HorizontalAlignment>(data);
        }

        public static void SetVerticalAlignment(this DocumentController document, VerticalAlignment alignment)
        {
            document.SetField<TextController>(KeyStore.VerticalAlignmentKey, alignment.ToString(), true);
        }
        public static VerticalAlignment GetVerticalAlignment(this DocumentController document)
        {
            var data =  document.GetField<TextController>(KeyStore.VerticalAlignmentKey)?.Data ; 
            return data == null ? VerticalAlignment.Stretch : Enum.Parse<VerticalAlignment>(data);
        }

        public static bool GetFitToParent(this DocumentController document)
        {
            var data = document.GetDereferencedField<TextController>(KeyStore.CollectionFitToParentKey, null);
            return data?.Data == "true";
        }
        public static void    SetFitToParent(this DocumentController document, bool fit)
        {
            document.SetField<TextController>(KeyStore.CollectionFitToParentKey, fit ? "true": "false", true);
        }
        public static void    SetTitle(this DocumentController document, string title)
        {
            document.SetField<TextController>(KeyStore.TitleKey, title, true);
        }

        public static bool    GetIsAdornment(this DocumentController document)
        {
            var data = document.GetDereferencedField<TextController>(KeyStore.IsAdornmentKey, null);
            return data?.Data == "true";
        }
        public static void    SetIsAdornment(this DocumentController document,bool adornment)
        {
            document.SetField<TextController>(KeyStore.IsAdornmentKey, adornment ? "true":"false", true);
        }

        public static  Color? GetBackgroundColor(this DocumentController document)
        {
            var col = document.GetDereferencedField<TextController>(KeyStore.BackgroundColorKey, null);

            return col != null ? (new StringToBrushConverter().ConvertDataToXaml(col.Data) as Windows.UI.Xaml.Media.SolidColorBrush).Color : (Color ?) null;
        }
        public static void    SetBackgroundColor(this DocumentController document, Color color)
        {
            document.SetField<TextController>(KeyStore.BackgroundColorKey, color.ToString(), true);
        }

        public static Point?  GetPosition(this DocumentController document)
        {
            return document.GetDereferencedField<PointController>(KeyStore.PositionFieldKey, null)?.Data;
        }
        public static void    SetPosition(this DocumentController document, Point pos)
        {
            document.SetField<PointController>(KeyStore.PositionFieldKey, pos, true);
        }

        public static void    SetActualSize(this DocumentController document, Point pos)
        {
            document.SetField<PointController>(KeyStore.ActualSizeKey, pos, true);
        }
        public static Point?  GetActualSize(this DocumentController document)
        {
            return document.GetField<PointController>(KeyStore.ActualSizeKey)?.Data;
        }

        public static bool    GetHidden(this DocumentController document)
        {
            var data = document.GetDereferencedField<BoolController>(KeyStore.HiddenKey, null);
            return data?.Data ?? false;
        }
        public static void    SetHidden(this DocumentController document, bool hidden)
        {
            //TODO This should use a BoolController
            document.SetField<BoolController>(KeyStore.HiddenKey, hidden, true);
        }

        public static void ToggleHidden(this DocumentController document)
        {
            var hiddenField = document.GetFieldOrCreateDefault<BoolController>(KeyStore.HiddenKey);
            hiddenField.Data = !hiddenField.Data;
        }

        public static ListController<DocumentController> GetLinks(this DocumentController document, KeyController linkFromOrToKey)
        {
            return document.GetDereferencedField<ListController<DocumentController>>(linkFromOrToKey, null);
        }
        public static void AddToLinks(this DocumentController document, KeyController LinkFromOrToKey, List<DocumentController> docs)
        {
            var todocs = document.GetLinks(LinkFromOrToKey);
            if (todocs == null)
            {
                document.SetField(LinkFromOrToKey, new ListController<DocumentController>(docs), true);
            }
            else
                todocs.AddRange(docs);
        }
        public static ListController<DocumentController> GetRegions(this DocumentController document)
        {
            return document.GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null);
        }
        public static void AddToRegions(this DocumentController document, List<DocumentController> regions)
        {
            var curRegions = document.GetLinks(KeyStore.RegionsKey);
            if (curRegions == null)
            {
                document.SetField(KeyStore.RegionsKey, new ListController<DocumentController>(regions), true);
            }
            else
                curRegions.AddRange(regions);
        }

        public static DocumentController GetRegionDefinition(this DocumentController document)
        {
            return document.GetDataDocument().GetDereferencedField<DocumentController>(KeyStore.RegionDefinitionKey, null);
        }
        public static void SetRegionDefinition(this DocumentController document, DocumentController regionParent)
        {
            document.GetDataDocument().SetField(KeyStore.RegionDefinitionKey, regionParent, true);
        }

        public static void SetAnnotationType(this DocumentController document, AnnotationType annotationType)
        {
            document.GetDataDocument().SetField<TextController>(KeyStore.RegionTypeKey, annotationType.ToString(), true);
        }

        public static AnnotationType GetAnnotationType(this DocumentController document)
        {
            var t = document.GetDataDocument().GetField<TextController>(KeyStore.RegionTypeKey);
            return t == null
                ? AnnotationType.None
                : Enum.Parse<AnnotationType>(t.Data);
        }

        public static DocumentController GetLinkedDocument(this DocumentController document, LinkDirection direction, bool inverse = false)
        {
            var key = (direction == LinkDirection.ToDestination ^ inverse) ? KeyStore.LinkDestinationKey : KeyStore.LinkSourceKey;
            return document.GetDataDocument().GetDereferencedField<DocumentController>(key, null);
        }

        public static void GotoRegion(this DocumentController document, DocumentController region,
            DocumentController link = null)
        {
            if (document.Equals(region))
            {
                return;
            }
            document.SetFields(new []
            {
                new KeyValuePair<KeyController, FieldControllerBase>(KeyStore.GoToRegionLinkKey, link),
                new KeyValuePair<KeyController, FieldControllerBase>(KeyStore.GoToRegionKey, region)
            }, true);
        }

        public static bool GetTransient(this DocumentController document)
        {
            var data = document.GetDereferencedField<BoolController>(KeyStore.TransientKey, null)?.Data;
            return data ?? false;
        }
        public static void SetTransient(this DocumentController document, bool hidden)
        {
            document.SetField<BoolController>(KeyStore.TransientKey, hidden, true);
        }

        public static int? GetSideCount(this DocumentController document)
        {
            return (int?)document.GetDereferencedField<NumberController>(KeyStore.SideCountKey, null)?.Data;
        }
        public static void SetSideCount(this DocumentController document, int count)
        {
            document.SetField<NumberController>(KeyStore.SideCountKey, count, true);
        }

        public static void SetWidth(this DocumentController document, double width)
        {
            document.SetField<NumberController>(KeyStore.WidthFieldKey, width, true);
        }

        public static double GetWidth(this DocumentController document)
        {
            return document.GetDereferencedField<NumberController>(KeyStore.WidthFieldKey, null)?.Data ?? 0;
        }

        public static void SetHeight(this DocumentController document, double height)
        {
            document.SetField<NumberController>(KeyStore.HeightFieldKey, height, true);
        }

        public static double GetHeight(this DocumentController document)
        {
            return document.GetDereferencedField<NumberController>(KeyStore.HeightFieldKey, null)?.Data ?? 0;
        }
        
    }
}