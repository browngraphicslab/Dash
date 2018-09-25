﻿using System.Diagnostics;
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
            r.HorizontalAlignment = HorizontalAlignment.Left;
            r.VerticalAlignment = VerticalAlignment.Top;
            InitializeAnnotationObject(r, pos, mode);
            LayoutRoot.Children.Add(r);
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
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection {2},
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
