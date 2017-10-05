﻿using Windows.Storage;

namespace DashShared
{
    public static class DashConstants
    {
        /// <summary>
        ///     Set the endpoints to local endpoints or server side endpoitns. If Local you must have cosmosDb emulator installed
        /// </summary>
        public const bool DEVELOP_LOCALLY = true;

        #region LocalCredentials

        /// <summary>
        ///     The resource name used to store credentials in the local credential store
        /// </summary>
        public const string ResourceName = "DashApp";

        #endregion

        public static class KeyStore
        {
            public static KeyModel LayoutListKey = new KeyModel("6546DD08-C753-4C34-924E-3C4016C4B95B", "_LayoutList");

            public static KeyModel ActiveLayoutKey =
                new KeyModel("BEBEC91F-F85A-4F72-A7D2-E2912571FBDA", "_ActiveLayout");

            public static KeyModel PrimaryKeyKey = new KeyModel("E3A498E8-E16B-408E-B939-3ADDFEA7BCC1", "_PrimaryKey");
            public static KeyModel ThisKey = new KeyModel("47B14309-D900-47C9-8D93-0777AD733496", "_This");
            public static KeyModel PrototypeKey = new KeyModel("866A6CC9-0B8D-49A3-B45F-D7954631A682", "_Prototype");
            public static KeyModel DelegatesKey = new KeyModel("D737A3D8-DB2C-40EB-8DAB-129D58BC6ADB", "_Delegates");
            public static KeyModel WidthFieldKey = new KeyModel("5B329D99-96BF-4703-8E28-9B7B1C1B837E", "Width");
            public static KeyModel HeightFieldKey = new KeyModel("9ED34365-C821-4FB2-A955-A8C0B10C77C5", "Height");
            public static KeyModel DataKey = new KeyModel("3B1BD1C3-1BCD-469D-B847-835B565B53EB", "Data");
            public static KeyModel PositionFieldKey = new KeyModel("E2AB7D27-FA81-4D88-B2FA-42B7888525AF", "Position");

            public static KeyModel ScaleCenterFieldKey =
                new KeyModel("FE4IMA9J-NOE9-3NGS-G09Q-JFOE9038S82S", "Scale Center");

            public static KeyModel ScaleAmountFieldKey =
                new KeyModel("AOEKMA9J-IP37-96HI-VJ36-IHFI39AHI8DE", "Scale Amount");

            public static KeyModel IconTypeFieldKey = new KeyModel("ICON7D27-FA81-4D88-B2FA-42B7888525AF", "_IconType");
        }

        public static class TypeStore
        {
            public static DocumentType FreeFormDocumentLayout =
                new DocumentType("0E2B8354-D3B3-4A45-8A47-C7BF9A46B46C", "Free Form Layout");

            public static DocumentType CollectionDocument =
                new DocumentType("2D4D93AE-6A88-4723-B254-7DA2959D0240", "collection");

            public static DocumentType MainDocumentType = 
                new DocumentType("011EFC3F-5405-4A27-8689-C0F37AAB9B2E", "Main Document");

            public static DocumentType HomePageType =
                new DocumentType("7E8A1CB6-9D2E-4FEF-8C35-1E85667A5395", "Home Page");
        }

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

        public static StorageFolder LocalStorageFolder = ApplicationData.Current.LocalFolder;

        public static string LocalServerDocumentFilepath = "dash.documents"; //
        public static string LocalServerKeyFilepath = "dash.keys"; //
        public static string LocalServerFieldFilepath =  "dash.fields"; //

        public static int MillisecondBetweenLocalSave = 5000;

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
    }
}