using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;

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
            ApiDocumentModel.setResults(docController, responseAsDocuments);
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
        public virtual async void makeRequest() {
            // load in parameters from listViews
            updateParametersFromListView();
            
            // check that all required fields are filled
            if (!(requiredPropertiesValid(parameters) &&
            requiredPropertiesValid(headers))) {
                // TODO: show error message here?
                return;
            }

            var strHeaders = apiPropertyDictionaryToStringDictionary(headers);
            var strParams = apiPropertyDictionaryToStringDictionary(parameters);
            var strAuthHeaders = apiPropertyDictionaryToStringDictionary(authHeaders);

            var trySetResponse = new Request(requestType, new Uri(apiUrlTB.Text)).SetHeaders(strHeaders)?
                .SetMessageBody(new HttpFormUrlEncodedContent(strParams))
                .SetAuthUri(authURI)
                .SetAuthHeaders(strAuthHeaders).TrySetResponse();
            if (trySetResponse != null)
            {
                await trySetResponse;
                ResponseAsDocuments = trySetResponse.Result?.GetResponseAsDocuments();
                if (ResponseAsDocuments == null) return;
                updateDocumentModelResults();
            }
            // add document to children
            
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
