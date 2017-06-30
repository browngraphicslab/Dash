using Dash.Sources.Api.XAML_Elements;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static Dash.MainPage;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash.Sources.Api {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ApiCreatorDisplay : UserControl {
        public DocumentController DocModel;
        public ApiSourceDisplay SourceDisplay;
        private ApiSource Source;

        // == CONSTRUCTORS ==
        public ApiCreatorDisplay(DocumentController docModel, ApiSourceDisplay display) {
            this.InitializeComponent();

            // todo: probably put collectionkey, docmodel, and display in a separate class for readability
            this.DocModel = docModel;
            xHeaderControl.DocModel = docModel;
            xParameterControl.DocModel = docModel;
            xAuthControl.HeaderControl.DocModel = docModel;
            xAuthControl.ParameterControl.DocModel = docModel;
            
            xHeaderControl.SourceDisplay = display;
            xParameterControl.SourceDisplay = display;
            xAuthControl.HeaderControl.SourceDisplay = display;
            xAuthControl.ParameterControl.SourceDisplay = display;

            xHeaderControl.parameterCollectionKey = CourtesyDocuments.ApiDocumentModel.HeadersKey;
            xParameterControl.parameterCollectionKey = CourtesyDocuments.ApiDocumentModel.ParametersKey;
            xAuthControl.ParameterControl.parameterCollectionKey = CourtesyDocuments.ApiDocumentModel.AuthParameters;
            xAuthControl.HeaderControl.parameterCollectionKey = CourtesyDocuments.ApiDocumentModel.AuthHeaders;
            SourceDisplay = display;

            updateSource();
        }
        public ApiCreatorDisplay() {
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
        private void createAPINodeTemplate(object sender, RoutedEventArgs e) {
        }

        private void updateSource() {

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
            if (!(Uri.TryCreate(xApiURLTB.Text, UriKind.RelativeOrAbsolute, out outUri))) {
                //debugger.Text = "Invalid API URL";
                return;
            }

            if (String.IsNullOrEmpty(xApiURLTB.Text))
                xApiURLTB.Text = "https://itunes.apple.com/search";

            // instantiate new APISource
            ApiSource newApi = new ApiSource(DocModel, requestType, xApiURLTB.Text, xAuthControl.AuthURL, xAuthControl.Secret,
                xAuthControl.Key);
            newApi.setApiDisplay(SourceDisplay);

        }

        private void xHeaderControl_Loaded(object sender, RoutedEventArgs e) {

        }

        private void xApiURLTB_TextChanged(object sender, TextChangedEventArgs e) {
            Debug.WriteLine((DocModel.Fields[CourtesyDocuments.ApiDocumentModel.UrlKey] as TextFieldModelController).Data);
        }
    }
}
