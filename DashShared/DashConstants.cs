

//using Windows.Storage;

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
        public const int DefaultFontSize = 12; //pt
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
            public static DocumentType FieldContentNote = new DocumentType("E9F45F55-3391-4714-8EEA-7EF5D9CF77B7", "Field Content Notification");
        }
    }
}
