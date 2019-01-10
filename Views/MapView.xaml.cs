using DashShared;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Dash.Popups;
using Color = Windows.UI.Color;
using Point = Windows.Foundation.Point;
using System.Web;
using Windows.UI.Input;
using Windows.UI.Xaml.Media.Imaging;
using MyToolkit.Multimedia;
using Windows.Storage.Pickers;
using Dash.Converters;
using Dash.Popups.TemplatePopups;
using static Dash.DocumentController;
using Dash.Controllers.Functions.Operators;
using TemplateType = Dash.TemplateList.TemplateType;
using Windows.UI.Xaml.Data;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class MapView : UserControl
    {
        private DispatcherTimer _mapTimer = new DispatcherTimer() { Interval = new TimeSpan(0,0,1) };
        private DocumentView     mapView  => LayoutRoot.Children.FirstOrDefault() as DocumentView;

        public MapView()
        {
            InitializeComponent();
            _mapTimer.Tick += (s,e) => (mapView?.ViewModel.Content as CollectionView)?.FitContents();
        }

        public void SetVisibility(Visibility visibility, Grid splitPanel)
        {
            Visibility = visibility;
            if (visibility == Visibility.Collapsed)
            {
                splitPanel.RowDefinitions.Last().Height = new GridLength(0);
                LayoutRoot.Children.Clear();
                _mapTimer.Stop();
            }
            else
            {
                splitPanel.RowDefinitions.Last().Height = new GridLength(300);
                SetTarget(SplitFrame.ActiveFrame.ViewModel.LayoutDocument);
                _mapTimer.Start();
            }
        }
        public void SetTarget(DocumentController mainDocumentCollection)
        {
            if (Visibility == Visibility.Visible)
            {
                initializeMap(mainDocumentCollection);
            }
        }

        private void xLeftStack_OnSizeChanged(object sender, SizeChangedEventArgs e) { xLeftStackClip.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height); }

        private void initializeMap(DocumentController mainDocumentCollection)
        {
            var xMapDocumentView = mapView ?? createMap();

            var panPosRef        = new DocumentFieldReference(mainDocumentCollection, KeyStore.PanPositionKey);
            var panZoomRef       = new DocumentFieldReference(mainDocumentCollection, KeyStore.PanZoomKey);
            var panZoomRef2      = new DocumentFieldReference(xMapDocumentView.ViewModel.LayoutDocument, KeyStore.PanZoomKey);
            var panPosRef2       = new DocumentFieldReference(xMapDocumentView.ViewModel.LayoutDocument, KeyStore.PanPositionKey);
            var transformBinding = new FieldMultiBinding<TranslateTransform>(panPosRef, panZoomRef, panPosRef2, panZoomRef2)
            {
                Converter = new PointToMapConverter(xMapDocumentView),
                Mode = BindingMode.OneWay,
                FallbackValue = new TranslateTransform()
            };
            var bindingw = new FieldMultiBinding<double>(panPosRef, panZoomRef, panPosRef2, panZoomRef2)
            {
                Mode      = BindingMode.OneWay,
                Converter = new DimToMapConverter(xMapDocumentView, true),
                FallbackValue = MainPage.Instance.xCanvas.ActualWidth
            };
            var bindingh = new FieldMultiBinding<double>(panPosRef, panZoomRef, panPosRef2, panZoomRef2)
            {
                Mode      = BindingMode.OneWay,
                Converter = new DimToMapConverter(xMapDocumentView, false),
                FallbackValue = MainPage.Instance.xCanvas.ActualHeight
            };
            var viewRegionRect = LayoutRoot.Children.Last() as Grid;
            viewRegionRect.AddFieldBinding(FrameworkElement.RenderTransformProperty, transformBinding);
            viewRegionRect.AddFieldBinding(FrameworkElement.WidthProperty, bindingw);
            viewRegionRect.AddFieldBinding(FrameworkElement.HeightProperty, bindingh);
            xMapDocumentView.ViewModel.LayoutDocument.SetField(KeyStore.DocumentContextKey, mainDocumentCollection.GetDataDocument(), true);
        }

        private DocumentView createMap()
        {
            var xMap = RESTClient.Instance.Fields.GetController<DocumentController>("3D6910FE-54B0-496A-87E5-BE33FF5BB59C") ?? new CollectionNote(new Point(), CollectionViewType.Freeform).Document;
            FieldControllerBase.MakeRoot(xMap);
            xMap.SetFitToParent(true);
            xMap.SetWidth(double.NaN);
            xMap.SetHeight(double.NaN);
            xMap.SetHorizontalAlignment(HorizontalAlignment.Stretch);
            xMap.SetVerticalAlignment(VerticalAlignment.Stretch);
            xMap.SetField(KeyStore.DataKey, new PointerReferenceController(new DocumentReferenceController(xMap, KeyStore.DocumentContextKey), KeyStore.DataKey), true);
            var xMapDocumentView = new DocumentView() { DataContext = new DocumentViewModel(xMap) };
            Grid.SetColumn(xMapDocumentView, 2);
            Grid.SetRow(xMapDocumentView, 0);

            var mapActivateBtn = new Button() { Content = "^:", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var overlay        = new Grid() { Background =  new SolidColorBrush(Color.FromArgb(0x70, 0xff, 0xff, 0xff)) };
            overlay.AddHandler(UIElement.TappedEvent, new TappedEventHandler((s, e) =>
            {
                if (!mapActivateBtn.GetDescendants().Contains(e.OriginalSource))
                {
                    MainPage.Instance.JavaScriptHack.Focus(FocusState.Programmatic);
                    var mapViewCanvas = xMapDocumentView.GetFirstDescendantOfType<CollectionFreeformView>()?.GetItemsControl().GetFirstDescendantOfType<Canvas>();
                    var mapPt = e.GetPosition(mapViewCanvas);

                    var mainFreeform = SplitFrame.ActiveFrame.GetFirstDescendantOfType<CollectionFreeformView>();
                    var mainFreeFormCanvas = mainFreeform?.GetItemsControl().GetFirstDescendantOfType<Canvas>();
                    var mainFreeformXf = ((mainFreeFormCanvas?.RenderTransform ?? new MatrixTransform()) as MatrixTransform)?.Matrix ?? new Matrix();
                    var mainDocCenter = new Point(SplitFrame.ActiveFrame.ActualWidth / 2 / mainFreeformXf.M11, SplitFrame.ActiveFrame.ActualHeight / 2 / mainFreeformXf.M22);
                    var mainScale = new Point(mainFreeformXf.M11, mainFreeformXf.M22);
                    mainFreeform?.SetTransformAnimated(
                        new TranslateTransform() { X = -mapPt.X + SplitFrame.ActiveFrame.ActualWidth / 2, Y = -mapPt.Y + SplitFrame.ActiveFrame.ActualHeight / 2 },
                        new ScaleTransform { CenterX = mapPt.X, CenterY = mapPt.Y, ScaleX = mainScale.X, ScaleY = mainScale.Y });
                }
            }), true);
            overlay.Children.Add(mapActivateBtn);
            mapActivateBtn.Click += (s, e) => overlay.Background = overlay.Background == null ? new SolidColorBrush(Color.FromArgb(0x70, 0xff, 0xff, 0xff)) : null;
            Grid.SetColumn(overlay, 2);
            Grid.SetRow(overlay, 0);

            var rect  = new Grid() { Background =  new SolidColorBrush(Color.FromArgb(0x20, 0, 0, 0)), BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness =new Thickness(0.5)};
            rect.Width = rect.Height = 50;
            rect.HorizontalAlignment = HorizontalAlignment.Left;
            rect.VerticalAlignment = VerticalAlignment.Top;
            rect.IsHitTestVisible = false;

            LayoutRoot.Children.Add(xMapDocumentView);
            LayoutRoot.Children.Add(overlay);
            LayoutRoot.Children.Add(rect);
            return xMapDocumentView;
        }

        private class PointToMapConverter : SafeDataToXamlConverter<List<object>, TranslateTransform>
        {
            private DocumentView _mapView;
            public override TranslateTransform ConvertDataToXaml(List<object> data, object parameter = null)
            {
                var srcMapPt   = (Point) data[0];
                var srcMapZoom = (Point) data[1];
                var mapPan     = _mapView.ViewModel.LayoutDocument.GetField<PointController>(KeyStore.PanPositionKey)?.Data ?? new Point();
                var mapZoom    = _mapView.ViewModel.LayoutDocument.GetField<PointController>(KeyStore.PanZoomKey)?.Data ?? new Point(1,1);
                return new TranslateTransform
                {
                    X = mapPan.X - srcMapPt.X * mapZoom.X / srcMapZoom.X,
                    Y = mapPan.Y - srcMapPt.Y * mapZoom.Y / srcMapZoom.Y
                };
            }
            public override List<object> ConvertXamlToData(TranslateTransform xaml, object parameter = null) { throw new NotImplementedException(); }
            public PointToMapConverter(DocumentView mapView) { _mapView = mapView; }
        }
        private class DimToMapConverter : SafeDataToXamlConverter<List<object>, double>
        {
            private DocumentView _mapView;
            private bool         _isWidth = false;
            public override double ConvertDataToXaml(List<object> data, object parameter = null)
            {
                var mapSize = (Point)data[1];
                var mapZoom = _mapView.ViewModel.LayoutDocument.GetField<PointController>(KeyStore.PanZoomKey)?.Data ?? new Point(1,1);
                return _isWidth ? MainPage.Instance.xCanvas.ActualWidth * mapZoom.X / mapSize.X : MainPage.Instance.xCanvas.ActualHeight * mapZoom.Y / mapSize.Y;
            }
            public override List<object> ConvertXamlToData(double xaml, object parameter = null) { throw new NotImplementedException(); }
            public DimToMapConverter(DocumentView mapView, bool width)
            {
                _isWidth = width;
                _mapView = mapView;
            }
        }
    }
}
