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
        private DocumentController _mainDocument;
        private DocumentView _mainDocView;

        public HomePage()
        {
            InitializeComponent();

            var mainDocuments = new List<DocumentController>();

            Task.Run(async () =>
            {
                await RESTClient.Instance.Documents.GetDocumentByType(DashConstants.TypeStore.MainDocumentType, docModelDtos =>
                {
                    mainDocuments.AddRange(docModelDtos.Select(dmDto => DocumentController.CreateFromServer(dmDto)));
                }, exception =>
                {
                    Debug.WriteLine(exception);
                });

            }).ContinueWith(task =>
            {

                var fields = new Dictionary<KeyController, FieldModelController>
                {
                    [DocumentCollectionFieldModelController.CollectionKey] = new DocumentCollectionFieldModelController(mainDocuments)
                };
                _mainDocument = new DocumentController(fields, DashConstants.TypeStore.HomePageType);

                var collectionDocumentController =
                    new CollectionBox(new ReferenceFieldModelController(_mainDocument.GetId(), DocumentCollectionFieldModelController.CollectionKey)).Document;

                _mainDocument.SetActiveLayout(collectionDocumentController, forceMask: true, addToLayoutList: true);

                _mainDocView = new DocumentView(new DocumentViewModel(_mainDocument));

                // set the main view's width and height to avoid NaN errors
                _mainDocView.Width = xOuterGrid.ActualWidth;
                _mainDocView.Height = xOuterGrid.ActualHeight;

                Grid.SetRow(_mainDocView, 1);

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void XOutterGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_mainDocView != null)
            {
                _mainDocView.Width = e.NewSize.Width;
                _mainDocView.Height = e.NewSize.Height;
            }
        }

        private void OnAddNewWorkspaceTapped(object sender, TappedRoutedEventArgs e)
        {
            var newDocument = CreateNewWorkspace();
            var collectionField =
                _mainDocument.GetField(DocumentCollectionFieldModelController.CollectionKey) as
                    DocumentCollectionFieldModelController;

            Debug.Assert(collectionField != null, "collection field should never be null if we created it in the constructor correctly");
            collectionField.AddDocument(newDocument);
        }

        private DocumentController CreateNewWorkspace()
        {
            // create the collection document model using a request
            var fields = new Dictionary<KeyController, FieldModelController>();
            fields[DocumentCollectionFieldModelController.CollectionKey] = new DocumentCollectionFieldModelController(new List<DocumentController>());
            var newDocument = new DocumentController(fields, DashConstants.TypeStore.MainDocumentType);
            var collectionDocumentController =
                new CollectionBox(new ReferenceFieldModelController(newDocument.GetId(), DocumentCollectionFieldModelController.CollectionKey)).Document;
            newDocument.SetActiveLayout(collectionDocumentController, forceMask: true, addToLayoutList: true);

            return newDocument;
        }
    }
}
       