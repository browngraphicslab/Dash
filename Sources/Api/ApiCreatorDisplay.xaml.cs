using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using RadialMenuControl.UserControl;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ApiCreatorDisplay : UserControl
    {
        public delegate void MakeApiHandler();

        public event MakeApiHandler MakeApi;

        public DocumentController Document;
        public ApiSourceDisplay SourceDisplay;
        private ApiSource Source;

        private DocumentController _operatorDocument;
        private ApiOperatorController _operatorController;

        // == CONSTRUCTORS ==
        public ApiCreatorDisplay(DocumentController document, ApiSourceDisplay display)
        {
            this.InitializeComponent();

            // todo: probably put collectionkey, docmodel, and display in a separate class for readability
            this.Document = document;
            xHeaderControl.Document = document;
            xParameterControl.Document = document;
            xAuthControl.HeaderControl.Document = document;
            xAuthControl.ParameterControl.Document = document;

            xHeaderControl.SourceDisplay = display;
            xParameterControl.SourceDisplay = display;
            xAuthControl.HeaderControl.SourceDisplay = display;
            xAuthControl.ParameterControl.SourceDisplay = display;

            xHeaderControl.parameterCollectionKey = ApiDocumentModel.HeadersKey;
            xParameterControl.parameterCollectionKey = ApiDocumentModel.ParametersKey;
            xAuthControl.ParameterControl.parameterCollectionKey = ApiDocumentModel.AuthParametersKey;
            xAuthControl.HeaderControl.parameterCollectionKey = ApiDocumentModel.AuthHeadersKey;
            SourceDisplay = display;

            updateSource();
        }
        public ApiCreatorDisplay()
        {
            this.InitializeComponent();

            // manipulator = new ManipulationControls(this);
        }

        // == GETTERS / SETTERS ==
        public TextBox UrlTB { get { return xApiURLTB; } set { this.xApiURLTB = value; } }
        public ComboBox RequestMethodCB { get { return requestTypePicker; } set { requestTypePicker = value; } }
        public ApiCreatorAuthenticationDisplay AuthDisplay { get { return xAuthControl; } set { xAuthControl = value; } }


        // == API FUNCTIONALITY ==

        /// <summary>
        /// Called on 'Create Api' button click from the ApiSourceCreator.
        /// Converts header and parameter listView values into dictionaries,
        /// parses other ApiSourceCreator form input into values, then generates
        /// a new ApiSource and adds that to our canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void createAPINodeTemplate(object sender, RoutedEventArgs e)
        {
            var method = (requestTypePicker.SelectedItem as ComboBoxItem).Content.ToString();
            var fields = new Dictionary<KeyControllerBase, FieldControllerBase>
            {
                {ApiOperatorController.MethodKey, new TextFieldModelController(method) },
                {ApiOperatorController.UrlKey, new TextFieldModelController(xApiURLTB.Text) }
            };

            if (xAuthControl.AuthURL != "")
            {
                fields[ApiOperatorController.AuthUrlKey] = new TextFieldModelController(xAuthControl.AuthURL);
            }
            if (xAuthControl.AuthMethod != "")
            {
                fields[ApiOperatorController.AuthMethodKey] = new TextFieldModelController(xAuthControl.AuthMethod);
            }
            if (xAuthControl.Key != "")
            {
                fields[ApiOperatorController.AuthKeyKey] = new TextFieldModelController(xAuthControl.Key);
            }
            if (xAuthControl.Secret != "")
            {
                fields[ApiOperatorController.AuthSecretKey] = new TextFieldModelController(xAuthControl.Secret);
            }

            void BuildParams(Dictionary<KeyControllerBase, string> keys, Dictionary<KeyControllerBase, string> values, Dictionary<KeyControllerBase, FieldControllerBase> fieldDict)
            {
                foreach (var key in keys)
                {
                    string value = "";
                    values.TryGetValue(key.Key, out value);
                    fieldDict[key.Key] = new TextFieldModelController(key.Value + ":" + value);
                }
                foreach (var key in values)
                {
                    if (fieldDict.ContainsKey(key.Key))
                    {
                        continue;
                    }
                    string value = "";
                    keys.TryGetValue(key.Key, out value);
                    fieldDict[key.Key] = new TextFieldModelController(value + ":" + key.Value);
                }
            }

            BuildParams(xParameterControl.Keys, xParameterControl.Values, fields);
            BuildParams(xAuthControl.ParameterControl.Keys, xAuthControl.ParameterControl.Values, fields);
           
            _operatorDocument.SetFields(fields, true);

            MakeApi?.Invoke();
        }

        private void updateSource()
        {
            // convert listviews to dictionaries
            Dictionary<string, ApiProperty> headers, parameters, authHeaders, authParameters;
            headers = new Dictionary<string, ApiProperty>();
            parameters = new Dictionary<string, ApiProperty>();
            authHeaders = new Dictionary<string, ApiProperty>();
            authParameters = new Dictionary<string, ApiProperty>();

            // dropdown to Httprequest type
            Windows.Web.Http.HttpMethod requestType;
            if (requestTypePicker.SelectedIndex == 0)
                requestType = Windows.Web.Http.HttpMethod.Get;
            else
                requestType = Windows.Web.Http.HttpMethod.Post;

            // validate URI
            Uri outUri;
            if (!(Uri.TryCreate(xApiURLTB.Text, UriKind.RelativeOrAbsolute, out outUri)))
            {
                //debugger.Text = "Invalid API URL";
                return;
            }

            if (String.IsNullOrEmpty(xApiURLTB.Text))
                xApiURLTB.Text = "https://itunes.apple.com/search";

            // instantiate new APISource
            Source = new ApiSource(Document, requestType, xApiURLTB, xAuthControl.AuthURL, xAuthControl.Secret, xAuthControl.Key);
            Source.setApiDisplay(SourceDisplay);

        }

        private void ApiCreatorDisplay_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var reference = (args.NewValue as FieldReference);
            _operatorDocument = reference.GetDocumentController(null);
            _operatorController = _operatorDocument.GetField(reference.FieldKey) as ApiOperatorController;
        }
    }
}
