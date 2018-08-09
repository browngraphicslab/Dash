using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public enum DockDirection
    {
        Left = 0,
        Right = 1,
        Top = 2,
        Bottom = 3,
        None = 4
    }

    public sealed partial class DockedView : UserControl
    {
        public DockedView NestedView { get; set; }
        public DockedView PreviousView { get; set; }
        public DockDirection Direction { get; set; }
        public DocumentController ContainedDocumentController { get; set; }
		public DocumentView ContainedDocumentView { get; set; }
        public event EventHandler<GridSplitterEventArgs> NestedLengthChanged;

        public DockedView(DockDirection direction, DocumentController dc)
        {
            this.InitializeComponent();
            Direction = direction;
            ContainedDocumentController = dc;
        }

        private void xCloseButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
        }

        public void ChangeView(DocumentView view)
        {
            Grid.SetColumn(view, 0);
            Grid.SetColumnSpan(view, 1);
            Grid.SetRow(view, 0);

	        ContainedDocumentView = view;
            xMainDockedView.Children.Clear();
	        xContentGrid.Children.Clear();
            view.Loaded += View_Loaded;
            xContentGrid.Children.Add(view);

			//change column/row span so it fills the entire available space
			Grid.SetColumn(view.xContentGrid, 0);
	        Grid.SetRow(view.xContentGrid, 0);
			Grid.SetColumnSpan(view.xContentGrid, 3);
			Grid.SetRowSpan(view.xContentGrid, 3);

			//re-add close button
			xMainDockedView.Children.Add(xCloseButton);
        }

        private void View_Loaded(object sender, RoutedEventArgs e)
        {
            //var view = sender as DocumentView;
            //if (view.GetFirstDescendantOfType<WebView>() != null)
            //{
            //    var webBoxView = view.GetFirstDescendantOfType<WebBoxView>();
            //    webBoxView.GetFirstDescendantOfType<WebView>().Height = 1008 - webBoxView.TextBlock.ActualHeight;
            //}
        }

        public void SetNestedViewSize(double dim)
        {
            if (NestedView == null) return;
            var gridLength = new GridLength(dim);
            switch (NestedView.Direction)
            {
                case DockDirection.Left:
                    xLeftNestedViewColumn.Width = gridLength;
                    break;
                case DockDirection.Right:
                    xRightNestedViewColumn.Width = gridLength;
                    break;
                case DockDirection.Top:
                    xTopNestedViewRow.Height = gridLength;
                    break;
                case DockDirection.Bottom:
                    xBottomNestedViewRow.Height = gridLength;
                    break;
            }
        }

        /// <summary>
        /// Gets the column or row height of this DockedView's NestedView.
        /// </summary>
        /// <returns></returns>
        public double GetNestedViewSize()
        {
            if (NestedView == null) return -1;
            switch (NestedView.Direction)
            {
                case DockDirection.Left:
                    return xLeftNestedViewColumn.Width.Value;
                case DockDirection.Right:
                    return xRightNestedViewColumn.Width.Value;
                case DockDirection.Top:
                    return xTopNestedViewRow.Height.Value;
                case DockDirection.Bottom:
                    return xBottomNestedViewRow.Height.Value;
                default:
                    return -1;
            }
        }

        public void ChangeNestedView(DockedView view)
        {
            // first time setting the nested view? If so, initialize dimensions
            if (NestedView == null)
            {
                switch (view.Direction)
                {
                    case DockDirection.Left:
                        xLeftSplitterColumn.Width = new GridLength(MainPage.GridSplitterThickness);
                        xLeftNestedViewColumn.Width = new GridLength(xMainDockedView.ActualWidth / 2);
                        break;
                    case DockDirection.Right:
                        xRightSplitterColumn.Width = new GridLength(MainPage.GridSplitterThickness);
                        xRightNestedViewColumn.Width = new GridLength(xMainDockedView.ActualWidth / 2);
                        break;
                    case DockDirection.Top:
                        xTopSplitterRow.Height = new GridLength(MainPage.GridSplitterThickness);
                        xTopNestedViewRow.Height = new GridLength(xMainDockedView.ActualHeight / 2);
                        break;
                    case DockDirection.Bottom:
                        xBottomSplitterRow.Height = new GridLength(MainPage.GridSplitterThickness);
                        xBottomNestedViewRow.Height = new GridLength(xMainDockedView.ActualHeight / 2);
                        break;
                }
            }
            else
            {
                xMasterGrid.Children.Remove(NestedView);
            }

            switch (view.Direction)
            {
                case DockDirection.Right:
                    Grid.SetColumn(view, 4);
                    Grid.SetRow(view, 0);
                    Grid.SetRowSpan(view, 5);
                    break;
                case DockDirection.Left:
                    Grid.SetColumn(view, 0);
                    Grid.SetRow(view, 0);
                    Grid.SetRowSpan(view, 5);
                    break;
                case DockDirection.Top:
                    Grid.SetRow(view, 0);
                    Grid.SetColumn(view, 0);
                    Grid.SetColumnSpan(view, 5);
                    break;
                case DockDirection.Bottom:
                    Grid.SetRow(view, 4);
                    Grid.SetColumn(view, 0);
                    Grid.SetColumnSpan(view, 5);
                    break;
            }

            NestedView = view;
            NestedView.HorizontalAlignment = HorizontalAlignment.Stretch;
            NestedView.VerticalAlignment = VerticalAlignment.Stretch;
            xMasterGrid.Children.Add(NestedView);
        }

        public DockedView ClearNestedView()
        {
            if (NestedView != null)
            {
                switch (NestedView.Direction)
                {
                    case DockDirection.Right:
                        xRightSplitterColumn.Width = new GridLength(0);
                        xRightNestedViewColumn.Width = new GridLength(0);
                        break;
                    case DockDirection.Left:
                        xLeftSplitterColumn.Width = new GridLength(0);
                        xLeftNestedViewColumn.Width = new GridLength(0);
                        break;
                    case DockDirection.Top:
                        xTopSplitterRow.Height = new GridLength(0);
                        xTopNestedViewRow.Height = new GridLength(0);
                        break;
                    case DockDirection.Bottom:
                        xBottomSplitterRow.Height = new GridLength(0);
                        xBottomNestedViewRow.Height = new GridLength(0);
                        break;
                }

                xMasterGrid.Children.Remove(NestedView);
            }

            var toReturn = NestedView;
            NestedView = null;
            return toReturn;
        }

        private void xSplitter_OnPointerReleased(object sender, ManipulationCompletedRoutedEventArgs manipulationCompletedRoutedEventArgs)
        {
            NestedLengthChanged?.Invoke(this, new GridSplitterEventArgs { NewLength = GetNestedViewSize(), DocumentToUpdate = NestedView.ContainedDocumentController });
        }

	    public void FlashSelection()
	    {
		    xFlashAnimation.Begin();
	    }
    }

    public class GridSplitterEventArgs : EventArgs
    {
        public double NewLength;
        public DocumentController DocumentToUpdate;
    }
}
