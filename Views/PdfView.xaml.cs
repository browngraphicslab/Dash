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
    public sealed partial class PdfView
    {
        /// <summary>
        /// The pdf viewer from xaml
        /// </summary>
        public SfPdfViewerControl Pdf => xPdfView;
        private AnnotationManager _annotationManager;
        private List<PDFRegionMarker> _dataRegions = new List<PDFRegionMarker>();
        private ScrollViewer _internalViewer;
        private Point _anchor;
        private bool _isDragging;
        private bool _isResizing;
        private PDFRegionMarker _selectedRegion;

        public PdfView()
        {
            InitializeComponent();
            SetupProgressRing();
            _annotationManager = new AnnotationManager(this);
            // disable thumbnails on the pdf
            xPdfView.IsThumbnailViewEnabled = false;
            xPdfView.Loaded += (sender, e) =>
            {
                xTemporaryRegionMarker.SetColor(new SolidColorBrush(Colors.Gold));
                var doc = DataContext as DocumentController;
                var curOffset = doc.GetDereferencedField<NumberController>(KeyStore.PdfVOffsetFieldKey, null)?.Data;
                GetInternalScrollViewer().ChangeView(null, curOffset ?? 0.0, null);
                xPdfView.GetFirstDescendantOfType<ScrollViewer>().Margin = new Thickness(0);
                var dataRegions = DataDocument.GetDataDocument()
                    .GetField<ListController<DocumentController>>(KeyStore.RegionsKey);
                if (dataRegions != null)
                {
                    var totalOffset = DataDocument.GetField<NumberController>(KeyStore.BackgroundImageOpacityKey)?.Data ?? 0;
                    xRegionsGrid.Height = totalOffset;
                    foreach (var region in dataRegions.TypedData)
                    {
                        var offset = region.GetDataDocument()
                            .GetField<NumberController>(KeyStore.BackgroundImageOpacityKey).Data;
                        var width = region.GetDataDocument().GetField<NumberController>(KeyStore.WidthFieldKey).Data;
                        var height = region.GetDataDocument().GetField<NumberController>(KeyStore.HeightFieldKey).Data;
                        var pos = region.GetDataDocument().GetField<PointController>(KeyStore.PositionFieldKey).Data;
                        MakeRegionMarker(offset, totalOffset, pos, new Size(width, height), region);
                    }
                }
                //if (_dataRegions != null)
                //{
                //    foreach (var region in _dataRegions.TypedData)
                //    {
                //        var offset = region.GetDataDocument().GetField<NumberController>(KeyStore.BackgroundImageOpacityKey).Data;
                //        var newBox = new PDFRegionMarker { LinkTo = region, Offset = offset };

                //        var offsetCollection = xPdfView.PageOffsetCollection;
                //        offsetCollection.TryGetValue(xPdfView.PageCount, out var endOffset);
                //        newBox.SetPosition(offset, endOffset);
                //        xAnnotationMarkers.Children.Add(newBox);
                //        newBox.PointerPressed += xMarker_OnPointerPressed;

                //    }
                //}
            };
            SizeChanged += PdfView_SizeChanged;
            
            
        }

        // when the sidebar marker gets pressed
        private void xMarker_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = false;
            this.MarkerSelected((PDFRegionMarker) sender);
            e.Handled = true;
        }

        // when the actual region on the PDF gets pressed
        private void xRegion_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = false;
            RegionSelected((PDFRegionMarker)sender, e.GetCurrentPoint(MainPage.Instance).Position);
            e.Handled = true;
        }

        // moves to the region's offset
        private void MarkerSelected(object region)
        {
            if (region == null) return;

            if (region is PDFRegionMarker pregion)
            {
                xRegionsScrollviewer.ChangeView(null, pregion.Offset, null);
                GetInternalScrollViewer().ChangeView(null, pregion.Offset, null);
            }

        }

        // toggles annotation visibility
        private void RegionSelected(object region, Point pos)
        {
            if (region == null) return;
            DocumentController theDoc;

            if (region is PDFRegionMarker pregion)
            {
                //get the linked doc of the selected region
                theDoc = pregion.LinkTo;
                if (theDoc == null) return;
                _selectedRegion = pregion;
                xTemporaryRegionMarker.SetSize(pregion.Size, pregion.Position, new Size(xRegionsGrid.ActualWidth, xRegionsGrid.Height));
            }
            else
                theDoc = DataDocument;

            if (pos.X == 0 && pos.Y == 0) pos = DataDocument.GetField<PointController>(KeyStore.PositionFieldKey).Data;
            _annotationManager.RegionPressed(theDoc, pos);
        }

        private void PdfView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataDocument.GetActualSize() == null)
            {
                var sp = xPdfView.GetDescendantsOfType<Canvas>().Where((d) => d.Name == "PdfDocumentPanel").FirstOrDefault();
                var native = sp.DesiredSize;
                if (native.Width > 0)
                {
                    this.DataDocument.SetActualSize(new Windows.Foundation.Point(native.Width, native.Height));
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

                var dc = new RichTextNote("PDF " + xPdfView.VerticalOffset).Document;
                dc.GetDataDocument().SetField<NumberController>(KeyStore.BackgroundImageOpacityKey, xPdfView.VerticalOffset, true);
                dc.GetDataDocument().SetField<NumberController>(KeyStore.WidthFieldKey, xTemporaryRegionMarker.Size.Width, true);
                dc.GetDataDocument().SetField<NumberController>(KeyStore.HeightFieldKey, xTemporaryRegionMarker.Size.Height, true);
                dc.GetDataDocument().SetField<PointController>(KeyStore.PositionFieldKey, xTemporaryRegionMarker.Position, true);
                dc.SetRegionDefinition(LayoutDocument);
                var regions = DataDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null);
                var offsetCollection = xPdfView.PageOffsetCollection;
                offsetCollection.TryGetValue(xPdfView.PageCount, out var endOffset);

                //otherwise, make a new doc controller for the selection
                if (regions == null)
                {
                    var dregions = new ListController<DocumentController>(dc);
                    DataDocument.SetField(KeyStore.RegionsKey, dregions, true);
                }
                else
                {
                    regions.Add(dc);
                }

                MakeRegionMarker(xPdfView.VerticalOffset, endOffset, xTemporaryRegionMarker.Position, xTemporaryRegionMarker.Size, dc);
                return dc;
            }

            return null;
        }

        // adds to the side of the PDFView
        private void MakeRegionMarker(double offset, double endOffset, Point pos, Size size, DocumentController dc)
        {
            PDFRegionMarker newMarker = new PDFRegionMarker();
            newMarker.SetPosition(offset, endOffset);
            newMarker.LinkTo = dc;
            newMarker.Offset = offset;
            newMarker.PointerPressed += xMarker_OnPointerPressed;
            xAnnotationMarkers.Children.Add(newMarker);
            PDFRegionMarker newRegion = new PDFRegionMarker();
            newRegion.SetSize(size, pos, new Size(xRegionsGrid.ActualWidth, xRegionsGrid.Height));
            newRegion.LinkTo = dc;
            newRegion.Offset = offset;
            newRegion.PointerPressed += xRegion_OnPointerPressed;
            xRegionsGrid.Children.Add(newRegion);
            _dataRegions.Add(newMarker);
            xTemporaryRegionMarker.Visibility = Visibility.Collapsed;
        }

        public bool Freeze()
        {
            return true;
        }
        public bool UnFreeze()
        {
            var native = this.DataDocument.GetActualSize().Value;
            var size = this.LayoutDocument.GetActualSize().Value;
            xPdfView.Width = native.X;
            xRegionsScrollviewer.Width = native.X;
            if (native.X < size.X)
            {
                var scaling = size.X / native.X;
                xZoom.RenderTransform = new MatrixTransform() { Matrix = new Matrix(scaling, 0, 0, scaling, 0, 0) };
                xPdfView.Height = size.Y / scaling;
            }
            else
            {
                var scaling = size.X / native.X;
                var val = 0;
                xZoom.RenderTransform = new MatrixTransform() { Matrix = new Matrix(scaling, 0, 0, scaling, val, 0) };
                xPdfView.Height = size.Y / scaling;
            }

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
            if (double.IsNaN(xRegionsGrid.Height))
            {
                xRegionsScrollviewer.Height = xPdfView.Height;
                var offsetCollection = xPdfView.PageOffsetCollection;
                offsetCollection.TryGetValue(xPdfView.PageCount, out var endOffset);
                if (endOffset == 0) return;
                DataDocument.SetField<NumberController>(KeyStore.BackgroundImageOpacityKey, endOffset - xPdfView.PageGap, true);
                xRegionsGrid.Height = endOffset - xPdfView.PageGap;
            }
            xRegionsScrollviewer.ChangeView(null, GetInternalScrollViewer().VerticalOffset, null);
            GetInternalScrollViewer().ChangeView(null, xRegionsScrollviewer.VerticalOffset, null);
            _isResizing = true;
        }

        private void XRegionsGrid_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                var pos = e.GetCurrentPoint(xRegionsGrid).Position;
                _anchor = pos;
                xTemporaryRegionMarker.SetSize(new Size(0, 0), _anchor, new Size(xRegionsGrid.ActualWidth, xRegionsGrid.Height));
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
                var pos = e.GetCurrentPoint(xRegionsGrid).Position;
                var x = Math.Min(pos.X, _anchor.X);
                var y = Math.Min(pos.Y, _anchor.Y);

                xTemporaryRegionMarker.SetSize(new Size(Math.Abs(_anchor.X - pos.X), Math.Abs(_anchor.Y - pos.Y)), new Point(x, y), new Size(xRegionsGrid.ActualWidth, xRegionsGrid.Height));
            }
        }

        private void XRegionsGrid_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isDragging = false;
            var pos = e.GetCurrentPoint(xRegionsGrid).Position;
            if (Math.Abs(_anchor.Y - pos.Y) < 30 && (Math.Abs(_anchor.X - pos.X)) < 30)
            {
                xTemporaryRegionMarker.Visibility = Visibility.Collapsed;
            }
        }

        private void xNextAnnotation_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine(_dataRegions.Count);
            var currOffset = xPdfView.VerticalOffset;
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
            var currOffset = xPdfView.VerticalOffset;
            PDFRegionMarker prevOffset = _dataRegions.First();

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
