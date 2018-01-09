using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Windows.PdfViewer;

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

        private void XOuterGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // resize the clipping rect used to hide pdf overlow
            xClippingRect.Rect = new Rect(0, 0, xOuterGrid.ActualWidth, xOuterGrid.ActualHeight);
        }
    }
    
}
