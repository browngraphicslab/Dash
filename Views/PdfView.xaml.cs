using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Syncfusion.Windows.PdfViewer;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;
using Syncfusion.Pdf.Interactive;

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
        private List<PDFRegionMarker> _markers = new List<PDFRegionMarker>();
        private ScrollViewer _internalViewer;
        private bool _isResizing;

        // events to communicate with the VisualAnnotationManager
        public event PointerEventHandler NewRegionStarted;
        public event PointerEventHandler NewRegionMoved;
        public event PointerEventHandler NewRegionEnded;

        public PdfView()
        {
            InitializeComponent();
            SetupProgressRing();
            // disable thumbnails on the pdf
            xPdfView.IsThumbnailViewEnabled = false;
            xPdfView.Loaded += (sender, e) =>
            {
                _annotationManager = new VisualAnnotationManager(this, DataDocument, xAnnotations);
                _annotationManager.NewRegionMade += OnNewRegionMade;
                _annotationManager.RegionRemoved += OnRegionRemoved;
                var doc = DataContext as DocumentController;
                var curOffset = doc.GetDereferencedField<NumberController>(KeyStore.PdfVOffsetFieldKey, null)?.Data;
                GetInternalScrollViewer().ChangeView(null, curOffset ?? 0.0, null);
                xPdfView.GetFirstDescendantOfType<ScrollViewer>().Margin = new Thickness(0);
                var dataRegions = DataDocument.GetDataDocument()
                    .GetField<ListController<DocumentController>>(KeyStore.RegionsKey);
                if (dataRegions != null)
                {
                    // the VisualAnnotationManager will take care of the regioning, but here we need to put on the side markers on
                    var totalOffset = DataDocument.GetField<NumberController>(KeyStore.BackgroundImageOpacityKey).Data;
                    xAnnotations.Height = totalOffset;
                    foreach (var region in dataRegions.TypedData)
                    {
                        var offset = region.GetDataDocument()
                            .GetField<NumberController>(KeyStore.PdfRegionVerticalOffsetKey).Data;
                        MakeRegionMarker(offset, totalOffset, region);
                    }
                }
            };
            SizeChanged += PdfView_SizeChanged;
        }

        private void OnNewRegionMade(object sender, RegionEventArgs e)
        {
            var offsetCollection = xPdfView.PageOffsetCollection;
            offsetCollection.TryGetValue(xPdfView.PageCount, out var endOffset);
            MakeRegionMarker(xRegionsScrollviewer.VerticalOffset, endOffset, e.Link);
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

        // when the sidebar marker gets pressed
        private void xMarker_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = false;
            MarkerSelected((PDFRegionMarker) sender);
            e.Handled = true;
        }

        // moves to the region's offset
        private void MarkerSelected(object region)
        {
            if (region == null) return;

            if (region is PDFRegionMarker pregion)
            {
                xRegionsScrollviewer.ChangeView(null, pregion.Offset, null);
                GetInternalScrollViewer().ChangeView(null, xRegionsScrollviewer.VerticalOffset, null);
                _annotationManager.SelectRegion(pregion.LinkTo);
            }

        }

        private void PdfView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataDocument.GetActualSize() == null)
            {
                var sp = xPdfView.GetDescendantsOfType<Canvas>().FirstOrDefault(d => d.Name == "PdfDocumentPanel");
                var native = sp.DesiredSize;
                if (native.Width > 0)
                {
                    DataDocument.SetActualSize(new Point(native.Width, native.Height));
                    UnFreeze();
                }
            }
            else
            {
                UnFreeze();
                SizeChanged -= PdfView_SizeChanged;
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
        public DocumentController DataDocument { get; set; }
        public DocumentController GetRegionDocument()
        {
            return _annotationManager.GetRegionDocument();
        }

        // adds to the side of the PDFView
        private void MakeRegionMarker(double offset, double endOffset, DocumentController dc)
        {
            PDFRegionMarker newMarker = new PDFRegionMarker();
            newMarker.SetPosition(offset, endOffset);
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
            var native = DataDocument.GetField<PointController>(KeyStore.ActualSizeKey).Data;
            var size = LayoutDocument.GetField<PointController>(KeyStore.ActualSizeKey).Data;
            xPdfView.Width = native.X;
            xRegionsScrollviewer.Width = native.X;
            var scaling = size.X / native.X;
            xZoom.RenderTransform = new MatrixTransform() { Matrix = new Matrix(scaling, 0, 0, scaling, 0, 0) };
            xPdfView.Height = size.Y / scaling;

            xRegionsScrollviewer.Height = xPdfView.Height;
            SetUpAnnotationsOverlay();

            return true;
        }

        private void ScrollViewer_OnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            var newOffset = xRegionsScrollviewer.VerticalOffset;
            // PSA: do not use the PDFView's own scroll method. It's broken.
            GetInternalScrollViewer().ChangeView(null, newOffset, null);
        }

        private ScrollViewer GetInternalScrollViewer()
        {
            if (_internalViewer == null)
            {
                _internalViewer = xPdfView.GetDescendantsOfType<ScrollViewer>()
                    .First(t => t.Name == "documentScrollViewer");
                _internalViewer.ViewChanged += InternalViewer_ViewChanged;
            }

            return _internalViewer;
        }

        private void InternalViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (_isResizing)
            {
                xRegionsScrollviewer.ChangeView(null, GetInternalScrollViewer().VerticalOffset, null);
            }
            _isResizing = false;
        }

        private void XPdfView_OnDocumentLoaded(object sender, DocumentLoadedEventArgs e)
        {
            SetUpAnnotationsOverlay();
        }

        private void SetUpAnnotationsOverlay()
        {
            if (double.IsNaN(xGridForStretching.Height))
            {
                xRegionsScrollviewer.Height = xPdfView.Height;
                var offsetCollection = xPdfView.PageOffsetCollection;
                offsetCollection.TryGetValue(xPdfView.PageCount, out var endOffset);
                if (endOffset == 0) return;
                DataDocument.SetField<NumberController>(KeyStore.BackgroundImageOpacityKey, endOffset, true);
                xGridForStretching.Height = endOffset;
            }
            xRegionsScrollviewer.ChangeView(null, GetInternalScrollViewer().VerticalOffset, null);
            GetInternalScrollViewer().ChangeView(null, xRegionsScrollviewer.VerticalOffset, null);
            _isResizing = true;
        }

        private void xNextAnnotation_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var currOffset = xRegionsScrollviewer.VerticalOffset;
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
            var currOffset = xRegionsScrollviewer.VerticalOffset;
            PDFRegionMarker prevOffset = _markers.First();

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
            var dc = new RichTextNote("PDF " + xRegionsScrollviewer.VerticalOffset).Document;
            dc.GetDataDocument().SetField<NumberController>(KeyStore.PdfRegionVerticalOffsetKey, xRegionsScrollviewer.VerticalOffset, true);
            dc.SetRegionDefinition(LayoutDocument);
            
            return dc;
        }

        public FrameworkElement Self()
        {
            return this;
        }

        public Size GetTotalDocumentSize()
        {
            var offsetCollection = xPdfView.PageOffsetCollection;
            offsetCollection.TryGetValue(xPdfView.PageCount, out var endOffset);
            var height = endOffset;

            return new Size(xPdfView.Width, height);
        }

        public FrameworkElement GetPositionReference()
        {
            return xGridForStretching;
        }

        public void RegionSelected(object region, Point pt, DocumentController chosenDoc = null)
        {
            _annotationManager.RegionSelected(region, pt, chosenDoc);
        }

        private void xScrollViewer_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            NewRegionStarted?.Invoke(sender, e); 
        }

        private void xScrollViewer_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            NewRegionMoved?.Invoke(sender, e);
        }

        private void xScrollViewer_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            NewRegionEnded?.Invoke(sender, e);
        }
    }
    
}
