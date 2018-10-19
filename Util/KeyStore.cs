using System;
using System.Collections.Generic;
using Windows.Foundation;
using DashShared;
using Windows.UI.Xaml;

namespace Dash
{
	public class KeyStore
	{
		//NOTE: Underscore prefacing registers the field as invisible
	    public static KeyController DocumentContextKey                   = KeyController.Get("DocumentContext", new Guid("17D4CFDE-9146-47E9-8AF0-0F9D546E94EC"));
		public static KeyController AbstractInterfaceKey                 = KeyController.Get("_AbstractInterface", new Guid("E579C81B-EE13-4B16-BB96-80688D30A73A"));
		public static KeyController LayoutListKey                        = KeyController.Get("_LayoutList", new Guid("6546DD08-C753-4C34-924E-3C4016C4B95B"));
		public static KeyController RegionsKey                           = KeyController.Get("Regions", new Guid("1B958E26-624B-4E9A-82C9-2E18609D6A39"));
		public static KeyController RegionDefinitionKey                  = KeyController.Get("RegionDefinition", new Guid("6EEDCB86-76F4-4937-AE0D-9C4BC6744310"));
		public static KeyController RegionTypeKey                        = KeyController.Get("RegionType", new Guid("8E64FAF2-1ED2-4F4D-9616-0EB3B2F4D1EC"));
		public static KeyController TitleKey                             = KeyController.Get("Title", new Guid("0C074CB4-6D05-4363-A867-C0A061C1573F"));
		public static KeyController CaptionKey                           = KeyController.Get("Caption", new Guid("D01D6702-A3AD-4546-9BFB-C5263F8D5599"));
		public static KeyController PrototypeKey                         = KeyController.Get("Prototype", new Guid("866A6CC9-0B8D-49A3-B45F-D7954631A682"));
        public static KeyController CollectionItemLayoutPrototypeKey     = KeyController.Get("CollectionItemLayoutPrototype", new Guid("7C6F72BA-EC97-4DA5-8ACE-5F4F1118F185"));    // layout prototype shared by each item in a collection -- set using TableExtraction, used in CollectionPageView
        public static KeyController LayoutPrototypeKey                   = KeyController.Get("LayoutPrototype", new Guid("77805171-247D-4485-A257-1751619C2C45"));                  // layout prototype used by a document - set using TableExtraction, used in CollectionDBSchema
        public static KeyController ColumnSortingKey                     = KeyController.Get("ColumnSort", new Guid("852DBACD-791C-422B-8B8E-5EC5E7E8DCA2"));                       // list containing column name and direction to sort a schema table
		public static KeyController DelegatesKey                         = KeyController.Get("_Delegates", new Guid("D737A3D8-DB2C-40EB-8DAB-129D58BC6ADB"));
		public static KeyController UserSetWidthKey                      = KeyController.Get("_userSetWidth", new Guid("7D3E7CDB-D0C7-4316-BA3B-3C032F24B5AA"));
		public static KeyController WidthFieldKey                        = KeyController.Get("Width", new Guid("5B329D99-96BF-4703-8E28-9B7B1C1B837E"));
		public static KeyController HeightFieldKey                       = KeyController.Get("Height", new Guid("9ED34365-C821-4FB2-A955-A8C0B10C77C5"));
		public static KeyController TransientKey                         = KeyController.Get("Transient", new Guid("7553FBFA-B4C4-46D7-AEA6-76B22C5A3425"));
		public static KeyController HiddenKey                            = KeyController.Get("Hidden", new Guid("A99659B8-F34A-4F0D-8BA7-0030DA8B4EA6"));
		public static KeyController DataKey                              = KeyController.Get("Data", new Guid("3B1BD1C3-1BCD-469D-B847-835B565B53EB"));
		public static KeyController SnapshotsKey                         = KeyController.Get("Snaphshots", new Guid("94358B4F-83DD-41A6-8440-BA5973DC9E97"));
		public static KeyController SourceUriKey                         = KeyController.Get("SourceUriKeys", new Guid("26594498-FF15-438D-A577-2C8506F4ECEF"));
		public static KeyController SourceTitleKey                       = KeyController.Get("SourceTitle", new Guid("E16A6779-2F91-4660-8510-E1FD906A6A5E"));
		public static KeyController IsAdornmentKey                       = KeyController.Get("IsAdornment", new Guid("FF3329BD-AA78-46E4-9A42-47CAB1E62123"));
		public static KeyController IsButtonKey                          = KeyController.Get("IsButton", new Guid("AC726B5B-B862-45C3-A603-9D2E54079656"));
		public static KeyController DocumentTextKey                      = KeyController.Get("DocumentText", new Guid("D5156A8F-9093-420B-96B7-507DD949360D"));
		public static KeyController TextWrappingKey                      = KeyController.Get("TextWrapping", new Guid("FF488D09-BBB7-4158-A5E4-0C4530DF2F56"));
		public static KeyController BackgroundColorKey                   = KeyController.Get("BackgroundColor", new Guid("6B597D2A-1A52-446F-901A-B9ED0BBE33E1"));
		public static KeyController SavedColorsKey                       = KeyController.Get("_SavedColorsFromPicker", new Guid("70AB09AE-F88D-45EC-B20E-721635DC20C4"));
		public static KeyController OpacitySliderValueKey                = KeyController.Get("OpacitySliderValue", new Guid("3FD448B7-8AEE-4FBD-B68C-514E098D8D31"));
		public static KeyController GroupBackgroundColorKey              = KeyController.Get("GroupBackgroundColor", new Guid("E1FA9844-6B13-4BE2-BAAA-F06B9D6672A6"));
		public static KeyController AdornmentShapeKey                    = KeyController.Get("Adornment Shape", new Guid("5DEBC829-A68B-4D2E-BC29-549DEB910EC6"));
		public static KeyController PositionFieldKey                     = KeyController.Get("Position", new Guid("E2AB7D27-FA81-4D88-B2FA-42B7888525AF"));
		public static KeyController LinkFromKey                          = KeyController.Get("LinkFrom", new Guid("9A3191FF-C8E6-472F-ABE5-B5A250D49D59"));
		public static KeyController LinkToKey                            = KeyController.Get("LinkTo", new Guid("649A7F35-C428-49EC-B914-5746E2590DAC"));
		public static KeyController LinkDestinationKey                   = KeyController.Get("LinkDestination", new Guid("FFF41A1C-9924-44FB-9109-F0CE843D9B96"));
		public static KeyController LinkSourceKey                        = KeyController.Get("LinkSource", new Guid("ED8119BB-F6C1-4FCB-9DF7-547D06091249"));
	    public static KeyController LinkBehaviorKey                      = KeyController.Get("LinkBehavior", new Guid("1B87D286-826E-466D-A076-06C313CFD7DE"));
	    public static KeyController LinkContextKey                       = KeyController.Get("LinkContext", new Guid("A8FD93F3-0C39-44AE-9B69-B28F0787D32B"));
        public static KeyController PdfVOffsetFieldKey                   = KeyController.Get("_PdfVOffset", new Guid("8990098B-83D2-4817-A275-82D8282ECD79"));
		public static KeyController ReferencesDictKey                    = KeyController.Get("_PDF Reference Mapping", new Guid("6B06B539-614C-486F-97C7-7CDAA729C421"));
		public static KeyController ReferenceNumKey                      = KeyController.Get("Reference #", new Guid("FD61D5F0-8C31-4132-A6B2-02C58067B5EA"));
		public static KeyController ReferenceDateKey                     = KeyController.Get("Date Published", new Guid("57BC205D-B2E0-4E55-8114-A993A9376E1B"));
		public static KeyController ScaleAmountFieldKey                  = KeyController.Get("_Scale Amount", new Guid("D154932B-D770-483B-903F-4887038394FD"));
		public static KeyController IconTypeFieldKey                     = KeyController.Get("_IconType", new Guid("8C8B7C69-8A09-40F3-BEE4-28B64E82CE08"));
		public static KeyController SystemUriKey                         = KeyController.Get("File Path", new Guid("CA740B60-10D5-4B2C-9C9A-E6E4A7D2CA4E"));
		public static KeyController ThumbnailFieldKey                    = KeyController.Get("_ThumbnailField", new Guid("67D3BD61-43EC-4BDE-913A-E459F9D15E76"));
		public static KeyController HeaderKey                            = KeyController.Get("Header", new Guid("93CF85C8-5522-4B00-927A-943982250729"));
		public static KeyController CollectionOutputKey                  = KeyController.Get("CollectionOutput", new Guid("D4FD93F5-A3DA-41CF-8FB2-3C7A659B7850"));
		public static KeyController OperatorKey                          = KeyController.Get("_Operator", new Guid("F5B0E5E0-2C1F-4E49-BD26-5F6CBCDE766A"));
		public static KeyController OperatorCacheKey                     = KeyController.Get("_OperatorCache", new Guid("1B1409DE-4BA5-4515-9BB4-B15AE8CC0041"));
		public static KeyController CollectionViewTypeKey                = KeyController.Get("Collection View Type", new Guid("EFC44F1C-3EB0-4111-8840-E694AB9DCB80"));
        public static KeyController CollectionOpenViewTypeKey            = KeyController.Get("Collection Open View Type", new Guid("5BCDDFA4-0465-4D6B-9C10-1373E12EB229")); // the view type of the collection when opened from its iconic form
        public static KeyController CollectionOpenWidthKey               = KeyController.Get("Collection Open Width", new Guid("6D145205-F236-4492-B51D-CE4A34DA8696")); // the width of the collection when opened from its iconic form
        public static KeyController CollectionOpenHeightKey              = KeyController.Get("Collection Open Height", new Guid("DE66CED9-BC52-447C-A40C-6659FD382441")); // the height of the collection when opened from its iconic form
        public static KeyController InkDataKey                           = KeyController.Get("_InkData", new Guid("1F6A3D2F-28D8-4365-ADA8-4C345C3AF8B6"));
		public static KeyController ParsedFieldsKey                      = KeyController.Get("_ParsedFields", new Guid("385D06F3-96A7-4ADF-B806-50DAB4488FD6"));
		public static KeyController WebContextKey                        = KeyController.Get("_WebContext", new Guid("EFD56382-F8BA-45D2-86D3-085974EF4D9D"));
		public static KeyController DateModifiedKey                      = KeyController.Get("DateModified", new Guid("CAAD33A3-DE94-42CF-A54A-F85C5F04940E"));
		public static KeyController DateCreatedKey                       = KeyController.Get("DateCreated", new Guid("1F322339-3FB0-4F70-918E-580D70F961EC"));
		public static KeyController AuthorKey                            = KeyController.Get("Author", new Guid("930E9E5F-06F5-4D61-A56A-8759FC4CC8DC"));
		public static KeyController VisibleTypeKey                       = KeyController.Get("Type", new Guid("B0150ECC-900E-42C4-9B86-824438647C12"));
		public static KeyController LastWorkspaceKey                     = KeyController.Get("_LastWorkspace", new Guid("66F05DB2-2F68-4E37-985D-36303A1AF4E4"));
		public static KeyController WorkspaceHistoryKey                  = KeyController.Get("_WorkspaceHistory", new Guid("D0630828-1488-4F7B-B0D7-9E89EF05497F"));
		public static KeyController WorkspaceFutureKey                   = KeyController.Get("_WorkspaceFuture", new Guid("A9CC973F-A2E1-4A21-8D0C-EE1EF503C333"));
		public static KeyController PanPositionKey                       = KeyController.Get("_PanPosition", new Guid("8778D978-AEA2-470C-8DBD-C684131BA9B4"));
		public static KeyController PanZoomKey                           = KeyController.Get("_PanZoomLevel", new Guid("4C4C676B-EEC8-4682-B15C-57866BF4933C"));
		public static KeyController ActualSizeKey                        = KeyController.Get("ActualSize", new Guid("529D7312-9A33-4A6E-80AF-FA173293DC36"));
		public static KeyController DocumentTypeKey                      = KeyController.Get("_DocumentType", new Guid("B1DE8ABE-5C04-49C6-913C-A2428ED566F8"));
		public static KeyController SelectedKey                          = KeyController.Get("_Selected", new Guid("86009EF6-7D77-4D67-8C7A-C5EA5704432F"));
		public static KeyController OriginalImageKey                     = KeyController.Get("_OriginalImage", new Guid("6226CC11-3616-4521-9C9E-731245FA1F4C"));
		public static KeyController SideCountKey                         = KeyController.Get("SideCount", new Guid("276302FF-0E5F-4009-A308-A4EE8B4224F7"));
		public static KeyController SettingsDocKey                       = KeyController.Get("_SettingsDoc", new Guid("EFD6D6B8-286F-4D34-AD44-BCFB72CD3F70"));
		public static KeyController SettingsNightModeKey                 = KeyController.Get("_Settings Night Mode", new Guid("7AA22643-3D28-433E-83E9-ECD6A7475270"));
		public static KeyController SettingsFontSizeKey                  = KeyController.Get("_Settings Font Size", new Guid("BD720922-FAD9-4821-9877-F62E3273DED8"));
		public static KeyController SettingsMouseFuncKey                 = KeyController.Get("_Settings Mouse Functionality", new Guid("867225EC-F9C7-4B14-9A5F-22B7BB71DCCB"));
		public static KeyController SettingsWebpageLayoutKey             = KeyController.Get("_Settings Webpage Layout Functionality", new Guid("7B04CE24-E876-49D7-88F9-36B25576BA07"));
		public static KeyController SettingsNumBackupsKey                = KeyController.Get("_Settings Number of Backups", new Guid("25F0DB4F-D6DE-4D48-A090-77E48C1F621C"));
		public static KeyController SettingsBackupIntervalKey            = KeyController.Get("_Settings Backup Interval", new Guid("8C00E2CD-6272-4E6C-ADC1-622B108A0D9F"));
		public static KeyController BackgroundImageStateKey              = KeyController.Get("_State of Background Image (Radio Buttons)", new Guid("3EAE5AB5-4503-4519-9EF3-0FA5BDDE59E6"));
		public static KeyController CustomBackgroundImagePathKey         = KeyController.Get("Custom Path to Background Image", new Guid("DA719660-D5CE-40CE-9BDE-D57B764CA6BF"));
		public static KeyController BackgroundImageOpacityKey            = KeyController.Get("_Background Image Opacity", new Guid("0A1CA35C-5A6F-4C8A-AF00-6C82D5DA0FEC"));
		public static KeyController SettingsUpwardPanningKey             = KeyController.Get("_Infinite Upward Panning Enabled", new Guid("3B354602-794D-4FC0-A289-1EBA7EC23FD1"));
		public static KeyController SettingsMarkdownModeKey              = KeyController.Get("_Markdown vs RTF", new Guid("2575EAFA-2689-40DD-A0A8-9EE0EC0720ED"));
		public static KeyController ActivationKey                        = KeyController.Get("_Document Template activation phase", new Guid("9BA4DB7E-304A-4F0F-8704-C4E4B970C7B9"));
		public static KeyController TemplateListKey                      = KeyController.Get("List of templates for the Mainpage", new Guid("8AC168A0-F540-455F-8DB7-553B58E8E11E"));
		public static KeyController RowInfoKey                           = KeyController.Get("List of grid row sizes", new Guid("70F35A73-89D3-40D0-941D-D964F6CB5A8D"));
		public static KeyController ColumnInfoKey                        = KeyController.Get("List of grid column sizes", new Guid("CC243D8B-8150-4C48-8DE7-F1E5EB59E3DC"));
		public static KeyController RowKey                               = KeyController.Get("GridRowNumber", new Guid("213520CB-3EE9-4948-A063-61E3B9D76953"));
		public static KeyController ColumnKey                            = KeyController.Get("GridColumnNumber", new Guid("37889D8E-86EB-4DCC-A30C-B3306E423AF2"));
		public static KeyController FontWeightKey                        = KeyController.Get("FontWeight", new Guid("02095FC5-6F49-46C1-A2DB-06FF894A5235"));
		public static KeyController FontSizeKey                          = KeyController.Get("FontSize", new Guid("75902765-7F0E-4AA6-A98B-3C8790DBF7CE"));
		public static KeyController PresentationItemsKey                 = KeyController.Get("_Presentation Items", new Guid("5AB85A0A-7983-4E08-8E51-2D53BBFB30FF"));
		public static KeyController DockedDocumentsLeftKey               = KeyController.Get("_Documents docked on the left", new Guid("0CCFCC20-DAF7-4329-B615-605A54A86014"));
		public static KeyController DockedDocumentsTopKey                = KeyController.Get("_Documents docked on the top", new Guid("5A5AC489-8988-44BE-AC06-AE76CF81FB04"));
		public static KeyController DockedDocumentsRightKey              = KeyController.Get("_Documents docked on the right", new Guid("F9E7580F-2053-49AA-B829-7B7347C65394"));
		public static KeyController DockedDocumentsBottomKey             = KeyController.Get("_Documents docked on the bottom", new Guid("F6E10E00-1644-40BE-8A9E-0C648FE4B223"));
		public static KeyController DockedLength                         = KeyController.Get("_Docked column/row length", new Guid("A31E063D-A314-4AF9-973E-595FF70A2592"));
		public static KeyController PdfRegionVerticalOffsetKey           = KeyController.Get("_Region on PDF vertical offset", new Guid("806A9F4F-1258-4630-A272-B325DC7503EC"));
		public static KeyController VisualRegionTopLeftPercentileKey     = KeyController.Get("_Top-left % of region", new Guid("FEA17CB1-3EFF-4B95-97F5-CCA67EEFB16C"));
		public static KeyController VisualRegionBottomRightPercentileKey = KeyController.Get("_Bottom-right & of region", new Guid("05BA4856-AAA4-4212-9A52-650C85F4A4D6"));
		public static KeyController SelectionRegionTopLeftKey            = KeyController.Get("_Selection Top Left", new Guid("B42844C8-B80A-4DE9-BFC3-AF3F94A83D2E"));
		public static KeyController SelectionRegionSizeKey               = KeyController.Get("_Selection Size", new Guid("34E957C1-A0FC-41B0-8862-174224FBE90B"));
		public static KeyController SelectionIndicesListKey              = KeyController.Get("_Selected Indices", new Guid("9856A787-23C9-4961-AA53-41AECA20653E"));
		public static KeyController IsAnnotationScrollVisibleKey         = KeyController.Get("Is the annotation pinned", new Guid("95734D71-5EC6-46EF-9744-608E2D8EA109"));
		public static KeyController ReplLineTextKey                      = KeyController.Get("_Repl Inputs", new Guid("EDB6FB6F-36B6-4A09-B7E5-ED3490262293"));
		public static KeyController ReplValuesKey                        = KeyController.Get("_Repl Outputs", new Guid("24D90B3A-73B9-4F51-81A3-484F43CB4265"));
		public static KeyController ReplCurrentIndentKey                 = KeyController.Get("_Repl Stored Tab Setting", new Guid("B590D696-AAD0-4DA3-A934-936C60A76394"));
		public static KeyController ReplScopeKey                         = KeyController.Get("_Repl Scope", new Guid("C1C62569-A534-4F31-8F17-94F6191B402B"));
		public static KeyController ScriptTextKey                        = KeyController.Get("_Script Text", new Guid("8BC898FC-6865-4C69-9FFF-DC64D53277B0"));
		public static KeyController ExceptionKey                         = KeyController.Get("Exception", new Guid("34171C23-6A1B-4760-BF71-06758507B26C"));
		public static KeyController ReceivedKey                          = KeyController.Get("Inputs Received", new Guid("BF6E2306-751E-4A6F-AC97-6B96DD9C5C5B"));
		public static KeyController ExpectedKey                          = KeyController.Get("Inputs Expected", new Guid("999EF787-80A7-4277-B0E4-DD36FBB48857"));
		public static KeyController FeedbackKey                          = KeyController.Get("Feedback", new Guid("EA22528F-BEBA-4CEA-AD5D-6CF58D3045B9"));
		public static KeyController CollectionFitToParentKey             = KeyController.Get("CollectionFitToParent", new Guid("61CA156E-F959-4607-A2F3-BFEFA5D00B64"));
		public static readonly KeyController HorizontalAlignmentKey      = KeyController.Get("Horizontal Alignment", new Guid("B43231DA-5A22-45A3-8476-005A62396686"));
		public static readonly KeyController VerticalAlignmentKey        = KeyController.Get("Vertical Alignment", new Guid("227B9887-BC09-40E4-A3F0-AD204D00E48D"));
		public static readonly KeyController SnapshotImage               = KeyController.Get("SnapshotImage", new Guid("1D3D649D-A29D-41DF-8608-3822D8546EEA"));
		public static KeyController AutoPlayKey                          = KeyController.Get("Is the mediaelementplayer autoplaying", new Guid("092983DC-266E-4F91-8935-1BE5CFE86A78"));
		public static KeyController GoToRegionKey                        = KeyController.Get("GotoRegion", new Guid("5A19BC33-4A83-4961-A230-4A0F8C949022"));
		public static KeyController GoToRegionLinkKey                    = KeyController.Get("GotoRegionLink", new Guid("150C5291-0830-4095-9C18-FAE1F315599F"));
		public static KeyController PresentationTitleKey                 = KeyController.Get("PresTitle", new Guid("3A153DAA-C2E1-40D9-9EE8-18CB09439EDD"));
		public static KeyController PresentationViewVisibleKey           = KeyController.Get("_Presentation Active", new Guid("7D999F66-A6A9-4A74-B2B3-AD12812FAAB6"));
		public static KeyController PresLinesVisibleKey                  = KeyController.Get("_Presentation Lines Visible", new Guid("60BC478B-DBA3-4373-A344-CD8B7398F74F"));
		public static KeyController PresLoopOnKey                        = KeyController.Get("_Presentation Loop Engaged", new Guid("DDC59860-27C9-42BF-A557-A2D97E047EB2"));
		public static KeyController PresTextRenamedKey                   = KeyController.Get("_Presentation Textbox Renamed", new Guid("AC13DAAF-5ED2-47F9-BFE5-98673ECEFFEF"));
		public static KeyController EmbeddedDocumentsKey                 = KeyController.Get("_EmbeddedDocuments", new Guid("814C3A09-3CC5-44DB-BDAC-ED5790D8F3AA"));
		public static KeyController AnonymousGroupsKey                   = KeyController.Get("Anonymous Groups", new Guid("A35F8DA4-5471-4EA7-90D1-8F76F501FFB5"));
		public static KeyController TitleMatchKey                        = KeyController.Get("Title Match", new Guid("FEBBA568-0DC5-4E8E-8FFE-339CC3E0B1D2"));
		public static KeyController TagsKey                              = KeyController.Get("Tags", new Guid("4E56A0DC-C096-4542-892C-2F4C979FF6BC"));
		public static KeyController RecentTagsKey                        = KeyController.Get("Recent Tags", new Guid("DE080F88-9A7A-4D5C-88E8-7DE1C445D6C5"));
		public static KeyController LinkTagKey                           = KeyController.Get("List of tags", new Guid("72371594-582C-46FE-BE81-9F2B95C5FD50"));
		public static KeyController SelectedSchemaRow                    = KeyController.Get("SelectedElement", new Guid("B9B5742B-E4C7-45BD-AD6E-F3C254E45027"));
		public static KeyController SchemaDisplayedColumns               = KeyController.Get("_Displayed Columns", new Guid("7424AFD5-D43B-449F-AD04-B48E686621AB"));
	    public static KeyController JoinInfoKey                          = KeyController.Get("Join Information", new Guid("08A0A6F9-6AC1-4B03-89CD-E7127646D9DB"));
	    public static KeyController AreContentsHitTestVisibleKey         = KeyController.Get("AreContentsHitTestVisible", new Guid("1F7E0A85-F5C3-483D-BB66-2A138CA8105E"));
        public static KeyController ImageStretchKey                      = KeyController.Get("ImageStretch", new Guid("3B25E910-F33B-46B2-9349-00DE53EA18F0"));

        public static KeyController TappedScriptKey                      = KeyController.Get("TappedEvent");
        public static KeyController FolderPreviewKey                     = KeyController.Get("FolderPreview");
        public static KeyController FolderPreviewDataBoxKey              = KeyController.Get("_FolderPreviewDataBox");

        public static void RegisterDocumentTypeRenderer(DocumentType type, MakeViewFunc makeViewFunc, MakeRegionFunc makeRegionFunc)
		{
			TypeRenderer[type] = makeViewFunc;
			RegionCreator[type] = makeRegionFunc;
		}

		public delegate FrameworkElement MakeViewFunc(DocumentController doc, Context context);
		public delegate DocumentController MakeRegionFunc(DocumentView view, Point? point = null);
		public static Dictionary<DocumentType, MakeViewFunc> TypeRenderer = new Dictionary<DocumentType, MakeViewFunc>();
		public static Dictionary<DocumentType, MakeRegionFunc> RegionCreator = new Dictionary<DocumentType, MakeRegionFunc>();

	}
}
