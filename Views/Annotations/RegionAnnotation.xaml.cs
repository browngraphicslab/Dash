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
        private Point _previewStartPoint;

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
                    RenderSubRegion(posList[i].Data, new Size(sizeList[i].Data.X, sizeList[i].Data.Y),PlacementMode.Top, selectionViewModel);
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

        private void RenderSubRegion(Point pos, Size size, PlacementMode mode, Selection vm)
        {
           
            if (size.Width < 50)
            {
                var y = new Path();
            y.StrokeThickness = 0.2;
            y.SetBinding(Path.StrokeProperty, ViewModel.GetFillBinding());
            y.Stroke = new SolidColorBrush(Colors.Black);
            var pf = new PathFigure() { StartPoint = new Point() };
            var fc = new PathFigureCollection();
            fc.Add(pf);
                y.Data = new PathGeometry() { Figures = fc };
                var bs = new BezierSegment();
                bs.Point1 = new Point(size.Width, 0);
                bs.Point2 = new Point(0, size.Height / 2);
                bs.Point3 = new Point(size.Width, size.Height / 2);
                pf.Segments.Add(bs);
                var bs2 = new BezierSegment();
                bs2.Point1 = new Point(0, size.Height / 2);
                bs2.Point2 = new Point(size.Width, size.Height);
                bs2.Point3 = new Point(0, size.Height);
                pf.Segments.Add(bs2);
                pf.IsClosed = false;
                InitializeAnnotationObject(y, pos, mode);
                LayoutRoot.Children.Add(y);
                y.HorizontalAlignment = HorizontalAlignment.Left;
                y.VerticalAlignment = VerticalAlignment.Top;
                y.Fill = new SolidColorBrush(Colors.Transparent);
            }
            else
            {
                var r = new Rectangle
                {
                    Width = size.Width,
                    Height = size.Height,
                    DataContext = vm,
                    IsDoubleTapEnabled = false
                };
                r.Stroke = new SolidColorBrush(Colors.Black);
                r.StrokeThickness = 0.5;
                r.StrokeDashArray = new DoubleCollection { 2 };
                r.HorizontalAlignment = HorizontalAlignment.Left;
                r.VerticalAlignment = VerticalAlignment.Top;
                InitializeAnnotationObject(r, pos, mode);
                LayoutRoot.Children.Add(r);
            }
        }

        public override void StartAnnotation(Point p)
        {
            _previewStartPoint = p;
            ParentOverlay.XPreviewRect.RenderTransform = new TranslateTransform
            {
                X = p.X,
                Y = p.Y
            };
            Debug.WriteLine("start" + p.X + " " + p.Y);
            XPos = p.X;
            YPos = p.Y;
            ParentOverlay.XPreviewRect.Width = 0;
            ParentOverlay.XPreviewRect.Height = 0;
            ParentOverlay.XPreviewRect.Visibility = Visibility.Visible;
            if (!ParentOverlay.XAnnotationCanvas.Children.Contains(ParentOverlay.XPreviewRect))
            {
                ParentOverlay.XAnnotationCanvas.Children.Insert(0, ParentOverlay.XPreviewRect);
            }
        }

        public override void UpdateAnnotation(Point p)
        {
            if (p.X < _previewStartPoint.X)
            {
                ParentOverlay.XPreviewRect.Width = _previewStartPoint.X - p.X;
                (ParentOverlay.XPreviewRect.RenderTransform as TranslateTransform).X = p.X;
            }
            else
            {
                ParentOverlay.XPreviewRect.Width = p.X - _previewStartPoint.X;
            }

            if (p.Y < _previewStartPoint.Y)
            {
                ParentOverlay.XPreviewRect.Height = _previewStartPoint.Y - p.Y;
                (ParentOverlay.XPreviewRect.RenderTransform as TranslateTransform).Y = p.Y;
            }
            else
            {
                ParentOverlay.XPreviewRect.Height = p.Y - _previewStartPoint.Y;
            }
            ParentOverlay.XPreviewRect.Visibility = Visibility.Visible;
        }

        Rectangle XRegionRect;

        public override void EndAnnotation(Point p)
        {
            XRegionRect = new Rectangle
            {
                StrokeThickness = 0.5,
                StrokeDashArray = new DoubleCollection { 2 },
                Fill = ParentOverlay.XPreviewRect.Fill,
                Opacity = ParentOverlay.XPreviewRect.Opacity,
                Stroke = new SolidColorBrush(Colors.Black),
                Width = ParentOverlay.XPreviewRect.Width,
                Height = ParentOverlay.XPreviewRect.Height,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                RenderTransform = ParentOverlay.XPreviewRect.RenderTransform
            };

            if (ParentOverlay.XPreviewRect.Width > 4 && ParentOverlay.XPreviewRect.Height > 4)
            {
                ParentOverlay.XAnnotationCanvas.Children.Add(XRegionRect);
                ParentOverlay.CurrentAnchorableAnnotations.Add(this);
            }
        }
        public override double AddToRegion(DocumentController region)
        {
            region.AddToListField(KeyStore.SelectionRegionTopLeftKey, new PointController((XRegionRect.RenderTransform as TranslateTransform).X, (XRegionRect.RenderTransform as TranslateTransform).Y));
            region.AddToListField(KeyStore.SelectionRegionSizeKey,    new PointController(XRegionRect.Width, XRegionRect.Height));

            return YPos;
        }
        
        private void LayoutRoot_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 49);
        }

        CoreCursor Arrow = new CoreCursor(CoreCursorType.Arrow, 1);
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
