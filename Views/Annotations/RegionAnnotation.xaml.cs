using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class RegionAnnotation
    {
        private Point            _previewStartPoint;
        private FrameworkElement _regionPreviewGeometry;
        private bool             _isRightToLeft = false;

        public RegionAnnotation(AnnotationOverlay parent, Selection selectionViewModel) :
            base(parent, selectionViewModel?.RegionDocument)
        {
            this.InitializeComponent();

            DataContext = selectionViewModel;

            AnnotationType = AnnotationType.Region;

            if (selectionViewModel != null)
            {
                var posList = RegionDocumentController.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey);
                var sizeList = RegionDocumentController.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionRegionSizeKey);

                Debug.Assert(posList.Count == sizeList.Count);

                for (var i = 0; i < posList.Count; ++i)
                {
                    RenderSubRegion(posList[i].Data, sizeList[i].Data.X, sizeList[i].Data.Y, PlacementMode.Top, selectionViewModel);
                }
            }
        }

        public override bool IsInView(Rect bounds)
        {
            foreach (var r in LayoutRoot.Children)
            {
                if (r is Rectangle annotationRect)
                {
                    var annotationBounds = annotationRect.GetBoundingRect(this);
                    annotationBounds.Intersect(bounds);
                    if (!annotationBounds.IsEmpty)
                        return true;
                }
            }

            return false;
        }

        private void RenderSubRegion(Point pos, double width, double height, PlacementMode mode, Selection vm)
        {
            var geometry = makeRegionPreview(width < 0, Math.Abs(width), height);
            InitializeAnnotationObject(geometry, pos, mode);
            geometry.IsHitTestVisible = true;
            LayoutRoot.Children.Add(geometry);
        }

        public override void StartAnnotation(Point p)
        {
            _previewStartPoint = p;
            ParentOverlay.XPreviewRect.RenderTransform = new TranslateTransform() { X = p.X, Y = p.Y};
            ParentOverlay.XPreviewRect.Width = 0;
            ParentOverlay.XPreviewRect.Height = 0;
            ParentOverlay.XPreviewRect.Visibility = Visibility.Visible;
            ParentOverlay.XPreviewRect.Children.Remove(_regionPreviewGeometry);
        }

        public override void UpdateAnnotation(Point p)
        {
            ParentOverlay.XPreviewRect.Width  = Math.Abs(_previewStartPoint.X - p.X);
            ParentOverlay.XPreviewRect.Height = Math.Abs(_previewStartPoint.Y - p.Y);
            ParentOverlay.XPreviewRect.Children.Remove(_regionPreviewGeometry);
            _isRightToLeft = p.X < _previewStartPoint.X;
            if (_isRightToLeft)
            {
                (ParentOverlay.XPreviewRect.RenderTransform as TranslateTransform).X = p.X;
            }
            if (p.Y < _previewStartPoint.Y)
            {
                (ParentOverlay.XPreviewRect.RenderTransform as TranslateTransform).Y = p.Y;
            }

            if (ParentOverlay.XPreviewRect.Width > 4 && ParentOverlay.XPreviewRect.Height > 4)
            {
                _regionPreviewGeometry = makeRegionPreview(_isRightToLeft, ParentOverlay.XPreviewRect.Width, ParentOverlay.XPreviewRect.Height);
                ParentOverlay.XPreviewRect.Children.Add(_regionPreviewGeometry);
            }
        }

        public override void EndAnnotation(Point p)
        {
            ParentOverlay.XPreviewRect.Children.Remove(_regionPreviewGeometry);

            if (ParentOverlay.XPreviewRect.Width > 4 && ParentOverlay.XPreviewRect.Height > 4)
            {
                _isRightToLeft = p.X < _previewStartPoint.X;
                _regionPreviewGeometry = makeRegionPreview(_isRightToLeft, ParentOverlay.XPreviewRect.Width, ParentOverlay.XPreviewRect.Height);
                _regionPreviewGeometry.RenderTransform = ParentOverlay.XPreviewRect.RenderTransform;
                ParentOverlay.XAnnotationCanvas.Children.Add(_regionPreviewGeometry);
                ParentOverlay.CurrentAnchorableAnnotations.Add(this);
            } 
        }
        public override double AddToRegion(DocumentController region)
        {
            region.AddToListField(KeyStore.SelectionRegionTopLeftKey, new PointController((_regionPreviewGeometry.RenderTransform as TranslateTransform).X, (_regionPreviewGeometry.RenderTransform as TranslateTransform).Y));
            region.AddToListField(KeyStore.SelectionRegionSizeKey,    new PointController(_regionPreviewGeometry.Width * (_isRightToLeft ? -1 : 1), _regionPreviewGeometry.Height));

            return _previewStartPoint.Y;
        }

        private FrameworkElement makeRegionPreview(bool flip, double width, double height)
        {
            FrameworkElement geometry = null;
            if (width < 50)
            {
                var y = new Path()
                {
                    StrokeThickness = 2,
                    Stroke = new SolidColorBrush(Colors.Black),
                };
                var pf = new PathFigure() { StartPoint = new Point(flip ? width : 0, 0) };
                var fc = new PathFigureCollection();
                fc.Add(pf);
                y.Data = new PathGeometry() { Figures = fc };
                var bs = new BezierSegment();
                bs.Point1 = new Point(flip ? 0 : width, 0);
                bs.Point2 = new Point(flip ? width : 0, height / 2);
                bs.Point3 = new Point(flip ? 0 : width, height / 2);
                pf.Segments.Add(bs);
                var bs2 = new BezierSegment();
                bs2.Point1 = new Point(flip ? width : 0, height / 2);
                bs2.Point2 = new Point(flip ? 0 : width, height);
                bs2.Point3 = new Point(flip ? width : 0, height);
                pf.Segments.Add(bs2);
                pf.IsClosed = false;
                var g = new Grid();
                g.Children.Add(y);
                g.Background = new SolidColorBrush(Colors.Transparent);
                geometry = g;
            }
            else
            {
                var r = new Rectangle
                {
                    Fill = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xff, 0)),
                    Opacity = ParentOverlay.XPreviewRect.Opacity,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 2 },
                    Stroke = new SolidColorBrush(Colors.Black),
                };
                geometry = r;
            }
            geometry.IsHitTestVisible = false;
            geometry.Width = width;
            geometry.Height = height;
            geometry.HorizontalAlignment = HorizontalAlignment.Left;
            geometry.VerticalAlignment = VerticalAlignment.Top;
            return geometry;
        }

        private void LayoutRoot_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 49);
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
