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

        public void ChangeNestedView(DockedView view)
        {
            // first time setting the nested view?
            if (NestedView == null)
            {
                ColumnDefinition splitterCol = new ColumnDefinition();
                splitterCol.Width = new GridLength(15);
                ColumnDefinition viewCol = new ColumnDefinition();
                viewCol.Width = new GridLength(1, GridUnitType.Star);
                xContentGrid.ColumnDefinitions.Add(splitterCol);
                xContentGrid.ColumnDefinitions.Add(viewCol);

                GridSplitter splitter = new GridSplitter();

                Grid.SetColumn(splitter, xContentGrid.ColumnDefinitions.Count - 2);
                xContentGrid.Children.Add(splitter);
                Grid.SetColumn(view, xContentGrid.ColumnDefinitions.Count - 1);

                NestedView = view;
            }
            else
            {
                xContentGrid.Children.Remove(NestedView);
                Grid.SetColumn(view, Grid.GetColumn(NestedView));
                NestedView = view;
                xContentGrid.Children.Add(NestedView);
            }

            xContentGrid.Children.Add(NestedView);
        }
    }
}
