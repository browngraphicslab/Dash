using Dash.Models;
using Dash.Sources.Api.XAML_Elements;
using DashShared;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace Dash.Sources.Api {
    /// <summary>
    /// Instantiations of this class represent a user-created API interface: an APISource.
    /// 
    /// TODO: Ask about abstracting HttpClient s.t. each NuSys Dashboard instance
    /// only has one HttpClient that makes all the requests.
    /// </summary>
    class ApiSource {
        // == MEMBERS ==
        private HttpMethod requestType; // POST, GET, etc.
        private Uri apiURI, authURI;
        private string secret, key;
        private Dictionary<string, ApiProperty> headers, parameters, authHeaders, authParameters;
        private HttpClient client;
        private TextBlock text;
        private HttpResponseMessage response;
        private List<DocumentModel> responseAsDocuments; // list of results formatted as documents
        private ApiSourceDisplay display;
        public TextBlock debugger;
        private Canvas testGrid;

        // == CONSTRUCTORS ==
        public ApiSource(HttpMethod requestType, string apiURL,
            Dictionary<string, ApiProperty> headerProperties, Dictionary<string, ApiProperty> parameterProperties,
                Dictionary<string, ApiProperty> authParameterProperties, Dictionary<string, ApiProperty> authHeaderProperties,
                string authURL, string secret, string key, Canvas testGridToAddDocumentsTo = null) {
            this.headers = headerProperties;
            this.parameters = parameterProperties;
            this.authHeaders = authHeaderProperties;
            this.authParameters = authParameterProperties;
            this.apiURI = new Uri(apiURL);
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
            text = new TextBlock();
            response = null;
            testGrid = testGridToAddDocumentsTo;
            client = new HttpClient(); // TODO: comment out for HttpClient abstraction
            responseAsDocuments = new List<DocumentModel>();
        }

        // == GETTERS / SETTERS ==
        public List<DocumentModel> ResponseAsDocuments { get { return this.responseAsDocuments; } set { this.responseAsDocuments = value; } }

        // == METHODS ==
        /// <summary>
        /// TODO: this is for debugging. It will be rewritten. Adds the first document in responseAsDocuments to the given
        /// test canvas.
        /// </summary>
        /// <param name="g"></param>
        public bool addResponseDocumentsToCanvas() {
            if (responseAsDocuments.Count == 0)
                return false;

            // add document to given canvas
            //DocumentModel testDocument = responseAsDocuments[0];

            ////set layout
            //testDocument.DocumentType = DocumentType.DefaultType;

            //// this part generates a defaultlayoutmodel that shows key/value string text pairs
            //DocumentViewModel testModel = new DocumentViewModel(testDocument);
            //testModel.SetLayoutModel(LayoutModel.DefaultLayoutModel(testDocument)); // TODO: simplify this in dvm constructor
            //DocumentView testView = new DocumentView();
            //testView.DataContext = testModel;
            //testGrid.Children.Add(testView);
            //LayoutModel.DefaultLayoutModel(testDocument);

            // put document results into collection model
            var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            var collection = docController.CreateDocumentAsync("collection"); //_example?
            collection.SetField(DocumentModel.GetFieldKeyByName("documents"), new DocumentCollectionFieldModel(responseAsDocuments));
            DocumentViewModel cm = new DocumentViewModel(collection);
            DocumentView v = new DocumentView(cm);
            testGrid.Children.Add(v);
            
            return true;
        }

        /// <summary>
        /// Adds a dictionary of key, value pair to a given list view, creating the key as a TextBlock
        /// and the value as a TextBox.
        /// </summary>
        /// <param name="view">ListView to add pair to</param>
        /// <param name="dictionary">dictionary to get pairs frm</param>
        void addToView(Dictionary<string, ApiProperty> dictionary, bool isParameter) {
            int lastRequiredPosition = 0;
            foreach (KeyValuePair<string, ApiProperty> entry in dictionary) {
                // only add editable header params for values that do not have defaults
                if (entry.Value.IsDisplayed) {
                    if (entry.Value.IsRequired) {
                        display.addToListView(entry.Value, lastRequiredPosition);
                        lastRequiredPosition++;
                    } else
                        display.addToListView(entry.Value);
                }
            }
        }

        /// <summary>
        /// Generates a physical Grid that can be displayed on canvas representing 
        /// the APINode template.
        /// 
        /// TODO: seperate layout for MVVM maybe?
        /// </summary>
        /// <returns>
        /// The generated grid object.
        /// </returns>
        public ApiSourceDisplay createAPISourceDisplay() {
            display = new ApiSourceDisplay();

            // add everyone to containing grid
            addToView(authHeaders, false);
            addToView(authParameters, false);
            addToView(headers, false);
            addToView(parameters, true);
            display.addButtonEventHandler(clickHandler);

            return display;
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
        /// Updates ApiSourceCreator to represent this ApiSource. The creator can
        /// then be used to edit the fields of this ApiSource.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editHandler(object sender, RoutedEventArgs e) {
            makeRequest();
        }

        /// <summary>
        /// Updates the in-node representation of the parameters list based on
        /// user input into editable ApiConnectionProperties of the listview.
        /// </summary>
        private void updateParametersFromListView() {
            foreach (ApiProperty grid in display.PropertiesListView.Items) {
                if (!grid.ForAuth) {
                    string key = grid.Key;
                    string val = grid.Value;
                    if (grid.IsParameter)
                        parameters[key].Value = val;
                    else
                        headers[key].Value = val;
                } else {
                    string key = grid.Key;
                    string val = grid.Value;
                    if (grid.IsParameter)
                        authParameters[key].Value = val;
                    else
                        authHeaders[key].Value = val;
                }
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

            // check that all required fields are filled
            if (!(requiredPropertiesValid(parameters) &&
            requiredPropertiesValid(headers))) {
                text.Text = "Please fill in all required fields (denoted by a *).";
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

            // if get, we add parameters to the URI
            if (requestType == HttpMethod.Get) {
                message.RequestUri = new Uri(apiURI.OriginalString + "?" + messageBody.ToString());
                Debug.WriteLine(apiURI.OriginalString + "?" + messageBody.ToString());
            } else {
                message.Content = messageBody;
            }


            // fetch authentication token if required
            string token;
        
            if (!(string.IsNullOrWhiteSpace(apiURI.AbsolutePath) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(secret))) {
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
                Debug.WriteLine(apiURI.OriginalString.ToString());
                // fetch token
                response = await client.SendRequestAsync(tokenmsg);

                // parse resulting bearer token
                JObject resultObject = JObject.Parse(response.Content.ToString());
                token = (string)resultObject.GetValue("access_token");
                message.Headers.Authorization = new HttpCredentialsHeaderValue("Bearer", token);
            }

            // send message
            response = await client.SendRequestAsync(message);
            text.Text = response.Content.ToString();
            Debug.WriteLine("Content: " + response.Content.ToString());

            // generate and store response document by parsing HTTP output
            // first try to parse it as a list of objects
            //
            // TODO: is it extra to loop into nested Objects in a JSON value and generate fields 
            // representing all of them? Ask how to have nested properties inside of documents
            //      -> document of documents? or create custom object type maybe?
            responseAsDocuments = new List<DocumentModel>();
            var apiDocType = new DocumentType(this.apiURI.Host.ToString().Split('.').First(), this.apiURI.Host.ToString().Split('.').First());
            try {
                var resultObjects = AllChildren(JObject.Parse(text.Text))
                    .First(c => c.Type == JTokenType.Array && c.Path.Contains("results"))
                    .Children<JObject>();

                // loop through all instantiated objects, making 
                foreach (JObject result in resultObjects) {
                    Dictionary<Key, FieldModel> toAdd = new Dictionary<Key, FieldModel>();
                    foreach (JProperty property in result.Properties()) {
                        //Debug.WriteLine(property.Name + ": " + property.Value);

                        // TODO: we can add special viewmodels for each of the JOBJECT types here
                        //       concerns: rabbit hole-ing?
                        toAdd.Add(new Key(apiURI.Host + property.Name, property.Name), new TextFieldModel(property.Value.ToString()));
                    }
                    responseAsDocuments.Add(new DocumentModel(toAdd, apiDocType)); // /*apiURL.Host.ToString()*/ DocumentType.DefaultType));
                }


                // at this point resultAsDocuments contains a list of all JSON results formatted
                // as documents! They all have the same type, indexed by api URL you could store 
                // this in a listview node or something wohoo
                //
                // TODO: generate unique identifiers for each ApiSource s.t. if two ApiSources had
                // the same URL, the DocumentModel can still distinguish them

                // then try and parse it as a single object
            } catch (InvalidOperationException e) {
                JObject result = JObject.Parse(text.Text);
                Dictionary<Key, FieldModel> resultAsDictionary = new Dictionary<Key, FieldModel>();
                foreach (JProperty property in result.Properties()) {
                    //Debug.WriteLine(property.Name + ": " + property.Value.Type);
                    resultAsDictionary.Add(new Key(apiURI.Host + property.Name, property.Name), new TextFieldModel(property.Value.ToString()));
                }

                // at this point, resultAsDocument is a new document
                //
                // TODO: unique identifiers as above
                responseAsDocuments.Add(new DocumentModel(resultAsDictionary, apiDocType)); // /*apiURL.Host.ToString()*/ DocumentType.DefaultType));
            }

            // add document to children
            addResponseDocumentsToCanvas();
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
