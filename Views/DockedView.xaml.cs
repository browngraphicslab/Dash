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

namespace Dash.Views
{
    public sealed partial class DockedView : UserControl
    {
        public DockedView()
        {
            this.InitializeComponent();
        }

        private void xCloseButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MainPage.Instance.Undock(this);
        }

        public void ChangeView(FrameworkElement view)
        {
            Grid.SetColumn(view, 0);
            Grid.SetColumnSpan(view, 2);
            Grid.SetRow(view, 0);
            Grid.SetRowSpan(view, 2);

            xContentGrid.Children.Clear();
            xContentGrid.Children.Add(view);
            xContentGrid.Children.Add(xCloseButton);
        }
    }
}
