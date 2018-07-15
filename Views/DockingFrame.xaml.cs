﻿using System;
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
using Windows.UI.Xaml.Shapes;

//The User Control item template is documented at https:
//using Dash;

//go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /// <summary>
    /// Set the element you want to anchor docking on as the DocumentContent property of this class in XAML.Be sure to set the DocumentController as well in the code-behind.
    /// </summary>
    public sealed partial class DockingFrame : UserControl
    {
        public static readonly DependencyProperty DocumentContentProperty = DependencyProperty.Register(
            "DocumentContent",
            typeof(object),
            typeof(object),
            new PropertyMetadata(default(object))
        );

        public object DocumentContent
        {
            get => GetValue(DocumentContentProperty);
            set => SetValue(DocumentContentProperty, value);
        }

        private readonly bool[] _firstDock = { true, true, true, true };
        private readonly DockedView[] _lastDockedViews = { null, null, null, null };
        private readonly ListController<DocumentController>[] _dockControllers = { null, null, null, null };
        private Rectangle[] _highlightRecs;
        private readonly DockDirection[] _directions = { DockDirection.Left, DockDirection.Right, DockDirection.Top, DockDirection.Bottom };
        public DocumentController DocController;

        public DockingFrame()
        {
            InitializeComponent();
            Loaded += (sender, e) =>
            {
                _highlightRecs = new[] { xDockLeft, xDockRight, xDockTop, xDockBottom };

            };
        }

        public void Dock(DocumentController toDock, DockDirection dir)
        {
            toDock = toDock.GetViewCopy();
            DocumentView copiedView = new DocumentView
            {
                DataContext = new DocumentViewModel(toDock),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ViewModel =
                        {
                            Width = Double.NaN,
                            Height = Double.NaN,
                            DisableDecorations = true
                        }
            };

            DockedView dockedView = new DockedView(dir, toDock);
            dockedView.NestedLengthChanged += OnNestedLengthChanged;
            dockedView.ChangeView(copiedView);
            dockedView.HorizontalAlignment = HorizontalAlignment.Stretch;
            dockedView.VerticalAlignment = VerticalAlignment.Stretch;

            if (_firstDock[(int)dir])
            {
                // make a new ListController
                _dockControllers[(int)dir] = new ListController<DocumentController>(toDock);
                double length = 300;

                if (toDock.GetDereferencedField<NumberController>(KeyStore.DockedLength, null) == null)
                    toDock.SetField(KeyStore.DockedLength, new NumberController(length), true);
                else
                    length = toDock.GetDereferencedField<NumberController>(KeyStore.DockedLength, null).Data;

                switch (dir)
                {
                    case DockDirection.Left:
                        xLeftDockSplitterColumn.Width = new GridLength(MainPage.GridSplitterThickness);
                        xLeftDockColumn.Width = new GridLength(length);
                        SetGridPosition(dockedView, 0, 1, 0, 5);
                        DocController.SetField(KeyStore.DockedDocumentsLeftKey, _dockControllers[(int)dir], true);
                        break;
                    case DockDirection.Right:
                        xRightDockSplitterColumn.Width = new GridLength(MainPage.GridSplitterThickness);
                        xRightDockColumn.Width = new GridLength(length);
                        SetGridPosition(dockedView, 4, 1, 0, 5);
                        DocController.SetField(KeyStore.DockedDocumentsRightKey, _dockControllers[(int)dir], true);
                        break;
                    case DockDirection.Top:
                        xTopDockSplitterRow.Height = new GridLength(MainPage.GridSplitterThickness);
                        xTopDockRow.Height = new GridLength(length);
                        SetGridPosition(dockedView, 2, 1, 0, 1);
                        DocController.SetField(KeyStore.DockedDocumentsTopKey, _dockControllers[(int)dir], true);
                        break;
                    case DockDirection.Bottom:
                        xBottomDockSplitterRow.Height = new GridLength(MainPage.GridSplitterThickness);
                        xBottomDockRow.Height = new GridLength(length);
                        SetGridPosition(dockedView, 2, 1, 4, 1);
                        DocController.SetField(KeyStore.DockedDocumentsBottomKey, _dockControllers[(int)dir], true);
                        break;
                }

                xMainGrid.Children.Add(dockedView);
                _firstDock[(int)dir] = false;
                _lastDockedViews[(int)dir] = dockedView;
            }
            else
            {
                DockedView tail = _lastDockedViews[(int)dir];
                tail.ChangeNestedView(dockedView);
                dockedView.PreviousView = tail;
                _lastDockedViews[(int)dir] = dockedView;
                _dockControllers[(int)dir].Add(toDock);

                // if there's no previous saved length, then set it. Otherwise, set it to that length.
                if (toDock.GetDereferencedField<NumberController>(KeyStore.DockedLength, null) == null)
                    toDock.SetField(KeyStore.DockedLength, new NumberController(tail.GetNestedViewSize()), true);
                else
                    tail.SetNestedViewSize(toDock.GetDereferencedField<NumberController>(KeyStore.DockedLength, null).Data);
            }
        }

        private void OnNestedLengthChanged(object sender, GridSplitterEventArgs e)
        {
            e.DocumentToUpdate.SetField(KeyStore.DockedLength, new NumberController(e.NewLength), true);
        }

        private void SetGridPosition(FrameworkElement e, int col, int colSpan, int row, int rowSpan)
        {
            Grid.SetColumn(e, col);
            Grid.SetColumnSpan(e, colSpan);
            Grid.SetRow(e, row);
            Grid.SetRowSpan(e, rowSpan);
        }

        public void HighlightDock(DockDirection dir)
        {
            _highlightRecs[(int)dir].Opacity = 0.4;
        }

        public void UnhighlightDock()
        {
            foreach (var rect in _highlightRecs)
            {
                rect.Opacity = 0;
            }
        }

        public void Undock(DockedView undock)
        {
            _dockControllers[(int)undock.Direction].Remove(undock.ContainedDocumentController);
            // means it's the last NestedView
            if (undock.NestedView == null)
            {
                // means it's also the first NestedView
                if (undock.PreviousView == null)
                {
                    switch (undock.Direction)
                    {
                        case DockDirection.Left:
                            xLeftDockSplitterColumn.Width = new GridLength(0);
                            xLeftDockColumn.Width = new GridLength(0);
                            break;
                        case DockDirection.Right:
                            xRightDockSplitterColumn.Width = new GridLength(0);
                            xRightDockColumn.Width = new GridLength(0);
                            break;
                        case DockDirection.Top:
                            xTopDockSplitterRow.Height = new GridLength(0);
                            xTopDockRow.Height = new GridLength(0);
                            break;
                        case DockDirection.Bottom:
                            xBottomDockSplitterRow.Height = new GridLength(0);
                            xBottomDockRow.Height = new GridLength(0);
                            break;
                    }
                    xMainGrid.Children.Remove(undock);
                    _firstDock[(int)undock.Direction] = true;
                    _lastDockedViews[(int)undock.Direction] = null;
                }
                else
                {
                    undock.PreviousView.ClearNestedView();
                    _lastDockedViews[(int)undock.Direction] = undock.PreviousView;
                }
            }
            else
            {
                // means it's the first NestedView
                if (undock.PreviousView == null)
                {
                    var newFirst = undock.ClearNestedView();
                    newFirst.PreviousView = null;
                    xMainGrid.Children.Remove(undock);
                    double newLength = 0;
                    switch (undock.Direction)
                    {
                        case DockDirection.Left:
                            SetGridPosition(newFirst, 0, 1, 0, 5);
                            newLength = xLeftDockColumn.Width.Value;
                            break;
                        case DockDirection.Right:
                            SetGridPosition(newFirst, 4, 1, 0, 5);
                            newLength = xRightDockColumn.Width.Value;
                            break;
                        case DockDirection.Top:
                            SetGridPosition(newFirst, 2, 1, 0, 1);
                            newLength = xTopDockRow.Height.Value;
                            break;
                        case DockDirection.Bottom:
                            SetGridPosition(newFirst, 2, 1, 4, 1);
                            newLength = xBottomDockRow.Height.Value;
                            break;
                    }
                    xMainGrid.Children.Add(newFirst);
                    OnNestedLengthChanged(this, new GridSplitterEventArgs {DocumentToUpdate = newFirst.ContainedDocumentController, NewLength = newLength });
                }
                else
                {
                    var newNext = undock.ClearNestedView();
                    newNext.PreviousView = undock.PreviousView;
                    undock.PreviousView.ChangeNestedView(newNext);
                    OnNestedLengthChanged(this, new GridSplitterEventArgs { DocumentToUpdate = newNext.ContainedDocumentController, NewLength = undock.PreviousView.GetNestedViewSize() });
                }
            }
        }

        public void LoadDockedItems()
        {
            var docs = new List<ListController<DocumentController>>
                    {
                        DocController.GetDereferencedField<ListController<DocumentController>>(KeyStore.DockedDocumentsLeftKey,
                            null),
                        DocController.GetDereferencedField<ListController<DocumentController>>(KeyStore.DockedDocumentsRightKey,
                            null),
                        DocController.GetDereferencedField<ListController<DocumentController>>(KeyStore.DockedDocumentsTopKey,
                            null),
                        DocController.GetDereferencedField<ListController<DocumentController>>(KeyStore.DockedDocumentsBottomKey,
                            null)
                    };

            for (int i = 0; i < 4; i++)
            {
                if (docs[i] != null)
                {
                    foreach (var d in docs[i].TypedData)
                    {
                        Dock(d, _directions[i]);
                    }
                }
            }
        }

        public DockDirection GetDockIntersection(Rect currentBoundingBox)
        {
            for (int i = 0; i < _highlightRecs.Length; i++)
            {
                var bounds =
                    new Rect(_highlightRecs[i].TransformToVisual(MainPage.Instance.xMainDocView).TransformPoint(new Point(0, 0)),
                        new Size(_highlightRecs[i].ActualWidth, _highlightRecs[i].ActualHeight));
                if (RectHelper.Intersect(currentBoundingBox, bounds) != RectHelper.Empty)
                {
                    return _directions[i];
                }
            }

            return DockDirection.None;
        }

        private void xRightSplitter_OnPointerReleased(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            OnNestedLengthChanged(this, new GridSplitterEventArgs { DocumentToUpdate = _dockControllers[1].GetElements().First(), NewLength = xRightDockColumn.Width.Value });
        }

        private void xLeftSplitter_OnPointerReleased(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            OnNestedLengthChanged(this, new GridSplitterEventArgs { DocumentToUpdate = _dockControllers[0].GetElements().First(), NewLength = xLeftDockColumn.Width.Value });
        }

        private void xTopSplitter_OnPointerReleased(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            OnNestedLengthChanged(this, new GridSplitterEventArgs { DocumentToUpdate = _dockControllers[2].GetElements().First(), NewLength = xTopDockRow.Height.Value });
        }

        private void xBottomSplitter_OnPointerReleased(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            OnNestedLengthChanged(this, new GridSplitterEventArgs { DocumentToUpdate = _dockControllers[3].GetElements().First(), NewLength = xBottomDockRow.Height.Value });
        }
    }
}