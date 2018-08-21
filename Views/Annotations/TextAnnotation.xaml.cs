using System;
using System.Collections.Generic;
using System.Diagnostics;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TextAnnotation : UserControl, IAnchorable
    {
        public DocumentController DocumentController { get; set; }
        private int[] _index = new int[2];
        private Point? _selectionStartPoint;
        private NewAnnotationOverlay _parentOverlay;

        public TextAnnotation(NewAnnotationOverlay parent)
        {
            this.InitializeComponent();
            _parentOverlay = parent;
        }

        public void Render()
        {
            if (DocumentController.GetField(KeyStore.PDFSubregionKey) == null)
            {
                var currentSelections = DocumentController.GetField<ListController<PointController>>(KeyStore.SelectionIndicesListKey);

                var indices = new List<int>();
                double minRegionY = double.PositiveInfinity;
                foreach (PointController selection in currentSelections)
                {
                    for (double i = selection.Data.X; i <= selection.Data.Y; i++)
                    {
                        if (!indices.Contains((int)i)) indices.Add((int)i);
                    }
                }

                var subRegionsOffsets = new List<double>();
                int prevIndex = -1;
                foreach (int index in indices)
                {
                    SelectableElement elem = _parentOverlay.TextSelectableElements[index];
                    if (prevIndex + 1 != index)
                    {
                        var pdfView = this.GetFirstAncestorOfType<PdfView>();
                        double scale = pdfView.Width / pdfView.PdfMaxWidth;
                        double vOffset = elem.Bounds.Y * scale;
                        double scrollRatio = vOffset / pdfView.TopScrollViewer.ExtentHeight;
                        subRegionsOffsets.Add(scrollRatio);
                    }
                    minRegionY = Math.Min(minRegionY, elem.Bounds.Y);
                    prevIndex = index;
                }

                if ((this.GetFirstAncestorOfType<PdfView>()) != null)
                {
                    DocumentController.SetField(KeyStore.PDFSubregionKey, new ListController<NumberController>(subRegionsOffsets.ConvertAll(i => new NumberController(i))), true);
                }
            }

            HelpRenderRegion();
        }

        public  void StartAnnotation(Point p)
        {
            if (!this.IsCtrlPressed())
            {
                if (_parentOverlay.CurrentSelections.Any() || _parentOverlay.RegionRectangles.Any())
                {
                    _parentOverlay.ClearSelection();
                }
            }
            // _currentSelections.Add(new KeyValuePair<int, int>(-1, -1));
            _selectionStartPoint = p;
        }

        public void UpdateAnnotation(Point p)
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

                _index[0] = startEle.Index;
                var currentEle = GetClosestElementInDirection(p, new Point(-dir.X, -dir.Y));
                if (currentEle == null)
                {
                    return;
                }

                _index[1] = currentEle.Index;
                _parentOverlay.SelectElements(Math.Min(startEle.Index, currentEle.Index), Math.Max(startEle.Index, currentEle.Index), _selectionStartPoint ?? new Point(), p);
            }
        }

        private SelectableElement GetClosestElementInDirection(Point p, Point dir)
        {
            SelectableElement ele = null;
            double closestDist = double.PositiveInfinity;
            foreach (var selectableElement in _parentOverlay.TextSelectableElements)
            {
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

        public  void EndAnnotation(Point p)
        {
            if (!_parentOverlay.CurrentSelections.Any() || _parentOverlay.CurrentSelections.Last().Key == -1) return;//Not currently selecting anything
            _selectionStartPoint = null;
            _parentOverlay.AnchorableAnnotations.Add(this);
        }

        private void HelpRenderRegion()
        {
            var posList = DocumentController.GetField<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey);
            var sizeList = DocumentController.GetField<ListController<PointController>>(KeyStore.SelectionRegionSizeKey);
            var indexList = DocumentController.GetField<ListController<PointController>>(KeyStore.SelectionIndicesListKey);

            Debug.Assert(posList.Count == sizeList.Count);

            var vm = new NewAnnotationOverlay.SelectionViewModel(DocumentController, new SolidColorBrush(Color.FromArgb(0x30, 0xff, 0, 0)), new SolidColorBrush(Color.FromArgb(100, 0xff, 0xff, 0)));

            for (var i = 0; i < posList.Count; ++i)
            {
                var r = new Rectangle
                {
                    Width = sizeList[i].Data.X,
                    Height = sizeList[i].Data.Y,
                    Fill = vm.UnselectedBrush,
                    DataContext = vm,
                    IsDoubleTapEnabled = false
                };
                RenderSubRegion(posList[i].Data, PlacementMode.Bottom, r, vm);
            }

            if (_parentOverlay.TextSelectableElements != null && indexList.Any())
            {
                var geometryGroup = new GeometryGroup();
                var topLeft = new Point(double.MaxValue, double.MaxValue);
                RectangleGeometry lastRect = null;
                foreach (var t in indexList)
                {
                    var range = t.Data;
                    for (var ind = (int)range.X; ind <= (int)range.Y; ind++)
                    {
                        var rect = _parentOverlay.TextSelectableElements[ind].Bounds;
                        topLeft.X = Math.Min(topLeft.X, rect.Left);
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
                    DataContext = vm,
                    IsDoubleTapEnabled = false,
                    Fill = vm.UnselectedBrush
                };
                RenderSubRegion(topLeft, PlacementMode.Mouse, path, vm);
            }

            _parentOverlay.Regions.Add(vm);
        }

        private void RenderSubRegion(Point pos, PlacementMode mode, Shape r, NewAnnotationOverlay.SelectionViewModel vm)
        {
            r.SetBinding(Shape.FillProperty, new Binding
            {
                Path = new PropertyPath(nameof(vm.SelectionColor)),
                Mode = BindingMode.OneWay
            });
            Canvas.SetLeft(r, pos.X);
            Canvas.SetTop(r, pos.Y);
            r.Tapped += (sender, args) =>
            {
                if (this.IsCtrlPressed() && this.IsAltPressed())
                {
                    _parentOverlay.XAnnotationCanvas.Children.Remove(r);
                }
                _parentOverlay.SelectRegion(vm, args.GetPosition(this));
                args.Handled = true;
            };
            //TOOLTIP TO SHOW TAGS
            var tip = new ToolTip { Placement = mode };
            ToolTipService.SetToolTip(r, tip);
            r.PointerExited += (s, e) => tip.IsOpen = false;
            r.PointerEntered += (s, e) =>
            {
                tip.IsOpen = true;
                var regionDoc = vm.RegionDocument.GetDataDocument();

                var allLinkSets = new List<IEnumerable<DocumentController>>
                {
                     regionDoc.GetLinks(KeyStore.LinkFromKey)?.Select(l => l.GetDataDocument()) ?? new DocumentController[] { },
                     regionDoc.GetLinks(KeyStore.LinkToKey)?.Select(l => l.GetDataDocument()) ?? new DocumentController[] { }
                };
                var allTagSets = allLinkSets.SelectMany(lset => lset.Select(l => l.GetLinkTags()));
                var allTags = regionDoc.GetLinks(null).SelectMany((l) => l.GetDataDocument().GetLinkTags().Select((tag) => tag.Data));

                //update tag content based on current tags of region
                tip.Content = allTags.Where((t, i) => i > 0).Aggregate(allTags.FirstOrDefault(), (input, str) => input += ", " + str);
            };
            r.SetBinding(VisibilityProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(NewAnnotationOverlay.AnnotationVisibility)),
                Converter = new BoolToVisibilityConverter()
            });

            _parentOverlay.FormatRegionOptionsFlyout(vm.RegionDocument, r);
            _parentOverlay.XAnnotationCanvas.Children.Add(r);
        }

        public double AddSubregionToRegion(DocumentController region)
        {
            var selection =
                _parentOverlay.CurrentSelections.Find(kvp => kvp.Key.Equals(_index[0]) && kvp.Value.Equals(_index[1]));
            var ind = _parentOverlay.CurrentSelections.IndexOf(selection);
            for (var i = selection.Key; i <= selection.Value; i++)
            {
                var elem = _parentOverlay.TextSelectableElements[i];
                if (_parentOverlay.CurrentSelectionClipRects[ind] == Rect.Empty || _parentOverlay.CurrentSelectionClipRects[ind]
                        .Contains(new Point(elem.Bounds.X + elem.Bounds.Width / 2,
                            elem.Bounds.Y + elem.Bounds.Height / 2)))
                {
                    // this will avoid double selecting any items
                    if (_parentOverlay.Indices.Contains(i))
                    {
                        _parentOverlay.Indices.Add(i);
                    }
                }
            }

            region.AddToListField(KeyStore.SelectionIndicesListKey, new PointController(selection.Key, selection.Value));

            var pdfView = this.GetFirstAncestorOfType<PdfView>();
            var scale = pdfView?.ActualWidth / pdfView?.PdfMaxWidth ?? 1;
            var vOffset = _parentOverlay.TextSelectableElements[selection.Key].Bounds.Y * scale;
            var scrollRatio = vOffset / pdfView.TopScrollViewer.ExtentHeight;
            return scrollRatio;
        }
    }
}
