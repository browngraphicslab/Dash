using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Syncfusion.Windows.PdfViewer;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;
using System.Linq;
using Windows.UI.Xaml.Input;
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

        public PdfView()
        {
            InitializeComponent();
            SetupProgressRing();

            // disable thumbnails on the pdf
            xPdfView.IsThumbnailViewEnabled = false;
            xPdfView.Loaded += (sender, e) =>
            {
                var doc = DataContext as DocumentController;
                var curOffset = doc.GetDereferencedField<NumberController>(KeyStore.PdfVOffsetFieldKey, null)?.Data;
                xPdfView.ScrollToVerticalOffset(curOffset ?? 0.0);
                xPdfView.GetFirstDescendantOfType<ScrollViewer>().Margin = new Thickness(0);
            };
            SizeChanged += PdfView_SizeChanged;

            xPdfView.ScrollChanged += (sender, e) =>
            {
                //System.Diagnostics.Debug.WriteLine("Scroll to " + xPdfView.VerticalOffset);
                //if (_scrollTarget != -1)
                //    xPdfView.ScrollToVerticalOffset(_scrollTarget);
            };



             // bcz: hack to test scrolling-- this will scroll back to the scroll offset when the last annotation was made
            xPdfView.Tapped += (s, e) =>
            {
                if (_scrollTarget != -1)
                    _scrollTarget = -1;
                else
                {
                    var regions = DataDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null)?.TypedData;
                    if (regions != null)
                    {
                        _scrollTarget = regions.Last().GetDataDocument().GetDereferencedField<NumberController>(KeyStore.BackgroundImageOpacityKey, null).Data;
                        xPdfView.ScrollToVerticalOffset((xPdfView.VerticalOffset + _scrollTarget)/2);
                    }
                }
                e.Handled = true;
            };
        }

        private void PdfView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.DataDocument.GetActualSize() == null)
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

        double _scrollTarget = -1;

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
            //otherwise, make a new doc controller for the selection
            var dc = new RichTextNote("PDF " + xPdfView.VerticalOffset).Document;
            dc.GetDataDocument().SetField<NumberController>(KeyStore.BackgroundImageOpacityKey, xPdfView.VerticalOffset, true);
            dc.SetRegionDefinition(this.LayoutDocument);
            var regions = DataDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null);
            if (regions == null)
            {
                var dregions = new ListController<DocumentController>(dc);
                DataDocument.SetField(KeyStore.RegionsKey, dregions, true);
            }
            else
            {
                regions.Add(dc);
            }

            return dc;
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
            return true;
        }
		
	}
    
}
