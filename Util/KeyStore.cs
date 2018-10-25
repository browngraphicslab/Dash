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
	    public static KeyController DocumentContextKey                   = KeyController.Get("DocumentContext");
		public static KeyController AbstractInterfaceKey                 = KeyController.Get("_AbstractInterface");
		public static KeyController LayoutListKey                        = KeyController.Get("_LayoutList");
		public static KeyController RegionsKey                           = KeyController.Get("Regions");
		public static KeyController RegionDefinitionKey                  = KeyController.Get("RegionDefinition");
		public static KeyController RegionTypeKey                        = KeyController.Get("RegionType");
		public static KeyController TitleKey                             = KeyController.Get("Title");
		public static KeyController CaptionKey                           = KeyController.Get("Caption");
		public static KeyController PrototypeKey                         = KeyController.Get("Prototype");
        public static KeyController CollectionItemLayoutPrototypeKey     = KeyController.Get("CollectionItemLayoutPrototype");    // layout prototype shared by each item in a collection -- set using TableExtraction, used in CollectionPageView
        public static KeyController LayoutPrototypeKey                   = KeyController.Get("LayoutPrototype");                  // layout prototype used by a document - set using TableExtraction, used in CollectionDBSchema
        public static KeyController ColumnSortingKey                     = KeyController.Get("ColumnSort");                       // list containing column name and direction to sort a schema table
		public static KeyController DelegatesKey                         = KeyController.Get("_Delegates");
		public static KeyController UserSetWidthKey                      = KeyController.Get("_userSetWidth");
		public static KeyController WidthFieldKey                        = KeyController.Get("Width");
		public static KeyController HeightFieldKey                       = KeyController.Get("Height");
		public static KeyController TransientKey                         = KeyController.Get("Transient");
		public static KeyController HiddenKey                            = KeyController.Get("Hidden");
		public static KeyController DataKey                             { get; } = KeyController.Get("Data");
		public static KeyController SnapshotsKey                         = KeyController.Get("Snaphshots");
		public static KeyController SourceUriKey                         = KeyController.Get("SourceUriKeys");
		public static KeyController SourceTitleKey                       = KeyController.Get("SourceTitle");
		public static KeyController IsAdornmentKey                       = KeyController.Get("IsAdornment");
		public static KeyController IsButtonKey                          = KeyController.Get("IsButton");
		public static KeyController DocumentTextKey                      = KeyController.Get("DocumentText");
		public static KeyController TextWrappingKey                      = KeyController.Get("TextWrapping");
		public static KeyController BackgroundColorKey                   = KeyController.Get("BackgroundColor");
		public static KeyController SavedColorsKey                       = KeyController.Get("_SavedColorsFromPicker");
		public static KeyController OpacitySliderValueKey                = KeyController.Get("OpacitySliderValue");
		public static KeyController GroupBackgroundColorKey              = KeyController.Get("GroupBackgroundColor");
		public static KeyController AdornmentShapeKey                    = KeyController.Get("Adornment Shape");
		public static KeyController PositionFieldKey                     = KeyController.Get("Position");
		public static KeyController LinkFromKey                          = KeyController.Get("LinkFrom");
		public static KeyController LinkToKey                            = KeyController.Get("LinkTo");
		public static KeyController LinkDestinationKey                   = KeyController.Get("LinkDestination");
		public static KeyController LinkSourceKey                        = KeyController.Get("LinkSource");
	    public static KeyController LinkBehaviorKey                      = KeyController.Get("LinkBehavior");
	    public static KeyController LinkContextKey                       = KeyController.Get("LinkContext");
        public static KeyController PdfVOffsetFieldKey                   = KeyController.Get("_PdfVOffset");
		public static KeyController ReferencesDictKey                    = KeyController.Get("_PDF Reference Mapping");
		public static KeyController ReferenceNumKey                      = KeyController.Get("Reference #");
		public static KeyController ReferenceDateKey                     = KeyController.Get("Date Published");
		public static KeyController ScaleAmountFieldKey                  = KeyController.Get("_Scale Amount");
		public static KeyController IconTypeFieldKey                     = KeyController.Get("_IconType");
		public static KeyController SystemUriKey                         = KeyController.Get("File Path");
		public static KeyController ThumbnailFieldKey                    = KeyController.Get("_ThumbnailField");
		public static KeyController HeaderKey                            = KeyController.Get("Header");
		public static KeyController CollectionOutputKey                  = KeyController.Get("CollectionOutput");
		public static KeyController OperatorKey                          = KeyController.Get("_Operator");
		public static KeyController OperatorCacheKey                     = KeyController.Get("_OperatorCache");
		public static KeyController CollectionViewTypeKey                = KeyController.Get("Collection View Type");
        public static KeyController CollectionOpenViewTypeKey            = KeyController.Get("Collection Open View Type"); // the view type of the collection when opened from its iconic form
        public static KeyController CollectionOpenWidthKey               = KeyController.Get("Collection Open Width"); // the width of the collection when opened from its iconic form
        public static KeyController CollectionOpenHeightKey              = KeyController.Get("Collection Open Height"); // the height of the collection when opened from its iconic form
        public static KeyController InkDataKey                           = KeyController.Get("_InkData");
		public static KeyController ParsedFieldsKey                      = KeyController.Get("_ParsedFields");
		public static KeyController WebContextKey                        = KeyController.Get("_WebContext");
		public static KeyController DateModifiedKey                      = KeyController.Get("DateModified");
		public static KeyController DateCreatedKey                       = KeyController.Get("DateCreated");
		public static KeyController AuthorKey                            = KeyController.Get("Author");
		public static KeyController VisibleTypeKey                       = KeyController.Get("Type");
		public static KeyController LastWorkspaceKey                     = KeyController.Get("_LastWorkspace");
		public static KeyController WorkspaceHistoryKey                  = KeyController.Get("_WorkspaceHistory");
		public static KeyController WorkspaceFutureKey                   = KeyController.Get("_WorkspaceFuture");
		public static KeyController PanPositionKey                       = KeyController.Get("_PanPosition");
		public static KeyController PanZoomKey                           = KeyController.Get("_PanZoomLevel");
		public static KeyController ActualSizeKey                        = KeyController.Get("ActualSize");
		public static KeyController DocumentTypeKey                      = KeyController.Get("_DocumentType");
		public static KeyController SelectedKey                          = KeyController.Get("_Selected");
		public static KeyController OriginalImageKey                     = KeyController.Get("_OriginalImage");
		public static KeyController SideCountKey                         = KeyController.Get("SideCount");
		public static KeyController SettingsDocKey                       = KeyController.Get("_SettingsDoc");
		public static KeyController SettingsNightModeKey                 = KeyController.Get("_Settings Night Mode");
		public static KeyController SettingsFontSizeKey                  = KeyController.Get("_Settings Font Size");
		public static KeyController SettingsMouseFuncKey                 = KeyController.Get("_Settings Mouse Functionality");
		public static KeyController SettingsWebpageLayoutKey             = KeyController.Get("_Settings Webpage Layout Functionality");
		public static KeyController SettingsNumBackupsKey                = KeyController.Get("_Settings Number of Backups");
		public static KeyController SettingsBackupIntervalKey            = KeyController.Get("_Settings Backup Interval");
		public static KeyController BackgroundImageStateKey              = KeyController.Get("_State of Background Image (Radio Buttons)");
		public static KeyController CustomBackgroundImagePathKey         = KeyController.Get("Custom Path to Background Image");
		public static KeyController BackgroundImageOpacityKey            = KeyController.Get("_Background Image Opacity");
		public static KeyController SettingsUpwardPanningKey             = KeyController.Get("_Infinite Upward Panning Enabled");
		public static KeyController SettingsMarkdownModeKey              = KeyController.Get("_Markdown vs RTF");
		public static KeyController ActivationKey                        = KeyController.Get("_Document Template activation phase");
		public static KeyController TemplateListKey                      = KeyController.Get("List of templates for the Mainpage");
		public static KeyController RowInfoKey                           = KeyController.Get("List of grid row sizes");
		public static KeyController ColumnInfoKey                        = KeyController.Get("List of grid column sizes");
		public static KeyController RowKey                               = KeyController.Get("GridRowNumber");
		public static KeyController ColumnKey                            = KeyController.Get("GridColumnNumber");
		public static KeyController FontWeightKey                        = KeyController.Get("FontWeight");
		public static KeyController FontSizeKey                          = KeyController.Get("FontSize");
		public static KeyController PresentationItemsKey                 = KeyController.Get("_Presentation Items");
		public static KeyController DockedDocumentsLeftKey               = KeyController.Get("_Documents docked on the left");
		public static KeyController DockedDocumentsTopKey                = KeyController.Get("_Documents docked on the top");
		public static KeyController DockedDocumentsRightKey              = KeyController.Get("_Documents docked on the right");
		public static KeyController DockedDocumentsBottomKey             = KeyController.Get("_Documents docked on the bottom");
		public static KeyController DockedLength                         = KeyController.Get("_Docked column/row length");
		public static KeyController PdfRegionVerticalOffsetKey           = KeyController.Get("_Region on PDF vertical offset");
		public static KeyController VisualRegionTopLeftPercentileKey     = KeyController.Get("_Top-left % of region");
		public static KeyController VisualRegionBottomRightPercentileKey = KeyController.Get("_Bottom-right & of region");
		public static KeyController SelectionRegionTopLeftKey            = KeyController.Get("_Selection Top Left");
		public static KeyController SelectionRegionSizeKey               = KeyController.Get("_Selection Size");
		public static KeyController SelectionIndicesListKey              = KeyController.Get("_Selected Indices");
		public static KeyController IsAnnotationScrollVisibleKey         = KeyController.Get("Is the annotation pinned");
		public static KeyController ReplLineTextKey                      = KeyController.Get("_Repl Inputs");
		public static KeyController ReplValuesKey                        = KeyController.Get("_Repl Outputs");
		public static KeyController ReplCurrentIndentKey                 = KeyController.Get("_Repl Stored Tab Setting");
		public static KeyController ReplScopeKey                         = KeyController.Get("_Repl Scope");
		public static KeyController ScriptTextKey                        = KeyController.Get("_Script Text");
		public static KeyController ExceptionKey                         = KeyController.Get("Exception");
		public static KeyController ReceivedKey                          = KeyController.Get("Inputs Received");
		public static KeyController ExpectedKey                          = KeyController.Get("Inputs Expected");
		public static KeyController FeedbackKey                          = KeyController.Get("Feedback");
		public static KeyController CollectionFitToParentKey             = KeyController.Get("CollectionFitToParent");
		public static readonly KeyController HorizontalAlignmentKey      = KeyController.Get("Horizontal Alignment");
		public static readonly KeyController VerticalAlignmentKey        = KeyController.Get("Vertical Alignment");
		public static readonly KeyController SnapshotImage               = KeyController.Get("SnapshotImage");
		public static KeyController AutoPlayKey                          = KeyController.Get("Is the mediaelementplayer autoplaying");
		public static KeyController GoToRegionKey                        = KeyController.Get("GotoRegion");
		public static KeyController GoToRegionLinkKey                    = KeyController.Get("GotoRegionLink");
		public static KeyController PresentationTitleKey                 = KeyController.Get("PresTitle");
		public static KeyController PresentationViewVisibleKey           = KeyController.Get("_Presentation Active");
		public static KeyController PresLinesVisibleKey                  = KeyController.Get("_Presentation Lines Visible");
		public static KeyController PresLoopOnKey                        = KeyController.Get("_Presentation Loop Engaged");
		public static KeyController PresTextRenamedKey                   = KeyController.Get("_Presentation Textbox Renamed");
		public static KeyController EmbeddedDocumentsKey                 = KeyController.Get("_EmbeddedDocuments");
		public static KeyController AnonymousGroupsKey                   = KeyController.Get("Anonymous Groups");
		public static KeyController TitleMatchKey                        = KeyController.Get("Title Match");
		public static KeyController TagsKey                              = KeyController.Get("Tags");
		public static KeyController RecentTagsKey                        = KeyController.Get("Recent Tags");
		public static KeyController LinkTagKey                           = KeyController.Get("List of tags");
		public static KeyController SelectedSchemaRow                    = KeyController.Get("SelectedElement");
		public static KeyController SchemaDisplayedColumns               = KeyController.Get("_Displayed Columns");
	    public static KeyController JoinInfoKey                          = KeyController.Get("Join Information");
	    public static KeyController AreContentsHitTestVisibleKey         = KeyController.Get("AreContentsHitTestVisible");
        public static KeyController ImageStretchKey                      = KeyController.Get("ImageStretch");

        public static KeyController TappedScriptKey                      = KeyController.Get("TappedEvent");
        public static KeyController FolderPreviewKey                     = KeyController.Get("FolderPreview");
        public static KeyController FolderPreviewDataBoxKey              = KeyController.Get("_FolderPreviewDataBox");
        public static KeyController FolderIconKey                        = KeyController.Get("FolderIcon");
        public static KeyController FolderIconDataBoxKey                 = KeyController.Get("_FolderIconDataBox");

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
