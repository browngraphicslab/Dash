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

        public double VerticalOffset {
            get => NativeOffset * xPdfView.Zoom / 100.0;
            set => NativeOffset = value / (xPdfView.Zoom / 100.0);
        }
        public double NativeOffset
        {
            get => LayoutDocument.GetDereferencedField<NumberController>(KeyStore.PdfVOffsetFieldKey, null)?.Data ?? 0;
            set
            {
                LayoutDocument.SetField<NumberController>(KeyStore.PdfVOffsetFieldKey, value, true);
                // PSA: do not use the PDFView's own scroll method. It's broken.
                GetInternalScrollViewer().ChangeView(null, value * xPdfView.Zoom / 100.0, null);
                xRegionsScrollviewer.ChangeView(null, value * xPdfView.Zoom / 100.0, null);
            }
        }

        public PdfView()
        {
            InitializeComponent();
            SetupProgressRing();
            _annotationManager = new AnnotationManager(this);
            // disable thumbnails on the pdf
            xPdfView.IsThumbnailViewEnabled = false;
            xPdfView.Loaded += (sender, e) =>
            {
                xPdfView.GetDescendantsOfType<Canvas>().FirstOrDefault(d => d.Name == "PdfDocumentPanel").SizeChanged += PdfView_SizeChanged;
                xPdfView.GetDescendantsOfType<Canvas>().FirstOrDefault(d => d.Name == "PdfDocumentPanel").LayoutUpdated += PdfView_LayoutUpdated;

                xTemporaryRegionMarker.SetColor(new SolidColorBrush(Colors.Gold));
                
                xPdfView.GetFirstDescendantOfType<ScrollViewer>().Margin = new Thickness(0);
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
            if (region is PDFRegionMarker pregion)
            {
                NativeOffset = pregion.Offset;
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
                xTemporaryRegionMarker.SetSize(pregion.Size, pregion.Position, new Size(xPdfView.Width / (xPdfView.Zoom / 100.0), xRegionsGrid.Height));
            }
            else
                theDoc = DataDocument;

            if (pos.X == 0 && pos.Y == 0) pos = DataDocument.GetField<PointController>(KeyStore.PositionFieldKey).Data;
            _annotationManager.RegionPressed(theDoc, pos);
        }


        private void PdfView_LayoutUpdated(object sender, object e)
        {
            var pdfDocPanel = xPdfView.GetDescendantsOfType<Canvas>().FirstOrDefault(d => d.Name == "PdfDocumentPanel");
            if (pdfDocPanel.DesiredSize.Width > 0)
            {
                 
                pdfDocPanel.LayoutUpdated -= PdfView_LayoutUpdated;
                updateInitialLayout(pdfDocPanel);
            }
        }

        private void PdfView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var pdfDocPanel = xPdfView.GetDescendantsOfType<Canvas>().FirstOrDefault(d => d.Name == "PdfDocumentPanel");
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
            UnFreeze();
            var dataRegions = DataDocument.GetDataDocument().GetRegions();
            if (dataRegions != null && _dataRegions.Count() == 0)
            {
                var totalOffset = DataDocument.GetField<NumberController>(KeyStore.PdfVOffsetFieldKey)?.Data ?? 0;
                xRegionsGrid.Height = totalOffset;
                xRegionsGrid.Width = double.NaN;
                xRegionsGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
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
                var docWidth = xTemporaryRegionMarker.Size.Width;
                var docHeight = xTemporaryRegionMarker.Size.Height;
                var docPos = new Point(xTemporaryRegionMarker.Position.X , xTemporaryRegionMarker.Position.Y);
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
            var newRegion = new PDFRegionMarker();
            newRegion.SetSize(size, pos, new Size(xPdfView.Width / (xPdfView.Zoom / 100.0), xRegionsGrid.Height));
            newRegion.LinkTo = dc;
            newRegion.Offset = offset ;
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
            xPdfView.Width = native.X + 1;//Syncfusion rounds down to 99 zoom instead of 100, this prevents that
            var scaling = size.X / native.X;
            if (scaling > 1)
            {
                xZoom.RenderTransform = new MatrixTransform() { Matrix = new Matrix(1, 0, 0, 1, 0, 0) };
                xPdfView.ZoomTo((int)(scaling * 100));
                var desired = xPdfView.GetDescendantsOfType<Canvas>().FirstOrDefault(d => d.Name == "PdfDocumentPanel").DesiredSize;
                xPdfView.Width = desired.Width*scaling;
                xPdfView.Height = size.Y;
            }
            else
            {
                xPdfView.ZoomTo(100);
                xZoom.RenderTransform = new MatrixTransform() { Matrix = new Matrix(scaling, 0, 0, scaling, 0, 0) };
                xPdfView.Height = size.Y / scaling;
            }

            xRegionsScrollviewer.Height = xPdfView.Height;
            xRegionsScrollviewer.Width = xPdfView.Width;
            NativeOffset = NativeOffset;
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
                 
            } else
            {

                _internalViewer.ViewChanged -= InternalViewer_ViewChanged;
                await Task.Delay(TimeSpan.FromMilliseconds(1000));
                NativeOffset = NativeOffset;
            }
        }


        private void XPdfView_OnDocumentLoaded(object sender, DocumentLoadedEventArgs e)
        {
            // configure annotations overlay grid (xRegionsGrid)
            if (double.IsNaN(xRegionsGrid.Height))
            {
                xRegionsScrollviewer.Height = xPdfView.Height;
                xRegionsScrollviewer.Width = xPdfView.Width;
                xPdfView.PageOffsetCollection.TryGetValue(xPdfView.PageCount, out var endOffset);
                if (endOffset != 0)
                {
                    DataDocument.SetField<NumberController>(KeyStore.PdfVOffsetFieldKey, endOffset - xPdfView.PageGap, true);
                    xRegionsGrid.Height = endOffset;
                }
            }
            _isResizing = true;
        }

        private void XRegionsGrid_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                var pos = e.GetCurrentPoint(xRegionsGrid).Position;
                _anchor = pos;
                xTemporaryRegionMarker.SetSize(new Size(0, 0), _anchor, new Size(xPdfView.Width / (xPdfView.Zoom / 100.0), xRegionsGrid.Height));
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
                
                xTemporaryRegionMarker.SetSize(new Size(Math.Abs(_anchor.X - pos.X) / (xPdfView.Zoom / 100.0), Math.Abs(_anchor.Y - pos.Y)), new Point(x / (xPdfView.Zoom / 100.0), y), new Size(xPdfView.Width / (xPdfView.Zoom / 100.0), xRegionsGrid.Height ));
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
            
            if (prevOffset != null)
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
