using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Syncfusion.Windows.PdfViewer;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;
using System.Linq;

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

            xPdfView.ScrollChanged += (sender, e) =>
            {
                var doc = DataContext as DocumentController;
                if (xPdfView.IsPointerOver() && doc != null)
                {
                    System.Diagnostics.Debug.WriteLine("Scroll to " + xPdfView.VerticalOffset);
                    doc.SetField<NumberController>(KeyStore.PdfVOffsetFieldKey, (double)xPdfView.VerticalOffset, true);
                }
                else
                {
                    var curOffset = doc.GetDereferencedField<NumberController>(KeyStore.PdfVOffsetFieldKey, null)?.Data;
                    System.Diagnostics.Debug.WriteLine("===> to " + xPdfView.VerticalOffset + "(" + curOffset );
                }
            };
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
        public bool Freeze()
        {
            var renderTargetBitmap = new RenderTargetBitmap();
            renderTargetBitmap.RenderAsync(xPdfView);
            xPdfFrozenView.Source = renderTargetBitmap;
            xPdfFrozenView.Opacity = 0.5;
            xPdfFrozenView.Visibility = Visibility.Visible;
            xPdfView.HorizontalAlignment = HorizontalAlignment.Left;
            xPdfView.VerticalAlignment = VerticalAlignment.Top;
            xPdfView.Width = xPdfView.ActualWidth;
            xPdfView.Height = xPdfView.ActualHeight;
            xPdfView.RenderTransform = new TranslateTransform() { X = 100000, Y = 0 };
            return true;
        }
        public bool UnFreeze()
        {
            xPdfFrozenView.Visibility = Visibility.Collapsed;
            xPdfView.Visibility = Visibility.Visible;
            xPdfView.HorizontalAlignment = HorizontalAlignment.Stretch;
            xPdfView.VerticalAlignment = VerticalAlignment.Stretch;
            xPdfView.Width = xPdfView.Height = double.NaN;
            xPdfView.RenderTransform = null;
            return true;
        }
    }
    
}
