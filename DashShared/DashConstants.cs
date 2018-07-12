

//using Windows.Storage;

using System;

namespace DashShared
{
    public static class DashConstants
    {
        /// <summary>
        ///     Set the endpoints to local endpoints or server side endpoitns. If Local you must have cosmosDb emulator installed
        /// </summary>
        public const bool DEVELOP_LOCALLY = true;

        #region DocumentDB

        /// <summary>
        ///     The endpoint for the local cosmosDb emulator. If you do not have a document DB emulator you must install it from
        ///     microsoft
        /// </summary>
        private const string DbLocalEndpointUrl = "https://localhost:8081";

        /// <summary>
        ///     The endpoint for the server cosmosDb database found on azure portal
        /// </summary>
        private const string DbProductionEndpointUrl = "https://dash.documents.azure.com:443/";

        /// <summary>
        ///     The endpoitn for the database used at runtime
        /// </summary>
        public const string DbEndpointUrl = DEVELOP_LOCALLY ? DbLocalEndpointUrl : DbProductionEndpointUrl;

        /// <summary>
        ///     The access key for the local cosmosDb emulator. This is default and is always the same
        /// </summary>
        private const string DbLocalPrimaryKey =
            "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        /// <summary>
        ///     The access key for the server cosmosDb database, this is secret and is found on the azure portal
        /// </summary>
        private const string DbProductionPrimaryKey =
            "GLuiQOtiC7AQQLsMlJJHmFevos5515q1HZeWBxkGTxZPNg8qXKEMgkeKnLOlxU3Sg9oS7BYrGGxSWGXx8Otkug==";

        /// <summary>
        ///     The access key used to connect to the database used at runtime
        /// </summary>
        public const string DbPrimaryKey = DEVELOP_LOCALLY ? DbLocalPrimaryKey : DbProductionPrimaryKey;


        /// <summary>
        ///     The name of our cosmosDb database
        /// </summary>
        public const string DocDbDatabaseId = "DashDB";

        /// <summary>
        ///     The name of our cosmosDb collection
        /// </summary>
        public const string DocDbCollectionId = "DashColl";

        #endregion

        #region BlobStore

        private const string BlobLocalAccountName = "devstoreaccount1";
        private const string BlobServerAccountName = "dashblobstore";
        public const string BlobAccountName = DEVELOP_LOCALLY ? BlobLocalAccountName : BlobServerAccountName;

        private const string BlobLocalAccountKey =
            "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

        private const string BlobServerAccountKey =
            "3i+E5XkCz3TJ0m5QOatiEnbRACz9V1qCW72L6ldiYGH1tLdfJWa2eQoRfYmPA68lx1a6YAcfYJfWHadIxQvhGQ==";

        public const string BlobAccountKey = DEVELOP_LOCALLY ? BlobLocalAccountKey : BlobServerAccountKey;

        public static readonly string BlobConnectionString =
            DEVELOP_LOCALLY
                ? "UseDevelopmentStorage=true;"
                : $"DefaultEndpointsProtocol=https;AccountName={BlobAccountName};AccountKey={BlobAccountKey};";

        public const string BlobContainerName = "maincontainer";

        #endregion

        #region LocalServer

       // public static StorageFolder LocalStorageFolder = ApplicationData.Current.LocalFolder;

        public static string LocalServerDocumentFilepath = "dash.documents"; //
        public static string LocalServerKeyFilepath = "dash.keys"; //
        public static string LocalServerFieldFilepath =  "dash.fields"; //
        public const int MillisecondBetweenLocalSave = 1000; //1 second

        //BACKUP CONSTANTS

        //Minimum
        public const int MinNumBackups = 1;
        public const int MinBackupInterval = 30;

        //Maximum
        public const int MaxNumBackups = 10;
        public const int MaxBackupInterval = 3600;

        //DEFAULT SETTINGS CONSTANTS

        public const bool DefaultNightModeEngaged = false; //theme state
        public const int DefaultFontSize = 16; //pt
        public const int DefaultNumBackups = 3; //backups
        public const int DefaultBackupInterval = 900; //seconds
        public const bool DefaultInfiniteUpwardPanningStatus = true;

        #endregion LocalServer

        #region Server

        /// <summary>
        ///     The base url for the local version of the server, should end with a /
        /// </summary>
        private const string ServerLocalBaseUrl = "http://localhost:2222/"; //

        /// <summary>
        ///     The base url for the production version of the server, should end with a /
        /// </summary>
        private const string ServerProductionBaseUrl = "http://dashapp.azurewebsites.net/";

        /// <summary>
        ///     The base url used to connect to the server at runtime
        /// </summary>
        public const string ServerBaseUrl = DEVELOP_LOCALLY ? ServerLocalBaseUrl : ServerProductionBaseUrl;

        /// <summary>
        ///     The endpoint on the server where tokens can be retrieved
        ///     <para>
        ///         To get the full path use <code>DashConstants.ServerBaseUrl + DashConstants.TokenEndpoint</code>
        ///     </para>
        /// </summary>
        public const string TokenEndpoint = "Token";

        /// <summary>
        ///     The minimum required length of a password for our users
        /// </summary>
        public const int PasswordMinimumLength = 1;

        public const string StoredProceduresDirectory = "StoredProcedures/";

        #endregion

        #region LocalCredentials

        /// <summary>
        /// The resource name used to store credentials in the local credential store
        /// </summary>
        public const string ResourceName = "DashApp";

        #endregion

        //SCRIPTING LANGUAGE

        public const int ScriptingInfiniteLoopTimeout = 5000; //milliseconds
        public const string ForInPhantomCounterName = "c";

        public static class KeyStore
        {
            public static KeyModel DocumentContextKey = new KeyModel("17D4CFDE-9146-47E9-8AF0-0F9D546E94EC", "_DocumentContext");
            public static KeyModel AbstractInterfaceKey = new KeyModel("E579C81B-EE13-4B16-BB96-80688D30A73A", "_AbstractInterface");
            public static KeyModel LayoutListKey = new KeyModel("6546DD08-C753-4C34-924E-3C4016C4B95B", "_LayoutList");
            public static KeyModel ActiveLayoutKey = new KeyModel("BEBEC91F-F85A-4F72-A7D2-E2912571FBDA", "ActiveLayout");
            public static KeyModel TitleKey = new KeyModel("0C074CB4-6D05-4363-A867-C0A061C1573F", "Title");
            public static KeyModel CaptionKey = new KeyModel("D01D6702-A3AD-4546-9BFB-C5263F8D5599", "Caption");
            public static KeyModel PrototypeKey = new KeyModel("866A6CC9-0B8D-49A3-B45F-D7954631A682", "_Prototype");
            public static KeyModel DelegatesKey = new KeyModel("D737A3D8-DB2C-40EB-8DAB-129D58BC6ADB", "_Delegates");
            public static KeyModel UserSetWidthKey = new KeyModel("7D3E7CDB-D0C7-4316-BA3B-3C032F24B5AA", "_userSetWidth");
            public static KeyModel WidthFieldKey = new KeyModel("5B329D99-96BF-4703-8E28-9B7B1C1B837E", "Width");
            public static KeyModel HeightFieldKey = new KeyModel("9ED34365-C821-4FB2-A955-A8C0B10C77C5", "Height");
            public static KeyModel DataKey = new KeyModel("3B1BD1C3-1BCD-469D-B847-835B565B53EB", "Data");
            public static KeyModel SourceUriKey = new KeyModel("26594498-FF15-438D-A577-2C8506F4ECEF", "SourceUriKeys");
            public static KeyModel DocumentTextKey = new KeyModel("D5156A8F-9093-420B-96B7-507DD949360D", "Document Text"); 
            public static KeyModel TextWrappingKey = new KeyModel("FF488D09-BBB7-4158-A5E4-0C4530DF2F56", "Text Wrapping");
            public static KeyModel BackgroundColorKey = new KeyModel("6B597D2A-1A52-446F-901A-B9ED0BBE33E1", "Background Color");
            public static KeyModel OpacitySliderValueKey = new KeyModel("3FD448B7-8AEE-4FBD-B68C-514E098D8D31", "Opacity Slider Value");
            public static KeyModel AdornmentShapeKey = new KeyModel("5DEBC829-A68B-4D2E-BC29-549DEB910EC6", "Adornment Shape");
            public static KeyModel AdornmentKey = new KeyModel("FF3329BD-AA78-46E4-9A42-47CAB1E62123", "Is Adornment");
            public static KeyModel PositionFieldKey = new KeyModel("E2AB7D27-FA81-4D88-B2FA-42B7888525AF", "Position");
            public static KeyModel LinkFromFieldKey = new KeyModel("9A3191FF-C8E6-472F-ABE5-B5A250D49D59", "Link From");
            public static KeyModel LinkToFieldKey = new KeyModel("649A7F35-C428-49EC-B914-5746E2590DAC", "Link To");
            public static KeyModel PdfVOffsetFieldKey = new KeyModel("8990098B-83D2-4817-A275-82D8282ECD79", "_PdfVOffset"); 
            public static KeyModel ScaleCenterFieldKey = new KeyModel("FE4IMA9J-NOE9-3NGS-G09Q-JFOE9038S82S" , "_Scale Center");
            public static KeyModel ScaleAmountFieldKey = new KeyModel("AOEKMA9J-IP37-96HI-VJ36-IHFI39AHI8DE", "_Scale Amount");
            public static KeyModel IconTypeFieldKey = new KeyModel("ICON7D27-FA81-4D88-B2FA-42B7888525AF", "_IconType");
            public static KeyModel SystemUriKey = new KeyModel("CA740B60-10D5-4B2C-9C9A-E6E4A7D2CA4E", "File Path");
            public static KeyModel ThumbnailFieldKey = new KeyModel("67D3BD61-43EC-4BDE-913A-E459F9D15E76", "_ThumbnailField");
            public static KeyModel HeaderKey = new KeyModel("93CF85C8-5522-4B00-927A-943982250729", "Header");
            public static KeyModel CollectionOutputKey = new KeyModel("D4FD93F5-A3DA-41CF-8FB2-3C7A659B7850", "Collection Output");
            public static KeyModel OperatorKey = new KeyModel("F5B0E5E0-2C1F-4E49-BD26-5F6CBCDE766A", "Operator");
            public static KeyModel OperatorCacheKey = new KeyModel("1B1409DE-4BA5-4515-9BB4-B15AE8CC0041", "_Operator Cache");
            public static KeyModel ParsedFieldsKey = new KeyModel("385D06F3-96A7-4ADF-B806-50DAB4488FD6", "_Parsed Fields");
            public static KeyModel WebContextKey = new KeyModel("EFD56382-F8BA-45D2-86D3-085974EF4D9D", "_WebContext");
            public static KeyModel ModifiedTimestampKey = new KeyModel("CAAD33A3-DE94-42CF-A54A-F85C5F04940E", "ModifiedTime");
            public static KeyModel LastWorkspaceKey = new KeyModel("66F05DB2-2F68-4E37-985D-36303A1AF4E4", "_Last Workspace");
            public static KeyModel WorkspaceHistoryKey = new KeyModel("D0630828-1488-4F7B-B0D7-9E89EF05497F", "_Workspace History");
            public static KeyModel PanPositionKey = new KeyModel("8778D978-AEA2-470C-8DBD-C684131BA9B4", "_Pan Position");
            public static KeyModel PanZoomKey = new KeyModel("4C4C676B-EEC8-4682-B15C-57866BF4933C", "_Pan Zoom Level");
            public static KeyModel ActualSizeKey = new KeyModel("529D7312-9A33-4A6E-80AF-FA173293DC36", "ActualSize");
            public static KeyModel DocumentTypeKey = new KeyModel("B1DE8ABE-5C04-49C6-913C-A2428ED566F8", "_DocumentType");
            public static KeyModel SelectedKey = new KeyModel("86009EF6-7D77-4D67-8C7A-C5EA5704432F", "_Selected");
            public static KeyModel OriginalImageKey = new KeyModel("6226CC11-3616-4521-9C9E-731245FA1F4C", "_Original Image");
            public static KeyModel SideCountKey = new KeyModel("276302FF-0E5F-4009-A308-A4EE8B4224F7", "Side Count");
            public static KeyModel SettingsDocKey = new KeyModel("EFD6D6B8-286F-4D34-AD44-BCFB72CD3F70", "Settings Doc");
            public static KeyModel SettingsNightModeKey = new KeyModel("7AA22643-3D28-433E-83E9-ECD6A7475270", "Settings Night Mode");
            public static KeyModel SettingsFontSizeKey = new KeyModel("BD720922-FAD9-4821-9877-F62E3273DED8", "Settings Font Size");
            public static KeyModel SettingsMouseFuncKey = new KeyModel("867225EC-F9C7-4B14-9A5F-22B7BB71DCCB", "Settings Mouse Functionality");
            public static KeyModel SettingsNumBackupsKey = new KeyModel("25F0DB4F-D6DE-4D48-A090-77E48C1F621C", "Settings Number of Backups");
            public static KeyModel SettingsBackupIntervalKey = new KeyModel("8C00E2CD-6272-4E6C-ADC1-622B108A0D9F", "Settings Backup Interval");
            public static KeyModel BackgroundImageStateKey = new KeyModel("3EAE5AB5-4503-4519-9EF3-0FA5BDDE59E6", "State of Background Image (Radio Buttons)");
            public static KeyModel CustomBackgroundImagePathKey = new KeyModel("DA719660-D5CE-40CE-9BDE-D57B764CA6BF", "Custom Path to Background Image");
            public static KeyModel BackgroundImageOpacityKey = new KeyModel("0A1CA35C-5A6F-4C8A-AF00-6C82D5DA0FEC", "Background Image Opacity");
            public static KeyModel SettingsUpwardPanningKey = new KeyModel("3B354602-794D-4FC0-A289-1EBA7EC23FD1", "Infinite Upward Panning Enabled");
            public static KeyModel SettingsMarkdownModeKey = new KeyModel("2575EAFA-2689-40DD-A0A8-9EE0EC0720ED", "Markdown vs RTF");

            public static KeyModel ReplLineTextsKey = new  KeyModel("C17B3D33-EAE2-4477-BC5B-0ECDADC48779", "Repl Inputs");
            public static KeyModel ReplValuesKey = new KeyModel("6FE39D8B-933D-4478-8499-71C8AAE887BA", "Repl Outputs");
            public static KeyModel ReplCurrentIndentKey = new KeyModel("AADD7D98-CABF-4355-AD19-81437F9A53C4", "Repl Stored Tab Setting");
            public static KeyModel ReplScopeKey = new   KeyModel("D4390CDC-FD08-4A69-912F-54A6C6FA9304", "Repl Scope");

            public static KeyModel ScriptTextKey = new KeyModel("9F34AB80-5ACA-4981-823A-45FD481507CE", "Script Text");

            public static KeyModel DockedDocumentsLeftKey = new KeyModel("0CCFCC20-DAF7-4329-B615-605A54A86014", "Documents docked on the left");
            public static KeyModel DockedDocumentsTopKey = new KeyModel("5A5AC489-8988-44BE-AC06-AE76CF81FB04", "Documents docked on the top");
            public static KeyModel DockedDocumentsRightKey = new KeyModel("F9E7580F-2053-49AA-B829-7B7347C65394", "Documents docked on the right");
            public static KeyModel DockedDocumentsBottomKey = new KeyModel("F6E10E00-1644-40BE-8A9E-0C648FE4B223", "Documents docked on the bottom");
        }

        public static class TypeStore
        {
            public static DocumentType FreeFormDocumentType = new DocumentType("0E2B8354-D3B3-4A45-8A47-C7BF9A46B46C", "Free Form Layout");
            public static DocumentType CollectionDocument  = new DocumentType("2D4D93AE-6A88-4723-B254-7DA2959D0240", "collection");
            public static DocumentType FileLinkDocument = new DocumentType("54442257-4BF4-4EB0-B3E8-B8868951F198", "File link");
            public static DocumentType OperatorType = new DocumentType("3FF64E84-A614-46AF-9742-FB5F6E2E37CE", "operator");
            public static DocumentType CollectionBoxType = new DocumentType("7C59D0E9-11E8-4F12-B355-20035B3AC359", "Collection Box");
            public static DocumentType MapOperatorBoxType = new DocumentType("AC7E7026-0522-4E8C-8F05-83AE7AB4000C", "Collection Map Box");
            public static DocumentType OperatorBoxType = new DocumentType("53FC9C82-F32C-4704-AF6B-E55AC805C84F", "Operator Box");
            public static DocumentType MainDocumentType = new DocumentType("011EFC3F-5405-4A27-8689-C0F37AAB9B2E", "Main Document");
            public static DocumentType MeltOperatorBoxDocumentType = new DocumentType("8A0E72A1-0FF4-4AAB-9C12-9DF09DCF39CA", "Melt Operator Box");
            public static DocumentType ExtractSentencesDocumentType = new DocumentType("3B6B9420-FD08-4CBA-99AA-5FAA261266AE", "Extract Sentences Operator Box");
            public static DocumentType SearchOperatorType = new DocumentType("7A83F04B-7715-40B3-A867-B29E7812B8C4", "Search Operator");
            public static DocumentType QuizletOperatorType = new DocumentType("7F97DB94-CE77-4082-8E1B-EF4518475C38", "Quizlet Operator");
            public static DocumentType ErrorType = new DocumentType("55F9E738-C215-4EA7-8878-4C561C16A5FC", "Error Message");
        }
    }
}