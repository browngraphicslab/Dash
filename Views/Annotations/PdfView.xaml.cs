using Dash.Annotations;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using iText.Kernel.Crypto;
using FrameworkElement = Windows.UI.Xaml.FrameworkElement;
using Point = Windows.Foundation.Point;
using Rectangle = Windows.UI.Xaml.Shapes.Rectangle;
using WPdf = Windows.Data.Pdf;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class PdfView : UserControl, INotifyPropertyChanged, ILinkHandler
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public static readonly DependencyProperty PdfUriProperty = DependencyProperty.Register(
            "PdfUri", typeof(Uri), typeof(PdfView), new PropertyMetadata(default(Uri), PropertyChangedCallback));

        //This might be more efficient as a linked list of KV pairs if our selections are always going to be contiguous
        private Dictionary<int, Rectangle> _selectedRectangles = new Dictionary<int, Rectangle>();
        private StorageFile       _file;
        private int               _currentPageCount = -1;
        private double            _pdfMaxWidth;
        private double            _pdfTotalHeight;
        public PdfAnnotationView                  DefaultView => _botPdf;
        public DocumentController                 DataDocument => (DataContext as DocumentViewModel).DataDocument;
        public DocumentController                 LayoutDocument => (DataContext as DocumentViewModel).LayoutDocument;
        public Uri                                PdfUri
        {
            get => (Uri)GetValue(PdfUriProperty);
            set => SetValue(PdfUriProperty, value);
        }
        public double                             PdfMaxWidth
        {
            get => _pdfMaxWidth;
            set
            {
                _pdfMaxWidth = value;
                OnPropertyChanged();
            }
        }
        public double                             PdfTotalHeight
        {
            get => _pdfTotalHeight;
            set
            {
                _pdfTotalHeight = value;
                OnPropertyChanged();
            }
        }
        public WPdf.PdfDocument                   PDFdoc { get; private set; }
        //This makes the assumption that both pdf views are always in the same annotation mode
        public AnnotationType                     CurrentAnnotationType => _botPdf.AnnotationOverlay.CurrentAnnotationType;

        public PdfView()
        {
            InitializeComponent();

            SizeChanged += (ss, ee) =>
            {
                if (xBar.Width != 0)
                {
                    xBar.Width = ActualWidth;
                }
            };

            xPdfContainer.SizeChanged += (ss, ee) =>
            {
                if (xFirstPanelRow.ActualHeight > xPdfContainer.ActualHeight - 5 &&
                    xPdfContainer.ActualHeight - 5 > 0)
                {
                    xFirstPanelRow.Height = new GridLength(xPdfContainer.ActualHeight - 4, GridUnitType.Pixel);
                }
                xFirstPanelRow.MaxHeight = xPdfContainer.ActualHeight;
            };
        }
        ~PdfView()
        {
            //Debug.WriteLine("FINALIZING PdfView");
        }
        
        public async Task<List<DocumentController>> ExplodePages()
        {
            var pages = new List<DocumentController>();
            var reader = new PdfReader(await _file.OpenStreamForReadAsync());
            var pdfDocument = new PdfDocument(reader);
            int n = pdfDocument.GetNumberOfPages();
            var title = DataDocument.GetTitleFieldOrSetDefault().Data;
            var psplit = new iText.Kernel.Utils.PdfSplitter(pdfDocument);
            for (int i = 1; i <= n; i++)
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var uniqueFilePath = _file.Path.Replace(".pdf", "-"+i+".pdf");
                var exists = await localFolder.TryGetItemAsync(Path.GetFileName(uniqueFilePath)) != null;
                var localFile = await localFolder.CreateFileAsync(Path.GetFileName(uniqueFilePath), CreationCollisionOption.OpenIfExists);
                if (!exists)
                {
                    var pw = new PdfWriter(new FileInfo(localFolder.Path + "/"+ Path.GetFileName(uniqueFilePath)));
                    var outDoc = new PdfDocument(pw);
                    pdfDocument.CopyPagesTo(new List<int>(new int[] { i }), outDoc);
                    outDoc.Close();
                }
                var doc = new PdfToDashUtil().GetPDFDoc(localFile, title.Substring(0,title.IndexOf(".pdf"))+":"+i+".pdf");
                doc.GetDataDocument().SetField<TextController>(KeyStore.SourceUriKey, DataDocument.Id, true);
                pages.Add(doc);
            }
            reader.Close();
            pdfDocument.Close();
            return pages;
        }
        public void GoToPage(double pageNum)
        {
            _botPdf.GoToPage(pageNum);
        }
        public void ShowRegions()
        {
            _topPdf.AnnotationOverlay.Visibility = Visibility.Visible;
            _botPdf.AnnotationOverlay.Visibility = Visibility.Visible;
        }
        public void HideRegions()
        {
            _topPdf.AnnotationOverlay.Visibility = Visibility.Collapsed;
            _botPdf.AnnotationOverlay.Visibility = Visibility.Collapsed;
        }
        public bool AreAnnotationsVisible()
        {
            //This makes the assumption that both overlays are kept in sync
            return _botPdf.AnnotationOverlay.Visibility == Visibility.Visible;
        }
        public LinkHandledResult HandleLink(DocumentController linkDoc, LinkDirection direction)
        {
            return _botPdf.HandleLink(linkDoc, direction);
        }
        public void SetAnnotationsVisibleOnScroll(bool? visibleOnScroll)
        {
            _botPdf.SetAnnotationsVisibleOnScroll(visibleOnScroll);
        }
        public void SetActivationMode(bool onoff)
        {
            xActivationMode.Visibility = onoff ? Visibility.Visible : Visibility.Collapsed;
            if (onoff)
                LinkActivationManager.ActivateDoc(this.GetFirstAncestorOfType<DocumentView>());
            else
                LinkActivationManager.DeactivateDoc(this.GetFirstAncestorOfType<DocumentView>());
        }
        public void SetAnnotationType(AnnotationType type)
        {
            _botPdf.AnnotationOverlay.CurrentAnnotationType = type;
            _topPdf.AnnotationOverlay.CurrentAnnotationType = type;
        }
        /// <summary>
        /// This creates a region document at a Point specified in the coordinates of the containing DocumentView
        /// </summary>
        /// <param name="docViewPoint"></param>
        /// <returns></returns>
        public DocumentController GetRegionDocument(Point? docViewPoint = null)
        {
            return _botPdf.GetRegionDocument(docViewPoint);
        }

        private void XOnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            //Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor =
            //    new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 1);
            if (sender is Grid button && ToolTipService.GetToolTip(button) is ToolTip tip)
                tip.IsOpen = true;
        }

        private void XOnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            //Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor =
            //    new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
            if (sender is Grid button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = false;

        }

        private static DocumentController RegionGetter(AnnotationType type)
        {
            return new RichTextNote().Document;
        }

        private async Task OnPdfUriChanged(bool force = false)
        {
            if (PdfUri == null)
            {
                return;
            }

            try
            {
                if (PdfUri.AbsoluteUri.StartsWith("ms-appx://") || PdfUri.AbsoluteUri.StartsWith("ms-appdata://"))
                {
                    _file = await StorageFile.GetFileFromApplicationUriAsync(PdfUri);
                }
                else
                {
                    _file = await StorageFile.GetFileFromPathAsync(PdfUri.LocalPath);

                }
            }
            catch (ArgumentException)
            {
                return;
            }

            var reader = new PdfReader(await _file.OpenStreamForReadAsync());
            PdfDocument pdfDocument;
            try
            {
                pdfDocument = new PdfDocument(reader);
            }
            catch (BadPasswordException)
            {
                return;
            }
            var strategy = new BoundsExtractionStrategy();
            var processor = new PdfCanvasProcessor(strategy);
            double offset = 0;
            double maxWidth = 0;
            for (var i = 1; i <= pdfDocument.GetNumberOfPages(); ++i)
            {
                var page = pdfDocument.GetPage(i);
                _topPdf.Pages.PageSizes.Add(new Size(page.GetPageSize().GetWidth(), page.GetPageSize().GetHeight()));
                _botPdf.Pages.PageSizes.Add(new Size(page.GetPageSize().GetWidth(), page.GetPageSize().GetHeight()));
                maxWidth = Math.Max(maxWidth, page.GetPageSize().GetWidth());
            }

            _topPdf.PdfMaxWidth = _botPdf.PdfMaxWidth = PdfMaxWidth = maxWidth;

            _topPdf.PDFdoc = _botPdf.PDFdoc = PDFdoc = await WPdf.PdfDocument.LoadFromFileAsync(_file);
            if (PDFdoc.PageCount != _currentPageCount)
            {
                _currentPageCount = (int)PDFdoc.PageCount;
            }

            if (MainPage.Instance.xSettingsView.UsePdfTextSelection)
            {
                await Task.Run(() =>
                {
                    for (var i = 1; i <= pdfDocument.GetNumberOfPages(); ++i)
                    {
                        var page = pdfDocument.GetPage(i);
                        var size = page.GetPageSize();
                        strategy.SetPage(i - 1, offset, size, page.GetRotation());
                        offset += page.GetPageSize().GetHeight() + 10;
                        processor.ProcessPageContent(page);
                    }
                });
            }
            else
            {
                for (var i = 1; i <= pdfDocument.GetNumberOfPages(); ++i)
                {
                    var page = pdfDocument.GetPage(i);
                    offset += page.GetPageSize().GetHeight() + 10;
                }
            }

            var (selectableElements, text, pages) = strategy.GetSelectableElements(0, pdfDocument.GetNumberOfPages());
            try
            {
                _botPdf.AnnotationOverlay.TextSelectableElements = selectableElements;
                _botPdf.AnnotationOverlay.PageEndIndices = pages;
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            DataDocument.SetField<TextController>(KeyStore.DocumentTextKey, text, true);

            reader.Close();
            pdfDocument.Close();
            _topPdf.PdfTotalHeight = _botPdf.PdfTotalHeight = PdfTotalHeight = offset - 10;
            _botPdf.Pages.Initialize();
            _topPdf.Pages.Initialize();

            foreach (var child in this.GetDescendantsOfType<TextAnnotation>())
            {
                child.HelpRenderRegion();
            }
        }

        private static async void PropertyChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            await ((PdfView)dependencyObject).OnPdfUriChanged();
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ScrollToRegion(DocumentController target, DocumentController source = null)
        {
            _botPdf.ScrollToRegion(target, source);
            
            // TODO: functionality for more than one split maybe?
            // if (splits.Any())
            // {
            //    var botOffset = 0.0;
            //    var annoWidth = xBottomAnnotationBox.ActualWidth;
            //    foreach (var size in sizes)
            //    {
            //        var scale = (BottomScrollViewer.ViewportWidth - annoWidth) / size.Width;

            //        if (botOffset + (size.Height * scale) + 15 - splits[0] >= -1)

            //        {
            //            break;
            //        }

            //        botOffset += (size.Height * scale) + 15;
            //    }

            //    var topOffset = 0.0;
            //    annoWidth = xTopAnnotationBox.ActualWidth;
            //    foreach (var size in sizes)
            //    {
            //        var scale = (TopScrollViewer.ViewportWidth - annoWidth) / size.Width;

            //        if (topOffset + (size.Height * scale) + 15 - firstOffset >= -1)
            //        {
            //            break;
            //        }

            //        topOffset += size.Height * scale + 15;
            //    }

               //xFirstPanelRow.Height = new GridLength(1, GridUnitType.Star);
               //xSecondPanelRow.Height = new GridLength(1, GridUnitType.Star);
            //    TopScrollViewer.ChangeView(null, Math.Floor(relativeOffsets.First()) - (BottomScrollViewer.ViewportHeight + TopScrollViewer.ViewportHeight) / 4, null);
            //    BottomScrollViewer.ChangeView(null, Math.Floor(relativeOffsets.Skip(1).First()) - (BottomScrollViewer.ViewportHeight + TopScrollViewer.ViewportHeight) / 4, null, true);
            //}
            //else
            {
                
                xFirstPanelRow.Height = new GridLength(0, GridUnitType.Star);
                xSecondPanelRow.Height = new GridLength(1, GridUnitType.Star);
            }
        }
        
        private void XPdfDivider_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {

            e.Handled = true;
        }

        private void XPdfDivider_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (this.IsRightBtnPressed())
            {
                e.Complete();
            }

            e.Handled = true;
        }

        private void XPdfDivider_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void XPdfDivider_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void xPdfDivider_Tapped(object sender, TappedRoutedEventArgs e) => xFirstPanelRow.Height = new GridLength(0);

        private void xToggleActivationButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            SetActivationMode(!LinkActivationManager.ActivatedDocs.Contains(this.GetFirstAncestorOfType<DocumentView>()));
            e.Handled = true;
        }
    }
}


