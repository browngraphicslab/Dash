using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Dash.Models.OperatorModels.Set;
using DashShared;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Dash
{
    /// <summary>
    /// Zoomable pannable canvas. Has an overlay canvas unaffected by pan / zoom. 
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public static MainPage Instance;

        public MainPage()
        {
            this.InitializeComponent();

            // adds items from the overlay canvas onto the freeform canvas
            xOverlayCanvas.OnAddDocumentsTapped += AddDocuments;
            xOverlayCanvas.OnAddCollectionTapped += AddCollection;
            xOverlayCanvas.OnAddAPICreatorTapped += AddApiCreator;
            xOverlayCanvas.OnAddImageTapped += AddImage;
            xOverlayCanvas.OnAddShapeTapped += AddShape;
            xOverlayCanvas.OnOperatorAdd += OnOperatorAdd;
            xOverlayCanvas.OnToggleEditMode += OnToggleEditMode;

            // create the collection document model using a request
            var collectionDocumentController = new CourtesyDocuments.GenericCollection(new DocumentCollectionFieldModel(new List<DocumentModel>())).Document;
            // set the main view's datacontext to be the collection
            MainDocView.DataContext = new DocumentViewModel(collectionDocumentController)
            {
                IsDetailedUserInterfaceVisible = false,
                IsMoveable = false
            };

            // set the main view's width and height to avoid NaN errors
            MainDocView.Width = MyGrid.ActualWidth;
            MainDocView.Height = MyGrid.ActualHeight;
            
            // Set the instance to be itself, there should only ever be one MainView
            Debug.Assert(Instance == null, "If the main view isn't null then it's been instantiated multiple times and setting the instance is a problem");
            Instance = this;

            //TODO this seriously slows down the document 
            var jsonDoc = JsonToDashUtil.RunTests();
            DisplayDocument(jsonDoc);
        }

        private void OnToggleEditMode(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            //xFreeformView.ToggleEditMode();
        }


        private void OnOperatorAdd(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            //Create Operator document
            var opModel =
                OperatorDocumentModel.CreateOperatorDocumentModel(new DivideOperatorFieldModelController(new OperatorFieldModel("Divide")));
            DocumentView view = new DocumentView
            {
                Width = 200,
                Height = 200
            };
            DocumentViewModel opvm = new DocumentViewModel(opModel);
            //OperatorDocumentViewModel opvm = new OperatorDocumentViewModel(opModel);
            view.DataContext = opvm;

            DisplayDocument(opModel);


            //xFreeformView.AddOperatorView(opvm, view, 50, 50);

            //// add union operator for testing 
            DocumentController intersectOpModel =
                OperatorDocumentModel.CreateOperatorDocumentModel(new IntersectionOperatorModelController(new OperatorFieldModel("Intersection")));
            DocumentView intersectView = new DocumentView
            {
                Width = 200,
                Height = 200
            };
            DocumentViewModel intersectOpvm = new DocumentViewModel(intersectOpModel);
            intersectView.DataContext = intersectOpvm;
            DisplayDocument(intersectOpModel);

            DocumentController unionOpModel =
                OperatorDocumentModel.CreateOperatorDocumentModel(new UnionOperatorFieldModelController(new OperatorFieldModel("Union")));
            DocumentView unionView = new DocumentView
            {
                Width = 200,
                Height = 200
            };
            DocumentViewModel unionOpvm = new DocumentViewModel(unionOpModel);
            unionView.DataContext = unionOpvm;
            DisplayDocument(unionOpModel);

            // add image url -> image operator for testing
            DocumentController imgOpModel =
                OperatorDocumentModel.CreateOperatorDocumentModel(new ImageOperatorFieldModelController(new OperatorFieldModel("ImageToUri")));
            DocumentView imgOpView = new DocumentView
            {
                Width = 200,
                Height = 200
            };
            DocumentViewModel imgOpvm = new DocumentViewModel(imgOpModel);
            imgOpView.DataContext = imgOpvm;
            DisplayDocument(imgOpModel);
        }

        private async void AddShape(object sender, TappedRoutedEventArgs e)
        {
            var shapeModel = new ShapeModel
            {
                Width = 300,
                Height = 300,
                X = 300,
                Y = 300,
                Id = $"{Guid.NewGuid()}"
            };

            var shapeEndpoint = App.Instance.Container.GetRequiredService<ShapeEndpoint>();
            var result = await shapeEndpoint.CreateNewShape(shapeModel);
            if (result.IsSuccess)
            {
                shapeModel = result.Content;
            }
            else
            {
                Debug.WriteLine(result.ErrorMessage);
                return;
            }

            throw new NotImplementedException();
            //var shapeController = new ShapeController(shapeModel);
            //throw new NotImplementedException("The shape controller has not been updated to work with controllers");
            ////ContentController.AddShapeController(shapeController);

            //var shapeVM = new ShapeViewModel(shapeController);
            //var shapeView = new ShapeView(shapeVM);


            //  xFreeformView.Canvas.Children.Add(shapeView);
        }


        public DocumentController MainDocument => (MainDocView.DataContext as DocumentViewModel)?.DocumentController;

        /// <summary>
        /// Adds new documents to the MainView document. New documents are added as children of the Main document.
        /// </summary>
        /// <param name="docModel"></param>
        /// <param name="where"></param>
        public void DisplayDocument(DocumentController docModel, Point? where = null)
        {
            var children = MainDocument.GetField(DashConstants.KeyStore.DataKey) as DocumentCollectionFieldModelController;
            if (children != null)
            {
                children.AddDocument(docModel);

                //if (where.HasValue)
                //{
                //    docModel.SetField(DocumentModel.GetFieldKeyByName("X"), new NumberFieldModel(((Point)where).X), false);
                //    docModel.SetField(DocumentModel.GetFieldKeyByName("Y"), new NumberFieldModel(((Point)where).Y), false);
                //}
            }
        }

        private void AddCollection(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            var twoImages = new CourtesyDocuments.TwoImages(false).Document;
            var twoImages2 = new CourtesyDocuments.TwoImages(false).Document;
            var numbers = new CourtesyDocuments.Numbers().Document;

            Dictionary<Key, FieldModel> fields = new Dictionary<Key, FieldModel>
            {
                {DocumentCollectionFieldModelController.CollectionKey, new DocumentCollectionFieldModel(new DocumentModel[] {twoImages.DocumentModel, twoImages2.DocumentModel, numbers.DocumentModel}) }
            };

            var col = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, new DocumentType("collection", "collection"))).GetReturnedDocumentController();
            var layoutDoc = new CourtesyDocuments.GenericCollection(new ReferenceFieldModel(col.GetId(), DocumentCollectionFieldModelController.CollectionKey)).Document;
            var documentFieldModel = new DocumentModelFieldModel(layoutDoc.DocumentModel);
            var layoutController = new DocumentFieldModelController(documentFieldModel);
            ContentController.AddModel(documentFieldModel);
            ContentController.AddController(layoutController);
            col.SetField(DashConstants.KeyStore.LayoutKey, layoutController, true);
            DisplayDocument(col);

            AddAnotherLol();
            /*
            Dictionary<Key, FieldModel> fields2 = new Dictionary<Key, FieldModel>
            {
                {DocumentCollectionFieldModelController.CollectionKey, new DocumentCollectionFieldModel(new DocumentModel[] {new Numbers().Document.DocumentModel}) }
            };

            var col2 = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields2, new DocumentType("collection", "collection"))).GetReturnedDocumentController();
            var layoutDoc2 = new GenericCollection(new ReferenceFieldModel(col2.GetId(), DocumentCollectionFieldModelController.CollectionKey)).Document;
            var documentFieldModel2 = new DocumentModelFieldModel(layoutDoc2.DocumentModel);
            var layoutController2 = new DocumentFieldModelController(documentFieldModel2);
            ContentController.AddModel(documentFieldModel2);
            ContentController.AddController(layoutController2);
            col2.SetField(DashConstants.KeyStore.LayoutKey, layoutController2, true);
            DisplayDocument(col2);
            */

        }

        private void AddApiCreator(object sender, TappedRoutedEventArgs tappedRoutedEventArgs) {
            var a = new CourtesyDocuments.ApiDocumentModel().Document;
            DisplayDocument(a);
        }

        private void AddAnotherLol()
        {
            // collection no.2
            var numbers = new CourtesyDocuments.Numbers().Document;
            var twoImages2 = new CourtesyDocuments.TwoImages(false).Document;

            Dictionary<Key, FieldModel> fields = new Dictionary<Key, FieldModel>
            {
                {DocumentCollectionFieldModelController.CollectionKey, new DocumentCollectionFieldModel(new DocumentModel[] { twoImages2.DocumentModel, numbers.DocumentModel}) }
            };

            var col = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, new DocumentType("collection", "collection"))).GetReturnedDocumentController();
            var layoutDoc = new CourtesyDocuments.GenericCollection(new ReferenceFieldModel(col.GetId(), DocumentCollectionFieldModelController.CollectionKey)).Document;
            var documentFieldModel = new DocumentModelFieldModel(layoutDoc.DocumentModel);
            var layoutController = new DocumentFieldModelController(documentFieldModel);
            ContentController.AddModel(documentFieldModel);
            ContentController.AddController(layoutController);
            col.SetField(DashConstants.KeyStore.LayoutKey, layoutController, true);
            DisplayDocument(col);
        }
        

        private void AddImage(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            throw new NotImplementedException();
            // xFreeformView.Canvas.Children.Add(new Sources.FilePicker.FilePickerDisplay());
            // xFreeformView.Canvas.Children.Add(new Sources.FilePicker.PDFFilePicker());
        }
        
        private void AddDocuments(object sender, TappedRoutedEventArgs e)
        {
            DisplayDocument(new CourtesyDocuments.PostitNote().Document);
            DisplayDocument(new CourtesyDocuments.TwoImages(true).Document);
            DisplayDocument(new CourtesyDocuments.Numbers().Document);
            //DisplayDocument(new CourtesyDocuments.NestedDocExample(true).Document);
            //DisplayDocument(new CourtesyDocuments.NestedDocExample(false).Document);
        }


        private void MyGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var child = xViewBox.Child as FrameworkElement;
            if (child != null)
            {
                child.Width = e.NewSize.Width;
                child.Height = e.NewSize.Height;
            }
        }

        // FILE DRAG AND DROP

        /// <summary>
        /// Handles drop events onto the canvas, usually by creating a copy document of the original and
        /// placing it into the canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">drag event arguments</param>
        private async void XCanvas_Drop(object sender, DragEventArgs e) {
            Image dragged = new Image();
            string url = "";
            
            // load items dragged from solution explorer
            if (e.DataView.Contains(StandardDataFormats.StorageItems)) {
                var items = await e.DataView.GetStorageItemsAsync();

                if (items.Any()) {
                    var storageFile = items[0] as StorageFile;
                    var contentType = storageFile.ContentType;

                    StorageFolder folder = ApplicationData.Current.LocalFolder;

                    // parse images dropped in
                    if (contentType == "image/jpg" || contentType == "image/png" || contentType == "image/jpeg") {
                        StorageFile newFile = await storageFile.CopyAsync(folder, storageFile.Name, NameCollisionOption.GenerateUniqueName);
                        url = newFile.Path;
                        BitmapImage bitmapImg = new BitmapImage();

                        bitmapImg.SetSource(await storageFile.OpenAsync(FileAccessMode.Read));
                        dragged.Source = bitmapImg;
                    }

                    // parse text files dropped in
                    if (contentType == "text/plain") {
                        // TODO: TEXT FILES
                        return;
                    }
                }
            }

            if (e.DataView.Properties["image"] != null)
                dragged = e.DataView.Properties["image"] as Image; // fetches stored drag object

            // make document
            // generate single-image document model
            ImageFieldModel m = new ImageFieldModel(new Uri(url));
            Dictionary<Key, FieldModel> fields = new Dictionary<Key, FieldModel> {
                [new Key("DRAGIMGF-1E74-4577-8ACC-0685111E451C", "image")] = m
            };

            var col = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, new DocumentType("dragimage", "dragimage"))).GetReturnedDocumentController();
            DisplayDocument(col);
        }

        private void XCanvas_DragOver_1(object sender, DragEventArgs e)
        {
            //e.AcceptedOperation = DataPackageOperation.Copy;
        }
    }
}
