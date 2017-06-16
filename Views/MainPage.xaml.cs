using Dash.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

            OverlayCanvas.OnAddDocumentsTapped += AddDocuments;
            OverlayCanvas.OnAddCollectionTapped += AddCollection;
        }
        DocumentViewModel model7, model4, model1;

        private void AddCollection(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            model7.DocumentModel.GetPrototype().SetField(DocumentModel.GetFieldKeyByName("content"), new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat2.jpeg")));

            var docController = App.Instance.Container.GetRequiredService<DocumentController>();
            var collection = docController.CreateDocumentAsync("newtype");
            collection.SetField(DocumentModel.GetFieldKeyByName("children"), new DocumentCollectionFieldModel(new List<DocumentModel>(new DocumentModel[] { model1.DocumentModel, model4.DocumentModel, model7.DocumentModel })));
            DocumentViewModel modelC = new DocumentViewModel(collection);
            DocumentView view1 = new DocumentView() { DataContext = modelC };
            FreeformView.Canvas.Children.Add(view1);
        }

        private async void AddDocuments(object sender, TappedRoutedEventArgs e)
        {
            DocumentModel umpire = DocumentModel.UmpireDocumentModel();
            DocumentModel recipe = DocumentModel.Food2ForkRecipeDocumentModel();
            DocumentModel image = DocumentModel.OneImage();
            DocumentModel image2 = DocumentModel.TwoImagesAndText();
            DocumentModel collection = await DocumentModel.CollectionExample();
            DocumentModel pricePerSqFt = await DocumentModel.PricePerSquareFootExample();


            model1 = new DocumentViewModel(umpire);
            DocumentViewModel model2 = new DocumentViewModel(recipe);
            DocumentViewModel model3 = new DocumentViewModel(image);
            model4 = new DocumentViewModel(image2);
            DocumentViewModel model5 = new DocumentViewModel(collection);
            DocumentViewModel model6 = new DocumentViewModel(pricePerSqFt);
            model7 = new DocumentViewModel(image2.MakeDelegate());
            model7.DocumentModel.SetField(DocumentModel.LayoutKey, new LayoutModelFieldModel(LayoutModel.TwoImagesAndTextModel(model7.DocumentModel.DocumentType, true)));


            DocumentView view1 = new DocumentView();
            DocumentView view2 = new DocumentView();
            DocumentView view3 = new DocumentView();
            DocumentView view4 = new DocumentView();
            DocumentView view5 = new DocumentView();
            DocumentView view6 = new DocumentView();
            DocumentView view7 = new DocumentView() { DataContext = model7 };

            view1.DataContext = model1;
            view2.DataContext = model2;
            view3.DataContext = model3;
            view4.DataContext = model4;
            view5.DataContext = model5;
            view6.DataContext = model6;

            //view1.Margin = new Thickness(20, 20, 0, 0);
            //view1.Width = 200;
            //view1.Height = 400;
            //view2.Margin = new Thickness(400, 20, 0, 0);
            //view2.Width = 200;
            //view2.Height = 400;


            //MyGrid.Children.Add(view1);
            //MyGrid.Children.Add(view2);
            //FreeformView.Canvas.Children.Add(view1);
            //FreeformView.Canvas.Children.Add(view2);
            FreeformView.Canvas.Children.Add(view3);
            FreeformView.Canvas.Children.Add(view4);
            //FreeformView.Canvas.Children.Add(view5);
            FreeformView.Canvas.Children.Add(view6);
            FreeformView.Canvas.Children.Add(view7);



        }
    }
}
