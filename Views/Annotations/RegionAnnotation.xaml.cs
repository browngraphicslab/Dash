using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Path = Windows.UI.Xaml.Shapes.Path;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class RegionAnnotation : UserControl, IAnchorable
    {
        public DocumentController DocumentController { get; set; }
        private NewAnnotationOverlay _parentOverlay;
        private Point _previewStartPoint;

        public RegionAnnotation(NewAnnotationOverlay parent)
        {
            this.InitializeComponent();
            
            _parentOverlay = parent;
        }

        public  void Render()
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

        public  void StartAnnotation(Point p)
        {
            if (!this.IsCtrlPressed())
            {
                if (_parentOverlay.RegionRectangles.Any() || _parentOverlay.CurrentSelections.Any())
                {
                    _parentOverlay.ClearSelection();
                }
            }
            _previewStartPoint = p;
            Canvas.SetLeft(_parentOverlay.XPreviewRect, p.X);
            Canvas.SetTop(_parentOverlay.XPreviewRect, p.Y);
            Canvas.SetLeft(this, p.X);
            Canvas.SetTop(this, p.Y);
            _parentOverlay.XPreviewRect.Width = 0;
            _parentOverlay.XPreviewRect.Height = 0;
            _parentOverlay.XPreviewRect.Visibility = Visibility.Visible;
            if (!_parentOverlay.XAnnotationCanvas.Children.Contains(_parentOverlay.XPreviewRect))
            {
                _parentOverlay.XAnnotationCanvas.Children.Insert(0, _parentOverlay.XPreviewRect);
            }
            _parentOverlay.RegionRectangles.Add(new Rect(p.X, p.Y, 0, 0));
        }

        public  void UpdateAnnotation(Point p)
        {
            if (p.X < _previewStartPoint.X)
            {
                _parentOverlay.XPreviewRect.Width = _previewStartPoint.X - p.X;
                Canvas.SetLeft(_parentOverlay.XPreviewRect, p.X);
            }
            else
            {
                _parentOverlay.XPreviewRect.Width = p.X - _previewStartPoint.X;
            }

            if (p.Y < _previewStartPoint.Y)
            {
                _parentOverlay.XPreviewRect.Height = _previewStartPoint.Y - p.Y;
                Canvas.SetTop(_parentOverlay.XPreviewRect, p.Y);
            }
            else
            {
                _parentOverlay.XPreviewRect.Height = p.Y - _previewStartPoint.Y;
            }
            _parentOverlay.XPreviewRect.Visibility = Visibility.Visible;
        }

        public  void EndAnnotation(Point p)
        {
            if (_parentOverlay.RegionRectangles.Count > 0)
            {
                _parentOverlay.RegionRectangles[_parentOverlay.RegionRectangles.Count - 1] =
                    new Rect(Canvas.GetLeft(_parentOverlay.XPreviewRect), Canvas.GetTop(_parentOverlay.XPreviewRect), _parentOverlay.XPreviewRect.Width,
                        _parentOverlay.XPreviewRect.Height);
                Width = _parentOverlay.XPreviewRect.Width;
                Height = _parentOverlay.XPreviewRect.Height;

                if (_parentOverlay.RegionRectangles.Last().Width < 4 || _parentOverlay.RegionRectangles.Last().Height < 4)
                {
                    _parentOverlay.RegionRectangles.RemoveAt(_parentOverlay.RegionRectangles.Count - 1);
                    return;
                }
            }

            XRegionRect.Width = _parentOverlay.XPreviewRect.Width;
            XRegionRect.Height = _parentOverlay.XPreviewRect.Height;
            _parentOverlay.XAnnotationCanvas.Children.Add(this);
            Canvas.SetLeft(XRegionRect, Canvas.GetLeft(_parentOverlay.XPreviewRect));
            Canvas.SetTop(XRegionRect, Canvas.GetTop(_parentOverlay.XPreviewRect));
            _parentOverlay.AnchorableAnnotations.Add(this);
        }

        public double AddSubregionToRegion(DocumentController region)
        {
            region.AddToListField(KeyStore.SelectionRegionTopLeftKey, new PointController(Canvas.GetLeft(this), Canvas.GetTop(this)));
            region.AddToListField(KeyStore.SelectionRegionSizeKey, new PointController(XRegionRect.Width, XRegionRect.Height));
            var pdfView = this.GetFirstAncestorOfType<PdfView>();
            var scale = pdfView?.ActualWidth / pdfView?.PdfMaxWidth ?? 1;
            var vOffset = Canvas.GetTop(this) * scale;
            var scrollRatio = vOffset / pdfView?.TopScrollViewer.ExtentHeight ?? 0;
            Debug.Assert(!double.IsNaN(scrollRatio));
            return scrollRatio;
        }
    }
}