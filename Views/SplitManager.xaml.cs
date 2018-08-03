﻿using System;
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
using Microsoft.Toolkit.Uwp.UI.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class SplitManager : UserControl
    {
        public SplitManager()
        {
            this.InitializeComponent();
        }

        public enum SplitMode
        {
            Vertical, Horizontal, Content
        }

        public SplitMode CurSplitMode { get; private set; } = SplitMode.Content;
        private SplitMode _allowedSplits = SplitMode.Content;

        public ColumnDefinitionCollection Columns => XContentGrid.ColumnDefinitions;
        public RowDefinitionCollection Rows => XContentGrid.RowDefinitions;

        public void SetContent(DocumentController document)
        {
            var viewCopy = document.GetViewCopy();
            viewCopy.SetWidth(double.NaN);
            viewCopy.SetHeight(double.NaN);
            var docView = new SplitFrame
            {
                DataContext = new DocumentViewModel(viewCopy)
            };
            SetContent(docView);

        }

        private void SetContent(SplitFrame frame)
        {
            XContentGrid.Children.Clear();
            XContentGrid.RowDefinitions.Clear();
            XContentGrid.ColumnDefinitions.Clear();
            XContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            XContentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetRow(frame, 0);
            Grid.SetColumn(frame, 0);
            XContentGrid.Children.Add(frame);
            CurSplitMode = SplitMode.Content;
        }

        public bool DocViewTrySplit(SplitFrame sender, SplitFrame.SplitEventArgs args)
        {
            switch (args.Direction)
            {
                case SplitFrame.SplitDirection.Down:
                case SplitFrame.SplitDirection.Up:
                    if (_allowedSplits == SplitMode.Horizontal)//Make new nested split manager and move split frame to new manager
                    {
                        return false;
                    }
                    else
                    {
                        if (CurSplitMode == SplitMode.Content)
                        {
                            sender.SplitCompleted += WrapFrameInManager;
                        }
                        CurSplitMode = SplitMode.Vertical;
                        bool up = args.Direction == SplitFrame.SplitDirection.Up;
                        var row = Grid.GetRow(sender);
                        var splitter = new GridSplitter
                        {
                            ResizeDirection = GridSplitter.GridResizeDirection.Rows,
                            ResizeBehavior = GridSplitter.GridResizeBehavior.PreviousAndNext
                        };
                        var newPane = new SplitFrame()
                        {
                            DataContext = new DocumentViewModel(sender.DocumentController.GetViewCopy())
                        };
                        var newManager = new SplitManager();
                        newManager.SetContent(newPane);
                        newManager._allowedSplits = SplitMode.Horizontal;
                        if (up)
                        {
                            XContentGrid.RowDefinitions.Insert(row + 1,
                                new RowDefinition { Height = new GridLength(0) }); //Content row
                            XContentGrid.RowDefinitions.Insert(row + 1,
                                new RowDefinition { Height = new GridLength(10) }); //Splitter row
                            Grid.SetColumn(splitter, row + 1);
                            Grid.SetColumn(newManager, row + 2);
                            UpdateRows(2, row + 1);
                            XContentGrid.Children.Add(splitter);
                            XContentGrid.Children.Add(newManager);
                        }
                        else
                        {
                            XContentGrid.RowDefinitions.Insert(row,
                                new RowDefinition() { Height = new GridLength(10) }); //Splitter row
                            XContentGrid.RowDefinitions.Insert(row,
                                new RowDefinition { Height = new GridLength(0) }); //Content row
                            Grid.SetRow(newManager, row);
                            Grid.SetRow(splitter, row + 1);
                            UpdateRows(2, row);
                            XContentGrid.Children.Add(splitter);
                            XContentGrid.Children.Add(newManager);
                        }
                    }

                    break;
                case SplitFrame.SplitDirection.Left:
                case SplitFrame.SplitDirection.Right:
                    if (_allowedSplits == SplitMode.Vertical)//Make new nested split manager and move split frame to new manager
                    {
                        return false;
                    }
                    else
                    {

                        if (CurSplitMode == SplitMode.Content)
                        {
                            sender.SplitCompleted += WrapFrameInManager;
                        }

                        CurSplitMode = SplitMode.Horizontal;
                        bool left = args.Direction == SplitFrame.SplitDirection.Left;
                        var col = Grid.GetColumn(sender);
                        var splitter = new GridSplitter
                        {
                            ResizeDirection = GridSplitter.GridResizeDirection.Columns,
                            ResizeBehavior = GridSplitter.GridResizeBehavior.PreviousAndNext
                        };
                        var newPane = new SplitFrame()
                        {
                            DataContext = new DocumentViewModel(sender.DocumentController.GetViewCopy())
                        };
                        var newManager = new SplitManager();
                        newManager.SetContent(newPane);
                        newManager._allowedSplits = SplitMode.Vertical;
                        if (left)
                        {
                            XContentGrid.ColumnDefinitions.Insert(col + 1,
                                new ColumnDefinition {Width = new GridLength(0)}); //Content col
                            XContentGrid.ColumnDefinitions.Insert(col + 1,
                                new ColumnDefinition {Width = new GridLength(10)}); //Splitter col
                            Grid.SetColumn(splitter, col + 1);
                            Grid.SetColumn(newManager, col + 2);
                            UpdateCols(2, col + 1);
                            XContentGrid.Children.Add(splitter);
                            XContentGrid.Children.Add(newManager);
                        }
                        else
                        {
                            XContentGrid.ColumnDefinitions.Insert(col,
                                new ColumnDefinition() {Width = new GridLength(10)}); //Splitter col
                            XContentGrid.ColumnDefinitions.Insert(col,
                                new ColumnDefinition {Width = new GridLength(0)}); //Content col
                            Grid.SetColumn(newManager, col);
                            Grid.SetColumn(splitter, col + 1);
                            UpdateCols(2, col);
                            XContentGrid.Children.Add(splitter);
                            XContentGrid.Children.Add(newManager);
                        }
                    }

                    break;
            }

            return true;
        }

        private void WrapFrameInManager(SplitFrame frame)
        {
            frame.SplitCompleted -= WrapFrameInManager;
            XContentGrid.Children.Remove(frame);
            var row = Grid.GetRow(frame);
            var col = Grid.GetColumn(frame);
            var nested = new SplitManager();
            nested.SetContent(frame);
            Grid.SetRow(nested, row);
            Grid.SetColumn(nested, col);
            XContentGrid.Children.Add(nested);
        }

        private void UpdateCols(int offset, int min)
        {
            foreach (var child in XContentGrid.Children)
            {
                var fe = child as FrameworkElement;
                var col = Grid.GetColumn(fe);
                if (col >= min)
                {
                    Grid.SetColumn(fe, col + offset);
                }
            }
        }

        private void UpdateRows(int offset, int min)
        {
            foreach (var child in XContentGrid.Children)
            {
                var fe = child as FrameworkElement;
                var col = Grid.GetRow(fe);
                if (col >= min)
                {
                    Grid.SetRow(fe, col + offset);
                }
            }
        }
    }
}
