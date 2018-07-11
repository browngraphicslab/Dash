using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Dash.Annotations;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Point = Windows.Foundation.Point;
using Rectangle = Windows.UI.Xaml.Shapes.Rectangle;
using WPdf = Windows.Data.Pdf;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CustomPdfView : UserControl, INotifyPropertyChanged, IVisualAnnotatable
    {
        public static readonly DependencyProperty PdfUriProperty = DependencyProperty.Register(
            "PdfUri", typeof(Uri), typeof(CustomPdfView), new PropertyMetadata(default(Uri), PropertyChangedCallback));

        public Uri PdfUri
        {
            get { return (Uri)GetValue(PdfUriProperty); }
            set { SetValue(PdfUriProperty, value); }
        }

        public static readonly DependencyProperty PageSpacingProperty = DependencyProperty.Register(
            "PageSpacing", typeof(double), typeof(CustomPdfView), new PropertyMetadata(20.0));

        public double PageSpacing
        {
            get { return (double) GetValue(PageSpacingProperty); }
            set { SetValue(PageSpacingProperty, value); }
        }

        public event EventHandler DocumentLoaded;

        private List<Image> _pages;
        public List<Image> Pages
        {
            get => _pages;
            set
            {
                _pages = value;
                OnPropertyChanged();
            }
        }

        private List<BoundsExtractionStrategy.SelectableElement> _selectableElements;

        private VisualAnnotationManager _annotationManager;
        private PageSize _pdfPageSize;

        public DocumentController LayoutDocument { get; }
        public DocumentController DataDocument { get; }

        public CustomPdfView()
        {
            this.InitializeComponent();
        }

        public CustomPdfView(DocumentController document)
        {
            this.InitializeComponent();
            LayoutDocument = document.GetActiveLayout() ?? document;
            DataDocument = document.GetDataDocument();
            _annotationManager = new VisualAnnotationManager(this, LayoutDocument, xAnnotations);
        }

        public DocumentController GetRegionDocument()
        {
            return _annotationManager?.GetRegionDocument();
        }

        private async Task OnPdfUriChanged()
        {
            if (PdfUri == null)
            {
                return;
            }

            StorageFile file;
            try
            {
                file = await StorageFile.GetFileFromApplicationUriAsync(PdfUri);
            }
            catch (ArgumentException)
            {
                try
                {
                    file = await StorageFile.GetFileFromPathAsync(PdfUri.LocalPath);
                }
                catch (ArgumentException)
                {
                    return;
                }
            }
            WPdf.PdfDocument document = await WPdf.PdfDocument.LoadFromFileAsync(file);
            List<Image> pages = new List<Image>((int)document.PageCount);
            for (uint i = 0; i < document.PageCount; ++i)
            {
                var stream = new InMemoryRandomAccessStream();
                await document.GetPage(i).RenderToStreamAsync(stream);
                var source = new BitmapImage();
                await source.SetSourceAsync(stream);
                Image page = new Image() { Source = source };
                pages.Add(page);
            }

            Pages = pages;
            DocumentLoaded?.Invoke(this, new EventArgs());

            PdfReader reader = new PdfReader(await file.OpenStreamForReadAsync());
            var doc = new PdfDocument(reader);
            _pdfPageSize = doc.GetDefaultPageSize();
            TestSelectionCanvas.Width = _pdfPageSize.GetWidth();
            TestSelectionCanvas.Height = _pdfPageSize.GetHeight() * doc.GetNumberOfPages() + (doc.GetNumberOfPages() - 1) * PageSpacing;
            var strategy = new BoundsExtractionStrategy(_pdfPageSize, PageSpacing);
            var processor = new PdfCanvasProcessor(strategy);
            for(int i = 1; i <= doc.GetNumberOfPages(); ++i)
            {
                strategy.SetPageNumber(i - 1);
                processor.ProcessPageContent(doc.GetPage(i));
            }

            _selectableElements = strategy.GetSelectableElements();
        }

        private static async void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            await ((CustomPdfView)dependencyObject).OnPdfUriChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RegionSelected(object region, Point pt, DocumentController chosenDoc = null)
        {
            _annotationManager?.RegionSelected(region, pt, chosenDoc);
        }

        public FrameworkElement Self()
        {
            return this;
        }

        public Size GetTotalDocumentSize()
        {
            return new Size(PageItemsControl.ActualWidth, PageItemsControl.ActualHeight);
        }

        public FrameworkElement GetPositionReference()
        {
            return PageItemsControl;
        }

        public DocumentController GetDocControllerFromSelectedRegion()
        {
            var dc = new RichTextNote("PDF " + ScrollViewer.VerticalOffset).Document;
            dc.SetRegionDefinition(LayoutDocument);
            
            return dc;
        }

        private void ItemsControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            //NewRegionEnded?.Invoke(sender, e);
            if (_selectionStart == null)
            {
                return;
            }
            var pos = e.GetCurrentPoint(PageItemsControl).Position;
            pos.X = pos.X / PageItemsControl.ActualWidth * _pdfPageSize.GetWidth();
            pos.Y = pos.Y / PageItemsControl.ActualHeight * _pdfPageSize.GetHeight();
            foreach (var selectableElement in _selectableElements)
            {
                if (selectableElement.Bounds.Contains(pos))
                {
                    int startIndex = Math.Min(selectableElement.Index, _selectionStart.Index);
                    int endIndex = Math.Max(selectableElement.Index, _selectionStart.Index);
                    for (int i = startIndex; i <= endIndex; ++i)
                    {
                        AddRect(_selectableElements[i]);
                    }
                    break;
                }
            }
        }

        private void ItemsControl_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            //NewRegionMoved?.Invoke(sender, e);
        }

        private BoundsExtractionStrategy.SelectableElement _selectionStart;

        private void ItemsControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //NewRegionStarted?.Invoke(sender, e);
            var pos = e.GetCurrentPoint(PageItemsControl).Position;
            pos.X = pos.X / PageItemsControl.ActualWidth * _pdfPageSize.GetWidth();
            pos.Y = pos.Y / PageItemsControl.ActualHeight * _pdfPageSize.GetHeight();
            foreach (var selectableElement in _selectableElements)
            {
                if (selectableElement.Bounds.Contains(pos))
                {
                    _selectionStart = selectableElement;
                    break;
                }
            }
        }

        private void AddRect(BoundsExtractionStrategy.SelectableElement element)
        {
            Rectangle r = new Rectangle
            {
                Width = element.Bounds.Width,
                Height = element.Bounds.Height,
            };
            Canvas.SetLeft(r, element.Bounds.X);
            Canvas.SetTop(r, element.Bounds.Y);
            r.Fill = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
            TestSelectionCanvas.Children.Add(r);
        }

        public event PointerEventHandler NewRegionStarted;
        public event PointerEventHandler NewRegionMoved;
        public event PointerEventHandler NewRegionEnded;
    }
}
