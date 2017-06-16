using AutoMapper;
using Dash.Models;
using Dash.Sources.Api;
using Dash.Sources.Api.XAML_Elements;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash 
{
    public sealed partial class OverlayCanvas : UserControl
    {
        public static OverlayCanvas Instance = null;

        public TappedEventHandler OnEllipseTapped;
        public TappedEventHandler OnEllipseTapped2;

        public OverlayCanvas() {
            this.InitializeComponent();

            Debug.Assert(Instance == null);
            Instance = this;


            ServerXAML.UIElement test = new ServerXAML.UIElement();
            test.Height = 100;
            test.Width = 100;
            test.Visibility = ServerXAML.Visibility.Visible;

            Mapper.Initialize(cfg => {
                cfg.CreateMap<ServerXAML.UIElement, FrameworkElement>();
            });

            FrameworkElement myMan = new TextBox();
            Debug.WriteLine(test.Height == myMan.Height);

            Mapper.Map(test, myMan);
            TextBox g = (TextBox)myMan;
            g.Text = "IT WORKS!";
            g.Background = new SolidColorBrush(Windows.UI.Colors.Red);
            g.Margin = new Thickness(0, 0, 0, 0);
            ApiSourceCreatorContainer.Children.Add(g);
        }

        private void UserControl_DragOver(object sender, DragEventArgs e) {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private void UserControl_Drop(object sender, DragEventArgs e) {
            Debug.WriteLine(e.GetPosition(this));
        }
    }
}
