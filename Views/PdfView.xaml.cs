using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Syncfusion.Windows.PdfViewer;
using Color = Windows.UI.Color;
using PdfReader = iText.Kernel.Pdf.PdfReader;
using Point = Windows.Foundation.Point;
using Size = Windows.Foundation.Size;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class PdfView : IVisualAnnotatable
    {
        /// <summary>
        /// The pdf viewer from xaml
        /// </summary>
        public SfPdfViewerControl Pdf => xPdfView;
        private VisualAnnotationManager _annotationManager;
        private List<PDFRegionMarker>   _markers = new List<PDFRegionMarker>();
        private ScrollViewer            _internalViewer;
        AnnotationOverlay               xAnnotations = new AnnotationOverlay();

        // events to communicate with the VisualAnnotationManager
        public event PointerEventHandler NewRegionStarted;
        public event PointerEventHandler NewRegionMoved;
        public event PointerEventHandler NewRegionEnded;
        
        public double ZoomFactor => xPdfView.Zoom / 100.0;
        private Size pdfNativeSize
        {
            get
            {
                var size = DataDocument.GetActualSize() ?? new Point();
                return new Size(size.X, size.Y);
            }
        }

        private Canvas pdfNativeCanvas => xPdfView.GetDescendantsOfType<Canvas>().FirstOrDefault(d => d.Name == "PdfDocumentPanel");
        
        public Canvas RegionsPanel
        {
            get
            {
                var ppar = pdfNativeCanvas.Parent as Panel;
                return ppar.Children.Count > 2 ? ppar.Children.Last() as Canvas : null;
            }
        }

        public double VerticalOffset
        {
            get => NativeOffset * ZoomFactor;
            set
            {
                GetInternalScrollViewer().ChangeView(null, value, null);
                LayoutDocument.SetField<NumberController>(KeyStore.PdfVOffsetFieldKey, value / ZoomFactor, true);
            }
        }
        public double NativeOffset => LayoutDocument.GetDereferencedField<NumberController>(KeyStore.PdfVOffsetFieldKey, null)?.Data ?? 0;
        public PdfView()
        {
            InitializeComponent();
            SetupProgressRing();
            // disable thumbnails on the pdf
            xPdfView.IsThumbnailViewEnabled = false;
            xPdfView.Loaded += (sender, e) =>
            {
                pdfNativeCanvas.SizeChanged += PdfView_SizeChanged;
                pdfNativeCanvas.LayoutUpdated += PdfView_LayoutUpdated;
                _annotationManager = new VisualAnnotationManager(this, DataDocument, xAnnotations);
                _annotationManager.NewRegionMade += OnNewRegionMade;
                _annotationManager.RegionRemoved += OnRegionRemoved;
                xAnnotations.PointerPressed += XAnnotations_PointerPressed;
                xAnnotations.PointerMoved += XAnnotations_PointerMoved;
                xAnnotations.PointerReleased += XAnnotations_PointerReleased;
                var doc = DataContext as DocumentController;
                var curOffset = doc.GetDereferencedField<NumberController>(KeyStore.PdfVOffsetFieldKey, null)?.Data;
                GetInternalScrollViewer().ChangeView(null, curOffset ?? 0.0, null);
                xPdfView.GetFirstDescendantOfType<ScrollViewer>().Margin = new Thickness(0);

                //PdfTest();
            };
        }

        private class BoundsTextExtractionStrategy : LocationTextExtractionStrategy
        {
            private Canvas c;
            private Size _canvasSize, _pageSize;
            private double _pageGap;
            public BoundsTextExtractionStrategy(Canvas c, Size canvasSize, Size pageSize, double pageGap)
            {
                this.c = c;
                _canvasSize = canvasSize;
                _pageSize = pageSize;
            }

            public override void EventOccurred(IEventData data, EventType type)
            {
                base.EventOccurred(data, type);
                if (type == EventType.RENDER_TEXT)
                {
                    var blockData = data as TextRenderInfo;
                    foreach (var textData in blockData.GetCharacterRenderInfos())
                    {
                        var rect = new Windows.UI.Xaml.Shapes.Rectangle
                        {
                            Width = 1,
                            Height = 1
                        };
                        var start = textData.GetDescentLine().GetStartPoint();
                        var end = textData.GetAscentLine().GetEndPoint();

                        double xScale = _canvasSize.Width / _pageSize.Width;
                        double yScale = _canvasSize.Height / _pageSize.Height;
                        var mat = new Matrix
                        {
                            M11 = (end.Get(0) - start.Get(0)) * xScale,
                            M22 = (end.Get(1) - start.Get(1)) * yScale,
                            OffsetX = start.Get(0) * xScale,
                            OffsetY = _pageGap + (_pageSize.Height - end.Get(1)) * yScale
                        };
                        rect.RenderTransform = new MatrixTransform { Matrix = mat };
                        Canvas.SetZIndex(rect, 500);
                        rect.Fill = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
                        c.Children.Add(rect);
                    }
                }
            }
        }

        private void PdfTest()
        {
            var pdfUri = DataDocument.GetField<ImageController>(KeyStore.DataKey).Data;

            PdfDocument pdfDocument =
                new PdfDocument(new PdfReader(UriToStreamConverter.Instance.ConvertDataToXaml(pdfUri)));
            var s = pdfDocument.GetDefaultPageSize();
            var strategy = new BoundsTextExtractionStrategy(pdfNativeCanvas, pdfNativeSize, new Size(s.GetWidth(), s.GetHeight()), xPdfView.PageGap);
            var processsor = new PdfCanvasProcessor(strategy);
            processsor.ProcessPageContent(pdfDocument.GetFirstPage());
        }

        private void OnNewRegionMade(object sender, RegionEventArgs e)
        {
            MakeRegionMarker(_internalViewer.VerticalOffset, e.Link);
        }

        private void OnRegionRemoved(object sender, RegionEventArgs e)
        {
            foreach (var child in xAnnotationMarkers.Children.ToList())
            {
                if (child is PDFRegionMarker box)
                {
                    if (box.LinkTo.Equals(e.Link))
                    {
                        xAnnotationMarkers.Children.Remove(child);
                        _markers.Remove(box);

                        if (_markers.Count == 0)
                        {
                            xAnnotationMarkers.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
        }
        
        private void PdfView_LayoutUpdated(object sender, object e)
        {
            var pdfDocPanel = pdfNativeCanvas;
            if (pdfDocPanel.DesiredSize.Width > 0)
            {
                pdfDocPanel.LayoutUpdated -= PdfView_LayoutUpdated;
                updateInitialLayout(pdfDocPanel);
            }
        }

        private void PdfView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var pdfDocPanel = pdfNativeCanvas;
            if (pdfDocPanel.DesiredSize.Width > 0)
            {
                pdfDocPanel.SizeChanged -= PdfView_SizeChanged;
                updateInitialLayout(pdfDocPanel);
            }
        }

        void updateInitialLayout(Canvas pdfDocPanel)
        {
            if (DataDocument.GetActualSize() == null)
            {
                //var offsetCollection = xPdfView.PageOffsetCollection;
                //offsetCollection.TryGetValue(xPdfView.PageCount, out var endOffset);
                this.DataDocument.SetActualSize(new Windows.Foundation.Point(pdfDocPanel.DesiredSize.Width, pdfDocPanel.DesiredSize.Height));
            }
            if (RegionsPanel == null)
            {
                var ppar = pdfDocPanel.Parent as Grid;
                var pchild = new Canvas();
                ppar.Children.Add(pchild);
                pchild.Children.Add(xAnnotations);
            }
            UnFreeze();
             
            var dataRegions = DataDocument.GetDataDocument()
                .GetField<ListController<DocumentController>>(KeyStore.RegionsKey);
            if (dataRegions != null)
            {
                // the VisualAnnotationManager will take care of the regioning, but here we need to put on the side markers on
                xAnnotations.Height = pdfNativeSize.Height;
                foreach (var region in dataRegions.TypedData)
                {
                    var offset = region.GetDataDocument().GetField<NumberController>(KeyStore.PdfRegionVerticalOffsetKey).Data;
                    MakeRegionMarker(offset, region);
                }
            }
        }

        /// <summary>
        /// Setup a progress ring used while the pdf is loading
        /// </summary>
        private void SetupProgressRing()
        {
            var progressRing = new ProgressRing()
            {
                Foreground = App.Instance.Resources["TranslucentWindowsBlue"] as SolidColorBrush,
                Width = 100,
                Height = 100
            };
            xPdfView.PdfProgressRing = progressRing;
        }
        public VisualAnnotationManager GetAnnotationManager()
        {
            return _annotationManager;
        }

        public DocumentController LayoutDocument { get; set; }
        public DocumentController DataDocument   { get; set; }
        public DocumentController GetRegionDocument()
        {
            return _annotationManager.GetRegionDocument();
        }

        // adds to the side of the PDFView
        private void MakeRegionMarker(double offset, DocumentController dc)
        {
            var newMarker = new PDFRegionMarker();
            newMarker.SetScrollPosition(offset, pdfNativeSize.Height);
            newMarker.LinkTo = dc;
            newMarker.Offset = offset;
            newMarker.PointerPressed += xMarker_OnPointerPressed;
            xAnnotationMarkers.Children.Add(newMarker);
            _markers.Add(newMarker);
            xAnnotationMarkers.Visibility = Visibility.Visible;
        }

        public bool Freeze()
        {
            return true;
        }
        public bool UnFreeze()
        {
            var native = this.DataDocument.GetActualSize().Value;
            var size   = this.LayoutDocument.GetActualSize().Value;
            xPdfView.Width = native.X + 1;//Syncfusion rounds down to 99 zoom instead of 100, this prevents that
            var scaling = size.X / native.X;
            if (scaling > 1)
            {
                xZoom.RenderTransform = new MatrixTransform() { Matrix = new Matrix(1, 0, 0, 1, 0, 0) };
                xPdfView.ZoomTo((int)(scaling * 100));
                xPdfView.Width = pdfNativeCanvas.DesiredSize.Width*scaling;
                xPdfView.Height = size.Y;
            }
            else
            {
                xPdfView.ZoomTo(100);
                xZoom.RenderTransform = new MatrixTransform() { Matrix = new Matrix(scaling, 0, 0, scaling, 0, 0) };
                xPdfView.Height = size.Y / scaling;
            }
            
            xAnnotations.Width = pdfNativeCanvas.Width;
            xAnnotations.Height = pdfNativeCanvas.Height;
            VerticalOffset = VerticalOffset;
            return true;
        }
        
        private ScrollViewer GetInternalScrollViewer()
        {
            if (_internalViewer == null)
            {
                _internalViewer = xPdfView.GetDescendantsOfType<ScrollViewer>()
                    .First(t => t.Name == "documentScrollViewer");
                _internalViewer.ViewChanged += InternalViewer_ViewChanged;
                _internalViewer = xPdfView.GetDescendantsOfType<ScrollViewer>().First(t => t.Name == "documentScrollViewer");
            }

            return _internalViewer;
        }

        private async void InternalViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (e.IsIntermediate)
            {
                 
            }
            else
            {

                _internalViewer.ViewChanged -= InternalViewer_ViewChanged;
                await Task.Delay(TimeSpan.FromMilliseconds(1000));
                VerticalOffset = VerticalOffset;
            }
        }


        private void XPdfView_OnDocumentLoaded(object sender, DocumentLoadedEventArgs e)
        {
            // configure annotations overlay grid (xRegionsGrid)
            if (double.IsNaN(pdfNativeCanvas.Height))
            {
                xPdfView.PageOffsetCollection.TryGetValue(xPdfView.PageCount, out var endOffset);
                if (endOffset != 0)
                {
                    DataDocument.SetField<NumberController>(KeyStore.PdfVOffsetFieldKey, endOffset/* - xPdfView.PageGap*/, true);
                }
            }
        }

        

        // when the sidebar marker gets pressed
        private void xMarker_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            MarkerSelected((PDFRegionMarker)sender);
            _annotationManager.SelectRegion(((PDFRegionMarker)sender).LinkTo);
            e.Handled = true;
        }

        // moves to the region's offset
        private void MarkerSelected(PDFRegionMarker region)
        {
            if (region != null)
            {
                VerticalOffset = region.Offset * ZoomFactor;
            }
        }
        private void xNextAnnotation_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var currOffset = NativeOffset;
            PDFRegionMarker nextOffset = null;

            foreach (var region in _markers)
            {
                if (region.Offset > currOffset && Math.Abs(region.Offset - currOffset) > 1 && (nextOffset == null || region.Offset < nextOffset.Offset))
                {
                    nextOffset = region;
                }
            }
            MarkerSelected(nextOffset);
        }

        private void xPrevAnnotation_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var currOffset = NativeOffset;
            PDFRegionMarker prevOffset = null;

            foreach (var region in _markers)
            {
                if (region.Offset < currOffset && Math.Abs(region.Offset - currOffset) > 1 && (prevOffset == null || region.Offset > prevOffset.Offset))
                {
                    prevOffset = region;
                }
            }
            MarkerSelected(prevOffset);
        }

        private void xRegionsScrollviewer_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (_markers.Count > 0) xAnnotationNavigation.Opacity = 0.8;
        }

        private void xRegionsScrollviewer_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            xAnnotationNavigation.Opacity = 0;
        }

        public void DisplayFlyout(MenuFlyout linkFlyout)
        {
            linkFlyout.ShowAt(this);
        }

        public DocumentController GetDocControllerFromSelectedRegion()
        {
            var dc = new RichTextNote("PDF " + _internalViewer.VerticalOffset).Document;
            dc.GetDataDocument().SetField<NumberController>(KeyStore.PdfRegionVerticalOffsetKey, _internalViewer.VerticalOffset, true);
            dc.SetRegionDefinition(LayoutDocument);
            
            return dc;
        }

        public FrameworkElement Self()
        {
            return this;
        }

        public Size GetTotalDocumentSize()
        {
            return pdfNativeSize;
        }

        public FrameworkElement GetPositionReference()
        {
            return pdfNativeCanvas;
        }

        public void RegionSelected(object region, Point pt, DocumentController chosenDoc = null)
        {
            _annotationManager.RegionSelected(region, pt, chosenDoc);
        }


        private void XAnnotations_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            NewRegionEnded?.Invoke(sender, e);
        }

        private void XAnnotations_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            NewRegionMoved?.Invoke(sender, e);
        }

        private void XAnnotations_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PdfTest();
            NewRegionStarted?.Invoke(sender, e);
        }
    }
    
}
