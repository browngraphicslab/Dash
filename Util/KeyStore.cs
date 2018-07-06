﻿using System.Collections.Generic;
using DashShared;
using Windows.UI.Xaml;

namespace Dash
{
    public class KeyStore
    {
        public static KeyController DocumentContextKey = new KeyController("_DocumentContext", "17D4CFDE-9146-47E9-8AF0-0F9D546E94EC");
        public static KeyController AbstractInterfaceKey = new KeyController("_AbstractInterface", "E579C81B-EE13-4B16-BB96-80688D30A73A");
        public static KeyController LayoutListKey = new KeyController("_LayoutList", "6546DD08-C753-4C34-924E-3C4016C4B95B");
        public static KeyController RegionsKey = new KeyController("Regions", "1B958E26-624B-4E9A-82C9-2E18609D6A39");
        public static KeyController RegionDefinitionKey = new KeyController("Region Definition", "6EEDCB86-76F4-4937-AE0D-9C4BC6744310");
        public static KeyController ActiveLayoutKey = new KeyController("ActiveLayout", "BEBEC91F-F85A-4F72-A7D2-E2912571FBDA");
        public static KeyController TitleKey = new KeyController("Title", "0C074CB4-6D05-4363-A867-C0A061C1573F");
        public static KeyController CaptionKey = new KeyController("Caption", "D01D6702-A3AD-4546-9BFB-C5263F8D5599");
        public static KeyController PrototypeKey = new KeyController("_Prototype", "866A6CC9-0B8D-49A3-B45F-D7954631A682");
        public static KeyController DelegatesKey = new KeyController("_Delegates", "D737A3D8-DB2C-40EB-8DAB-129D58BC6ADB");
        public static KeyController UserSetWidthKey = new KeyController("_userSetWidth", "7D3E7CDB-D0C7-4316-BA3B-3C032F24B5AA");
        public static KeyController WidthFieldKey = new KeyController("Width", "5B329D99-96BF-4703-8E28-9B7B1C1B837E");
        public static KeyController HeightFieldKey = new KeyController("Height", "9ED34365-C821-4FB2-A955-A8C0B10C77C5");
        public static KeyController TransientKey = new KeyController("Transient", "7553FBFA-B4C4-46D7-AEA6-76B22C5A3425");
        public static KeyController HiddenKey = new KeyController("Hidden", "A99659B8-F34A-4F0D-8BA7-0030DA8B4EA6");
        public static KeyController DataKey = new KeyController("Data", "3B1BD1C3-1BCD-469D-B847-835B565B53EB");
        public static KeyController SnapshotsKey = new KeyController("Snaphshots", "94358B4F-83DD-41A6-8440-BA5973DC9E97");
        public static KeyController SourecUriKey = new KeyController("SourceUriKeys", "26594498-FF15-438D-A577-2C8506F4ECEF");
        public static KeyController IsAdornmentKey = new KeyController("Is Adornment", "FF3329BD-AA78-46E4-9A42-47CAB1E62123");
        public static KeyController DocumentTextKey = new KeyController("Document Text", "D5156A8F-9093-420B-96B7-507DD949360D");
        public static KeyController TextWrappingKey = new KeyController("Text Wrapping", "FF488D09-BBB7-4158-A5E4-0C4530DF2F56");
        public static KeyController BackgroundColorKey = new KeyController("Background Color", "6B597D2A-1A52-446F-901A-B9ED0BBE33E1");
        public static KeyController OpacitySliderValueKey = new KeyController("Opacity Slider Value", "3FD448B7-8AEE-4FBD-B68C-514E098D8D31");
        public static KeyController AdornmentShapeKey = new KeyController("Adornment Shape", "5DEBC829-A68B-4D2E-BC29-549DEB910EC6");
        public static KeyController PositionFieldKey = new KeyController("Position", "E2AB7D27-FA81-4D88-B2FA-42B7888525AF");
        public static KeyController LinkFromKey = new KeyController("Link From", "9A3191FF-C8E6-472F-ABE5-B5A250D49D59");
        public static KeyController LinkToKey = new KeyController("Link To", "649A7F35-C428-49EC-B914-5746E2590DAC");
        public static KeyController PdfVOffsetFieldKey = new KeyController("_PdfVOffset", "8990098B-83D2-4817-A275-82D8282ECD79");
        public static KeyController ScaleAmountFieldKey = new KeyController("_Scale Amount", "AOEKMA9J-IP37-96HI-VJ36-IHFI39AHI8DE");
        public static KeyController IconTypeFieldKey = new KeyController("_IconType", "ICON7D27-FA81-4D88-B2FA-42B7888525AF");
        public static KeyController SystemUriKey = new KeyController("File Path", "CA740B60-10D5-4B2C-9C9A-E6E4A7D2CA4E");
        public static KeyController ThumbnailFieldKey = new KeyController("_ThumbnailField", "67D3BD61-43EC-4BDE-913A-E459F9D15E76");
        public static KeyController HeaderKey = new KeyController("Header", "93CF85C8-5522-4B00-927A-943982250729");
        public static KeyController CollectionOutputKey = new KeyController("Collection Output", "D4FD93F5-A3DA-41CF-8FB2-3C7A659B7850");
        public static KeyController OperatorKey = new KeyController("Operator", "F5B0E5E0-2C1F-4E49-BD26-5F6CBCDE766A");
        public static KeyController OperatorCacheKey = new KeyController("_Operator Cache", "1B1409DE-4BA5-4515-9BB4-B15AE8CC0041");
        public static KeyController CollectionViewTypeKey = new KeyController("Collection View Type", "EFC44F1C-3EB0-4111-8840-E694AB9DCB80");
        public static KeyController InkDataKey = new KeyController("_InkData", "1F6A3D2F-28D8-4365-ADA8-4C345C3AF8B6");
        public static KeyController ParsedFieldsKey = new KeyController("_Parsed Fields", "385D06F3-96A7-4ADF-B806-50DAB4488FD6");
        public static KeyController WebContextKey = new KeyController("_WebContext", "EFD56382-F8BA-45D2-86D3-085974EF4D9D");
        public static KeyController ModifiedTimestampKey = new KeyController("ModifiedTime", "CAAD33A3-DE94-42CF-A54A-F85C5F04940E");
        public static KeyController LastWorkspaceKey = new KeyController("_Last Workspace", "66F05DB2-2F68-4E37-985D-36303A1AF4E4");
        public static KeyController WorkspaceHistoryKey = new KeyController("_Workspace History", "D0630828-1488-4F7B-B0D7-9E89EF05497F");
        public static KeyController PanPositionKey = new KeyController("_Pan Position", "8778D978-AEA2-470C-8DBD-C684131BA9B4");
        public static KeyController PanZoomKey = new KeyController("_Pan Zoom Level", "4C4C676B-EEC8-4682-B15C-57866BF4933C");
        public static KeyController ActualSizeKey = new KeyController("ActualSize", "529D7312-9A33-4A6E-80AF-FA173293DC36");
        public static KeyController DocumentTypeKey = new KeyController("_DocumentType", "B1DE8ABE-5C04-49C6-913C-A2428ED566F8");
        public static KeyController SelectedKey = new KeyController("_Selected", "86009EF6-7D77-4D67-8C7A-C5EA5704432F");
        public static KeyController OriginalImageKey = new KeyController("_Original Image", "6226CC11-3616-4521-9C9E-731245FA1F4C");
        public static KeyController SideCountKey = new KeyController("Side Count", "276302FF-0E5F-4009-A308-A4EE8B4224F7");
        public static KeyController SettingsDocKey = new KeyController("Settings Doc", "EFD6D6B8-286F-4D34-AD44-BCFB72CD3F70");
        public static KeyController SettingsNightModeKey = new KeyController("Settings Night Mode", "7AA22643-3D28-433E-83E9-ECD6A7475270");
        public static KeyController SettingsFontSizeKey = new KeyController("Settings Font Size", "BD720922-FAD9-4821-9877-F62E3273DED8");
        public static KeyController SettingsMouseFuncKey = new KeyController("Settings Mouse Functionality", "867225EC-F9C7-4B14-9A5F-22B7BB71DCCB");
        public static KeyController SettingsNumBackupsKey = new KeyController("Settings Number of Backups", "25F0DB4F-D6DE-4D48-A090-77E48C1F621C");
        public static KeyController SettingsBackupIntervalKey = new KeyController("Settings Backup Interval", "8C00E2CD-6272-4E6C-ADC1-622B108A0D9F");
        public static KeyController BackgroundImageStateKey = new KeyController("State of Background Image (Radio Buttons)", "3EAE5AB5-4503-4519-9EF3-0FA5BDDE59E6");
        public static KeyController CustomBackgroundImagePathKey = new KeyController("Custom Path to Background Image", "DA719660-D5CE-40CE-9BDE-D57B764CA6BF");
        public static KeyController BackgroundImageOpacityKey = new KeyController("Background Image Opacity", "0A1CA35C-5A6F-4C8A-AF00-6C82D5DA0FEC");
        public static KeyController SettingsUpwardPanningKey = new KeyController("Infinite Upward Panning Enabled", "3B354602-794D-4FC0-A289-1EBA7EC23FD1");
        public static KeyController SettingsMarkdownModeKey = new KeyController("Markdown vs RTF", "2575EAFA-2689-40DD-A0A8-9EE0EC0720ED");
        public static KeyController DockedDocumentsLeftKey = new KeyController("Documents docked on the left", "0CCFCC20-DAF7-4329-B615-605A54A86014");
        public static KeyController DockedDocumentsTopKey = new KeyController("Documents docked on the top", "5A5AC489-8988-44BE-AC06-AE76CF81FB04");
        public static KeyController DockedDocumentsRightKey = new KeyController("Documents docked on the right", "F9E7580F-2053-49AA-B829-7B7347C65394");
        public static KeyController DockedDocumentsBottomKey = new KeyController("Documents docked on the bottom", "F6E10E00-1644-40BE-8A9E-0C648FE4B223");

        public static KeyController ReplInputsKey = new KeyController(DashConstants.KeyStore.ReplInputsKey);
        public static KeyController ReplOutputsKey = new KeyController(DashConstants.KeyStore.ReplOutputsKey);
        public static KeyController ReplScopeKey = new KeyController(DashConstants.KeyStore.ReplScopeKey);

        public static KeyController ScriptTextKey = new KeyController(DashConstants.KeyStore.ScriptTextKey);

        public static class SearchResultDocumentOutline
        {
            public static KeyController SearchResultTitleKey = new KeyController(new KeyModel("_searchResultTitle", "4ACEB999-6E82-4B40-9602-BD6D362CAC54"));
            public static KeyController SearchResultIdKey = new KeyController(new KeyModel("_searchResultId", "F3740B30-C63F-4549-A814-832CC3E01558"));//TODO TFS make this a doc reference
            public static KeyController SearchResultHelpTextKey = new KeyController(new KeyModel("_searchResultHelpText", "4712CBF3-BDD9-4A92-8AC5-043F4BA14AAB"));
        }

        public static KeyController CollectionFitToParentKey = new KeyController("CollectionFitToParent", "61CA156E-F959-4607-A2F3-BFEFA5D00B64");
        public static readonly KeyController HorizontalAlignmentKey = new KeyController("_Horizontal Alignment", "B43231DA-5A22-45A3-8476-005A62396686");
        public static readonly KeyController VerticalAlignmentKey = new KeyController("_Vertical Alignment", "227B9887-BC09-40E4-A3F0-AD204D00E48D");

        /// <summary>
        /// The selected row in the schema view for a collection. This always will contain a Document Field Model Controller
        /// </summary>
        public static KeyController SelectedSchemaRow = new KeyController("Selected Element", "B9B5742B-E4C7-45BD-AD6E-F3C254E45027");
        public static void RegisterDocumentTypeRenderer(DocumentType type, MakeViewFunc makeViewFunc, MakeRegionFunc makeRegionFunc)
        {
            TypeRenderer[type] = makeViewFunc;
            RegionCreator[type] = makeRegionFunc;
        }

        public delegate FrameworkElement MakeViewFunc(DocumentController doc, Context context);
        public delegate DocumentController MakeRegionFunc(DocumentView view);
        public static Dictionary<DocumentType, MakeViewFunc> TypeRenderer = new Dictionary<DocumentType, MakeViewFunc>();
        public static Dictionary<DocumentType, MakeRegionFunc> RegionCreator = new Dictionary<DocumentType, MakeRegionFunc>();

    }
}
