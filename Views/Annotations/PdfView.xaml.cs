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
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using iText.Kernel.Crypto;
using FrameworkElement = Windows.UI.Xaml.FrameworkElement;
using Point = Windows.Foundation.Point;
using Rectangle = Windows.UI.Xaml.Shapes.Rectangle;
using WPdf = Windows.Data.Pdf;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.UI.Xaml.Data;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class PdfView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public static readonly DependencyProperty PdfUriProperty = DependencyProperty.Register(
            "PdfUri", typeof(Uri), typeof(PdfView), new PropertyMetadata(default(Uri), PropertyChangedCallback));

        //This might be more efficient as a linked list of KV pairs if our selections are always going to be contiguous
        private Dictionary<int, Rectangle> _selectedRectangles = new Dictionary<int, Rectangle>();
        private StorageFile _file;
        private double _pdfMaxWidth;
        public PdfAnnotationView DefaultView => _botPdf;
        public DocumentController DataDocument => (DataContext as DocumentViewModel).DataDocument;
        public DocumentController LayoutDocument => (DataContext as DocumentViewModel).LayoutDocument;
        public Uri PdfUri
        {
            get => (Uri)GetValue(PdfUriProperty);
            set => SetValue(PdfUriProperty, value);
        }
        public double PdfMaxWidth
        {
            get => _pdfMaxWidth;
            set
            {
                _pdfMaxWidth = value;
                OnPropertyChanged();
            }
        }
        //This makes the assumption that both pdf views are always in the same annotation mode
        public AnnotationType CurrentAnnotationType => _botPdf.AnnotationOverlay.CurrentAnnotationType;

        private static LocalPDFEndpoint _pdfEndpoint = RESTClient.Instance.GetPDFEndpoint();
        private int _searchEnd = 0;

        public PdfView()
        {
            InitializeComponent();
            _topPdf.Visibility = Visibility.Collapsed;
            Loaded += (s, e) =>
            {
                LayoutDocument.AddWeakFieldUpdatedListener(this, KeyStore.GoToRegionKey,
                        (view, controller, arg3) => view.GoToUpdatedFieldChanged(controller, arg3));
                LayoutDocument.AddFieldUpdatedListener(KeyStore.SearchIndexKey, SearchIndexUpdated);
                LayoutDocument.AddFieldUpdatedListener(KeyStore.SearchStringKey, SearchStringUpdated);
                LayoutDocument.AddFieldUpdatedListener(KeyStore.SearchPreviousIndexKey, SearchPreviousPressed);
            };
            Unloaded += (s, e) =>
            {
                LayoutDocument.RemoveFieldUpdatedListener(KeyStore.SearchIndexKey, SearchIndexUpdated);
            };
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

            SelectionManager.SelectionChanged += SelectionManager_SelectionChanged;

            //_botPdf.CanSetAnnotationVisibilityOnScroll = true;
        }

        private void SearchPreviousPressed(DocumentController sender,
            DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            if (LayoutDocument.GetField<BoolController>(KeyStore.SearchPreviousIndexKey).Data)
            {
                if (_previousSelections.Count < 2)
                {
                    LayoutDocument.SetField<BoolController>(KeyStore.SearchPreviousIndexKey, false, true);
                    return;
                }
                var searchString = sender.GetField<TextController>(KeyStore.SearchStringKey).Data.ToLower();
                _previousSelections.Remove(_previousSelections.Last());
                _botPdf.AnnotationOverlay.ClearSelection();
                _searchEnd = _previousSelections.Last() + searchString.Length;
                for (int i = 0; i < searchString.Length; i++)
                {
                    _botPdf.AnnotationOverlay.SelectIndex(_searchEnd - i);
                }

                _botPdf.ScrollToPosition(_botPdf.AnnotationOverlay.TextSelectableElements[_searchEnd].Bounds
                    .Top);
                LayoutDocument.SetField<BoolController>(KeyStore.SearchPreviousIndexKey, false, true);
            }
        }

        private void SearchStringUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            LayoutDocument.RemoveFieldUpdatedListener(KeyStore.SearchIndexKey, SearchIndexUpdated);
            _previousSelections.Clear();
            LayoutDocument.SetField<NumberController>(KeyStore.SearchIndexKey, 0, true);
            prevIndex = -1;
            _searchEnd = 0;
            LayoutDocument.AddFieldUpdatedListener(KeyStore.SearchIndexKey, SearchIndexUpdated);
        }

        private int prevIndex = -1;
        private readonly List<int> _previousSelections = new List<int>();

        private void SearchIndexUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            var searchString = sender.GetField<TextController>(KeyStore.SearchStringKey).Data.ToLower();
            int i = 0;
            var searchIndex = (int)sender.GetField<NumberController>(KeyStore.SearchIndexKey).Data;

            // if (searchIndex + 1 > prevIndex)
            {
                for (var index = _searchEnd; index < _botPdf.AnnotationOverlay.TextSelectableElements.Count; index++)
                {
                    var elem = _botPdf.AnnotationOverlay.TextSelectableElements[index];
                    if (i >= searchString.Length || (elem.Contents as string).ToLower()[0].Equals(searchString[i]))
                    {
                        i++;
                        if (searchString.Length == i)
                        {
                            _botPdf.AnnotationOverlay.ClearSelection();

                            _searchEnd = index;
                            for (int j = 0; j < i; j++)
                            {
                                _botPdf.AnnotationOverlay.SelectIndex(index - j);
                            }

                            prevIndex = (int)sender.GetField<NumberController>(KeyStore.SearchIndexKey).Data;

                            _botPdf.ScrollToPosition(_botPdf.AnnotationOverlay.TextSelectableElements[index - i].Bounds
                                .Top);
                            _previousSelections.Add(index - i);
                            return;
                        }
                    }
                    else
                    {
                        i = 0;
                    }
                }

                //sender.SetField<NumberController>(KeyStore.SearchIndexKey, 0, true);
            }
            /*else
            {
                if (searchIndex < 1)
                {
                    _searchEnd = _botPdf.AnnotationOverlay.TextSelectableElements.Count;
                }
                var reversedString = searchString.Reverse().ToList();
                for (var index = _searchEnd - searchString.Length; index >= 0; index--)
                {
                    var elem = _botPdf.AnnotationOverlay.TextSelectableElements[index];
                    if (i >= searchString.Length || (elem.Contents as string).ToLower()[0].Equals(reversedString[i]))
                    {
                        i++;
                        if (searchString.Length == i)
                        {
                            _botPdf.AnnotationOverlay.ClearSelection();

                            _searchEnd = index;
                            for (int j = 0; j < i; j++)
                            {
                                _botPdf.AnnotationOverlay.SelectIndex(index + j);
                            }

                            prevIndex = (int)sender.GetField<NumberController>(KeyStore.SearchIndexKey).Data;

                            _botPdf.ScrollToPosition(_botPdf.AnnotationOverlay.TextSelectableElements[index - i].Bounds.Top);
                            sender.SetField<NumberController>(KeyStore.SearchIndexKey, searchIndex - 1, true);
                            return;
                        }
                    }
                    else
                    {
                        i = 0;
                    }
                }
            }*/
        }

        private async void SelectionManager_SelectionChanged(DocumentSelectionChangedEventArgs args)
        {
            if (SelectionManager.IsSelected(this.GetFirstAncestorOfType<DocumentView>()))
            {
                var uri = PdfUri;
                string textToSet = null;
                await Task.Run(async () =>
                {
                    bool hasPdf;
                    try
                    {
                        hasPdf = await _pdfEndpoint.ContainsPDF(uri);
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine(ex.ToString());
                        hasPdf = false;
                    }

                    if (hasPdf)
                    {
                        try
                        {
                            var (elems, pages) = await _pdfEndpoint.GetSelectableElements(uri);
                            _botPdf.AnnotationOverlay.TextSelectableElements =
                                new List<SelectableElement>(elems);
                            _botPdf.AnnotationOverlay.PageEndIndices = pages;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                    else
                    {
                        var reader = new PdfReader(await _file.OpenStreamForReadAsync());
                        var pdfDocument = new PdfDocument(reader);
                        var newstrategy = new BoundsExtractionStrategy();
                        var pdfTotalHeight = 0.0;
                        for (var i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                        {
                            //if (MainPage.Instance.xSettingsView.UsePdfTextSelection)
                            var page = pdfDocument.GetPage(i);
                            newstrategy.SetPage(i - 1, pdfTotalHeight, page.GetPageSize(), page.GetRotation());
                            new PdfCanvasProcessor(newstrategy).ProcessPageContent(page);
                            pdfTotalHeight += page.GetPageSize().GetHeight() + 10;
                        }

                        var (selectableElements, text, pages, vagueSections) =
                            newstrategy.GetSelectableElements(0, pdfDocument.GetNumberOfPages());
                        _botPdf.AnnotationOverlay.TextSelectableElements =
                            new List<SelectableElement>(selectableElements);
                        _botPdf.AnnotationOverlay.PageEndIndices = pages;
                        textToSet = text;
                        await _pdfEndpoint.AddPdf(uri, pages, selectableElements);
                    }
                });
                if (textToSet != null)
                {
                    _botPdf.DataDocument.SetField<TextController>(KeyStore.DocumentTextKey, textToSet, true);
                }
            }
            else if (_botPdf.AnnotationOverlay.TextSelectableElements?.Any() ?? false)
            {
                _botPdf.AnnotationOverlay.TextSelectableElements = new List<SelectableElement>();
                _botPdf.AnnotationOverlay.PageEndIndices = new List<int>();
            }
        }

        ~PdfView()
        {
            Debug.WriteLine("FINALIZING PdfView");
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
            reader.Close();
            pdfDocument.Close();
            return pages;
        }
        public void GoToPage(double pageNum)
        {
            _botPdf.GoToPage(pageNum);
        }

        public void NextPage() { _botPdf.XNextPageButton_OnPointerPressed(); }
        public void PrevPage() { _botPdf.XPreviousPageButton_OnPointerPressed(); }
        public void ScrollBack() { _botPdf.XScrollBack_OnPointerPressed(); }
        public void ScrollForward() { _botPdf.XScrollForward_OnPointerPressed(); }
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
        public int PageNum() { return _botPdf.PageNum(); }
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
                    ScrollToRegion(source, activeView: activePdf);
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
        public async Task<DocumentController> GetRegionDocument(Point? docViewPoint = null)
        {
            return await _botPdf.GetRegionDocument(!docViewPoint.HasValue ? docViewPoint : Util.PointTransformFromVisual(docViewPoint.Value, this, _botPdf.AnnotationOverlay));
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

            _botPdf.Bind();
            _topPdf.Bind();

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
            var pdfTotalHeight = 0.0;


            // PdfUri
            await Task.Run(() =>
            {
                for (var i = 1; i <= pdfDocument.GetNumberOfPages(); ++i)
                {
                    var page = pdfDocument.GetPage(i);
                    pdfTotalHeight += page.GetPageSize().GetHeight() + 10;
                }
            });

            _topPdf.PDFdoc = _botPdf.PDFdoc = await WPdf.PdfDocument.LoadFromFileAsync(_file);
            var uri = PdfUri;
            await Task.Run(async () =>
            {
                bool hasPdf;
                try
                {
                    hasPdf = await _pdfEndpoint.ContainsPDF(uri);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.ToString());
                    hasPdf = false;
                }

                if (hasPdf)
                {
                    try
                    {
                        /*var (elems, pages) = await _pdfEndpoint.GetSelectableElements(uri);
                        _botPdf.AnnotationOverlay.TextSelectableElements =
                            new List<SelectableElement>(elems);
                        _botPdf.AnnotationOverlay.PageEndIndices = pages;*/
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                else
                {
                    try
                    {
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }

                if (_botPdf.AnnotationOverlay != null)
                {
                    _botPdf.AnnotationOverlay.TextSelectableElements = new List<SelectableElement>();
                    _botPdf.AnnotationOverlay.PageEndIndices = new List<int>();
                }
            });

            //try
            //{
            //    _botPdf.AnnotationOverlay.TextSelectableElements = selectableElements;
            //    _botPdf.AnnotationOverlay.PageEndIndices = pages;
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}

            //var numSections = vagueSections.Aggregate(0, (i, list) => i + list.Count);
            //byte aIncrement = (byte) (128 / (10));
            //byte a = 0;

            //foreach (var sectionList in vagueSections)
            //{
            //    foreach (var section in sectionList)
            //    {
            //        var rect = new Rectangle
            //        {
            //            HorizontalAlignment = HorizontalAlignment.Left,
            //            VerticalAlignment = VerticalAlignment.Top,
            //            Width = section.Bounds.Width,
            //            Height = section.Bounds.Height,
            //            RenderTransform = new TranslateTransform
            //            {
            //                X = section.Bounds.X,
            //                Y = section.Bounds.Y
            //            },
            //            Fill = new SolidColorBrush(Color.FromArgb(a, 255, 0, 0)),
            //            Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)),
            //            StrokeThickness = 1
            //        };
            //        a += aIncrement;
            //        _bottomAnnotationOverlay.XAnnotationCanvas.Children.Add(rect);
            //    }
            //}

            return pdfTotalHeight - 10;
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

        public void ScrollToRegion(DocumentController target, DocumentController source = null, PdfAnnotationView activeView = null)
        {
            var absoluteOffsets = target.GetField<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey);
            if (absoluteOffsets != null)
            {
                var relativeOffsets = absoluteOffsets.Select(p => p.Data.Y * (xTBotPdfGrid.ActualWidth / PdfMaxWidth)).ToList();
                var maxOffset = _botPdf.ScrollViewer.ViewportHeight;
                var firstSplit = relativeOffsets.Skip(1).FirstOrDefault(ro => ro - relativeOffsets.First() > maxOffset);

                if (firstSplit != 0)
                {
                    _topPdf.ScrollViewer.ChangeView(null, Math.Floor(relativeOffsets.First()) - ActualHeight / 4, null);
                    if (xFirstPanelRow.ActualHeight < 20)
                    {
                        xFirstPanelRow.Height = new GridLength(xSecondPanelRow.Height.Value / 2, GridUnitType.Star);
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
                (firstSplit == 0 ? activeView ?? _botPdf : _botPdf).ScrollViewer.ChangeView(null, (firstSplit == 0 ? relativeOffsets.First() : firstSplit) - ((ActualHeight - (xFirstPanelRow.Height.Value / (xFirstPanelRow.Height.Value + xSecondPanelRow.Height.Value)) * ActualHeight) / 2), null);
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
            var margin = Math.Max(0, xPdfContainer.ActualWidth - e.GetCurrentPoint(xPdfContainer).Position.X);
            xRightMargin.Margin = new Thickness(0, 0, margin - 2.5, 0);
            _botPdf.SetRightMargin(margin);
        }
        private void xRightMarginPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            xRightMargin.ReleasePointerCapture(e.Pointer);
            xRightMargin.PointerMoved -= xRightMarginPointerMoved;
        }

        private void xLeftMarginPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            xLeftMargin.PointerMoved += xLeftMarginPointerMoved;
            xLeftMargin.CapturePointer(e.Pointer);
            e.Handled = true;
        }
        private void xLeftMarginPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var margin = Math.Max(0, e.GetCurrentPoint(xPdfContainer).Position.X);
            xLeftMargin.Margin = new Thickness(margin - 2.5, 0, 0, 0);
            _botPdf.SetLeftMargin(margin);
        }
        private void xLeftMarginPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            xLeftMargin.ReleasePointerCapture(e.Pointer);
            xLeftMargin.PointerMoved -= xLeftMarginPointerMoved;
        }
    }
}


