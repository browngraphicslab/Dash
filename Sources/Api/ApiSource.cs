﻿using DashShared;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using static Dash.CourtesyDocuments;

namespace Dash {
    /// <summary>
    /// Instantiations of this class represent a user-created API interface: an APISource.
    /// 
    /// TODO: Ask about abstracting HttpClient s.t. each NuSys Dashboard instance
    /// only has one HttpClient that makes all the requests.
    /// </summary>
    class ApiSource {
        // == MEMBERS ==
        private HttpMethod requestType; // POST, GET, etc.
        private Uri authURI;
        private string secret, key;
        private Dictionary<string, ApiProperty> headers, parameters, authHeaders, authParameters;
        private HttpClient client;
        private TextBox apiUrlTB;
        private HttpResponseMessage response;
        private List<DocumentController> responseAsDocuments; // list of results formatted as documents
        private ApiSourceDisplay display;
        public TextBox debugger;
        private Canvas testGrid;
        private DocumentController docController;

        // == CONSTRUCTORS ==
        public ApiSource(DocumentController docController, HttpMethod requestType, TextBox apiURLTB, string authURL, string secret, string key, Canvas testGridToAddDocumentsTo = null) {
            this.headers = new Dictionary<string, ApiProperty>();
            this.parameters = new Dictionary<string, ApiProperty>();
            this.authHeaders = new Dictionary<string, ApiProperty>();
            this.authParameters = new Dictionary<string, ApiProperty>();
            this.docController = docController;
            apiUrlTB = apiURLTB;
            if (!string.IsNullOrWhiteSpace(authURL)) {
                this.authURI = new Uri(authURL);
                this.secret = secret;
                this.key = key;
            } else {
                this.authURI = null;
                this.secret = null;
                this.key = null;
            }
            this.requestType = requestType;
            response = null;
            testGrid = testGridToAddDocumentsTo;
            client = new HttpClient(); // TODO: comment out for HttpClient abstraction
            responseAsDocuments = new List<DocumentController>();
        }

        // == GETTERS / SETTERS ==
        public List<DocumentController> ResponseAsDocuments { get { return this.responseAsDocuments; } set { this.responseAsDocuments = value; } }

        // == METHODS ==
        /// <summary>
        /// TODO: this is for debugging. It will be rewritten. Adds the first document in responseAsDocuments to the given
        /// test canvas.
        /// </summary>
        /// <param name="g"></param>
        public bool updateDocumentModelResults() {
            

            CourtesyDocuments.ApiDocumentModel.setResults(docController, responseAsDocuments);
            return true;
        }

        /// <summary>
        /// Sets the display to an existing source. Probably, you should use that.
        /// </summary>
        /// <param name="sdisplay"></param>
        public void setApiDisplay(ApiSourceDisplay sdisplay) {
            sdisplay.AddButtonEventHandler(clickHandler);
            display = sdisplay;
        }

        /// <summary>
        /// Just a wrapper to call makeRequest() when the query button is hit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clickHandler(object sender, RoutedEventArgs e) {
            makeRequest();
        }

        /// <summary>
        /// Updates the in-node representation of the parameters list based on
        /// user input into editable ApiConnectionProperties of the listview.
        /// </summary>
        private void updateParametersFromListView() {
            this.headers = new Dictionary<string, ApiProperty>();
            this.parameters = new Dictionary<string, ApiProperty>();
            this.authHeaders = new Dictionary<string, ApiProperty>();
            this.authParameters = new Dictionary<string, ApiProperty>();
            foreach (ApiProperty grid in display.PropertiesListView.Items) {
                if (grid.Type == ApiProperty.ApiPropertyType.AuthHeader)
                    authHeaders.Add(grid.Key, grid);
                if (grid.Type == ApiProperty.ApiPropertyType.AuthParameter)
                    authParameters.Add(grid.Key, grid);
                if (grid.Type == ApiProperty.ApiPropertyType.Header)
                    headers.Add(grid.Key, grid);
                if (grid.Type == ApiProperty.ApiPropertyType.Parameter)
                    parameters.Add(grid.Key, grid);
            }
        }

        /// <summary>
        /// For simplification purposes. Converts a dictionary of strings to ApiConnection
        /// Properties to a dictionary of string keys to string values by taking the value
        /// property of each ApiProperty object.
        /// </summary>
        /// <param name="dictionary">dictionary to convert</param>
        /// <returns>The "dictionary" field as a string to string dictionary where each value
        /// is the ApiProperty's Value property.</returns>
        private Dictionary<string, string> apiPropertyDictionaryToStringDictionary(Dictionary<string, ApiProperty> dictionary) {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            foreach (KeyValuePair<string, ApiProperty> entry in dictionary) {
                ret.Add(entry.Key, entry.Value.Value);
            }

            return ret;
        }

        /// <summary>
        /// Checks if all properties user can fill in are validly formatted (all required
        /// fields are filled in.)
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns>Returns true if all required fields are filled in. False otherwise.</returns>
        private bool requiredPropertiesValid(Dictionary<string, ApiProperty> dictionary) {
            foreach (KeyValuePair<string, ApiProperty> entry in dictionary) {
                if (entry.Value.isInvalid())
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Make request queries the API with both editable properties that simple
        /// users change and stored properties set by the superuser on instatiation.
        /// Messages are received as HttpResponseMessages which can then be parsed 
        /// into JSON, XML, or any other desired format.
        /// 
        /// Fails for invalid property adds. Assumes that provided API URI does not 
        /// already contain the '?' character at the end of the URL for GET requests.
        /// </summary>
        public async virtual void makeRequest() {
            // load in parameters from listViews
            updateParametersFromListView();
            var apiURI = new Uri(apiUrlTB.Text);
            
            // check that all required fields are filled
            if (!(requiredPropertiesValid(parameters) &&
            requiredPropertiesValid(headers))) {
                // TODO: show error message here?
                return;
            }

            // initialize request message and headers
            HttpRequestMessage message = new HttpRequestMessage(requestType, apiURI);

            // populate headers with user input
            foreach (KeyValuePair<string, ApiProperty> entry in headers) {
                // add custom header properties to request
                if (!message.Headers.UserAgent.TryParseAdd(entry.Key + "=" + entry.Value.Value))
                    return; // TODO: have some error happen here
            }

            // populate parameters with URL encoded user input
            HttpFormUrlEncodedContent messageBody
                = new HttpFormUrlEncodedContent(apiPropertyDictionaryToStringDictionary(parameters));

            // if get, we add parameters to the URI either in URL for GET or in body for POST requests
            if (requestType == HttpMethod.Get) {
                if (!String.IsNullOrWhiteSpace(messageBody.ToString()))
                    message.RequestUri = new Uri(apiURI.OriginalString + "?" + messageBody.ToString());
            } else {
                message.Content = messageBody;
            }


            // fetch authentication token if required
            if (!(string.IsNullOrWhiteSpace(apiURI.AbsolutePath) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(secret))) {
                string token;
                HttpRequestMessage tokenmsg = new HttpRequestMessage(HttpMethod.Post, authURI);
                var byteArray = Encoding.ASCII.GetBytes("my_client_id:my_client_secret");
                var header = new HttpCredentialsHeaderValue("Basic", Convert.ToBase64String(byteArray));

                // apply auth headers & parameters
                tokenmsg.Content = new HttpFormUrlEncodedContent(apiPropertyDictionaryToStringDictionary(authParameters));
                tokenmsg.Headers.Authorization = new HttpCredentialsHeaderValue("Basic", Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(
                    string.Format("{0}:{1}", key, secret))));
                foreach (KeyValuePair<string, ApiProperty> entry in authHeaders) {
                    if (!tokenmsg.Headers.UserAgent.TryParseAdd(entry.Key + "=" + entry.Value.Value))
                        return;
                }
                // fetch token
                response = await client.SendRequestAsync(tokenmsg);

                // parse resulting bearer token
                JObject resultObject = JObject.Parse(response.Content.ToString());
                token = (string)resultObject.GetValue("access_token");
                message.Headers.Authorization = new HttpCredentialsHeaderValue("Bearer", token);
            }

            // send message
            response = await client.SendRequestAsync(message);
            Debug.WriteLine("Content: " + response.Content.ToString());

            // generate and store response document by parsing HTTP output
            // first try to parse it as a list of objects
            responseAsDocuments = new List<DocumentController>();
            var apiDocType = new DocumentType(apiURI.Host.ToString().Split('.').First(), apiURI.Host.ToString().Split('.').First());
            try {
                /*
                var resultObjects = AllChildren(JObject.Parse(text.Text))
                    .First(c => c.Type == JTokenType.Array && c.Path.Contains("results"))
                    .Children<JObject>();

                int max = 100000, i = 0; // this limits the # of results returned
                // loop through all instantiated objects, making 
                foreach (JObject result in resultObjects) {
                    if (i > max)
                        break;
                    i++;
                    Dictionary<Key, FieldModel> toAdd = new Dictionary<Key, FieldModel>();
                    foreach (JProperty property in result.Properties()) {
                        //Debug.WriteLine(property.Name + ": " + property.Value);

                        // TODO: we can add special viewmodels for each of the JOBJECT types here
                        //       concerns: rabbit hole-ing?
                        toAdd.Add(new Key(apiURI.Host + property.Name, property.Name), new TextFieldModel(property.Value.ToString()));
                    }

                    DocumentController Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(toAdd, new DocumentType(apiURI.Host))).GetReturnedDocumentController();
                    responseAsDocuments.Add(Document); // /*apiURL.Host.ToString() DocumentType.DefaultType));
                }
                    
                */

                // parse JSON result. place first-tier documents in collection view. if there are none, simply
                // put a single document into the collection view
                DocumentController documentModel = JsonToDashUtil.Parse(response.Content.ToString());

                var layoutDocModel = new DocumentModel(new Dictionary<Key, FieldModel>(), CourtesyDocuments.CollectionBox.DocumentType);
                var layDocCtrl = new DocumentController(layoutDocModel);
                var dcfm = new DocumentCollectionFieldModel(new DocumentModel[] { });
                ContentController.AddModel(dcfm);
                ContentController.AddController(layDocCtrl);
                var cbox = new CollectionBox(dcfm).Document;
                var cfm = new DocumentModelFieldModel(cbox.DocumentModel);
                ContentController.AddModel(cfm);
                var cfmc = new DocumentFieldModelController(cfm);
                ContentController.AddController(cfmc);
                var widthField = new NumberFieldModel(200);
                ContentController.AddModel(widthField);
                var widthFieldCtrl = new NumberFieldModelController(widthField);
                ContentController.AddController(widthFieldCtrl);
                var heightField = new NumberFieldModel(200);
                ContentController.AddModel(heightField);
                var heightFieldCtrl = new NumberFieldModelController(heightField);
                ContentController.AddController(heightFieldCtrl);
                layDocCtrl.SetField(DashConstants.KeyStore.LayoutKey, cfmc, false);
                layDocCtrl.SetField(DashConstants.KeyStore.WidthFieldKey, widthFieldCtrl, false);
                layDocCtrl.SetField(DashConstants.KeyStore.HeightFieldKey, heightFieldCtrl, false);

                var dataFieldModel = new DocumentCollectionFieldModel(new List<DocumentModel>());
                ContentController.AddModel(dataFieldModel);
                var dataFieldModelController = new DocumentCollectionFieldModelController(dataFieldModel);
                ContentController.AddController(dataFieldModelController);
                layDocCtrl.SetField(DashConstants.KeyStore.DataKey, dataFieldModelController, true);

                // essentially, removes the outlying wrapper document JSONParser returns. this is a hack and
                // the parser should be reworked to auto do this or do it in a more user-friendly way
                foreach (var f in documentModel.EnumFields()) {
                    Debug.WriteLine(f.Value.GetType().ToString());
                    if (f.Value is DocumentFieldModelController)
                        ResponseAsDocuments.Add((f.Value as DocumentFieldModelController).Data);
                    if (f.Value is DocumentCollectionFieldModelController)
                        ResponseAsDocuments = (f.Value as DocumentCollectionFieldModelController).Documents;
                   
                } 

                if (ResponseAsDocuments.Count == 0)
                    ResponseAsDocuments.Add(documentModel);

                var newresponseDocs = new List<DocumentController>();
                foreach (var doc in ResponseAsDocuments)
                {
                    // make doc a delegate of the response document and make that 
                    var prototypeFieldModel = new DocumentModelFieldModel(layDocCtrl.DocumentModel);
                    ContentController.AddModel(prototypeFieldModel);
                    var prototypeFieldController = new DocumentFieldModelController(prototypeFieldModel);
                    ContentController.AddController(prototypeFieldController);
                    doc.SetField(DashConstants.KeyStore.PrototypeKey, prototypeFieldController, true);

                    // add the delegate to our delegates field
                    var currentDelegates = layDocCtrl.GetDelegates();
                    currentDelegates.GetDocuments().Add(doc);
                    CourtesyDocument.SetLayoutForDocument(doc, layDocCtrl.DocumentModel);

                    newresponseDocs.Add(doc);
                }

                ResponseAsDocuments = newresponseDocs;


                // at this point resultAsDocuments contains a list of all JSON results formatted
                // as documents! They all have the same type, indexed by api URL you could store 
                // this in a listview node or something wohoo
                //
                // TODO: generate unique identifiers for each ApiSource s.t. if two ApiSources had
                // the same URL, the DocumentModel can still distinguish them

                // then try and parse it as a single object
            } catch (InvalidOperationException e) {
                JObject result = JObject.Parse(response.Content.ToString());
                Dictionary<Key, FieldModel> toAdd = new Dictionary<Key, FieldModel>();
                foreach (JProperty property in result.Properties()) {
                    toAdd.Add(new Key(apiURI.Host + property.Name, property.Name), new TextFieldModel(property.Value.ToString()));
                }

                // at this point, resultAsDocument is a new document
                //
                // TODO: unique identifiers as above
                DocumentController Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(toAdd, new DocumentType(apiURI.Host))).GetReturnedDocumentController();
                responseAsDocuments.Add(Document); // /*apiURL.Host.ToString()*/ DocumentType.DefaultType));
            }

            // add document to children
            updateDocumentModelResults();
        }

        // recursively yield all children of json
        private static IEnumerable<JToken> AllChildren(JToken json) {
            foreach (var c in json.Children()) {
                yield return c;
                foreach (var cc in AllChildren(c)) {
                    yield return cc;
                }
            }
        }
    }
}
