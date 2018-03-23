using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class KeyStore
    {
        public static KeyController DocumentContextKey = new KeyController(DashConstants.KeyStore.DocumentContextKey);
        public static KeyController AbstractInterfaceKey = new KeyController(DashConstants.KeyStore.AbstractInterfaceKey);
        public static KeyController LayoutListKey = new KeyController(DashConstants.KeyStore.LayoutListKey);
        public static KeyController ActiveLayoutKey = new KeyController(DashConstants.KeyStore.ActiveLayoutKey);
        public static KeyController TitleKey = new KeyController(DashConstants.KeyStore.TitleKey);
        public static KeyController CaptionKey = new KeyController(DashConstants.KeyStore.CaptionKey);
        public static KeyController PrototypeKey = new KeyController(DashConstants.KeyStore.PrototypeKey);
        public static KeyController DelegatesKey = new KeyController(DashConstants.KeyStore.DelegatesKey);
        public static KeyController WidthFieldKey = new KeyController(DashConstants.KeyStore.WidthFieldKey);
        public static KeyController HeightFieldKey = new KeyController(DashConstants.KeyStore.HeightFieldKey);
        public static KeyController DataKey = new KeyController(DashConstants.KeyStore.DataKey);
        public static KeyController SourecUriKey = new KeyController(DashConstants.KeyStore.SourceUriKey);
        public static KeyController DocumentTextKey = new KeyController(DashConstants.KeyStore.DocumentTextKey);
        public static KeyController BackgroundColorKey = new KeyController(DashConstants.KeyStore.BackgroundColorKey);
        public static KeyController PositionFieldKey = new KeyController(DashConstants.KeyStore.PositionFieldKey);
        public static KeyController ScaleAmountFieldKey = new KeyController(DashConstants.KeyStore.ScaleAmountFieldKey);
        public static KeyController IconTypeFieldKey = new KeyController(DashConstants.KeyStore.IconTypeFieldKey);
        public static KeyController SystemUriKey = new KeyController(DashConstants.KeyStore.SystemUriKey);
        public static KeyController ThumbnailFieldKey = new KeyController(DashConstants.KeyStore.ThumbnailFieldKey);
        public static KeyController HeaderKey = new KeyController(DashConstants.KeyStore.HeaderKey);
        public static KeyController CollectionOutputKey = new KeyController(DashConstants.KeyStore.CollectionOutputKey);
        public static KeyController OperatorKey = new KeyController("F5B0E5E0-2C1F-4E49-BD26-5F6CBCDE766A", "_Operator");
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
        public static KeyController ActualWidthKey = new KeyController(DashConstants.KeyStore.ActualWidthKey);
        public static KeyController ActualHeightKey = new KeyController(DashConstants.KeyStore.ActualHeightKey);
        public static KeyController DocumentTypeKey = new KeyController(DashConstants.KeyStore.DocumentTypeKey);
        public static KeyController SelectedKey = new KeyController(DashConstants.KeyStore.SelectedKey);

        public static KeyController CollectionFitToParentKey = new KeyController("61CA156E-F959-4607-A2F3-BFEFA5D00B64", "_CollectionFitToParent");

        public static readonly KeyController HorizontalAlignmentKey = new KeyController("B43231DA-5A22-45A3-8476-005A62396686", "_Horizontal Alignment");
        public static readonly KeyController VerticalAlignmentKey = new KeyController("227B9887-BC09-40E4-A3F0-AD204D00E48D", "_Vertical Alignment");

        /// <summary>
        /// The selected row in the schema view for a collection. This always will contain a Document Field Model Controller
        /// </summary>
        public static KeyController SelectedSchemaRow = new KeyController("B9B5742B-E4C7-45BD-AD6E-F3C254E45027", "Selected Element");


    }
}
