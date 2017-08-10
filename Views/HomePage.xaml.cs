using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using DashShared;
using Flurl;
using Flurl.Http;
using Flurl.Http.Content;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Visibility = Windows.UI.Xaml.Visibility;
using static Dash.NoteDocuments;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Dash
{
    /// <summary>
    ///     Zoomable pannable canvas. Has an overlay canvas unaffected by pan / zoom.
    /// </summary>
    public sealed partial class HomePage : Page
    {

        public HomePage()
        {
            InitializeComponent();

            RESTClient.Instance.Documents.GetDocumentByType(DashConstants.TypeStore.MainDocumentType);

            // set the main view's datacontext to be the collection
            MainDocView.DataContext = new DocumentViewModel(MainDocument);

            // set the main view's width and height to avoid NaN errors
            MainDocView.Width = MyGrid.ActualWidth;
            MainDocView.Height = MyGrid.ActualHeight;
        }

        private void MyGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MainDocView.Width = e.NewSize.Width;
            MainDocView.Height = e.NewSize.Height;
        }
    }
}
       