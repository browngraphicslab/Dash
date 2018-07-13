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
            MainPage.Instance.DockManager.Undock(this);
        }

        public void ChangeView(DocumentView view)
        {
            Grid.SetColumn(view, 0);
            Grid.SetColumnSpan(view, 2);
            Grid.SetRow(view, 1);
            view.Margin = new Thickness(5);
	        ContainedDocumentView = view;

            xMainDockedView.Children.Clear();
            xMainDockedView.Children.Add(view);
            xMainDockedView.Children.Add(xCloseButton);
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
                xContentGrid.Children.Remove(NestedView);
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
            xContentGrid.Children.Add(NestedView);
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

                xContentGrid.Children.Remove(NestedView);
            }

            var toReturn = NestedView;
            NestedView = null;
            return toReturn;
        }

        private void xSplitter_OnPointerReleased(object sender, ManipulationCompletedRoutedEventArgs manipulationCompletedRoutedEventArgs)
        {
            NestedLengthChanged?.Invoke(this, new GridSplitterEventArgs { NewLength = GetNestedViewSize(), DocumentToUpdate = NestedView.ContainedDocumentController });
        }
    }

    public class GridSplitterEventArgs : EventArgs
    {
        public double NewLength;
        public DocumentController DocumentToUpdate;
    }
}
