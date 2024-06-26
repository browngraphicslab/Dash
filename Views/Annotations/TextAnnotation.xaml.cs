﻿using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using DashShared;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TextAnnotation
    {
        public int StartIndex = -1;
        public int EndIndex = -1;
        public Rect ClipRect = Rect.Empty;
        private Point? _selectionStartPoint;
        private Selection _selectionViewModel;

        public TextAnnotation(AnnotationOverlay parent, Selection selectionViewModel) :
            base(parent, selectionViewModel?.RegionDocument)
        {
            this.InitializeComponent();

            DataContext = selectionViewModel;

            AnnotationType = AnnotationType.Selection;

            _selectionViewModel = selectionViewModel;
            HelpRenderRegion();
        }
        public override bool IsInView(Rect bounds)
        {
            foreach (var p in LayoutRoot.Children.OfType<Path>())
            {
                var pbounds = p.GetBoundingRect(this);
                pbounds.Intersect(bounds);
                if (!pbounds.IsEmpty)
                {
                    return true;
                }
            }
            return false;
        }
        public override void StartAnnotation(Point p)
        {
            if (ParentOverlay.TextSelectableElements != null && ParentOverlay.TextSelectableElements.Any() && ParentOverlay.PageEndIndices.Any())
            {
                _selectionStartPoint = p;
            }
        }
        public SelectableElement GetClosestElementInDirection(Point p, Point dir)
        {
            SelectableElement ele = null;
            double closestDist = double.PositiveInfinity;

            var startPage = ParentOverlay.GetPageOf(p.Y);
            if (startPage >= ParentOverlay.PageEndIndices.Count)
            {
                return null;
            }
            // startIndex is either 0 or the last page's end index + 1
            var startIndex = startPage > 0 ? ParentOverlay.PageEndIndices[startPage - 1] + 1 : 0;
            var endIndex = ParentOverlay.PageEndIndices[startPage];
            for (var index = startIndex; index < endIndex; index++)
            {
                var selectableElement = ParentOverlay.TextSelectableElements[index];
                var b = selectableElement.Bounds;
                if (b.Contains(p) && !string.IsNullOrWhiteSpace(selectableElement.Contents as string))
                {
                    return selectableElement;
                }

                var dist = GetMinRectDist(b, p, out var closest);
                if (dist < closestDist && (closest.X - p.X) * dir.X + (closest.Y - p.Y) * dir.Y > 0)
                {
                    ele = selectableElement;
                    closestDist = dist;
                }
            }

            return ele;
        }

        private double GetMinRectDist(Rect r, Point p, out Point closest)
        {
            var x1Dist = p.X - r.Left;
            var x2Dist = p.X - r.Right;
            var y1Dist = p.Y - r.Top;
            var y2Dist = p.Y - r.Bottom;
            x1Dist *= x1Dist;
            x2Dist *= x2Dist;
            y1Dist *= y1Dist;
            y2Dist *= y2Dist;
            closest.X = x1Dist < x2Dist ? r.Left : r.Right;
            closest.Y = y1Dist < y2Dist ? r.Top : r.Bottom;
            return Math.Min(x1Dist, x2Dist) + Math.Min(y1Dist, y2Dist);
        }

        public override void UpdateAnnotation(Point p)
        {
            if (ParentOverlay.TextSelectableElements != null && ParentOverlay.TextSelectableElements.Any() && ParentOverlay.PageEndIndices.Any())
            {
                if (_selectionStartPoint.HasValue)
                {
                    if (Math.Abs(_selectionStartPoint.Value.X - p.X) < 3 &&
                        Math.Abs(_selectionStartPoint.Value.Y - p.Y) < 3)
                    {
                        return;
                    }

                    var dir = new Point(p.X - _selectionStartPoint.Value.X, p.Y - _selectionStartPoint.Value.Y);
                    var startEle = GetClosestElementInDirection(_selectionStartPoint.Value, dir);
                    if (startEle == null)
                    {
                        return;
                    }

                    var currentEle = GetClosestElementInDirection(p, new Point(-dir.X, -dir.Y));
                    if (currentEle == null)
                    {
                        return;
                    }

                    ParentOverlay.SelectElements(Math.Min(startEle.Index, currentEle.Index),
                        Math.Max(startEle.Index, currentEle.Index), _selectionStartPoint ?? new Point(), p);
                    XPos = Math.Min(XPos, startEle.Bounds.X);
                    YPos = Math.Min(YPos, startEle.Bounds.Y);
                }
            }
        }

        public override void EndAnnotation(Point p)
        {
            _selectionStartPoint = null;
            if (StartIndex != -1 && EndIndex != -1)
            {
                ParentOverlay.CurrentAnchorableAnnotations.Add(this);
            }
        }

        public void HelpRenderRegion()
        {
            if (RegionDocumentController == null)
            {
                return;
            }
            var indexList = RegionDocumentController.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionIndicesListKey);
            var boundsList =
                RegionDocumentController.GetDataDocument().GetFieldOrCreateDefault<ListController<RectController>>(
                    KeyStore.SelectionBoundsKey);

            if (indexList.Any() && boundsList.Any() && _selectionViewModel != null)
            {
                var geometryGroup = new GeometryGroup();
                var topLeft = new Point(double.MaxValue, double.MaxValue);
                RectangleGeometry lastRect = null;
                int start = 0;
                foreach (var t in indexList)
                {
                    var range = t.Data;
                    for (var ind = (int)range.X; ind <= (int)range.Y; ind++)
                    {
                        var rect  = boundsList[start++].Data;
                        topLeft.X = Math.Min(topLeft.X, rect.X);
                        topLeft.Y = Math.Min(topLeft.Y, rect.Y);
                        if (lastRect != null && Math.Abs(lastRect.Rect.Right - rect.X) < 7 && Math.Abs(lastRect.Rect.Y - rect.Y) < 2) // bcz: watch out for magic numbers-- should probably be based on font size 
                            lastRect.Rect = new Rect(lastRect.Rect.X, lastRect.Rect.Y, rect.X + rect.Width - lastRect.Rect.X, rect.Y + rect.Height - lastRect.Rect.Y);
                        else
                            geometryGroup.Children.Add(lastRect = new RectangleGeometry { Rect = rect });
                    }
                }
                foreach (var rect in geometryGroup.Children.OfType<RectangleGeometry>())
                {
                    rect.Rect = new Rect(new Point(rect.Rect.X - topLeft.X, rect.Rect.Y - topLeft.Y), new Size(rect.Rect.Width, rect.Rect.Height));
                }
                var path = new Path
                {
                    Data = geometryGroup,
                    DataContext = _selectionViewModel,
                    IsDoubleTapEnabled = false,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                InitializeAnnotationObject(path, topLeft, PlacementMode.Mouse);
                LayoutRoot.Children.Add(path);
            }
        }

        public override double AddToRegion(DocumentController region)
        {
            if (ClipRect != Rect.Empty)
            {
                var prevUsedIndex = StartIndex - 1;
                var prevStartIndex = StartIndex;
                for (var i = StartIndex; i <= EndIndex; i++)
                {
                    var elem = ParentOverlay.TextSelectableElements[i];
                    if (ClipRect.Contains(new Point(elem.Bounds.X + elem.Bounds.Width / 2, elem.Bounds.Y + elem.Bounds.Height / 2)))
                    {
                        if (i != prevUsedIndex + 1)
                        {
                            region.AddToListField(KeyStore.SelectionIndicesListKey, new PointController(prevStartIndex, prevUsedIndex));
                            prevStartIndex = i;
                        }

                        region.GetDataDocument().AddToListField(KeyStore.SelectionBoundsKey,
                                new RectController(ParentOverlay.TextSelectableElements[i].Bounds));

                        prevUsedIndex = i;
                        YPos = ClipRect.Y;
                        XPos = ClipRect.X;
                    }
                }
                region.AddToListField(KeyStore.SelectionIndicesListKey, new PointController(prevStartIndex, prevUsedIndex));
            } 
            else
            {
                region.AddToListField(KeyStore.SelectionIndicesListKey, new PointController(StartIndex, EndIndex));
                region.GetDataDocument().AddToListField(KeyStore.SelectionIndicesListKey, new PointController(StartIndex, EndIndex));
                for (var i = StartIndex; i <= EndIndex; i++)
                {
                    region.GetDataDocument().AddToListField(KeyStore.SelectionBoundsKey,
                            new RectController(ParentOverlay.TextSelectableElements[i].Bounds));
                }
            }

            region.AddToListField(KeyStore.SelectionRegionTopLeftKey, new PointController(0, YPos));

            return YPos;
        }

        private CoreCursor Arrow = new CoreCursor(CoreCursorType.Arrow, 1);
        private void LayoutRoot_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!this.IsLeftBtnPressed() && !this.IsRightBtnPressed())
            {
                Window.Current.CoreWindow.PointerCursor = Arrow;

                e.Handled = true;
            }
        }
    }
}
