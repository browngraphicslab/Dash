﻿using Dash.Annotations;
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
using Microsoft.Toolkit.Uwp.UI.Controls;

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
        private double            _pdfMaxWidth;
        private DocumentViewModel _lastVm = null;
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
        //This makes the assumption that both pdf views are always in the same annotation mode
        public AnnotationType                     CurrentAnnotationType => _botPdf.AnnotationOverlay.CurrentAnnotationType;

        public PdfView()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                LayoutDocument.AddFieldUpdatedListener(KeyStore.GoToRegionKey, GoToUpdatedFieldChanged);
                _lastVm = DataContext as DocumentViewModel;
            };
            Unloaded += (s,e) => _lastVm.LayoutDocument.RemoveFieldUpdatedListener(KeyStore.GoToRegionKey, GoToUpdatedFieldChanged);
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
            if (_botPdf.AnnotationOverlay.RegionDocsList.Contains(linkDoc.GetDataDocument()
                .GetField<DocumentController>(KeyStore.LinkSourceKey)))
            {
                var src = linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkSourceKey);
                ScrollToRegion(src);
                //return LinkHandledResult.HandledClose;
            }

            var target = linkDoc.GetLinkedDocument(direction);
            if (_botPdf.AnnotationOverlay.RegionDocsList.Contains(target))
            {
                ScrollToRegion(target, linkDoc.GetLinkedDocument(direction, true));
                return LinkHandledResult.HandledClose;
            }

            return LinkHandledResult.Unhandled;
        }
        public void SetAnnotationsVisibleOnScroll(bool? visibleOnScroll)
        {
            _botPdf.SetAnnotationsVisibleOnScroll(visibleOnScroll);
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

        private async Task OnPdfUriChanged()
        {
            try
            {
                _file = (PdfUri.AbsoluteUri.StartsWith("ms-appx://") || PdfUri.AbsoluteUri.StartsWith("ms-appdata://")) ?
                    await StorageFile.GetFileFromApplicationUriAsync(PdfUri) :
                    await StorageFile.GetFileFromPathAsync(PdfUri.LocalPath);
            }
            catch (ArgumentException)
            {
                return;
            }

            var reader      = new PdfReader(await _file.OpenStreamForReadAsync());
            var pdfDocument = new PdfDocument(reader);
            _topPdf.PdfMaxWidth    = _botPdf.PdfMaxWidth = PdfMaxWidth = CalculateMaxPDFWidth(pdfDocument);
            _topPdf.PdfTotalHeight = _botPdf.PdfTotalHeight = await LoadPdfFromFile(pdfDocument);
            reader.Close();
            pdfDocument.Close();

            _botPdf.Pages.Initialize();
            _topPdf.Pages.Initialize();

            this.GetDescendantsOfType<TextAnnotation>().ToList().ForEach((child) => child.HelpRenderRegion());
        }

        private double CalculateMaxPDFWidth(PdfDocument pdfDocument)
        {
            var maxWidth = 0.0;
            for (var i = 1; i <= pdfDocument.GetNumberOfPages(); ++i)
            {
                var page = pdfDocument.GetPage(i);
                _topPdf.Pages.PageSizes.Add(new Size(page.GetPageSize().GetWidth(), page.GetPageSize().GetHeight()));
                _botPdf.Pages.PageSizes.Add(new Size(page.GetPageSize().GetWidth(), page.GetPageSize().GetHeight()));
                maxWidth = Math.Max(maxWidth, page.GetPageSize().GetWidth());
            }
            return maxWidth;
        }

        private async Task<double> LoadPdfFromFile(PdfDocument pdfDocument)
        {
            var strategy       = new BoundsExtractionStrategy();
            var pdfTotalHeight = 0.0;
            await Task.Run(() =>
            {
                for (var i = 1; i <= pdfDocument.GetNumberOfPages(); ++i)
                {
                    var page = pdfDocument.GetPage(i);
                    pdfTotalHeight += page.GetPageSize().GetHeight() + 10;
                    if (MainPage.Instance.xSettingsView.UsePdfTextSelection)
                    {
                        strategy.SetPage(i - 1, pdfTotalHeight, page.GetPageSize(), page.GetRotation());
                        new PdfCanvasProcessor(strategy).ProcessPageContent(page);
                    }
                }
            });

            _topPdf.PDFdoc = _botPdf.PDFdoc = await WPdf.PdfDocument.LoadFromFileAsync(_file);
            var (selectableElements, text, pages) = strategy.GetSelectableElements(0, pdfDocument.GetNumberOfPages());
            try
            {
                _botPdf.AnnotationOverlay.TextSelectableElements = selectableElements;
                _botPdf.AnnotationOverlay.PageEndIndices = pages;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return pdfTotalHeight-10;
        }

        private void GoToUpdatedFieldChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            if (args.NewValue != null && (sender.GetField(KeyStore.GoToRegionKey) != null || sender.GetField(KeyStore.GoToRegionLinkKey) != null))
            {
                ScrollToRegion(args.NewValue as DocumentController);
                _botPdf.AnnotationOverlay.SelectRegion(args.NewValue as DocumentController);

                sender.RemoveField(KeyStore.GoToRegionKey);
                sender.RemoveField(KeyStore.GoToRegionLinkKey);
            }
        }

        private static async void PropertyChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is PdfView pdfView && pdfView.PdfUri != null)
            {
                try
                {
                    await pdfView.OnPdfUriChanged();
                }
                catch (BadPasswordException)
                {
                }
            }
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ScrollToRegion(DocumentController target, DocumentController source = null)
        {
            var absoluteOffsets = target.GetField<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey);
            if (absoluteOffsets != null)
            {
                var relativeOffsets = absoluteOffsets.TypedData.Select(p => p.Data.Y * (xPdfCol.ActualWidth / PdfMaxWidth)).ToList();
                var maxOffset       = _botPdf.ScrollViewer.ViewportHeight;
                var firstSplit      = relativeOffsets.Skip(1).Where((ro) => ro - relativeOffsets.First() > maxOffset).FirstOrDefault();

                if (firstSplit != 0)
                {
                    _topPdf.ScrollViewer.ChangeView(null, Math.Floor(relativeOffsets.First()) - ActualHeight / 4, null);
                }
                xFirstPanelRow.Height  = new GridLength(firstSplit != 0 ? 1 : 0, GridUnitType.Star);
                xSecondPanelRow.Height = new GridLength(1, GridUnitType.Star);
                // bcz: Ugh need to update layout because the Scroll viewer may not end up in the right place if its viewport size has just changed
                _botPdf.UpdateLayout();
                _botPdf.ScrollViewer.ChangeView(null, (firstSplit == 0 ? relativeOffsets.First() : firstSplit) - ActualHeight / (firstSplit != 0 ? 4 : 2), null);
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

        private void xPdfColSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (xPdfCol.Width.GridUnitType == GridUnitType.Pixel)
                xPdfNotesCol.Width = new GridLength(1, GridUnitType.Star);
            _botPdf.xPdfCol.Width = new GridLength(xPdfCol.ActualWidth);
            _topPdf.xPdfCol.Width = new GridLength(xPdfCol.ActualWidth);

        }
        private void xSiderbarSplitter_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xPdfNotesCol.Width = new GridLength(1, GridUnitType.Star);
            xPdfCol.Width = xPdfCol.ActualWidth == ActualWidth - 10 ? new GridLength(ActualWidth / 2) : new GridLength(ActualWidth - 10);
            _botPdf.xPdfCol.Width = new GridLength(xPdfCol.Width.Value);
            _topPdf.xPdfCol.Width = new GridLength(xPdfCol.Width.Value);
        }
        private void xSiderbarSplitter_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {

            e.Handled = true;
        }

        private void xSiderbarSplitter_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (this.IsRightBtnPressed())
            {
                e.Complete();
            }

            e.Handled = true;
        }

        private void xSiderbarSplitter_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void xSiderbarSplitter_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }
    }
}


