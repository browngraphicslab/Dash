using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash 
{
    public sealed partial class OverlayCanvas : UserControl
    {
        public static OverlayCanvas Instance = null;


        public TappedEventHandler OnAddDocumentsTapped, OnAddCollectionTapped, OnAddAPICreatorTapped, OnAddImageTapped, OnAddShapeTapped;
                
        public OverlayCanvas()
        {
            this.InitializeComponent();

            Debug.Assert(Instance == null);
            Instance = this;

            // test server to API

            // set uop server XAML object
            ServerXAML.UIElement test = new ServerXAML.UIElement();
            test.Height = 100;
            test.Width = 100;
            test.Visibility = ServerXAML.Visibility.Visible;

            // initialize mapping
            Mapper.Initialize(cfg => {
                cfg.CreateMap<ServerXAML.UIElement, FrameworkElement>();
            });

            // create obj to be mapped to 
            FrameworkElement myMan = new TextBox();
            Debug.WriteLine(test.Height == myMan.Height);

            // map test into myMan
            Mapper.Map(test, myMan);

            // just add visual stuff s.t. we can see the result
            TextBox g = (TextBox)myMan;
            g.Text = "IT WORKS!";
            g.Background = new SolidColorBrush(Windows.UI.Colors.Red);
            g.Margin = new Thickness(0, 0, 0, 0);

            // the server XAML maps the 100 width and height and visibility
            // into the new element!
            XContainer.Children.Add(g);
        }

        private void AddDocumentsTapped(object sender, TappedRoutedEventArgs e)
        {
            OnAddDocumentsTapped?.Invoke(sender, e);
        }

        private void AddCollectionTapped(object sender, TappedRoutedEventArgs e)
        {
            OnAddCollectionTapped?.Invoke(sender, e);
        }

        private void AddShapeTapped(object sender, TappedRoutedEventArgs e)
        {
            OnAddShapeTapped?.Invoke(sender, e);
        }

        private void image1_Tapped(object sender, TappedRoutedEventArgs e) {
            OnAddImageTapped?.Invoke(sender, e);
        }

        private void image_Tapped(object sender, TappedRoutedEventArgs e) {
            OnAddAPICreatorTapped?.Invoke(sender, e);
        }
    }
}
