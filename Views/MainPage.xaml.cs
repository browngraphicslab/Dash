using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Models.OperatorModels.Set;
using Dash.Views;
using DashShared;
using Microsoft.Extensions.DependencyInjection;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Dash
{
    /// <summary>
    ///     Zoomable pannable canvas. Has an overlay canvas unaffected by pan / zoom.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Instance;


        public DocumentController MainDocument => (MainDocView.DataContext as DocumentViewModel)?.DocumentController;

        public MainPage()
        {
            InitializeComponent();

            // create the collection document model using a request
            var collectionDocumentController =
                new CourtesyDocuments.CollectionBox(
                    new DocumentCollectionFieldModelController(new List<DocumentController>())).Document;
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
            Debug.Assert(Instance == null,
                "If the main view isn't null then it's been instantiated multiple times and setting the instance is a problem");
            Instance = this;

            //TODO this seriously slows down the document 

            var jsonDoc = JsonToDashUtil.RunTests();
            DisplayDocument(jsonDoc);
            var radialMenu = new RadialMenuView(xCanvas);
        }

        public void AddOperator()
        {
            //Create Operator document
            var opModel =
                OperatorDocumentModel.CreateOperatorDocumentModel(
                    new DivideOperatorFieldModelController(new OperatorFieldModel("Divide")));
            var view = new DocumentView
            {
                Width = 200,
                Height = 200
            };
            var opvm = new DocumentViewModel(opModel);
            //OperatorDocumentViewModel opvm = new OperatorDocumentViewModel(opModel);
            view.DataContext = opvm;

            DisplayDocument(opModel);

            //// add union operator for testing 
            var intersectOpModel =
                OperatorDocumentModel.CreateOperatorDocumentModel(
                    new IntersectionOperatorModelController(new OperatorFieldModel("Intersection")));
            var intersectView = new DocumentView
            {
                Width = 200,
                Height = 200
            };
            var intersectOpvm = new DocumentViewModel(intersectOpModel);
            intersectView.DataContext = intersectOpvm;
            DisplayDocument(intersectOpModel);

            var unionOpModel =
                OperatorDocumentModel.CreateOperatorDocumentModel(
                    new UnionOperatorFieldModelController(new OperatorFieldModel("Union")));
            var unionView = new DocumentView
            {
                Width = 200,
                Height = 200
            };
            var unionOpvm = new DocumentViewModel(unionOpModel);
            unionView.DataContext = unionOpvm;
            DisplayDocument(unionOpModel);

            // add image url -> image operator for testing
            var imgOpModel =
                OperatorDocumentModel.CreateOperatorDocumentModel(
                    new ImageOperatorFieldModelController(new OperatorFieldModel("ImageToUri")));
            var imgOpView = new DocumentView
            {
                Width = 200,
                Height = 200
            };
            var imgOpvm = new DocumentViewModel(imgOpModel);
            imgOpView.DataContext = imgOpvm;
            DisplayDocument(imgOpModel);
        }

        /// <summary>
        ///     Adds new documents to the MainView document. New documents are added as children of the Main document.
        /// </summary>
        /// <param name="docModel"></param>
        /// <param name="where"></param>
        public void DisplayDocument(DocumentController docModel, Point? where = null)
        {
            var children =
                MainDocument.GetDereferencedField(DashConstants.KeyStore.DataKey,
                    new List<DocumentController>()) as DocumentCollectionFieldModelController;
            if (children != null)
                children.AddDocument(docModel);
        }

        public void AddCollection(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            var twoImages = new CourtesyDocuments.TwoImages(false).Document;
            var twoImages2 = new CourtesyDocuments.TwoImages(false).Document;
            var numbers = new CourtesyDocuments.Numbers().Document;

            var fields = new Dictionary<Key, FieldModelController>
            {
                {
                    DocumentCollectionFieldModelController.CollectionKey,
                    new DocumentCollectionFieldModelController(new[] {numbers})
                }
            };

            var col = new DocumentController(fields, new DocumentType("collection", "collection"));
            var layoutDoc =
                new CourtesyDocuments.CollectionBox(new ReferenceFieldModelController(col.GetId(),
                    DocumentCollectionFieldModelController.CollectionKey)).Document;
            var layoutController = new DocumentFieldModelController(layoutDoc);
            col.SetField(DashConstants.KeyStore.LayoutKey, layoutController, true);
            DisplayDocument(col);

            AddAnotherLol();
        }

        public void AddApiCreator(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            var a = new CourtesyDocuments.ApiDocumentModel().Document;
            DisplayDocument(a);
        }

        private void AddAnotherLol()
        {
            var numbers = new CourtesyDocuments.Numbers().Document;
            var twoImages2 = new CourtesyDocuments.TwoImages(false).Document;

            var fields = new Dictionary<Key, FieldModelController>
            {
                [DocumentCollectionFieldModelController.CollectionKey] =
                new DocumentCollectionFieldModelController(new[]
                    {numbers, twoImages2})
            };

            var col = new DocumentController(fields, new DocumentType("collection", "collection"));
            var layoutDoc =
                new CourtesyDocuments.CollectionBox(new ReferenceFieldModelController(col.GetId(),
                    DocumentCollectionFieldModelController.CollectionKey)).Document;
            var layoutController = new DocumentFieldModelController(layoutDoc);
            col.SetField(DashConstants.KeyStore.LayoutKey, layoutController, true);
            DisplayDocument(col);
        }


        private void AddImage(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            throw new NotImplementedException();
            // xFreeformView.Canvas.Children.Add(new Sources.FilePicker.FilePickerDisplay());
            // xFreeformView.Canvas.Children.Add(new Sources.FilePicker.PDFFilePicker());
        }

        public void AddDocuments(object sender, TappedRoutedEventArgs e)
        {
            DisplayDocument(new CourtesyDocuments.PostitNote().Document);
            DisplayDocument(new CourtesyDocuments.TwoImages(false).Document);
            DisplayDocument(new CourtesyDocuments.Numbers().Document);
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

        //// FILE DRAG AND DROP

        /// <summary>
        ///     Handles drop events onto the canvas, usually by creating a copy document of the original and
        ///     placing it into the canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">drag event arguments</param>
        private async void XCanvas_Drop(object sender, DragEventArgs e)
        {
            var dragged = new Image();
            var url = "";

            // load items dragged from solution explorer
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();

                if (items.Any())
                {
                    var storageFile = items[0] as StorageFile;
                    var contentType = storageFile.ContentType;

                    var folder = ApplicationData.Current.LocalFolder;

                    // parse images dropped in
                    if (contentType == "image/jpg" || contentType == "image/png" || contentType == "image/jpeg")
                    {
                        var newFile = await storageFile.CopyAsync(folder, storageFile.Name,
                            NameCollisionOption.GenerateUniqueName);
                        url = newFile.Path;
                        var bitmapImg = new BitmapImage();

                        bitmapImg.SetSource(await storageFile.OpenAsync(FileAccessMode.Read));
                        dragged.Source = bitmapImg;
                    }

                    // parse text files dropped in
                    if (contentType == "text/plain")
                        return;
                }
            }

            if (e.DataView.Properties["image"] != null)
                dragged = e.DataView.Properties["image"] as Image; // fetches stored drag object

            // make document
            // generate single-image document model
            var m = new ImageFieldModelController(new Uri(url));
            var fields = new Dictionary<Key, FieldModelController>
            {
                [new Key("DRAGIMGF-1E74-4577-8ACC-0685111E451C", "image")] = m
            };

            var col = new DocumentController(fields, new DocumentType("dragimage", "dragimage"));
            DisplayDocument(col);
        }

        public void xCanvas_DragOver(object sender, DragEventArgs e)
        {
            //e.AcceptedOperation = DataPackageOperation.Copy;
        }

        public void DisplayElement(UIElement elementToDisplay, Point upperLeft, UIElement fromCoordinateSystem)
        {
            var dropPoint = fromCoordinateSystem.TransformToVisual(xCanvas).TransformPoint(upperLeft);

            xCanvas.Children.Add(elementToDisplay);
            Canvas.SetLeft(elementToDisplay, dropPoint.X);
            Canvas.SetTop(elementToDisplay, dropPoint.Y);
        }
    }
}