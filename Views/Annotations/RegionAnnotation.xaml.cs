using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
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

        public RegionAnnotation(NewAnnotationOverlay parent, Selection selectionViewModel) :
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
                    var r = new Rectangle
                    {
                        Width = sizeList[i].Data.X,
                        Height = sizeList[i].Data.Y,
                        DataContext = selectionViewModel,
                        IsDoubleTapEnabled = false
                    };
                    RenderSubRegion(posList[i].Data, PlacementMode.Top, r, selectionViewModel);
                }
            }
        }

        private void RenderSubRegion(Point pos, PlacementMode mode, Shape r, Selection vm)
        {
            r.Stroke = new SolidColorBrush(Colors.Black);
            r.StrokeThickness = 2;
            r.StrokeDashArray = new DoubleCollection {2};
            InitializeAnnotationObject(r, pos, mode);
            LayoutRoot.Children.Add(r);
        }

        public override void StartAnnotation(Point p)
        {
            _previewStartPoint = p;
            Canvas.SetLeft(ParentOverlay.XPreviewRect, p.X);
            Canvas.SetTop(ParentOverlay.XPreviewRect, p.Y);
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

        Rectangle XRegionRect;

        public override void EndAnnotation(Point p)
        {
            XRegionRect = new Rectangle();
            XRegionRect.StrokeThickness = 2;
            XRegionRect.StrokeDashArray = new DoubleCollection();
            XRegionRect.StrokeDashArray.Add(2);
            XRegionRect.Fill = ParentOverlay.XPreviewRect.Fill;
            XRegionRect.Opacity = ParentOverlay.XPreviewRect.Opacity;
            XRegionRect.Stroke = new SolidColorBrush(Colors.Black);
            XRegionRect.Width = ParentOverlay.XPreviewRect.Width;
            XRegionRect.Height = ParentOverlay.XPreviewRect.Height;
            Canvas.SetLeft(XRegionRect, ParentOverlay.XPreviewRect.GetBoundingRect(ParentOverlay).Left);
            Canvas.SetTop(XRegionRect, ParentOverlay.XPreviewRect.GetBoundingRect(ParentOverlay).Top);

            if (ParentOverlay.XPreviewRect.Width > 4 && ParentOverlay.XPreviewRect.Height > 4)
            {
                ParentOverlay.XAnnotationCanvas.Children.Add(XRegionRect);
                ParentOverlay.CurrentAnchorableAnnotations.Add(this);
            }
        }
        public override double AddToRegion(DocumentController region)
        {
            region.AddToListField(KeyStore.SelectionRegionTopLeftKey, new PointController(Canvas.GetLeft(XRegionRect), Canvas.GetTop(XRegionRect)));
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