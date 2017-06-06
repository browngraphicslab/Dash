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

namespace PanZoomCanvas
{
    /// <summary>
    /// Zoomable pannable canvas. Has an overlay canvas unaffected by pan / zoom. 
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void Ellipse_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //for (int i = 0; i < 10000; ++i)
            //{
            //    Ellipse el = new Ellipse
            //    {
            //        Width = 40,
            //        Height = 80,
            //        Fill = new SolidColorBrush(Colors.Green)
            //    }; 
            //    Canvas.SetLeft(el, 500);
            //    Canvas.SetTop(el, 500);
            //    MyCanvas.Children.Add(el);
            //}

            //var ellipses = MyCanvas.Children.Where(el => el as Ellipse != null);
            //foreach (var ell in ellipses)
            //{
            //            ell.Visibility = Visibility.Collapsed;
            //    ;
            //}
        }
    }
}
