using System;
using System.Collections.Generic;
using System.Linq;

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
        /// The endpoint for the local cosmosDb emulator. If you do not have a document DB emulator you must install it from microsoft
        /// </summary>
        private const string DbLocalEndpointUrl = "https://localhost:8081";

        /// <summary>
        /// The endpoint for the server cosmosDb database found on azure portal
        /// </summary>
        private const string DbProductionEndpointUrl = "https://dash.documents.azure.com:443/";

        /// <summary>
        /// The endpoitn for the database used at runtime
        /// </summary>
        public const string DbEndpointUrl = DEVELOP_LOCALLY ? DbLocalEndpointUrl : DbProductionEndpointUrl;

        /// <summary>
        /// The access key for the local cosmosDb emulator. This is default and is always the same
        /// </summary>
        private const string DbLocalPrimaryKey =
            "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        /// <summary>
        /// The access key for the server cosmosDb database, this is secret and is found on the azure portal
        /// </summary>
        private const string DbProductionPrimaryKey =
            "GLuiQOtiC7AQQLsMlJJHmFevos5515q1HZeWBxkGTxZPNg8qXKEMgkeKnLOlxU3Sg9oS7BYrGGxSWGXx8Otkug==";

        /// <summary>
        /// The access key used to connect to the database used at runtime
        /// </summary>
        public const string DbPrimaryKey = DEVELOP_LOCALLY ? DbLocalPrimaryKey : DbProductionPrimaryKey;


        /// <summary>
        /// The name of our cosmosDb database
        /// </summary>
        public const string DocDbDatabaseId = "DashDB";

        /// <summary>
        /// The name of our cosmosDb collection
        /// </summary>
        public const string DocDbCollectionId = "DashColl";

        #endregion

        #region Server

        /// <summary>
        /// The base url for the local version of the server, should end with a /
        /// </summary>
        private const string ServerLocalBaseUrl = "http://localhost:2693/"; //

        /// <summary>
        /// The base url for the production version of the server, should end with a /
        /// </summary>
        private const string ServerProductionBaseUrl = "http://dashapp.azurewebsites.net";

        /// <summary>
        /// The base url used to connect to the server at runtime
        /// </summary>
        public const string ServerBaseUrl = DEVELOP_LOCALLY ? ServerLocalBaseUrl : ServerProductionBaseUrl;

        /// <summary>
        /// The endpoint on the server where tokens can be retrieved
        /// <para>
        /// To get the full path use <code>DashConstants.ServerBaseUrl + DashConstants.TokenEndpoint</code>
        /// </para>
        /// </summary>
        public const string TokenEndpoint = "Token";

        /// <summary>
        /// The minimum required length of a password for our users
        /// </summary>
        public const int PasswordMinimumLength = 1;


        #endregion

        #region Signalr

        /// <summary>
        /// The endpoint on the server where signalr code is accessed
        /// <para>
        /// To get the full path use <code>DashConstants.ServerBaseUrl + DashConstants.SignalrBaseUrl</code>
        /// </para>
        /// </summary>
        public const string SignalrBaseUrl = "signalr";

        /// <summary>
        /// The name of the shape hub
        /// </summary>
        public const string HubShapeName = "shapeHub";
        #endregion

        #region LocalCredentials

        /// <summary>
        /// The resource name used to store credentials in the local credential store
        /// </summary>
        public const string ResourceName = "DashApp";

        #endregion

        public static class KeyStore
        {

            public static Key LayoutKey = new Key("4CD28733-93FB-4DF4-B878-289B14D5BFE1", "Layout");
            public static Key PrototypeKey = new Key("866A6CC9-0B8D-49A3-B45F-D7954631A682", "Prototype");
            public static Key DelegatesKey = new Key("D737A3D8-DB2C-40EB-8DAB-129D58BC6ADB", "Delegates");
            public static Key XPositionFieldKey = new Key("CC6B105F-D73C-4076-973F-FAF2A4CA6218", "XPositionFieldKey");
            public static Key YPositionFieldKey = new Key("EF7CCB37-3512-463B-915A-EB8078702F14", "YPositionFieldKey");
            public static Key WidthFieldKey = new Key("5B329D99-96BF-4703-8E28-9B7B1C1B837E", "WidthFieldKey");
            public static Key HeightFieldKey = new Key("9ED34365-C821-4FB2-A955-A8C0B10C77C5", "HeightFieldKey");
            public static Key DataKey = new Key("3B1BD1C3-1BCD-469D-B847-835B565B53EB", "Data");
        }

        public static class DocumentTypeStore
        {
            public static DocumentType OperatorDocumentType = new DocumentType("3FF64E84-A614-46AF-9742-FB5F6E2E37CE", "operator");

            public static DocumentType TextBoxDocumentType = new DocumentType("D63DDB00-1A66-4E3A-A19B-6B06BBD6DAC8", "Text Box");
            public static DocumentType FreeFormCollectionDocumentType = new DocumentType("7C59D0E9-11E8-4F12-B355-20035B3AC359", "Free Form Collection");
        }


    }
}