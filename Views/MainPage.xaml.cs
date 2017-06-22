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
using DashShared;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Dash
{
    /// <summary>
    /// Zoomable pannable canvas. Has an overlay canvas unaffected by pan / zoom. 
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            // adds items from the overlay canvas onto the freeform canvas
            xOverlayCanvas.OnAddDocumentsTapped += AddDocuments;
            xOverlayCanvas.OnAddCollectionTapped += AddCollection;
            xOverlayCanvas.OnAddAPICreatorTapped += AddApiCreator;
            xOverlayCanvas.OnAddImageTapped += AddImage;
            xOverlayCanvas.OnAddShapeTapped += AddShape;
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

            var shapeController = new ShapeController(shapeModel);
            ContentController.AddShapeController(shapeController);

            var shapeVM = new ShapeViewModel(shapeController);
            var shapeView = new ShapeView(shapeVM);


            xFreeformView.Canvas.Children.Add(shapeView);
        }
        
        DocumentViewModel model7, twotxtModel, model1;
        DocumentModel docCollection = null;

        private void AddCollection(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            model7.DocumentModel.GetPrototype().SetField(DocumentModel.GetFieldKeyByName("content"), new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat2.jpeg")), false);

            var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            var collection = docController.CreateDocumentAsync("newtype");
            collection.SetField(DocumentModel.GetFieldKeyByName("children"), new DocumentCollectionFieldModel(new List<DocumentModel>(new DocumentModel[] { model1.DocumentModel, twotxtModel.DocumentModel, model7.DocumentModel })));
            if (docCollection == null) {
                docCollection = docController.CreateDocumentAsync("newtype");
                docCollection.SetField(DocumentModel.GetFieldKeyByName("children"), new DocumentCollectionFieldModel(
                  new List<DocumentModel>(new DocumentModel[] { model1.DocumentModel.MakeDelegate(), twotxtModel.DocumentModel.MakeDelegate(), model7.DocumentModel.MakeDelegate() })), false);
            }
            DocumentViewModel model = new DocumentViewModel(docCollection);
            DocumentView view1 = new DocumentView(model);
            xFreeformView.Canvas.Children.Add(view1);
        }

        private void AddApiCreator(object sender, TappedRoutedEventArgs tappedRoutedEventArgs) {
            xFreeformView.Canvas.Children.Add(new Sources.Api.ApiCreatorDisplay());
        }
        private void AddImage(object sender, TappedRoutedEventArgs tappedRoutedEventArgs) {
            xFreeformView.Canvas.Children.Add(new Sources.FilePicker.FilePickerDisplay());
            xFreeformView.Canvas.Children.Add(new Sources.FilePicker.PDFFilePicker());
        }

        private async void AddDocuments(object sender, TappedRoutedEventArgs e)
        {
            DocumentModel umpire = DocumentModel.UmpireDocumentModel();
            DocumentModel recipe = DocumentModel.Food2ForkRecipeDocumentModel();
            
            DocumentModel twotxtDocModel = DocumentModel.TwoImagesAndText();
            DocumentModel collection = await DocumentModel.CollectionExample();
            DocumentModel pricePerSqFt = await DocumentModel.PricePerSquareFootExample();

            model1 = new DocumentViewModel(umpire);
            DocumentViewModel model2 = new DocumentViewModel(recipe);
            twotxtModel = new DocumentViewModel(twotxtDocModel);
            DocumentViewModel model5 = new DocumentViewModel(collection);
            DocumentViewModel model6 = new DocumentViewModel(pricePerSqFt);

            model7 = new DocumentViewModel(twotxtDocModel.MakeDelegate());
            model7.DocumentModel.SetField(DocumentModel.LayoutKey, new LayoutModelFieldModel(LayoutModel.TwoImagesAndTextModel(model7.DocumentModel.DocumentType, true)));

            DocumentView view1 = new DocumentView(model1);
            DocumentView view2 = new DocumentView(model2);
            DocumentView twotxtView = new DocumentView(twotxtModel);
            DocumentView view5 = new DocumentView(model5);
            DocumentView view6 = new DocumentView(model6);
            DocumentView view7 = new DocumentView(model7);

            // makes oneimage doc model
            DocumentModel image = DocumentModel.OneImage();
            DocumentViewModel model3 = new DocumentViewModel(image);
            DocumentView view3 = new DocumentView(model3);
            
            xFreeformView.Canvas.Children.Add(view3);
            xFreeformView.Canvas.Children.Add(twotxtView);
            xFreeformView.Canvas.Children.Add(view6);
            xFreeformView.Canvas.Children.Add(view7);

            
        }
    }
}
