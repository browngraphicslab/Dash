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
using Microsoft.Toolkit.Uwp.UI.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class DockedView : UserControl
    {
        public DockedView NestedView { get; set; }
        public DockedView PreviousView { get; set; }

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

            xMainDockedView.Children.Clear();
            xMainDockedView.Children.Add(view);
            xMainDockedView.Children.Add(xCloseButton);
        }

        public void ChangeNestedView(DockedView view)
        {
            // first time setting the nested view?
            if (NestedView == null)
            {
                xSplitterColumn.Width = new GridLength(15);
                xNestedViewColumn.Width = new GridLength(xMainDockedView.ActualWidth / 2);
                Grid.SetColumn(view, 2);
                NestedView = view;
            }
            else
            {
                xContentGrid.Children.Remove(NestedView);
                Grid.SetColumn(view, Grid.GetColumn(NestedView));
                NestedView = view;
            }

            xContentGrid.Children.Add(NestedView);
        }

        public DockedView ClearNestedView()
        {
            if (NestedView != null)
            {
                xSplitterColumn.Width = new GridLength(0);
                xNestedViewColumn.Width = new GridLength(0);
                xContentGrid.Children.Remove(NestedView);
            }

            var toReturn = NestedView;
            NestedView = null;
            return toReturn;
        }
    }
}
