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
        private DocumentController _homePageDocument;
        private DocumentView _mainDocView;

        public HomePage()
        {
            InitializeComponent();
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
                _homePageDocument.GetField(DocumentCollectionFieldModelController.CollectionKey) as
                    DocumentCollectionFieldModelController;

            Debug.Assert(collectionField != null, "collection field should never be null if we created it in the constructor correctly");
            collectionField.AddDocument(newDocument);
        }

        private DocumentController CreateNewWorkspace()
        {
            // create the collection document model using a request
            var fields = new Dictionary<KeyController, FieldModelController>
            {
                [DocumentCollectionFieldModelController.CollectionKey] =
                new DocumentCollectionFieldModelController(new List<DocumentController>())
            };
            var model = new DocumentModel(fields.ToDictionary(kvp => kvp.Key.Model, kvp => kvp.Value.Model), DashConstants.TypeStore.MainDocumentType, "main-document-" + Guid.NewGuid());
            var newDocument = new DocumentController(model);
            var collectionDocumentController =
                new CollectionBox(new ReferenceFieldModelController(newDocument.GetId(), DocumentCollectionFieldModelController.CollectionKey)).Document;
            newDocument.SetActiveLayout(collectionDocumentController, forceMask: true, addToLayoutList: true);

            return newDocument;
        }   

        private void OnDeleteAllDocumentsTapped(object sender, TappedRoutedEventArgs e)
        {
            RESTClient.Instance.Documents.DeleteAllDocuments(() =>
            {
                
            }, exception =>
            {
                
            });
        }

        private void OnCreateOrGetHomePage(object sender, TappedRoutedEventArgs e)
        {
            DocumentViewModel homePageViewModel;

            async Task Success(IEnumerable<DocumentModel> homePageDocDtos)
            {
                var documentModelDto = homePageDocDtos.FirstOrDefault();
                if (documentModelDto != null)
                {
                   _homePageDocument = new DocumentController(documentModelDto);
                }
                else
                {
                    var fields = new Dictionary<KeyController, FieldModelController>
                    {
                        [DocumentCollectionFieldModelController.CollectionKey] = new DocumentCollectionFieldModelController()
                    };

                    var model = new DocumentModel(fields.ToDictionary(kvp => kvp.Key.Model, kvp => kvp.Value.Model), DashConstants.TypeStore.HomePageType, "home-document-" + Guid.NewGuid());

                    _homePageDocument = new DocumentController(model);
                    var collectionDocumentController =
                        new CollectionBox(new ReferenceFieldModelController(_homePageDocument.GetId(), DocumentCollectionFieldModelController.CollectionKey)).Document;

                    _homePageDocument.SetActiveLayout(collectionDocumentController, forceMask: true, addToLayoutList: true);
                }

                UITask.Run(() =>
                {
                    homePageViewModel = new DocumentViewModel(_homePageDocument);
                    _mainDocView = new DocumentView(homePageViewModel);

                    // set the main view's width and height to avoid NaN errors
                    _mainDocView.Width = xOuterGrid.ActualWidth;
                    _mainDocView.Height = xOuterGrid.ActualHeight;

                    Grid.SetRow(_mainDocView, 1);
                    xOuterGrid.Children.Add(_mainDocView);
                });

            }

            RESTClient.Instance.Documents.GetDocumentByType(DashConstants.TypeStore.HomePageType, Success, exception => throw exception);
        }

        private void OnPopulatedHomePagedTapped(object sender, TappedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
       