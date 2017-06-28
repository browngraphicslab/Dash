using Dash.Sources.Api.XAML_Elements;
using System;
using System.Collections.Generic;
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
        private ManipulationControls manipulator;

        public ApiCreatorDisplay() {
            this.InitializeComponent();

           // manipulator = new ManipulationControls(this);
        }

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

            // convert listviews to dictionaries
            Dictionary<string, ApiProperty> headers, parameters, authHeaders, authParameters;
            headers = new Dictionary<string, ApiProperty>();
            parameters = new Dictionary<string, ApiProperty>();
            authHeaders = new Dictionary<string, ApiProperty>();
            authParameters = new Dictionary<string, ApiProperty>();

            // auth params to dictionary
            foreach (ApiCreatorProperty p in xAuthControl.ParameterListView.Items) {
                if (!(string.IsNullOrWhiteSpace(p.PropertyName)))
                    authParameters.Add(p.PropertyName, new ApiProperty(p.PropertyName, p.PropertyValue, true, p.Required, p.ToDisplay, true));
            }

            // auth headers to dictionary
            foreach (ApiCreatorProperty p in xAuthControl.HeaderListView.Items) {
                if (!(string.IsNullOrWhiteSpace(p.PropertyName)))
                    authHeaders.Add(p.PropertyName, new ApiProperty(p.PropertyName, p.PropertyValue, true, p.Required, p.ToDisplay, true));
            }

            // headers to dictionary
            foreach (ApiCreatorProperty p in xHeaderControl.ItemListView.Items) {
                parameters.Add(p.PropertyName, new ApiProperty(p.PropertyName, p.PropertyValue, false, p.Required, p.ToDisplay));
            }

            // params to dictionary
            foreach (ApiCreatorProperty p in xParameterControl.ItemListView.Items) {
                if (!(string.IsNullOrWhiteSpace(p.PropertyName)))
                    parameters.Add(p.PropertyName, new ApiProperty(p.PropertyName, p.PropertyValue,
                        true, p.Required, p.ToDisplay));
            }

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

            // instantiate new APISource
            ApiSource newApi = new ApiSource(requestType, xApiURLTB.Text, headers, parameters,
                authParameters, authHeaders, xAuthControl.AuthURL, xAuthControl.Secret,
                xAuthControl.Key);
            MainPage.Instance.DisplayDocument(new ApiSourceDoc(newApi.createAPISourceDisplay()).Document);

        }
    }
}
