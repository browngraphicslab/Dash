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

            OverlayCanvas.OnEllipseTapped += Ellipse_Tapped;
            OverlayCanvas.OnEllipseTapped2 += EllipseTapped2;
        }

        private void EllipseTapped2(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            WindowView window = new WindowView();
            FreeformView.Canvas.Children.Add(window);
            Canvas.SetLeft(window, (MyGrid.ActualWidth - window.Width) /2);
            Canvas.SetTop(window, (MyGrid.ActualHeight - window.Height) / 2);
        }

        private void Ellipse_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DocumentModel umpire = DocumentModel.UmpireDocumentModel();
            DocumentModel recipe = DocumentModel.Food2ForkRecipeDocumentModel();
            DocumentModel image = DocumentModel.OneImage();
            DocumentModel image2 = DocumentModel.TwoImagesAndText();

            DocumentViewModel model1 = new DocumentViewModel(umpire, DocumentLayoutModelSource.DefaultLayoutModelSource);
            DocumentViewModel model2 = new DocumentViewModel(recipe, DocumentLayoutModelSource.DefaultLayoutModelSource);
            DocumentViewModel model3 = new DocumentViewModel(image, DocumentLayoutModelSource.DefaultLayoutModelSource);
            DocumentViewModel model4 = new DocumentViewModel(image2, DocumentLayoutModelSource.DefaultLayoutModelSource);

            DocumentView view1 = new DocumentView();
            DocumentView view2 = new DocumentView();
            DocumentView view3 = new DocumentView();
            DocumentView view4 = new DocumentView();

            view1.DataContext = model1;
            view2.DataContext = model2;
            view3.DataContext = model3;
            view4.DataContext = model4;

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


        }
    }
}
