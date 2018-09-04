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

        public RegionAnnotation(NewAnnotationOverlay parent, DocumentController documentController) : base(parent, documentController)
        {
            this.InitializeComponent();

            AnnotationType = AnnotationType.Region;
        }

        public override void Render(SelectionViewModel vm)
        {
            var posList = RegionDocumentController.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey);
            var sizeList = RegionDocumentController.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionRegionSizeKey);

            Debug.Assert(posList.Count == sizeList.Count);

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
        }

        private void RenderSubRegion(Point pos, PlacementMode mode, Shape r, SelectionViewModel vm)
        {
            r.Stroke = new SolidColorBrush(Colors.Black);
            r.StrokeThickness = 2;
            r.StrokeDashArray = new DoubleCollection {2};
            InitializeAnnotationObject(r, pos, mode);
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