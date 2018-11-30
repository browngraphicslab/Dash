using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Dash.Annotations;
using iText.Kernel.Crypto;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using DashShared;
using Microsoft.Toolkit.Uwp.Helpers;
using Point = Windows.Foundation.Point;
using Rectangle = Windows.UI.Xaml.Shapes.Rectangle;
using WPdf = Windows.Data.Pdf;
using Windows.UI.Core;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class PdfView : UserControl, INotifyPropertyChanged, ILinkHandler
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public static readonly DependencyProperty PdfUriProperty = DependencyProperty.Register("PdfUri", typeof(Uri), typeof(PdfView), new PropertyMetadata(default(Uri), PropertyChangedCallback));

        //This might be more efficient as a linked list of KV pairs if our selections are always going to be contiguous
        private static LocalPDFEndpoint    _pdfEndpoint = RESTClient.Instance.GetPDFEndpoint();
        private Dictionary<int, Rectangle> _selectedRectangles = new Dictionary<int, Rectangle>();
        private readonly List<int>         _previousSelections = new List<int>();
        private double                     _pdfMaxWidth;
        private StorageFile                _file;
        private int                        _searchEnd = 0;
        private bool                       _pdfTextInitialized =false;
        public StorageFile        File => _file;
        public PdfAnnotationView  DefaultView => _botPdf;
        public DocumentController DataDocument => (DataContext as DocumentViewModel)?.DataDocument;
        public DocumentController LayoutDocument => (DataContext as DocumentViewModel)?.LayoutDocument;
        public Uri                PdfUri
        {
            get => (Uri)GetValue(PdfUriProperty);
            set => SetValue(PdfUriProperty, value);
        }
        public double             PdfMaxWidth
        {
            get => _pdfMaxWidth;
            set
            {
                _pdfMaxWidth = value;
                OnPropertyChanged();
            }
        }
        //This makes the assumption that both pdf views are always in the same annotation mode
        public AnnotationType     CurrentAnnotationType => _botPdf.AnnotationOverlay.CurrentAnnotationType;
        public LocalPDFEndpoint   PdfEndpoint => _pdfEndpoint;

        public PdfView()
        {
            InitializeComponent();
            _topPdf.Visibility = Visibility.Collapsed;
            bool initialized = false;
            Loaded += (s, e) =>
            {
                SelectionManager.SelectionChanged += SelectionManager_SelectionChanged;
                if (LayoutDocument.GetField(KeyStore.GoToRegionKey) != null)
                {
                    GoToUpdatedFieldChanged(LayoutDocument, null);
                }

                if (!initialized)
                {
                    LayoutDocument.AddWeakFieldUpdatedListener(this, KeyStore.SearchIndexKey,  (view, controller, arg3) => view.SearchIndexUpdated(controller, arg3));
                    LayoutDocument.AddWeakFieldUpdatedListener(this, KeyStore.SearchStringKey, (view, controller, arg3) => view.SearchStringUpdated(controller, arg3));
                    LayoutDocument.AddWeakFieldUpdatedListener(this, KeyStore.GoToRegionKey,   (view, controller, arg3) => view.GoToUpdatedFieldChanged(controller, arg3));

                    initialized = true;
                }
            };
            Unloaded += (s, e) => SelectionManager.SelectionChanged -= SelectionManager_SelectionChanged;
            SizeChanged += (ss, ee) =>
            {
                if (xBar.Width != 0)
                {
                    xBar.Width = ActualWidth;
                    if (ee.PreviousSize.Width > 0)
                    {
                        _botPdf.SetLeftMargin(_botPdf.LeftMargin * ee.NewSize.Width / ee.PreviousSize.Width);
                        _botPdf.SetRightMargin(_botPdf.RightMargin * ee.NewSize.Width / ee.PreviousSize.Width);
                        xRightMargin.Margin = new Thickness(0, 0, _botPdf.RightMargin, 0);
                        xLeftMargin.Margin = new Thickness(_botPdf.LeftMargin, 0, 0, 0);
                    }
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
            Debug.WriteLine("FINALIZING PdfView");
        }

        public async Task<List<DocumentController>> ExplodePages()
        {
            var pages = new List<DocumentController>();
            var pdfDocument = new PdfDocument(new PdfReader(await _file.OpenStreamForReadAsync()));
            int n = pdfDocument.GetNumberOfPages();
            var title = DataDocument.GetTitleFieldOrSetDefault().Data;
            var psplit = new iText.Kernel.Utils.PdfSplitter(pdfDocument);
            for (int i = 1; i <= n; i++)
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var uniqueFilePath = _file.Path.Replace(".pdf", "-" + i + ".pdf");
                var exists = await localFolder.TryGetItemAsync(Path.GetFileName(uniqueFilePath)) != null;
                var localFile = await localFolder.CreateFileAsync(Path.GetFileName(uniqueFilePath), CreationCollisionOption.OpenIfExists);
                if (!exists)
                {
                    var pw = new PdfWriter(new FileInfo(localFolder.Path + "/" + Path.GetFileName(uniqueFilePath)));
                    var outDoc = new PdfDocument(pw);
                    pdfDocument.CopyPagesTo(new List<int>(new int[] { i }), outDoc);
                    outDoc.Close();
                }
                var doc = new PdfToDashUtil().GetPDFDoc(localFile, title.Substring(0, title.IndexOf(".pdf")) + ":" + i + ".pdf");
                doc.GetDataDocument().SetField<TextController>(KeyStore.SourceUriKey, DataDocument.Id, true);
                pages.Add(doc);
            }
            pdfDocument.Close();
            return pages;
        }

        public int  PageNum()       { return _botPdf.PageNum(); }
        public void NextPage()      { _botPdf.PageNext(); }
        public void PrevPage()      { _botPdf.PagePrev(); }
        public void ScrollBack()    { _botPdf.PopBackStack(); }
        public void ScrollForward() { _botPdf.PopForwardStack(); }
        public void ScrollToRegion(DocumentController target, PdfAnnotationView activeView = null)
        {
            var absoluteRegionOffsets = target.GetDataDocument().GetField<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey) ?? new ListController<PointController>();
            var absoluteTextOffsets  = target.GetDataDocument().GetField<ListController<RectController>>(KeyStore.SelectionBoundsKey) ?? new ListController<RectController>();
            var relativeOffsets  = absoluteRegionOffsets.Select(a => a.Data.Y).Concat(absoluteTextOffsets.Select(a => a.Data.Y));
            if (relativeOffsets.Count() > 0 && PdfMaxWidth > 0)
            {
                var maxOffset = _botPdf.ScrollViewer.ViewportHeight;
                var firstSplit = relativeOffsets.Skip(1).FirstOrDefault(ro => ro - relativeOffsets.First() > maxOffset);

                if (firstSplit != 0)
                {
                    _topPdf.ScrollToPosition(Math.Floor(relativeOffsets.First()));
                    if (xFirstPanelRow.ActualHeight < 20)
                    {
                        xFirstPanelRow.Height  = new GridLength(xSecondPanelRow.Height.Value / 2, GridUnitType.Star);
                        xSecondPanelRow.Height = new GridLength(xSecondPanelRow.Height.Value / 2, GridUnitType.Star);
                    }
                }
                else
                {
                    xFirstPanelRow.Height = activeView == null ? new GridLength(0, GridUnitType.Star) : xFirstPanelRow.Height;
                }
                if (xFirstPanelRow.Height.Value != 0)
                {
                    _topPdf.Visibility = Visibility.Visible;
                }
                else
                {
                    _topPdf.Visibility = Visibility.Collapsed;
                }
                // bcz: Ugh need to update layout because the Scroll viewer may not end up in the right place if its viewport size has just changed
                (activeView ?? _botPdf).UpdateLayout();
                (firstSplit == 0 ? activeView ?? _botPdf : _botPdf).ScrollToPosition((firstSplit == 0 ? relativeOffsets.First() : firstSplit));
            }
        }

        public void GoToPage(double pageNum)
        {
            _botPdf.GoToPage(pageNum);
        }
        public void ShowRegions()
        {
            _topPdf.AnnotationOverlay.Visibility      = Visibility.Visible;
            _topPdf.xPdfGridWithEmbeddings.Visibility = Visibility.Visible;
            _botPdf.AnnotationOverlay.Visibility      = Visibility.Visible;
            _botPdf.xPdfGridWithEmbeddings.Visibility = Visibility.Visible;
        }
        public void HideRegions()
        {
            _topPdf.AnnotationOverlay.Visibility      = Visibility.Collapsed;
            _topPdf.xPdfGridWithEmbeddings.Visibility = Visibility.Collapsed;
            _botPdf.AnnotationOverlay.Visibility      = Visibility.Collapsed;
            _botPdf.xPdfGridWithEmbeddings.Visibility = Visibility.Collapsed;
        }
        public bool AreAnnotationsVisible()
        {
            //This makes the assumption that both overlays are kept in sync
            return _botPdf.AnnotationOverlay.Visibility == Visibility.Visible;
        }
        public LinkHandledResult HandleLink(DocumentController linkDoc, LinkDirection direction)
        {
            var activePdf = _topPdf.ActiveView ? _topPdf : _botPdf;
            var source = linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkSourceKey);
            if (activePdf.AnnotationOverlay.RegionDocsList.Contains(source))
            {
                var absoluteOffsets = source.GetField<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey);
                if (absoluteOffsets.Count > 1)
                {
                    ScrollToRegion(source, activePdf);
                }
                var src = activePdf.GetDescendantsOfType<DocumentView>().Where((dv) => dv.ViewModel.DataDocument.Equals(source.GetDataDocument()));
                if (src.Count() > 0)
                {
                    src.ToList().ForEach((s) => s.ViewModel.LayoutDocument.ToggleHidden());
                    return LinkHandledResult.HandledClose;
                }
            }
            var target = linkDoc.GetLinkedDocument(direction);
            var tgts = activePdf.GetDescendantsOfType<DocumentView>().Where((dv) => dv.ViewModel.DataDocument.Equals(target?.GetDataDocument()));
            if (tgts.Count() > 0)
            {
                tgts.ToList().ForEach((tgt) =>
                {
                    tgt.ViewModel.LayoutDocument.ToggleHidden();
                    tgt.ViewModel.SetHighlight(!tgt.ViewModel.LayoutDocument.GetHidden());
                });
                return LinkHandledResult.HandledClose;
            }
            return LinkHandledResult.Unhandled;
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
        public async Task<DocumentController> GetRegionDocument(Point? docViewPoint = null)
        {
            return await _botPdf.GetRegionDocument(!docViewPoint.HasValue ? docViewPoint : Util.PointTransformFromVisual(docViewPoint.Value, this, _botPdf.AnnotationOverlay));
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

            var reader = new PdfReader(await _file.OpenStreamForReadAsync());
            var pdfDocument = new PdfDocument(reader);
            _topPdf.PdfMaxWidth = _botPdf.PdfMaxWidth = PdfMaxWidth = CalculateMaxPDFWidth(pdfDocument);
            _topPdf.PdfTotalHeight = _botPdf.PdfTotalHeight = await LoadPdfFromFile(pdfDocument);
            if (this.IsInVisualTree())  // bcz: super hack! --  shouldn't need to set the PdfHeight anyway, or at least not here (used for the export Publisher)
            {
                DataDocument.SetField<PointController>(KeyStore.PdfHeightKey, new Point(pdfDocument.GetPage(1).GetPageSize().GetWidth(), pdfDocument.GetPage(1).GetPageSize().GetHeight()), true);
            }
            reader.Close();
            pdfDocument.Close();

            _botPdf.Pages.Initialize();
            _topPdf.Pages.Initialize();

            this.GetDescendantsOfType<TextAnnotation>().ToList().ForEach((child) => child.HelpRenderRegion());
            if (LayoutDocument?.GetField(KeyStore.GoToRegionKey) != null)
            {
                GoToUpdatedFieldChanged(LayoutDocument, null);
            }
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
            var pdfTotalHeight = 0.0;
            await Task.Run(() =>
            {
                for (var i = 1; i <= pdfDocument.GetNumberOfPages(); ++i)
                {
                    var page = pdfDocument.GetPage(i);
                    pdfTotalHeight += page.GetPageSize().GetHeight() + 10;
                }
            });

            _topPdf.PDFdoc = _botPdf.PDFdoc = await WPdf.PdfDocument.LoadFromFileAsync(_file);

            return pdfTotalHeight - 10;
        }

        private void GoToUpdatedFieldChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            var newValue = args?.NewValue != null ? args.NewValue as DocumentController : sender.GetField<DocumentController>(KeyStore.GoToRegionKey);
            if (newValue != null && (sender.GetField(KeyStore.GoToRegionKey) != null || sender.GetField(KeyStore.GoToRegionLinkKey) != null))
            {
                ScrollToRegion(newValue);
                _botPdf.AnnotationOverlay?.SelectRegion(newValue);
                _topPdf.AnnotationOverlay?.SelectRegion(newValue);
            }
        }

        private static async void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
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

        private void SearchStringUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            _previousSelections.Clear();
            _searchEnd = 0;
            if (DataContext != null)
            {
                var prevIndex = LayoutDocument.GetDereferencedField<NumberController>(KeyStore.SearchIndexKey, null)?.Data ?? -1;
                if (prevIndex != -1)
                {
                    if (prevIndex > 0)
                    {
                        LayoutDocument.SetField<NumberController>(KeyStore.SearchIndexKey, 0, true);
                    }
                    else
                    {
                        refreshSearch((args.NewValue as TextController)?.Data ?? "", 0);
                    }
                }
            }
        }
        private void SearchIndexUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            var searchString = sender.GetField<TextController>(KeyStore.SearchStringKey)?.Data.ToLower();
            refreshSearch(searchString, (int)((args.NewValue as NumberController)?.Data ?? 0));
        }
        private void refreshSearch(string searchString, int searchIndex)
        {
            if (!string.IsNullOrEmpty(searchString) && _botPdf.AnnotationOverlay.TextSelectableElements != null)
            {
                if (searchIndex < _previousSelections.Count)
                {
                    _topPdf.UpdateMatchedSearch(searchString, _previousSelections[searchIndex]);
                    if (xTopPdfGrid.ActualHeight == 0)
                    {
                        _botPdf.UpdateMatchedSearch(searchString, _previousSelections[searchIndex]);
                    }
                }
                else
                {
                    for (int i = 0; _searchEnd < _botPdf.AnnotationOverlay.TextSelectableElements.Count; _searchEnd++)
                    {
                        var elem = _botPdf.AnnotationOverlay.TextSelectableElements[_searchEnd];
                        if (elem.Contents is string str && str.ToLower()[0] == searchString[i++])
                        {
                            if (searchString.Length == i)
                            {
                                _topPdf.UpdateMatchedSearch(searchString, _searchEnd - i + 1);
                                if (xTopPdfGrid.ActualHeight == 0)
                                {
                                    _botPdf.UpdateMatchedSearch(searchString, _searchEnd - i + 1);
                                }
                                _previousSelections.Add(_searchEnd - i + 1);
                                return;
                            }
                        }
                        else
                        {
                            i = 0;
                        }
                    }
                    if (_previousSelections.Count > 0) // if we had previous selections, start over
                    {
                        LayoutDocument.SetField<NumberController>(KeyStore.SearchIndexKey, 0, true);
                    }
                }
            }
        }
        private async void SelectionManager_SelectionChanged(DocumentSelectionChangedEventArgs args)
        {
            if (SelectionManager.IsSelected(this.GetFirstAncestorOfType<DocumentView>()) && PdfUri != null && !_pdfTextInitialized)
            {
                var uri = PdfUri;
                try
                {
                    if (_pdfTextInitialized = await _pdfEndpoint.ContainsPDF(uri))
                    {
                        var (elems, pages) = await _pdfEndpoint.GetSelectableElements(uri);
                        _topPdf.InitializeRegions(elems, pages);
                        _botPdf.InitializeRegions(elems, pages);
                    }
                }
                catch (Exception ex) {  Console.WriteLine(ex.ToString()); }

                if (!_pdfTextInitialized)
                {
                    await Task.Run(async () =>
                    {
                        _pdfTextInitialized = true;
                        var newstrategy = new BoundsExtractionStrategy(new PdfDocument(new PdfReader(await _file.OpenStreamForReadAsync())));
                        var (selectableElements, authors, text, pages, vagueSections) = newstrategy.GetSelectableElements(0, newstrategy.Pages.Count);
                        await _pdfEndpoint.AddPdf(uri, pages, selectableElements);
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>  // needs to run on UI thread but may be called from a PDF Task
                        {
                            DataDocument?.SetField<TextController>(KeyStore.AuthorKey, authors, true);
                            DataDocument?.SetField<TextController>(KeyStore.DocumentTextKey, text, true);
                            _topPdf.InitializeRegions(selectableElements, pages);
                            _botPdf.InitializeRegions(selectableElements, pages);
                        });
                    });
                }
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
            _topPdf.Visibility = Visibility.Visible;
            e.Handled = true;
        }
        private void XPdfDivider_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }
        private void xPdfDivider_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _topPdf.Visibility = Visibility.Collapsed;
            xFirstPanelRow.Height = new GridLength(0);
        }

        private void xRightMarginPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            xRightMargin.PointerMoved += xRightMarginPointerMoved;
            xRightMargin.CapturePointer(e.Pointer);
            e.Handled = true;
        }
        private void xRightMarginPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var margin = Math.Max(0, xPdfContainer.ActualWidth - e.GetCurrentPoint(xPdfContainer).Position.X - 90);
            xRightMargin.Margin = new Thickness(0, 0, margin - 2.5, 0);
            _botPdf.SetRightMargin(margin);
            _topPdf.SetRightMargin(margin);

            HackToFixMargins();
        }
        private void xRightMarginPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            xRightMargin.ReleasePointerCapture(e.Pointer);
            xRightMargin.PointerMoved -= xRightMarginPointerMoved;
        }
        private void xRightMargin_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _botPdf.SetRightMargin(_botPdf.RightMargin > 0 ? 0 : ActualWidth / 6);
            _topPdf.SetRightMargin(_botPdf.RightMargin);
            xRightMargin.Margin = new Thickness(0, 0, _botPdf.RightMargin - 2.5, 0);
            HackToFixMargins();
        }

        private void xLeftMarginPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            xLeftMargin.PointerMoved += xLeftMarginPointerMoved;
            xLeftMargin.CapturePointer(e.Pointer);
            e.Handled = true;
        }
        private void xLeftMarginPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var margin = Math.Max(0, e.GetCurrentPoint(xPdfContainer).Position.X - 90);
            xLeftMargin.Margin = new Thickness(margin - 2.5, 0, 0, 0);
            _botPdf.SetLeftMargin(margin);
            _topPdf.SetLeftMargin(margin);

            HackToFixMargins();
        }
        private void xLeftMarginPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            xLeftMargin.ReleasePointerCapture(e.Pointer);
            xLeftMargin.PointerMoved -= xLeftMarginPointerMoved;
        }
        private void xLeftMargin_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _botPdf.SetLeftMargin(_botPdf.LeftMargin > 0 ? 0 : ActualWidth / 6);
            _topPdf.SetLeftMargin(_botPdf.LeftMargin);
            xLeftMargin.Margin = new Thickness(_botPdf.LeftMargin - 2.5, 0, 0, 0);

            HackToFixMargins();
        }
        
        private void HackToFixMargins()
        {
            for (int i = 0; i < 5; i++)
            {
                UpdateLayout(); // bcz: need to figure out what's making things take multiple updates to get back in synch => Calculate what ScrollViewer.ExtentHeight will be...
                _botPdf.UpdateMargins();
                _topPdf.UpdateMargins();
            }
        }
    }
}


