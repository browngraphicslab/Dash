﻿using System;
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
    }
}
