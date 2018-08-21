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
    public sealed partial class RegionAnnotation
    {
        private Point _previewStartPoint;

        public RegionAnnotation(NewAnnotationOverlay parent) : base(parent)
        {
            this.InitializeComponent();
        }

        public override void Render()
        {
            var posList = DocumentController.GetField<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey);
            var sizeList = DocumentController.GetField<ListController<PointController>>(KeyStore.SelectionRegionSizeKey);
            var indexList = DocumentController.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionIndicesListKey);

            Debug.Assert(posList.Count == sizeList.Count);

            var vm = new SelectionViewModel(DocumentController, new SolidColorBrush(Color.FromArgb(0x30, 0xff, 0, 0)), new SolidColorBrush(Color.FromArgb(100, 0xff, 0xff, 0)));

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

            if (ParentOverlay.TextSelectableElements != null && indexList.Any())
            {
                var geometryGroup = new GeometryGroup();
                var topLeft = new Point(double.MaxValue, double.MaxValue);
                RectangleGeometry lastRect = null;
                foreach (var t in indexList)
                {
                    var range = t.Data;
                    for (var ind = (int)range.X; ind <= (int)range.Y; ind++)
                    {
                        var rect = ParentOverlay.TextSelectableElements[ind].Bounds;
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
                    rect.Rect = new Rect(new Point(rect.Rect.X - topLeft.X, rect.Rect.Y - topLeft.Y),
                        new Size(rect.Rect.Width, rect.Rect.Height));
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

            ParentOverlay.Regions.Add(vm);
        }

        private void RenderSubRegion(Point pos, PlacementMode mode, Shape r, SelectionViewModel vm)
        {
            r.SetBinding(Shape.FillProperty, new Binding
            {
                Path = new PropertyPath(nameof(vm.SelectionColor)),
                Mode = BindingMode.OneWay
            });
            r.Stroke = new SolidColorBrush(Colors.Black);
            r.StrokeThickness = 2;
            r.StrokeDashArray = new DoubleCollection {2};
            InitializeAnnotationObject(r, pos, mode, vm);
        }

        public override void StartAnnotation(Point p)
        {
            if (!this.IsCtrlPressed())
            {
                if (ParentOverlay.CurrentAnchorableAnnotations.Any())
                {
                    ParentOverlay.ClearSelection();
                }
            }
            _previewStartPoint = p;
            Canvas.SetLeft(ParentOverlay.XPreviewRect, p.X);
            Canvas.SetTop(ParentOverlay.XPreviewRect, p.Y);
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
                Canvas.SetLeft(ParentOverlay.XPreviewRect, p.X);
            }
            else
            {
                ParentOverlay.XPreviewRect.Width = p.X - _previewStartPoint.X;
            }

            if (p.Y < _previewStartPoint.Y)
            {
                ParentOverlay.XPreviewRect.Height = _previewStartPoint.Y - p.Y;
                Canvas.SetTop(ParentOverlay.XPreviewRect, p.Y);
            }
            else
            {
                ParentOverlay.XPreviewRect.Height = p.Y - _previewStartPoint.Y;
            }
            ParentOverlay.XPreviewRect.Visibility = Visibility.Visible;
        }

        public override void EndAnnotation(Point p)
        {
            Canvas.SetLeft(this, XPos);
            Canvas.SetTop(this, YPos);
            Width = ParentOverlay.XPreviewRect.Width;
            Height = ParentOverlay.XPreviewRect.Height;

            if (Width < 4 || Height < 4)
            {
                return;
            }

            XRegionRect.Width = ParentOverlay.XPreviewRect.Width;
            XRegionRect.Height = ParentOverlay.XPreviewRect.Height;
            ParentOverlay.XAnnotationCanvas.Children.Add(this);
            ParentOverlay.CurrentAnchorableAnnotations.Add(this);
        }

        public override double AddSubregionToRegion(DocumentController region)
        {
            region.AddToListField(KeyStore.SelectionRegionTopLeftKey, new PointController(Canvas.GetLeft(this), Canvas.GetTop(this)));
            region.AddToListField(KeyStore.SelectionRegionSizeKey,
                new PointController(XRegionRect.Width, XRegionRect.Height));

            return YPos;
        }
    }
}