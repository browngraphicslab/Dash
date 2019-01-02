using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI;
using Dash.Converters;
using DashShared;
using static Dash.AnchorableAnnotation;
using Windows.UI.Xaml.Media;
using Dash.Controllers.Operators;

namespace Dash
{
    /// <summary>
    /// This class provides base functionality for creating and providing layouts to documents which contain data
    /// </summary>
    public abstract class CourtesyDocument
    {
        private static Dictionary<DocumentType, DocumentController> _prototypeDictionary =
            new Dictionary<DocumentType, DocumentController>();
        protected DocumentController GetLayoutPrototype(DocumentType documentType, string prototypeId, string abstractInterface)
        {
            if (_prototypeDictionary.TryGetValue(documentType, out var proto))
            {
                return proto;
            }
            proto = RESTClient.Instance.Fields.GetController<DocumentController>(prototypeId) ??
                   InstantiatePrototypeLayout(documentType, abstractInterface, prototypeId);
            _prototypeDictionary[documentType] = proto;
            return proto;
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
            throw new Exception("ActiveLayout code has not been updated yet");
            //dataDocument.SetActiveLayout(layoutDoc, forceMask: forceMask, addToLayoutList: addToLayoutList);
        }

        public static void BindWidth(FrameworkElement element, DocumentController docController, Context context)
        {
            FieldBinding<NumberController> binding = new FieldBinding<NumberController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = KeyStore.WidthFieldKey,
                Context = context,
                Tag="BindWidth in CourtesyDocument"
            };

            element.AddFieldBinding(FrameworkElement.WidthProperty, binding);
        }

        public static void BindHeight(FrameworkElement element, DocumentController docController, Context context)
        {
            var binding = new FieldBinding<NumberController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = KeyStore.HeightFieldKey,
                Context = context,
                Tag="BindHeight in CourtesyDocument"
            };

            element.AddFieldBinding(FrameworkElement.HeightProperty, binding);
        }

        public static void BindHorizontalAlignment(FrameworkElement element, DocumentController docController,
            HorizontalAlignment defaultValue)
        {
            var binding = docController == null ? null : new FieldBinding<TextController>()
            {
                Mode = BindingMode.OneWay,
                Document = docController,
                Key = KeyStore.HorizontalAlignmentKey,
                Converter = new StringToEnumConverter<HorizontalAlignment>(),
                Tag = "HorizontalAlignment binding in CourtesyDocument",
                FallbackValue = defaultValue
            };

            element.AddFieldBinding(FrameworkElement.HorizontalAlignmentProperty, binding);
        }

        public static void BindVerticalAlignment(FrameworkElement element, DocumentController docController,
            VerticalAlignment defaultValue)
        {
            var binding = docController == null ? null : new FieldBinding<TextController>()
            {
                Mode = BindingMode.OneWay,
                Document = docController,
                Key = KeyStore.VerticalAlignmentKey,
                Converter = new StringToEnumConverter<VerticalAlignment>(),
                Tag = "VerticalAlignment binding in CourtesyDocument",
                FallbackValue = defaultValue
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
				[KeyStore.InitialSizeKey] = new PointController(size.Width, size.Height),
                [KeyStore.WidthFieldKey] = new NumberController(size.Width),
                [KeyStore.HeightFieldKey] = new NumberController(size.Height),
                [KeyStore.PositionFieldKey] = new PointController(pos),
                [KeyStore.HorizontalAlignmentKey] = new TextController(HorizontalAlignment.Left.ToString()),
                [KeyStore.VerticalAlignmentKey] = new TextController(VerticalAlignment.Top.ToString()),
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
    }

    public enum LinkBehavior {
        Follow,
        Annotate,
        Dock,
        Float,
        Overlay,
        ShowRegion,
        ShowDocument
    }

    public static class CourtesyDocumentExtensions
    {
        public static  DocumentController GetDataDocument(this DocumentController document)
        {
            return document.GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, null) ?? document;
        }

        public static string GetTitle(this DocumentController document)
        {
            return document.GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data ??
                   document.GetDataDocument().GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data ??
                   document.DocumentType.Type;
        }
        public static void   SetTitle(this DocumentController document, string title)
        {
            document.SetField<TextController>(KeyStore.TitleKey, title, true);
        }

        public static void         SetLinkBehavior(this DocumentController document, LinkBehavior behavior)
        {
            document.SetField<TextController>(KeyStore.LinkBehaviorKey, behavior.ToString(), true);
        }
        public static LinkBehavior GetLinkBehavior(this DocumentController document)
        {
            var data = document.GetDereferencedField<TextController>(KeyStore.LinkBehaviorKey, null)?.Data;
            return data == null ? LinkBehavior.Annotate : Enum.Parse<LinkBehavior>(data);
        }

        public static void   SetXaml(this DocumentController document, string xaml)
        {
            document.SetField<TextController>(KeyStore.XamlKey, xaml, true);
        }
        public static string GetXaml(this DocumentController document)
        {
            return document.GetDereferencedField<TextController>(KeyStore.XamlKey,null)?.Data;
        }

        public static void                SetHorizontalAlignment(this DocumentController document, HorizontalAlignment alignment)
        {
            document.SetField<TextController>(KeyStore.HorizontalAlignmentKey, alignment.ToString(), true);
        }
        public static HorizontalAlignment GetHorizontalAlignment(this DocumentController document)
        {
            var data = document.GetDereferencedField<TextController>(KeyStore.HorizontalAlignmentKey,null)?.Data;
            return data == null ? HorizontalAlignment.Stretch : Enum.Parse<HorizontalAlignment>(data);
        }

        public static void              SetVerticalAlignment(this DocumentController document, VerticalAlignment alignment)
        {
            document.SetField<TextController>(KeyStore.VerticalAlignmentKey, alignment.ToString(), true);
        }
        public static VerticalAlignment GetVerticalAlignment(this DocumentController document)
        {
            var data =  document.GetDereferencedField<TextController>(KeyStore.VerticalAlignmentKey,null)?.Data ; 
            return data == null ? VerticalAlignment.Stretch : Enum.Parse<VerticalAlignment>(data);
        }

        public static bool    GetFitToParent(this DocumentController document)
        {
            return document.GetDereferencedField<BoolController>(KeyStore.CollectionFitToParentKey, null)?.Data ?? false;
        }
        public static void    SetFitToParent(this DocumentController document, bool fit)
        {
            document.SetField<BoolController>(KeyStore.CollectionFitToParentKey, fit, true);
        }

        public static bool    GetIsButton(this DocumentController document)
        {
            var data = document.GetDereferencedField<BoolController>(KeyStore.IsButtonKey, null);
            return data?.Data == true;
        }
        public static void    SetIsButton(this DocumentController document, bool button)
        {
            document.SetField<BoolController>(KeyStore.IsButtonKey, button, true);
        }

        public static bool    GetAreContentsHitTestVisible(this DocumentController document)
        {
            var data = document.GetDereferencedField<BoolController>(KeyStore.AreContentsHitTestVisibleKey, null);
            return data?.Data != false;
        }
        public static void    SetAreContentsHitTestVisible(this DocumentController document, bool adornment)
        {
            document.SetField<BoolController>(KeyStore.AreContentsHitTestVisibleKey, adornment, true);
        }

        public static bool    GetIsAdornment(this DocumentController document)
        {
            var data = document.GetDereferencedField<BoolController>(KeyStore.IsAdornmentKey, null);
            return data?.Data == true;
        }
        public static void    SetIsAdornment(this DocumentController document,bool adornment)
        {
            document.SetField<BoolController>(KeyStore.IsAdornmentKey, adornment, true);
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

        public static Point   GetPosition(this DocumentController document)
        {
            return document.GetDereferencedField<PointController>(KeyStore.PositionFieldKey, null)?.Data ?? new Point();
        }
        public static void    SetPosition(this DocumentController document, Point pos)
        {
            document.SetField<PointController>(KeyStore.PositionFieldKey, pos, true);
        }

        public static void    SetActualSize(this DocumentController document, Point pos)
        {
            document.SetField<PointController>(KeyStore.ActualSizeKey, pos, true);
        }
        public static Point   GetActualSize(this DocumentController document)
        {
            return document.GetDereferencedField<PointController>(KeyStore.ActualSizeKey, null)?.Data ?? new Point();
        }

        public static bool    GetHidden(this DocumentController document)
        {
            return document.GetDereferencedField<BoolController>(KeyStore.HiddenKey, null)?.Data ?? false;
        }
        public static void    SetHidden(this DocumentController document, bool hidden)
        {
            document.SetField<BoolController>(KeyStore.HiddenKey, hidden, true);
        }
        public static void    ToggleHidden(this DocumentController document)
        {
            document.SetHidden(!document.GetHidden());
        }

        public static ListController<OperatorController> GetBehaviors(this DocumentController document, KeyController behaviorKey)
        {
            return document.GetDataDocument().GetField<ListController<OperatorController>>(behaviorKey);
        }

        public static ListController<DocumentController> GetLinks(this DocumentController document, KeyController linkFromOrToKey)
        {
            if (linkFromOrToKey == null)
            {
                var fromLinks = document.GetLinks(KeyStore.LinkFromKey);
                var toLinks   = document.GetLinks(KeyStore.LinkToKey);
                var allinks   = new ListController<DocumentController>(fromLinks);
                allinks.AddRange(toLinks);
                return allinks;
            }
            return document.GetDereferencedField<ListController<DocumentController>>(linkFromOrToKey, null) ?? new ListController<DocumentController>();
        }
        public static void                               AddToLinks(this DocumentController document, KeyController LinkFromOrToKey, List<DocumentController> docs)
        {
            var todocs = document.GetDereferencedField<ListController<DocumentController>>(LinkFromOrToKey, null);
            if (todocs == null)
            {
                document.SetField(LinkFromOrToKey, new ListController<DocumentController>(docs), true);
            }
            else
            {
                todocs.AddRange(docs);
            }
        }

        public static ListController<DocumentController> GetRegions(this DocumentController document)
        {
            return document.GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null);
        }
        public static void                               AddToRegions(this DocumentController document, List<DocumentController> regions)
        {
            var curRegions = document.GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null);
            if (curRegions == null)
            {
                document.SetField(KeyStore.RegionsKey, new ListController<DocumentController>(regions), true);
            }
            else
                curRegions.AddRange(regions);
        }
        
        public static TextController     GetLinkTag(this DocumentController document)
        {
            return document.GetDereferencedField<TextController>(KeyStore.LinkTagKey, null);
        }
        public static void               SetLinkTag(this DocumentController document, string tag)
        {
            document.SetField<TextController>(KeyStore.LinkTagKey, tag, true);
        }

        public static DocumentController GetRegionDefinition(this DocumentController document)
        {
            return document.GetDataDocument().GetDereferencedField<DocumentController>(KeyStore.RegionDefinitionKey, null);
        }
        public static void               SetRegionDefinition(this DocumentController document, DocumentController regionParent)
        {
            document.GetDataDocument().SetField(KeyStore.RegionDefinitionKey, regionParent, true);
        }

        public static void               SetAnnotationType(this DocumentController document, AnnotationType annotationType)
        {
            document.GetDataDocument().SetField<TextController>(KeyStore.RegionTypeKey, annotationType.ToString(), true);
        }
        public static AnnotationType     GetAnnotationType(this DocumentController document)
        {
            var t = document.GetDataDocument().GetField<TextController>(KeyStore.RegionTypeKey);
            return t == null
                ? AnnotationType.None
                : Enum.Parse<AnnotationType>(t.Data);
        }

        public static bool   GetTransient(this DocumentController document)
        {
            var data = document.GetDereferencedField<BoolController>(KeyStore.TransientKey, null)?.Data;
            return data ?? false;
        }
        public static void   SetTransient(this DocumentController document, bool hidden)
        {
            document.SetField<BoolController>(KeyStore.TransientKey, hidden, true);
        }

        public static int?   GetSideCount(this DocumentController document)
        {
            return (int?)document.GetDereferencedField<NumberController>(KeyStore.SideCountKey, null)?.Data;
        }
        public static void   SetSideCount(this DocumentController document, int count)
        {
            document.SetField<NumberController>(KeyStore.SideCountKey, count, true);
        }

        public static void   SetWidth(this DocumentController document, double width)
        {
            document.SetField<NumberController>(KeyStore.WidthFieldKey, width, true);
        }
        public static double GetWidth(this DocumentController document)
        {
            return document.GetDereferencedField<NumberController>(KeyStore.WidthFieldKey, null)?.Data ?? double.NaN;
        }

        public static void   SetHeight(this DocumentController document, double height)
        {
            document.SetField<NumberController>(KeyStore.HeightFieldKey, height, true);
        }
        public static double GetHeight(this DocumentController document)
        {
            return document.GetDereferencedField<NumberController>(KeyStore.HeightFieldKey, null)?.Data ?? double.NaN;
        }

        public static Rect   GetBounds(this DocumentController document)
        {
            var pos = document.GetPosition();
            return new TranslateTransform { X = pos.X, Y = pos.Y }.TransformBounds(new Rect(0, 0, document.GetActualSize().X, document.GetActualSize().Y));
        }

        public static string GetDocType(this DocumentController document)
        {
            return document.GetDereferencedField<TextController>(KeyStore.DocumentTypeKey, null)?.Data;
        }

        public static string GetAuthor(this DocumentController document)
        {
            return document.GetDereferencedField<TextController>(KeyStore.AuthorKey, null)?.Data;
        }

        public static DocumentController   GetLinkedDocument(this DocumentController document, LinkDirection direction, bool inverse = false)
        {
            var key = (direction == LinkDirection.ToDestination ^ inverse) ? KeyStore.LinkDestinationKey : KeyStore.LinkSourceKey;
            return document.GetDataDocument().GetDereferencedField<DocumentController>(key, null);
        }
        public static void                 GotoRegion(this DocumentController document, DocumentController region, DocumentController link = null)
        {
            if (!document.Equals(region))
            {
                document.RemoveField(KeyStore.GoToRegionLinkKey);
                document.RemoveField(KeyStore.GoToRegionKey);
                document.SetFields(new[] {
                    new KeyValuePair<KeyController, FieldControllerBase>(KeyStore.GoToRegionLinkKey, link),
                    new KeyValuePair<KeyController, FieldControllerBase>(KeyStore.GoToRegionKey, region)
                }, true);
            }
        }
        public static AnchorableAnnotation CreateAnnotationAnchor(this DocumentController regionDocumentController, AnnotationOverlay overlay)
        {
            var t = regionDocumentController.GetDataDocument().GetDereferencedField<TextController>(KeyStore.RegionTypeKey, null);
            var annoType = t == null
                ? AnnotationType.None
                : Enum.Parse<AnnotationType>(t.Data);

            switch (annoType)
            {
            case AnnotationType.Pin:
                return new PinAnnotation(overlay, new Selection(regionDocumentController,
                              new SolidColorBrush(Color.FromArgb(255, 0x1f, 0xff, 0)), new SolidColorBrush(Colors.Red)));
            case AnnotationType.Region: return new RegionAnnotation(overlay, new Selection(regionDocumentController));
            case AnnotationType.Selection: return new TextAnnotation(overlay, new Selection(regionDocumentController, new SolidColorBrush(Color.FromArgb(128, 200, 200, 0)), new SolidColorBrush(Colors.LightGray)));
            }
            return null;
        }
        /// <summary>
        /// Links this document to a target document with the specified link following behavior and an optional title.
        /// </summary>
        public static DocumentController  Link(this DocumentController source, DocumentController target, LinkBehavior behavior, string specTitle = null)
        {
            //document that represents the actual link
            var linkDocument = new RichTextNote("<add link description here>").Document;
            linkDocument.GetDataDocument().RemoveOperatorForKey(KeyStore.TitleKey);
            linkDocument.GetDataDocument().SetField(KeyStore.LinkSourceTitleKey,
                new PointerReferenceController(new DocumentReferenceController(linkDocument.GetDataDocument(), KeyStore.LinkSourceKey), KeyStore.TitleKey), true);
            linkDocument.GetDataDocument().SetField(KeyStore.LinkDestinationTitleKey,
                new PointerReferenceController(new DocumentReferenceController(linkDocument.GetDataDocument(), KeyStore.LinkDestinationKey), KeyStore.TitleKey), true);
            linkDocument.GetDataDocument().GetFieldOrCreateDefault<ListController<OperatorController>>(KeyStore.OperatorKey, true).
                                   AddRange(new OperatorController[] { new LinkTitleOperatorController(), new LinkDescriptionTextOperator() });

            linkDocument.GetDataDocument().SetLinkBehavior(behavior);
            linkDocument.GetDataDocument().SetField(KeyStore.LinkSourceKey,      source, true);
            linkDocument.GetDataDocument().SetField(KeyStore.LinkDestinationKey, target, true);
            linkDocument.GetDataDocument().SetField<TextController>(KeyStore.LinkTagKey, specTitle ?? "Annotation", true);
            target.GetDataDocument().AddToLinks(KeyStore.LinkFromKey, new List<DocumentController> { linkDocument });
            source.GetDataDocument().AddToLinks(KeyStore.LinkToKey, new List<DocumentController> { linkDocument });
            linkDocument.SetXaml(
//@"<Grid
//    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
//    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
//    xmlns:dash=""using:Dash""
//    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"">

//    <Grid Padding=""10"" Background=""LightGray"">
//        <Grid.RowDefinitions>
//            <RowDefinition Height=""25""/>
//            <RowDefinition/>
//        </Grid.RowDefinitions>
//        <dash:RichEditView x:Name=""xRichTextFieldData"" Foreground=""White"" HorizontalAlignment=""Stretch"" Grid.Row=""0"" VerticalAlignment=""Top"" />

//        <Grid Grid.Row=""1"">
//            <Grid.ColumnDefinitions >
//                <ColumnDefinition MinWidth=""200"" />
//                <ColumnDefinition MinWidth=""200"" />
//            </Grid.ColumnDefinitions>
//            <dash:DocumentView x:Name=""xDocumentFieldLinkSource"" Grid.Column=""0"" HorizontalAlignment=""Stretch"" VerticalAlignment=""Top"" />
//            <dash:DocumentView x:Name=""xDocumentFieldLinkDestination"" Grid.Column=""1"" HorizontalAlignment=""Stretch"" VerticalAlignment=""Top""/>
//        </Grid>
//    </Grid>
//</Grid>");
@"<Grid 
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:controls= ""using:Microsoft.Toolkit.Uwp.UI.Controls""
    xmlns:dash=""using:Dash""
    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
    >
    <Grid.RowDefinitions>
        <RowDefinition Height=""Auto""/>
        <RowDefinition Height=""Auto""/>
        <RowDefinition Height=""*""/>
    </Grid.RowDefinitions>
    <controls:Expander Padding=""10"" Grid.Row=""0"">
        <controls:Expander.Header>
            <RelativePanel>
                <TextBlock x:Name=""xSrc"" Text=""Source: "" />
                <Grid RelativePanel.RightOf=""xSrc"">
                    <dash:EditableTextBlock x:Name=""xTextFieldLinkSource__Title"" />
                </Grid>
            </RelativePanel>
        </controls:Expander.Header>
        <controls:Expander.Content>
            <dash:DocumentView x:Name=""xDocumentFieldLinkSource"" Grid.Row=""0""/>
        </controls:Expander.Content>
    </controls:Expander>
    <controls:Expander Padding=""10"" Grid.Row=""1"" IsExpanded=""True"">
        <controls:Expander.Header>
            <RelativePanel>
                <TextBlock x:Name=""xTarget"" Text=""Target: "" />
                <Grid RelativePanel.RightOf=""xTarget"">
                    <dash:EditableTextBlock x:Name=""xTextFieldLinkDestination__Title"" />
                </Grid>
            </RelativePanel>
        </controls:Expander.Header>
        <controls:Expander.Content>
            <dash:DocumentView x:Name=""xDocumentFieldLinkDestination"" Grid.Row=""0""/>
        </controls:Expander.Content>
    </controls:Expander>
    <controls:Expander Padding=""10"" Grid.Row=""2"" >
        <controls:Expander.Header>
            <RelativePanel>
                <TextBlock x:Name=""xDesc"" Text=""Description: "" />
                <Grid RelativePanel.RightOf=""xDesc"">
                    <dash:EditableTextBlock x:Name=""xTextFieldLinkTag"" />
                </Grid>
            </RelativePanel>
        </controls:Expander.Header>
        <controls:Expander.Content>
            <dash:RichEditView x:Name=""xRichTextFieldData"" MinWidth=""100"" RelativePanel.RightOf=""xSource""/>
        </controls:Expander.Content>
    </controls:Expander>
</Grid>");

            return linkDocument;
        }
    }
}
