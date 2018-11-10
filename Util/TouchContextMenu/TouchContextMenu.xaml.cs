using System;
using System.Collections.Generic;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TouchContextMenu : UserControl
    {
        public TouchContextMenu(Point location)
        {
            this.InitializeComponent();
            Canvas.SetLeft(this, location.X);
            Canvas.SetTop(this, location.Y);
            MainPage.Instance.xCanvas.Children.Add(this);
            //use TouchInteractions.HeldDocument for target doc view

        }

        private void RadialMenu_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
