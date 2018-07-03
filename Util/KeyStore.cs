using System.Collections.Generic;
using DashShared;
using Windows.UI.Xaml;

namespace Dash
{
    public class KeyStore
    {
        public static KeyController DocumentContextKey = new KeyController("17D4CFDE-9146-47E9-8AF0-0F9D546E94EC", "_DocumentContext");
        public static KeyController AbstractInterfaceKey = new KeyController("E579C81B-EE13-4B16-BB96-80688D30A73A", "_AbstractInterface");
        public static KeyController LayoutListKey = new KeyController("6546DD08-C753-4C34-924E-3C4016C4B95B", "_LayoutList");
        public static KeyController RegionsKey = new KeyController("1B958E26-624B-4E9A-82C9-2E18609D6A39", "Regions");
        public static KeyController RegionDefinitionKey = new KeyController("6EEDCB86-76F4-4937-AE0D-9C4BC6744310", "Region Definition");
        public static KeyController ActiveLayoutKey = new KeyController("BEBEC91F-F85A-4F72-A7D2-E2912571FBDA", "ActiveLayout");
        public static KeyController TitleKey = new KeyController("0C074CB4-6D05-4363-A867-C0A061C1573F", "Title");
        public static KeyController CaptionKey = new KeyController("D01D6702-A3AD-4546-9BFB-C5263F8D5599", "Caption");
        public static KeyController PrototypeKey = new KeyController("866A6CC9-0B8D-49A3-B45F-D7954631A682", "_Prototype");
        public static KeyController DelegatesKey = new KeyController("D737A3D8-DB2C-40EB-8DAB-129D58BC6ADB", "_Delegates");
        public static KeyController UserSetWidthKey = new KeyController("7D3E7CDB-D0C7-4316-BA3B-3C032F24B5AA", "_userSetWidth");
        public static KeyController WidthFieldKey = new KeyController("5B329D99-96BF-4703-8E28-9B7B1C1B837E", "Width");
        public static KeyController HeightFieldKey = new KeyController("9ED34365-C821-4FB2-A955-A8C0B10C77C5", "Height");
        public static KeyController TransientKey = new KeyController("7553FBFA-B4C4-46D7-AEA6-76B22C5A3425", "Transient");
        public static KeyController HiddenKey = new KeyController("A99659B8-F34A-4F0D-8BA7-0030DA8B4EA6", "Hidden");
        public static KeyController DataKey = new KeyController("3B1BD1C3-1BCD-469D-B847-835B565B53EB", "Data");
        public static KeyController SnapshotsKey = new KeyController("94358B4F-83DD-41A6-8440-BA5973DC9E97", "Snaphshots");
        public static KeyController SourecUriKey = new KeyController("26594498-FF15-438D-A577-2C8506F4ECEF", "SourceUriKeys");
        public static KeyController IsAdornmentKey = new KeyController("FF3329BD-AA78-46E4-9A42-47CAB1E62123", "Is Adornment");
        public static KeyController DocumentTextKey = new KeyController("D5156A8F-9093-420B-96B7-507DD949360D", "Document Text");
        public static KeyController TextWrappingKey = new KeyController("FF488D09-BBB7-4158-A5E4-0C4530DF2F56", "Text Wrapping");
        public static KeyController BackgroundColorKey = new KeyController("6B597D2A-1A52-446F-901A-B9ED0BBE33E1", "Background Color");
        public static KeyController OpacitySliderValueKey = new KeyController("3FD448B7-8AEE-4FBD-B68C-514E098D8D31", "Opacity Slider Value");
        public static KeyController AdornmentShapeKey = new KeyController("5DEBC829-A68B-4D2E-BC29-549DEB910EC6", "Adornment Shape");
        public static KeyController PositionFieldKey = new KeyController("E2AB7D27-FA81-4D88-B2FA-42B7888525AF", "Position");
        public static KeyController LinkFromKey = new KeyController("9A3191FF-C8E6-472F-ABE5-B5A250D49D59", "Link From");
        public static KeyController LinkToKey = new KeyController("649A7F35-C428-49EC-B914-5746E2590DAC", "Link To");
        public static KeyController PdfVOffsetFieldKey = new KeyController("8990098B-83D2-4817-A275-82D8282ECD79", "_PdfVOffset");
        public static KeyController ScaleAmountFieldKey = new KeyController("AOEKMA9J-IP37-96HI-VJ36-IHFI39AHI8DE", "_Scale Amount");
        public static KeyController IconTypeFieldKey = new KeyController("ICON7D27-FA81-4D88-B2FA-42B7888525AF", "_IconType");
        public static KeyController SystemUriKey = new KeyController("CA740B60-10D5-4B2C-9C9A-E6E4A7D2CA4E", "File Path");
        public static KeyController ThumbnailFieldKey = new KeyController("67D3BD61-43EC-4BDE-913A-E459F9D15E76", "_ThumbnailField");
        public static KeyController HeaderKey = new KeyController("93CF85C8-5522-4B00-927A-943982250729", "Header");
        public static KeyController CollectionOutputKey = new KeyController("D4FD93F5-A3DA-41CF-8FB2-3C7A659B7850", "Collection Output");
        public static KeyController OperatorKey = new KeyController("F5B0E5E0-2C1F-4E49-BD26-5F6CBCDE766A", "Operator");
        public static KeyController OperatorCacheKey = new KeyController("1B1409DE-4BA5-4515-9BB4-B15AE8CC0041", "_Operator Cache");
        public static KeyController CollectionViewTypeKey = new KeyController("EFC44F1C-3EB0-4111-8840-E694AB9DCB80", "Collection View Type");
        public static KeyController InkDataKey = new KeyController("1F6A3D2F-28D8-4365-ADA8-4C345C3AF8B6", "_InkData");
        public static KeyController ParsedFieldsKey = new KeyController("385D06F3-96A7-4ADF-B806-50DAB4488FD6", "_Parsed Fields");
        public static KeyController WebContextKey = new KeyController("EFD56382-F8BA-45D2-86D3-085974EF4D9D", "_WebContext");
        public static KeyController ModifiedTimestampKey = new KeyController("CAAD33A3-DE94-42CF-A54A-F85C5F04940E", "ModifiedTime");
        public static KeyController LastWorkspaceKey = new KeyController("66F05DB2-2F68-4E37-985D-36303A1AF4E4", "_Last Workspace");
        public static KeyController WorkspaceHistoryKey = new KeyController("D0630828-1488-4F7B-B0D7-9E89EF05497F", "_Workspace History");
        public static KeyController PanPositionKey = new KeyController("8778D978-AEA2-470C-8DBD-C684131BA9B4", "_Pan Position");
        public static KeyController PanZoomKey = new KeyController("4C4C676B-EEC8-4682-B15C-57866BF4933C", "_Pan Zoom Level");
        public static KeyController ActualSizeKey = new KeyController("529D7312-9A33-4A6E-80AF-FA173293DC36", "ActualSize");
        public static KeyController DocumentTypeKey = new KeyController("B1DE8ABE-5C04-49C6-913C-A2428ED566F8", "_DocumentType");
        public static KeyController SelectedKey = new KeyController("86009EF6-7D77-4D67-8C7A-C5EA5704432F", "_Selected");
        public static KeyController OriginalImageKey = new KeyController("6226CC11-3616-4521-9C9E-731245FA1F4C", "_Original Image");
        public static KeyController SideCountKey = new KeyController("276302FF-0E5F-4009-A308-A4EE8B4224F7", "Side Count");
        public static KeyController SettingsDocKey = new KeyController("EFD6D6B8-286F-4D34-AD44-BCFB72CD3F70", "Settings Doc");
        public static KeyController SettingsNightModeKey = new KeyController("7AA22643-3D28-433E-83E9-ECD6A7475270", "Settings Night Mode");
        public static KeyController SettingsFontSizeKey = new KeyController("BD720922-FAD9-4821-9877-F62E3273DED8", "Settings Font Size");
        public static KeyController SettingsMouseFuncKey = new KeyController("867225EC-F9C7-4B14-9A5F-22B7BB71DCCB", "Settings Mouse Functionality");
        public static KeyController SettingsNumBackupsKey = new KeyController("25F0DB4F-D6DE-4D48-A090-77E48C1F621C", "Settings Number of Backups");
        public static KeyController SettingsBackupIntervalKey = new KeyController("8C00E2CD-6272-4E6C-ADC1-622B108A0D9F", "Settings Backup Interval");
        public static KeyController BackgroundImageStateKey = new KeyController("3EAE5AB5-4503-4519-9EF3-0FA5BDDE59E6", "State of Background Image (Radio Buttons)");
        public static KeyController CustomBackgroundImagePathKey = new KeyController("DA719660-D5CE-40CE-9BDE-D57B764CA6BF", "Custom Path to Background Image");
        public static KeyController BackgroundImageOpacityKey = new KeyController("0A1CA35C-5A6F-4C8A-AF00-6C82D5DA0FEC", "Background Image Opacity");
        public static KeyController SettingsUpwardPanningKey = new KeyController("3B354602-794D-4FC0-A289-1EBA7EC23FD1", "Infinite Upward Panning Enabled");
        public static KeyController SettingsMarkdownModeKey = new KeyController("2575EAFA-2689-40DD-A0A8-9EE0EC0720ED", "Markdown vs RTF");
        public static KeyController DockedDocumentsLeftKey = new KeyController("0CCFCC20-DAF7-4329-B615-605A54A86014", "Documents docked on the left");
        public static KeyController DockedDocumentsTopKey = new KeyController("5A5AC489-8988-44BE-AC06-AE76CF81FB04", "Documents docked on the top");
        public static KeyController DockedDocumentsRightKey = new KeyController("F9E7580F-2053-49AA-B829-7B7347C65394", "Documents docked on the right");
        public static KeyController DockedDocumentsBottomKey = new KeyController("F6E10E00-1644-40BE-8A9E-0C648FE4B223", "Documents docked on the bottom");

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

        public delegate FrameworkElement MakeViewFunc(DocumentController doc, Context context);
        public delegate DocumentController MakeRegionFunc(DocumentView view);
        public static Dictionary<DocumentType, MakeViewFunc> TypeRenderer = new Dictionary<DocumentType, MakeViewFunc>();
        public static Dictionary<DocumentType, MakeRegionFunc> RegionCreator = new Dictionary<DocumentType, MakeRegionFunc>();

    }
}
