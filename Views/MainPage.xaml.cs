using Dash.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Dash.ServerClient.Requests;
using Dash.ViewModels;
using DashShared;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Dash
{
    /// <summary>
    /// Zoomable pannable canvas. Has an overlay canvas unaffected by pan / zoom. 
    /// </summary>
    public sealed partial class MainPage : Page
    {

        static public MainPage Instance;

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
            var documentsField = new Dictionary<Key, FieldModel>
            {
                [DashConstants.KeyStore.CollectionDocumentsListFieldKey] = new DocumentCollectionFieldModel(new List<DocumentModel>())       
            };
            var newCollectionDocumentRequestArgs = new CreateNewDocumentRequestArgs(documentsField, DashConstants.DocumentTypeStore.CollectionDocumentType);
            var newCollectionDocumentRequest = new CreateNewDocumentRequest(newCollectionDocumentRequestArgs);
            var collectionDocumentController = newCollectionDocumentRequest.GetReturnedDocumentController();

            // set the main view's datacontext to be the collection
            MainDocView.DataContext = new DocumentViewModel(collectionDocumentController);
            
            // set the main view's width and height to avoid NaN errors
            MainDocView.Width = MyGrid.ActualWidth;
            MainDocView.Height = MyGrid.ActualHeight;

            // TODO someone who understands this explain what it does
            MainDocView.ManipulationMode = ManipulationModes.None;
            MainDocView.Manipulator.RemoveAllButHandle();
            MainDocView.DraggerButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed; 

            // Set the instance to be itself, there should only ever be one MainView
            Debug.Assert(Instance == null, "If the main view isn't null then it's been instantiated multiple times and setting the instance is a problem");
            Instance = this;
        }

        private void OnToggleEditMode(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            //xFreeformView.ToggleEditMode();
        }

        private void OnOperatorAdd(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            throw new NotImplementedException();
            ////Create Operator document
            //var docEndpoint = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            //DocumentModel opModel =
            //    OperatorDocumentModel.CreateOperatorDocumentModel(new DivideOperatorModel());
            //docEndpoint.UpdateDocumentAsync(opModel);
            //DocumentView view = new DocumentView
            //{
            //    Width = 200,
            //    Height = 200
            //};
            //DocumentViewModel opvm = new DocumentViewModel(opModel);
            ////OperatorDocumentViewModel opvm = new OperatorDocumentViewModel(opModel);
            //view.DataContext = opvm;


            //DisplayDocument(opModel);
            ////xFreeformView.AddOperatorView(opvm, view, 50, 50);
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

        public void DisplayDocument(DocumentModel docModel, Point? where = null)
        {
            var children = MainDocument.Fields[DashConstants.KeyStore.CollectionDocumentsListFieldKey] as DocumentCollectionFieldModelController;
            throw new NotImplementedException("have to get displaying documents to work");
            //if (children != null) { 
            //    children.AddDocumentModel(docModel);
            //    if (where.HasValue)
            //    {
            //        docModel.SetField(DocumentModel.GetFieldKeyByName("X"), new NumberFieldModel(((Point)where).X), false);
            //        docModel.SetField(DocumentModel.GetFieldKeyByName("Y"), new NumberFieldModel(((Point)where).Y), false);
            //    }
            //}
        }

        private void AddCollection(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            throw new NotImplementedException();
            //DocumentModel image2 = DocumentModel.TwoImagesAndText();
            //DocumentModel image2Del = image2.MakeDelegate();
            //DocumentModel umpireDoc = DocumentModel.UmpireDocumentModel();
            //image2Del.SetField(DocumentModel.LayoutKey, new LayoutModelFieldModel(LayoutModel.TwoImagesAndTextModel(image2Del.DocumentType, true)), true);
            //image2Del.SetField(DocumentModel.GetFieldKeyByName("content"), new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat2.jpeg")), true);

            //var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            //if (docCollection == null) {
            //    docCollection = docController.CreateDocumentAsync("newtype");
            //    docCollection.SetField(DocumentModel.GetFieldKeyByName("children"), new DocumentCollectionFieldModel(new DocumentModel[] {image2, image2Del, umpireDoc}), false);
            //}
            //DisplayDocument(docCollection);
        }

        private void AddApiCreator(object sender, TappedRoutedEventArgs tappedRoutedEventArgs) {
            throw new NotImplementedException();

            // xFreeformView.Canvas.Children.Add(new Sources.Api.ApiCreatorDisplay());
        }

        private void AddImage(object sender, TappedRoutedEventArgs tappedRoutedEventArgs) {
            throw new NotImplementedException();

            // xFreeformView.Canvas.Children.Add(new Sources.FilePicker.FilePickerDisplay());
            // xFreeformView.Canvas.Children.Add(new Sources.FilePicker.PDFFilePicker());
        }

        private async void AddDocuments(object sender, TappedRoutedEventArgs e)
        {
            throw new NotImplementedException();

            //DocumentModel recipe = DocumentModel.Food2ForkRecipeDocumentModel();
            //DocumentModel pricePerSqFt = await DocumentModel.PricePerSquareFootExample();
            //DocumentModel collection = await DocumentModel.CollectionExample();
            //DocumentModel image = DocumentModel.OneImage();

            //DisplayDocument(recipe);
            //DisplayDocument(pricePerSqFt);
            //DisplayDocument(image);
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
    }
}
