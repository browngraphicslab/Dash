using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Syncfusion.Windows.PdfViewer;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Interactive;
using Point = Windows.Foundation.Point;
using Size = Windows.Foundation.Size;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class PdfView
    {
        /// <summary>
        /// Display anchor for link regions 
        /// </summary>
        public class RegionGrid : Grid
        {
            public DocumentController LinkTo;
        }
        /// <summary>
        /// The pdf viewer from xaml
        /// </summary>
        public SfPdfViewerControl Pdf => xPdfView;
        private AnnotationManager _annotationManager;
        private List<PDFRegionMarker> _dataRegions = new List<PDFRegionMarker>();
        private ScrollViewer _internalViewer;
        private Point _anchorNative;
        private bool _isDragging;
        private PDFRegionMarker _selectedRegion;
        private Grid xTemporaryRegionMarker = new Grid();
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
                // PSA: do not use the PDFView's own scroll method. It's broken.
                xRegionsScrollviewer.ChangeView(null, value, null);
                LayoutDocument.SetField<NumberController>(KeyStore.PdfVOffsetFieldKey, value / ZoomFactor, true);
            }
        }
        public double NativeOffset => LayoutDocument.GetDereferencedField<NumberController>(KeyStore.PdfVOffsetFieldKey, null)?.Data ?? 0;

        public PdfView()
        {
            InitializeComponent();
            SetupProgressRing();
            _annotationManager = new AnnotationManager(this);
            xTemporaryRegionMarker.Background = new SolidColorBrush(Colors.Blue);
            xTemporaryRegionMarker.Opacity = 0.4;
            // disable thumbnails on the pdf
            xPdfView.IsThumbnailViewEnabled = false;
            xPdfView.Loaded += (sender, e) =>
            {
                pdfNativeCanvas.SizeChanged += PdfView_SizeChanged;
                pdfNativeCanvas.LayoutUpdated += PdfView_LayoutUpdated;

                xPdfView.GetFirstDescendantOfType<ScrollViewer>().Margin = new Thickness(0);
            };
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
                this.DataDocument.SetActualSize(new Windows.Foundation.Point(pdfDocPanel.DesiredSize.Width, pdfDocPanel.DesiredSize.Height));
            }
            if (RegionsPanel == null)
            {
                var ppar = pdfDocPanel.Parent as Grid;
                var pchild = new Canvas();
                ppar.Children.Add(pchild);
                pchild.Children.Add(xTemporaryRegionMarker);
            }
            UnFreeze();
            var dataRegions = DataDocument.GetDataDocument().GetRegions();
            if (dataRegions != null && _dataRegions.Count() == 0)
            {
                var totalOffset = this.DataDocument.GetActualSize().Value.Y;
                xRegionsGrid.Height = totalOffset * ZoomFactor;
                foreach (var region in dataRegions.TypedData)
                {
                    var width = region.GetDataDocument().GetField<NumberController>(KeyStore.WidthFieldKey).Data;
                    var height = region.GetDataDocument().GetField<NumberController>(KeyStore.HeightFieldKey).Data;
                    var pos = region.GetDataDocument().GetField<PointController>(KeyStore.PositionFieldKey).Data;
                    MakeRegionMarker(pos.Y, totalOffset, pos, new Size(width, height), region);
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

        public DocumentController LayoutDocument { get; set; }
        public DocumentController DataDocument { get; set; }
        public DocumentController GetRegionDocument()
        {
            if (_selectedRegion != null)
            {
                return _selectedRegion.LinkTo;
            }

            if (xTemporaryRegionMarker.Visibility == Visibility.Visible)
            {
                var region = new RichTextNote("PDF " + xPdfView.VerticalOffset).Document;
                var docWidth = xTemporaryRegionMarker.Width;
                var docHeight = xTemporaryRegionMarker.Height;
                var docPos = new Point(xTemporaryRegionMarker.RenderTransform.TransformPoint(new Point()).X, xTemporaryRegionMarker.RenderTransform.TransformPoint(new Point()).Y);
                region.GetDataDocument().SetWidth(docWidth);
                region.GetDataDocument().SetHeight(docHeight);
                region.GetDataDocument().SetPosition(docPos);
                region.SetRegionDefinition(LayoutDocument);
                xPdfView.PageOffsetCollection.TryGetValue(xPdfView.PageCount, out var endOffset);

                this.DataDocument.AddToRegions(new List<DocumentController>(new DocumentController[] { region }));

                MakeRegionMarker(docPos.Y, endOffset, docPos, new Size(docWidth, docHeight), region);
                return region;
            }

            return null;
        }

        // adds to the side of the PDFView
        private void MakeRegionMarker(double offset, double endOffset, Point pos, Size size, DocumentController dc)
        {
            var newMarker = new PDFRegionMarker();
            newMarker.SetPosition(offset, endOffset);
            newMarker.LinkTo = dc;
            newMarker.Offset = offset;
            newMarker.PointerPressed += xMarker_OnPointerPressed;
            xAnnotationMarkers.Children.Add(newMarker);
            _dataRegions.Add(newMarker);

            var newRegion = new RegionGrid();
            newRegion.RenderTransform = new TranslateTransform() { X = pos.X, Y = pos.Y };
            newRegion.LinkTo = dc;
            newRegion.Width = size.Width;
            newRegion.Height = size.Height;
            newRegion.Background = new SolidColorBrush(Colors.Gold);
            newRegion.Opacity = 0.4;
            RegionsPanel.Children.Add(newRegion);
            xTemporaryRegionMarker.Visibility = Visibility.Collapsed;
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
                this.xRegionsGrid.Height = native.Y * ZoomFactor;
            }
            else
            {
                xPdfView.ZoomTo(100);
                xZoom.RenderTransform = new MatrixTransform() { Matrix = new Matrix(scaling, 0, 0, scaling, 0, 0) };
                xPdfView.Height = size.Y / scaling;
                this.xRegionsGrid.Height = native.Y;
            }

            xRegionsScrollviewer.Width = xPdfView.Width;
            xRegionsScrollviewer.Height = xPdfView.Height;
            VerticalOffset = VerticalOffset;
            return true;
        }

        private void xRegionsScrollviewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            VerticalOffset = xRegionsScrollviewer.VerticalOffset;
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
                xRegionsScrollviewer.Height = xPdfView.Height;
                xRegionsScrollviewer.Width  = xPdfView.Width;
                xPdfView.PageOffsetCollection.TryGetValue(xPdfView.PageCount, out var endOffset);
                if (endOffset != 0)
                {
                    DataDocument.SetField<NumberController>(KeyStore.PdfVOffsetFieldKey, endOffset/* - xPdfView.PageGap*/, true);
                    xRegionsGrid.Height = endOffset * ZoomFactor;
                }
            }
        }
        private void XRegionsGrid_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                var pos = e.GetCurrentPoint(pdfNativeCanvas).Position;
                _anchorNative = new Point(pos.X, pos.Y);
                xTemporaryRegionMarker.RenderTransform = new TranslateTransform() { X = _anchorNative.X, Y = _anchorNative.Y };
                xTemporaryRegionMarker.Width = xTemporaryRegionMarker.Height = 0;
                xTemporaryRegionMarker.Visibility = Visibility.Visible;
                _isDragging = true;
                _selectedRegion = null;
            }
            else
                _isDragging = false;
        }

        private void XRegionsGrid_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                var pos = e.GetCurrentPoint(pdfNativeCanvas).Position;
                var posNative = new Point(pos.X , pos.Y);
                var xNative = Math.Min(posNative.X, _anchorNative.X);
                var yNative = Math.Min(posNative.Y, _anchorNative.Y);

                xTemporaryRegionMarker.Width = Math.Abs(_anchorNative.X - posNative.X);
                xTemporaryRegionMarker.Height = Math.Abs(_anchorNative.Y - posNative.Y);
            }
        }


        private void XRegionsGrid_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isDragging = false;
            
            var pos = e.GetCurrentPoint(pdfNativeCanvas).Position;
            if (Math.Abs(_anchorNative.Y - pos.Y) < 30 && (Math.Abs(_anchorNative.X - pos.X)) < 30)
            {
                xTemporaryRegionMarker.Visibility = Visibility.Collapsed;
                // toggle annotation visibility
                foreach (var region in RegionsPanel.Children.OfType<RegionGrid>())
                    if (region.GetBoundingRect(region).Contains(e.GetCurrentPoint(region).Position))
                        _annotationManager.RegionPressed(region.LinkTo, e.GetCurrentPoint(MainPage.Instance).Position);
            }
        }

        // when the sidebar marker gets pressed
        private void xMarker_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            MarkerSelected((PDFRegionMarker)sender);
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

            foreach (var region in _dataRegions)
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
            var currOffset =NativeOffset;
            PDFRegionMarker prevOffset = null;

            foreach (var region in _dataRegions)
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
            xAnnotationNavigation.Opacity = 0.8;
        }

        private void xRegionsScrollviewer_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            xAnnotationNavigation.Opacity = 0;
        }
    }
    
}
