using System.Collections.Generic;
using DashShared;
using Windows.UI.Xaml;

namespace Dash
{
    public class KeyStore
    {
        public static KeyController DocumentContextKey = new KeyController(DashConstants.KeyStore.DocumentContextKey);
        public static KeyController AbstractInterfaceKey = new KeyController(DashConstants.KeyStore.AbstractInterfaceKey);
        public static KeyController LayoutListKey = new KeyController(DashConstants.KeyStore.LayoutListKey);
        public static KeyController RegionsKey = new KeyController("1B958E26-624B-4E9A-82C9-2E18609D6A39", "Regions");
        public static KeyController RegionDefinitionKey = new KeyController("6EEDCB86-76F4-4937-AE0D-9C4BC6744310", "Region Definition");
        public static KeyController ActiveLayoutKey = new KeyController(DashConstants.KeyStore.ActiveLayoutKey);
        public static KeyController TitleKey = new KeyController(DashConstants.KeyStore.TitleKey);
        public static KeyController CaptionKey = new KeyController(DashConstants.KeyStore.CaptionKey);
        public static KeyController PrototypeKey = new KeyController(DashConstants.KeyStore.PrototypeKey);
        public static KeyController DelegatesKey = new KeyController(DashConstants.KeyStore.DelegatesKey);
        public static KeyController UserSetWidthKey = new KeyController(DashConstants.KeyStore.UserSetWidthKey);
        public static KeyController WidthFieldKey = new KeyController(DashConstants.KeyStore.WidthFieldKey);
        public static KeyController HeightFieldKey = new KeyController(DashConstants.KeyStore.HeightFieldKey);
        public static KeyController TransientKey = new KeyController("7553FBFA-B4C4-46D7-AEA6-76B22C5A3425", "Transient");
        public static KeyController HiddenKey = new KeyController("A99659B8-F34A-4F0D-8BA7-0030DA8B4EA6", "Hidden");
        public static KeyController DataKey = new KeyController(DashConstants.KeyStore.DataKey);
        public static KeyController SourecUriKey = new KeyController(DashConstants.KeyStore.SourceUriKey);
        public static KeyController IsAdornmentKey = new KeyController(DashConstants.KeyStore.AdornmentKey);
        public static KeyController DocumentTextKey = new KeyController(DashConstants.KeyStore.DocumentTextKey);
        public static KeyController TextWrappingKey = new KeyController(DashConstants.KeyStore.TextWrappingKey);
        public static KeyController BackgroundColorKey = new KeyController(DashConstants.KeyStore.BackgroundColorKey);
        public static KeyController OpacitySliderValueKey = new KeyController(DashConstants.KeyStore.OpacitySliderValueKey);
        public static KeyController AdornmentShapeKey = new KeyController(DashConstants.KeyStore.AdornmentShapeKey);
        public static KeyController PositionFieldKey = new KeyController(DashConstants.KeyStore.PositionFieldKey);
        public static KeyController LinkFromKey = new KeyController(DashConstants.KeyStore.LinkFromFieldKey);
        public static KeyController LinkToKey = new KeyController(DashConstants.KeyStore.LinkToFieldKey);
        public static KeyController PdfVOffsetFieldKey = new KeyController(new KeyModel("8990098B-83D2-4817-A275-82D8282ECD79", "_PdfVOffset"));
        public static KeyController ScaleAmountFieldKey = new KeyController(DashConstants.KeyStore.ScaleAmountFieldKey);
        public static KeyController IconTypeFieldKey = new KeyController(DashConstants.KeyStore.IconTypeFieldKey);
        public static KeyController SystemUriKey = new KeyController(DashConstants.KeyStore.SystemUriKey);
        public static KeyController ThumbnailFieldKey = new KeyController(DashConstants.KeyStore.ThumbnailFieldKey);
        public static KeyController HeaderKey = new KeyController(DashConstants.KeyStore.HeaderKey);
        public static KeyController CollectionOutputKey = new KeyController(DashConstants.KeyStore.CollectionOutputKey);
        public static KeyController OperatorKey = new KeyController(DashConstants.KeyStore.OperatorKey);
        public static KeyController OperatorCacheKey = new KeyController(DashConstants.KeyStore.OperatorCacheKey);
        public static KeyController CollectionViewTypeKey = new KeyController("EFC44F1C-3EB0-4111-8840-E694AB9DCB80", "Collection View Type");
        public static KeyController InkDataKey = new KeyController("1F6A3D2F-28D8-4365-ADA8-4C345C3AF8B6", "_InkData");
        public static KeyController ParsedFieldKey = new KeyController(DashConstants.KeyStore.ParsedFieldsKey);
        public static KeyController WebContextKey = new KeyController(DashConstants.KeyStore.WebContextKey);
        public static KeyController ModifiedTimestampKey = new KeyController(DashConstants.KeyStore.ModifiedTimestampKey);
        public static KeyController LastWorkspaceKey = new KeyController(DashConstants.KeyStore.LastWorkspaceKey);
        public static KeyController WorkspaceHistoryKey = new KeyController(DashConstants.KeyStore.WorkspaceHistoryKey);
        public static KeyController PanPositionKey = new KeyController(DashConstants.KeyStore.PanPositionKey);
        public static KeyController PanZoomKey = new KeyController(DashConstants.KeyStore.PanZoomKey);
        public static KeyController ActualSizeKey = new KeyController(DashConstants.KeyStore.ActualSizeKey);
        public static KeyController DocumentTypeKey = new KeyController(DashConstants.KeyStore.DocumentTypeKey);
        public static KeyController SelectedKey = new KeyController(DashConstants.KeyStore.SelectedKey);
        public static KeyController OriginalImageKey = new KeyController(DashConstants.KeyStore.OriginalImageKey);
        public static KeyController SideCountKey = new KeyController(DashConstants.KeyStore.SideCountKey);


        public static class SearchResultDocumentOutline
        {
            public static KeyController SearchResultTitleKey = new KeyController(new KeyModel("4ACEB999-6E82-4B40-9602-BD6D362CAC54", "_searchResultTitle"));
            public static KeyController SearchResultIdKey = new KeyController(new KeyModel("F3740B30-C63F-4549-A814-832CC3E01558", "_searchResultId"));//TODO TFS make this a doc reference
            public static KeyController SearchResultHelpTextKey = new KeyController(new KeyModel("4712CBF3-BDD9-4A92-8AC5-043F4BA14AAB", "_searchResultHelpText"));
        }


        public static KeyController CollectionFitToParentKey = new KeyController("61CA156E-F959-4607-A2F3-BFEFA5D00B64", "CollectionFitToParent");

        public static readonly KeyController HorizontalAlignmentKey = new KeyController("B43231DA-5A22-45A3-8476-005A62396686", "_Horizontal Alignment");
        public static readonly KeyController VerticalAlignmentKey = new KeyController("227B9887-BC09-40E4-A3F0-AD204D00E48D", "_Vertical Alignment");

        /// <summary>
        /// The selected row in the schema view for a collection. This always will contain a Document Field Model Controller
        /// </summary>
        public static KeyController SelectedSchemaRow = new KeyController("B9B5742B-E4C7-45BD-AD6E-F3C254E45027", "Selected Element");
        public static void RegisterDocumentTypeRenderer(DocumentType type, MakeViewFunc makeViewFunc, MakeRegionFunc makeRegionFunc)
        {
            TypeRenderer[type] = makeViewFunc;
            RegionCreator[type] = makeRegionFunc;
        }

        public delegate FrameworkElement   MakeViewFunc(DocumentController doc, Context context);
        public delegate DocumentController MakeRegionFunc(DocumentView view);
        public static Dictionary<DocumentType, MakeViewFunc>   TypeRenderer  = new Dictionary<DocumentType, MakeViewFunc>();
        public static Dictionary<DocumentType, MakeRegionFunc> RegionCreator = new Dictionary<DocumentType, MakeRegionFunc>();

    }
}
