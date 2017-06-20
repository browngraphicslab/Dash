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
    }
}