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
        public static readonly DependencyProperty PdfUriProperty = DependencyProperty.Register(
            "PdfUri", typeof(Uri), typeof(PdfView), new PropertyMetadata(default(Uri), PropertyChangedCallback));

        private List<PDFRegionMarker> _markers = new List<PDFRegionMarker>();

        public Uri PdfUri
        {
            get => (Uri)GetValue(PdfUriProperty);
            set => SetValue(PdfUriProperty, value);
        }

        private double _pdfMaxWidth;

        public double PdfMaxWidth
        {
            get => _pdfMaxWidth;
            set
            {
                _pdfMaxWidth = value;
                OnPropertyChanged();
            }
        }

        private double _pdfTotalHeight;

        public double PdfTotalHeight
        {
            get => _pdfTotalHeight;
            set
            {
                _pdfTotalHeight = value;
                OnPropertyChanged();
            }
        }

        public event EventHandler DocumentLoaded;

        public DataVirtualizationSource TopPages { get; set; }

        public DataVirtualizationSource BottomPages { get; set; }

        private ObservableCollection<DocumentView> _topAnnotationList = new ObservableCollection<DocumentView>();

        public ObservableCollection<DocumentView> TopAnnotations
        {
            get => _topAnnotationList;
            set
            {
                _topAnnotationList = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<DocumentView> BottomAnnotations
        {
            get => _bottomAnnotationList;
            set
            {
                _bottomAnnotationList = value;
                OnPropertyChanged();
            }
        }

        public List<DocumentController> DocControllers { get; set; }

        public DocumentController LayoutDocument { get; }
        public DocumentController DataDocument { get; }

        //This makes the assumption that both pdf views are always in the same annotation mode
        public AnnotationType CurrentAnnotationType => _bottomAnnotationOverlay.CurrentAnnotationType;


        private Stack<double> _topBackStack;
        private Stack<double> _bottomBackStack;

        private Stack<double> _topForwardStack;
        private Stack<double> _bottomForwardStack;

        private DispatcherTimer _topTimer;
        private DispatcherTimer _bottomTimer;

        public Grid TopAnnotationBox => xTopAnnotationBox;
        public Grid BottomAnnotationBox => xBottomAnnotationBox;

        public WPdf.PdfDocument PDFdoc { get; private set; }
        private void CustomPdfView_Loaded(object sender, RoutedEventArgs routedEventArgs)
        {
            LayoutDocument.AddFieldUpdatedListener(KeyStore.GoToRegionKey, GoToUpdated);
            this.KeyDown += CustomPdfView_KeyDown;
            SelectionManager.SelectionChanged += SelectionManagerOnSelectionChanged;
        }

        class SelRange
        {
            public KeyValuePair<int, int> Range;
            public Rect ClipRect;
        }

        private void CustomPdfView_KeyDown(object sender, KeyRoutedEventArgs args)
        {

            if (args.Key == VirtualKey.Space)
                MainPage.Instance.xToolbar.xPdfToolbar.Update(
                    CurrentAnnotationType == AnnotationType.Region ? AnnotationType.Selection : AnnotationType.Region);
            if (!MainPage.Instance.IsShiftPressed())
            {
                if (args.Key == VirtualKey.PageDown)
                    PageNext(BottomScrollViewer);
                if (args.Key == VirtualKey.PageUp)
                    PagePrev(BottomScrollViewer);
            }
            if (this.IsCtrlPressed())
            {
                var bottomTextAnnos = _bottomAnnotationOverlay.CurrentAnchorableAnnotations.OfType<TextAnnotation>();
                var bottomSelections = bottomTextAnnos.Select(i => new KeyValuePair<int, int>(i.StartIndex, i.EndIndex));
                var bottomClipRects = bottomTextAnnos.Select(i => i.ClipRect);
                var topTextAnnos = _topAnnotationOverlay.CurrentAnchorableAnnotations.OfType<TextAnnotation>();
                var topSelections = topTextAnnos.Select(i => new KeyValuePair<int, int>(i.StartIndex, i.EndIndex));
                var topClipRects = topTextAnnos.Select(i => i.ClipRect);

                var selections = new List<List<SelRange>>
                {
                    bottomSelections.Zip(bottomClipRects, (map, clip) => new SelRange() {Range = map, ClipRect = clip}).ToList(),
                    topSelections.Zip(topClipRects, (map, clip) => new SelRange() {Range = map, ClipRect = clip}).ToList()
                };
                var allSelections = selections.SelectMany(s => s.ToList()).ToList();
                if (args.Key == VirtualKey.C && allSelections.Count > 0 && allSelections.Last().Range.Key != -1)
                {
                    Debug.Assert(allSelections.Last().Range.Value != -1);
                    Debug.Assert(allSelections.Last().Range.Value >= allSelections.Last().Range.Key);
                    StringBuilder fontStringBuilder = new StringBuilder("\\fonttbl ");
                    Dictionary<string, int> fontMap = new Dictionary<string, int>();
                    int fontNum = 0;
                    foreach (var selection in allSelections)
                    {
                        for (var i = selection.Range.Key; i <= selection.Range.Value; i++)
                        {
                            var ele = _bottomAnnotationOverlay.TextSelectableElements[i];
                            var fontFamily = ele.TextData?.GetFont()?.GetFontProgram()?.GetFontNames()?.GetFontName();
;
                            var correctedFont = fontFamily;
                            if ((fontFamily?.Contains("Times", StringComparison.OrdinalIgnoreCase) ?? false))
                            {
                                correctedFont = "Georgia";
                            }
                            else if (fontFamily?.Contains("Impact", StringComparison.OrdinalIgnoreCase) ?? false)
                            {
                                correctedFont = "Impact";
                            }

                            if (!fontMap.ContainsKey(fontFamily))
                            {
                                fontMap.Add(fontFamily, fontNum);
                                fontStringBuilder.Append("\\f" + fontNum + " " + correctedFont + "; ");
                                fontNum++;
                            }
                        }
                    }


                    StringBuilder sb = new StringBuilder();
                    sb.Append("{\\rtf1\\ansi {" + fontStringBuilder + "}\\pard{\\sa120 ");
                    allSelections.Sort((s1, s2) => Math.Sign(s1.Range.Key - s2.Range.Key));

                    // get the indices from our selections and ignore any duplicate selections
                    var indices = new List<int>();
                    foreach (var selection in allSelections)
                    {
                        for (var i = selection.Range.Key; i <= selection.Range.Value; i++)
                        {
                            if (!indices.Contains(i))
                            {
                                var eleBounds = _bottomAnnotationOverlay.TextSelectableElements[i].Bounds;
                                if (selection.ClipRect == null || selection.ClipRect == Rect.Empty ||
                                    selection.ClipRect.Contains(new Point(eleBounds.X + eleBounds.Width / 2,
                                        eleBounds.Y + eleBounds.Height / 2)))
                                    indices.Add(i);
                            }
                        }
                    }

                    // if there's ever a jump in our indices, insert two line breaks before adding the next index
                    var prevIndex = indices.First() - 1;
                    var currentFontSize = 0;
                    var isItalic = false;
                    var isBold = false;
                    var currentFont = "";
                    foreach (var index in indices)
                    {
                        if (prevIndex + 1 != index)
                        {
                            sb.Append("\\par}\\pard{\\sa120 \\fs" + 2 * currentFontSize);
                        }

                        var selectableElement = _bottomAnnotationOverlay.TextSelectableElements[index];
                        var nchar = ((string)selectableElement.Contents).First();
                        if (prevIndex > 0 && sb.Length > 0 &&
                            (nchar > 128 || char.IsUpper(nchar) ||
                             (!char.IsWhiteSpace(sb[sb.Length - 1]) && !char.IsPunctuation(sb[sb.Length - 1]) &&
                              !char.IsLower(sb[sb.Length - 1]))) &&
                            _bottomAnnotationOverlay.TextSelectableElements[prevIndex].Bounds.Bottom <
                            _bottomAnnotationOverlay.TextSelectableElements[index].Bounds.Top)
                        {
                            sb.Append("\\par}\\pard{\\sa120 \\fs" + 2 * currentFontSize);
                        }
                        var font = selectableElement.TextData.GetFont().GetFontProgram().GetFontNames()
                            .GetFontName();
                        if (selectableElement.Type == SelectableElement.ElementType.Text)
                        {
                            var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                            var fontSize = (int) (selectableElement.Bounds.Height * 72 / dpi);
                            if (fontSize != currentFontSize)
                            {
                                sb.Append("\\fs" + 2 * fontSize);
                                currentFontSize = fontSize;
                            }

                            if (!isBold && selectableElement.Bounds.Width > 1.05 * selectableElement.TextData.GetFont().GetFontProgram().GetAvgWidth())
                            {
                                sb.Append("{\\b");
                                isBold = true;
                            }
                            else if (isBold && selectableElement.Bounds.Width <
                                     1.05 * selectableElement.TextData.GetFont().GetFontProgram().GetAvgWidth())
                            {
                                sb.Append("}");
                                isBold = false;
                            }

                            //if (isBold && !font.Contains("Bold"))
                            //{
                            //    sb.Append("}");
                            //    isBold = false;
                            //}
                            //else if (!isBold && font.Contains("Bold"))
                            //{
                            //    sb.Append("{\\sa120\\b");
                            //    sb.Append("\\fs" + 2 * fontSize);
                            //    isBold = true;
                            //}

                            if (font != currentFont)
                            {
                                sb.Append("}{\\sa120\\f" + fontMap[font]);
                                sb.Append("\\fs" + 2 * fontSize);
                                currentFont = font;
                            }

                            var contents = (string)selectableElement.Contents;
                            if (char.IsWhiteSpace(contents, 0))
                            {
                                sb.Append("\\~");
                            }
                            else if (contents.Equals("-") || contents.Equals("—") || contents.Equals("--"))
                            {
                                sb.Append("\\_");
                            }
                            else
                            {
                                sb.Append((string)selectableElement.Contents);
                            }
                        }

                        prevIndex = index;
                    }

                    var dataPackage = new DataPackage();
                    dataPackage.SetRtf(sb.ToString());
                    dataPackage.Properties[nameof(DocumentController)] = LayoutDocument;
                    Clipboard.SetContent(dataPackage);
                    args.Handled = true;
                }
            }
        }
        private void GoToUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args,
            Context context)
        {
            if (args.NewValue == null || (sender.GetField(KeyStore.GoToRegionKey) == null && sender.GetField(KeyStore.GoToRegionLinkKey) == null))
            {
                return;
            }

            ScrollToRegion(args.NewValue as DocumentController);
            _bottomAnnotationOverlay.SelectRegion(args.NewValue as DocumentController);

            sender.RemoveField(KeyStore.GoToRegionKey);
            sender.RemoveField(KeyStore.GoToRegionLinkKey);
        }

        private void CustomPdfView_Unloaded(object sender, RoutedEventArgs e)
        {
            LayoutDocument.RemoveFieldUpdatedListener(KeyStore.GoToRegionKey, GoToUpdated);
            _bottomAnnotationOverlay.TextSelectableElements?.Clear();
            _topAnnotationOverlay.TextSelectableElements?.Clear();
            SelectionManager.SelectionChanged -= SelectionManagerOnSelectionChanged;
        }

        private readonly AnnotationOverlay _topAnnotationOverlay;
        private readonly AnnotationOverlay _bottomAnnotationOverlay;

        public PdfView(DocumentController document)
        {
            this.InitializeComponent();
            SetUpToolTips();
            LayoutDocument = document;
            DataDocument = document.GetDataDocument();
            TopPages = new DataVirtualizationSource(this, TopScrollViewer, TopPageItemsControl);
            BottomPages = new DataVirtualizationSource(this, BottomScrollViewer, BottomPageItemsControl);

            Loaded += CustomPdfView_Loaded;
            Unloaded += CustomPdfView_Unloaded;

            _bottomAnnotationOverlay = new AnnotationOverlay(LayoutDocument, RegionGetter);
            _topAnnotationOverlay = new AnnotationOverlay(LayoutDocument, RegionGetter);
            xTopPdfGrid.Children.Add(_topAnnotationOverlay);
            xBottomPdfGrid.Children.Add(_bottomAnnotationOverlay);

            BottomScrollViewer.SizeChanged += (ss, ee) =>
            {
                if (xBar.Width != 0)
                {
                    xBar.Width = BottomScrollViewer.ExtentWidth;
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

            _topBackStack = new Stack<double>();
            _topBackStack.Push(0);
            _bottomBackStack = new Stack<double>();
            _bottomBackStack.Push(0);
            _topForwardStack = new Stack<double>();
            _bottomForwardStack = new Stack<double>();

            BottomScrollViewer.ViewChanged += ScrollViewer_ViewChanged;
            TopScrollViewer.ViewChanged += ScrollViewer_ViewChanged;

            Canvas.SetZIndex(xBottomButtonPanel, 999);
            Canvas.SetZIndex(xTopButtonPanel, 999);

            _topTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 500) };
            _topTimer.Tick += TimerTick;

            _bottomTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 500) };
            _bottomTimer.Tick += TimerTick;

            _topTimer.Start();
            _bottomTimer.Start();

            SetAnnotationType(AnnotationType.Region);
        }
        ~PdfView()
        {

            Debug.WriteLine("FINALIZING PdfView");
        }


        private void SelectionManagerOnSelectionChanged(DocumentSelectionChangedEventArgs args)
        {
            var docview = this.GetFirstAncestorOfType<DocumentView>();
            if (SelectionManager.IsSelected(docview))
            {
                ShowPdfControls();
            }
            else
            {
                HidePdfControls();
            }
        }

        private void TimerTick(object sender, object o)
        {
            if (sender.Equals(_topTimer))
            {
                _topTimer.Stop();
                AddToStack(_topBackStack, TopScrollViewer);
                //_topTimer.Start();

            }
            else if (sender.Equals(_bottomTimer))
            {
                _bottomTimer.Stop();
                AddToStack(_bottomBackStack, BottomScrollViewer);
                //_bottomTimer.Start();
            }
        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (sender.Equals(TopScrollViewer))
            {
                if (TopScrollViewer.ExtentHeight != 0)
                {
                    _topTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
                    _topTimer.Start();
                }
            }

            else if (sender.Equals(BottomScrollViewer))
            {
                if (BottomScrollViewer.ExtentHeight != 0)
                {
                    _bottomTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
                    _bottomTimer.Start();
                }

                //check if annotations have left the screen
                foreach (var child in _bottomAnnotationOverlay.XAnnotationCanvas.Children.OfType<FrameworkElement>())
                {
                    //get linked annotations
                    var regionDoc = (child.DataContext as AnchorableAnnotation.Selection)?.RegionDocument;

                    if (regionDoc == null)
                        continue;
                    
                    //bool for checking whether child is currently in view of scrollviewer
                    var inView = new Rect(0, 0, BottomScrollViewer.ActualWidth, BottomScrollViewer.ActualHeight).Contains(child.TransformToVisual(BottomScrollViewer).TransformPoint(new Point(0, 0)));

                    foreach (var link in regionDoc.GetDataDocument().GetLinks(null))
                    {
                        bool pinned = link.GetDataDocument().GetField<BoolController>(KeyStore.IsAnnotationScrollVisibleKey)?.Data ?? 
                                        !MainPage.Instance.xToolbar.xPdfToolbar.xAnnotationsVisibleOnScroll.IsChecked ?? false;

                        if (link.GetDataDocument().GetLinkedDocument(LinkDirection.ToSource)      is DocumentController sourceDoc) sourceDoc.SetHidden(!inView && !pinned);
                        if (link.GetDataDocument().GetLinkedDocument(LinkDirection.ToDestination) is DocumentController destDoc) destDoc.SetHidden(!inView && !pinned);
                    }
                }
            }
        }

        public void CheckForVisibilityButtonToggle()
        {

        }

        public void SetAnnotationType(AnnotationType type)
        {
            _bottomAnnotationOverlay.CurrentAnnotationType = type;
            _topAnnotationOverlay.CurrentAnnotationType = type;
        }

        //private void OnNewRegionMade(object sender, RegionEventArgs e)
        //{
        //    MakeRegionMarker(TopScrollViewer.VerticalOffset, e.Link);
        //}

        // adds to the side of the PDFView
        private void MakeRegionMarker(double offset, DocumentController dc)
        {
            var newMarker = new PDFRegionMarker();
            newMarker.SetScrollPosition(offset, TopScrollViewer.ExtentHeight);
            newMarker.LinkTo = dc;
            newMarker.Offset = offset;
            newMarker.PointerPressed += xMarker_OnPointerPressed;
            xAnnotationMarkers.Children.Add(newMarker);
            _markers.Add(newMarker);

            xAnnotationMarkers.Visibility = Visibility.Visible;
        }

        //private void OnRegionRemoved(object sender, RegionEventArgs e)
        //{
        //    foreach (var child in xAnnotationMarkers.Children.ToList())
        //    {
        //        if (child is PDFRegionMarker box)
        //        {
        //            if (box.LinkTo.Equals(e.Link))
        //            {
        //                xAnnotationMarkers.Children.Remove(child);
        //                _markers.Remove(box);

        //                if (_markers.Count == 0)
        //                {
        //                    xAnnotationMarkers.Visibility = Visibility.Collapsed;
        //                }
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// This creates a region document at a Point specified in the coordinates of the containing DocumentView
        /// </summary>
        /// <param name="docViewPoint"></param>
        /// <returns></returns>
        public DocumentController GetRegionDocument(Point? docViewPoint = null)
        {
            var regionDoc = _bottomAnnotationOverlay.CreateRegionFromPreviewOrSelection();
            if (regionDoc == null) {
                if (docViewPoint != null) {

                    //else, make a new push pin region closest to given point
                    var bottomOverlayPoint = Util.PointTransformFromVisual(docViewPoint.Value, this.GetFirstAncestorOfType<DocumentView>(), _bottomAnnotationOverlay);
                    var newPoint = calculateClosestPointOnPDF(bottomOverlayPoint);

                    regionDoc = _bottomAnnotationOverlay.CreatePinRegion(newPoint);
                } else
                    regionDoc = LayoutDocument;
            }
            return regionDoc;
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

        private Point calculateClosestPointOnPDF(Point p)
        {
            return new Point(p.X < 0 ? 30 : p.X > this._bottomAnnotationOverlay.ActualWidth  ? this._bottomAnnotationOverlay.ActualWidth - 30 : p.X,
                             p.Y < 0 ? 30 : p.Y > this._bottomAnnotationOverlay.ActualHeight ? this._bottomAnnotationOverlay.ActualHeight - 30 : p.Y);
        }

        private static DocumentController RegionGetter(AnnotationType type)
        {
            return new RichTextNote().Document;
        }

        //This might be more efficient as a linked list of KV pairs if our selections are always going to be contiguous
        private Dictionary<int, Rectangle> _selectedRectangles = new Dictionary<int, Rectangle>();

        StorageFile _file;
        private async Task OnPdfUriChanged()
        {
            if (PdfUri == null)
            {
                return;
            }
            
            try
            {
                _file = await StorageFile.GetFileFromApplicationUriAsync(PdfUri);
            }
            catch (ArgumentException)
            {
                try
                {
                    _file = await StorageFile.GetFileFromPathAsync(PdfUri.LocalPath);
                }
                catch (ArgumentException)
                {
                    return;
                }
            }

            var reader = new PdfReader(await _file.OpenStreamForReadAsync());
            PdfDocument pdfDocument;
            try
            {
                pdfDocument = new PdfDocument(reader);
            }
            catch(BadPasswordException)
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
                TopPages.PageSizes.Add(new Size(page.GetPageSize().GetWidth(), page.GetPageSize().GetHeight()));
                BottomPages.PageSizes.Add(new Size(page.GetPageSize().GetWidth(), page.GetPageSize().GetHeight()));
                maxWidth = Math.Max(maxWidth, page.GetPageSize().GetWidth());
            }

            PdfMaxWidth = maxWidth;

            PDFdoc = await WPdf.PdfDocument.LoadFromFileAsync(_file);
            bool add = PDFdoc.PageCount != _currentPageCount;
            if (add)
            {
                _currentPageCount = (int)PDFdoc.PageCount;
            }

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

            var (selectableElements, text, pages) = strategy.GetSelectableElements(0, pdfDocument.GetNumberOfPages());
            _topAnnotationOverlay.TextSelectableElements = selectableElements;
            _topAnnotationOverlay.PageEndIndices = pages;
            _bottomAnnotationOverlay.TextSelectableElements = selectableElements;
            _bottomAnnotationOverlay.PageEndIndices = pages;

            DataDocument.SetField<TextController>(KeyStore.DocumentTextKey, text, true);

            reader.Close();
            pdfDocument.Close();
            PdfTotalHeight = offset - 10;
            DocumentLoaded?.Invoke(this, new EventArgs());
            
            MainPage.Instance.ClosePopup();
        }

        public BoundsExtractionStrategy Strategy { get; set; }

        private int _currentPageCount = -1;

        private static async void PropertyChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            await ((PdfView)dependencyObject).OnPdfUriChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void XPdfGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (CurrentAnnotationType == AnnotationType.Region)
            {
                using (UndoManager.GetBatchHandle())
                {
                    var overlay = sender == xTopPdfGrid ? _topAnnotationOverlay : _bottomAnnotationOverlay;
                    overlay.EmbedDocumentWithPin(e.GetPosition(overlay));
                }
            }
        }

        #region Region/Selection Events

        private void XPdfGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            (sender as FrameworkElement).PointerMoved -= XPdfGrid_PointerMoved;
            var currentPoint = e.GetCurrentPoint(TopPageItemsControl);
            if (currentPoint.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonReleased)
            {
                return;
            }

            var overlay = sender == xTopPdfGrid ? _topAnnotationOverlay : _bottomAnnotationOverlay;
            overlay.EndAnnotation(e.GetCurrentPoint(overlay).Position);
            e.Handled = true;
            var curPt = e.GetCurrentPoint(this).Position;
            var delta = new Point(curPt.X - _downPt.X, curPt.Y - _downPt.Y);
            var dist = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
            if (!SelectionManager.IsSelected(this.GetFirstAncestorOfType<DocumentView>()) && dist > 10)
            {
                SelectionManager.Select(this.GetFirstAncestorOfType<DocumentView>(), this.IsShiftPressed());
            }

            this.Focus(FocusState.Pointer);
        }

        private void XPdfGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(TopPageItemsControl);
            var overlay = sender == xTopPdfGrid ? _topAnnotationOverlay : _bottomAnnotationOverlay;
            if (currentPoint.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                overlay.EndAnnotation(e.GetCurrentPoint(overlay).Position);
                return;
            }

            if (currentPoint.Properties.IsLeftButtonPressed)
            {
                overlay.UpdateAnnotation(e.GetCurrentPoint(overlay).Position);
            }

            //e.Handled = true;
        }

        Point _downPt = new Point();

        private void XPdfGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _downPt = e.GetCurrentPoint(this).Position;
            var currentPoint = e.GetCurrentPoint(TopPageItemsControl);
            var overlay = sender == xTopPdfGrid ? _topAnnotationOverlay : _bottomAnnotationOverlay;
            if (currentPoint.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                overlay.StartAnnotation(CurrentAnnotationType, e.GetCurrentPoint(overlay).Position);
                (sender as FrameworkElement).PointerMoved -= XPdfGrid_PointerMoved;
                (sender as FrameworkElement).PointerMoved += XPdfGrid_PointerMoved;
            }
        }

        #endregion

        // ScrollViewers don't deal well with being resized so we have to manually track the scroll ratio and restore it on SizeChanged
        private double _topScrollRatio;
        private double _bottomScrollRatio;
        private double _height;
        private ObservableCollection<DocumentView> _bottomAnnotationList = new ObservableCollection<DocumentView>();

        public void CustomPdfView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            TopScrollViewer.ChangeView(null, _topScrollRatio * TopScrollViewer.ExtentHeight, null, true);
            BottomScrollViewer.ChangeView(null, _bottomScrollRatio * BottomScrollViewer.ExtentHeight, null, true);
        }

        private void ScrollViewer_OnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            if (sender.Equals(TopScrollViewer))
            {
                _topScrollRatio = e.FinalView.VerticalOffset / TopScrollViewer.ExtentHeight;
            }
            else if (sender.Equals(BottomScrollViewer))
            {
                _bottomScrollRatio = e.FinalView.VerticalOffset / BottomScrollViewer.ExtentHeight;
            }


            //LayoutDocument.SetField<NumberController>(KeyStore.PdfVOffsetFieldKey, _topScrollRatio, true);
        }


        public void ScrollToPosition(double pos)
        {
            var sizes = BottomPages.PageSizes;
            var botOffset = 0.0;
            var annoWidth = xBottomAnnotationBox.ActualWidth;
            foreach (var size in sizes)
            {
                var scale = (BottomScrollViewer.ViewportWidth - annoWidth) / size.Width;
                if (botOffset + (size.Height * scale) - pos > 1)
                {
                    break;
                }

                botOffset += (size.Height * scale) + 15;
            }

            xFirstPanelRow.Height = new GridLength(0, GridUnitType.Star);
            xSecondPanelRow.Height = new GridLength(1, GridUnitType.Star);
            BottomScrollViewer.ChangeView(null, botOffset, null);
        }

        public void ScrollToRegion(DocumentController target, DocumentController source = null)
        {
            var absoluteOffsets = target.GetField<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey);
            if (absoluteOffsets == null) return;

            var relativeOffsets = absoluteOffsets.TypedData.Select(p => p.Data.Y * (ActualWidth / PdfMaxWidth)).ToList();

            var currOffset = relativeOffsets.First();
            var firstOffset = relativeOffsets.First();
            var maxOffset = BottomScrollViewer.ViewportHeight;
            var splits = new List<double>();


            if (source != null)
            {
                currOffset = 0;
                foreach (var offset in relativeOffsets)
                {
                    if (currOffset == 0 || offset - currOffset > maxOffset)
                    {
                        splits.Add(offset);
                        currOffset = offset;
                    }
                }

                var off = source.GetField<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey)[0].Data.Y *
                          BottomScrollViewer.ExtentHeight;
                splits.Insert(1, off);
                relativeOffsets.Insert(1, off);
            }
            else
            {
                foreach (var offset in relativeOffsets.Skip(1))
                {
                    if (offset - currOffset > maxOffset)
                    {
                        splits.Add(offset);
                        currOffset = offset;
                    }
                }
            }

            Debug.WriteLine($"{splits} screen splits are needed to show everything");

            var sizes = BottomPages.PageSizes;
            // TODO: functionality for more than one split maybe?
            if (splits.Any())
            {
                var botOffset = 0.0;
                var annoWidth = xBottomAnnotationBox.ActualWidth;
                foreach (var size in sizes)
                {
                    var scale = (BottomScrollViewer.ViewportWidth - annoWidth) / size.Width;

                    if (botOffset + (size.Height * scale) + 15 - splits[0] >= -1)

                    {
                        break;
                    }

                    botOffset += (size.Height * scale) + 15;
                }

                var topOffset = 0.0;
                annoWidth = xTopAnnotationBox.ActualWidth;
                foreach (var size in sizes)
                {
                    var scale = (TopScrollViewer.ViewportWidth - annoWidth) / size.Width;

                    if (topOffset + (size.Height * scale) + 15 - firstOffset >= -1)
                    {
                        break;
                    }

                    topOffset += size.Height * scale + 15;
                }

                xFirstPanelRow.Height = new GridLength(1, GridUnitType.Star);
                xSecondPanelRow.Height = new GridLength(1, GridUnitType.Star);
                TopScrollViewer.ChangeView(null, Math.Floor(relativeOffsets.First())  - (BottomScrollViewer.ViewportHeight + TopScrollViewer.ViewportHeight) / 4, null);
                BottomScrollViewer.ChangeView(null, Math.Floor(relativeOffsets.Skip(1).First())  - (BottomScrollViewer.ViewportHeight + TopScrollViewer.ViewportHeight) / 4, null, true);
            }
            else
            {
                var annoWidth = xBottomAnnotationBox.ActualWidth;
                var botOffset = 0.0;
                foreach (var size in sizes)
                {
                    var scale = (BottomScrollViewer.ViewportWidth - annoWidth) / size.Width;

                    if (botOffset + (size.Height * scale) + 15 - firstOffset >= -1)

                    {
                        break;
                    }

                    botOffset += (size.Height * scale) + 15;
                }

                xFirstPanelRow.Height = new GridLength(0, GridUnitType.Star);
                xSecondPanelRow.Height = new GridLength(1, GridUnitType.Star);
                BottomScrollViewer.ChangeView(null, relativeOffsets.First() - (TopScrollViewer.ViewportHeight + BottomScrollViewer.ViewportHeight) / 2, null);
            }
        }

        // when the sidebar marker gets pressed
        private void xMarker_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //MarkerSelected((PDFRegionMarker)sender);
            //AnnotationManager.SelectRegionFromParent(((PDFRegionMarker)sender).LinkTo);
            //e.Handled = true;
        }

        // moves to the region's offset
        private void MarkerSelected(PDFRegionMarker region)
        {
            if (region != null)
            {
                // todo: do we need the zoom factor multiplied by offset here?
                TopScrollViewer.ChangeView(null, region.Offset, null);

            }
        }

        private void xNextAnnotation_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var currOffset = TopScrollViewer.VerticalOffset;
            PDFRegionMarker nextOffset = null;

            foreach (var region in _markers)
            {
                if (region.Offset > currOffset && Math.Abs(region.Offset - currOffset) > 1 &&
                    (nextOffset == null || region.Offset < nextOffset.Offset))
                {
                    nextOffset = region;
                }
            }

            MarkerSelected(nextOffset);
        }

        private void xPrevAnnotation_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var currOffset = TopScrollViewer.VerticalOffset;
            PDFRegionMarker prevOffset = null;

            foreach (var region in _markers)
            {
                if (region.Offset < currOffset && Math.Abs(region.Offset - currOffset) > 1 &&
                    (prevOffset == null || region.Offset > prevOffset.Offset))
                {
                    prevOffset = region;
                }
            }

            MarkerSelected(prevOffset);
        }

        private void Scrollviewer_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (_markers.Count > 0) xAnnotationNavigation.Opacity = 0.8;
        }

        private void Scrollviewer_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            xAnnotationNavigation.Opacity = 0;
        }

        private void XTopAnnotationsToggleButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (xTopAnnotationBox.Width.Equals(0))
            {
                xTopAnnotationBox.Width = 200;
                xBottomAnnotationBox.Width = 200;

            }
            else
            {
                xTopAnnotationBox.Width = 0;
                xBottomAnnotationBox.Width = 0;
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

        private void XBottomScrollToTop_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            BottomScrollViewer.ChangeView(null, 0, null);
            _bottomBackStack.Push(0);
        }

        private void XBottomNextPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PageNext(BottomScrollViewer);

        }

        private void XBottomPreviousPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PagePrev(BottomScrollViewer);
        }

        private void XBottomScrollBack_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PopBackStack(_bottomBackStack, _bottomForwardStack, BottomScrollViewer);
        }

        private void XTopNextPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PageNext(TopScrollViewer);
        }

        private void XTopPreviousPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PagePrev(TopScrollViewer);
        }

        private void XTopScrollToTop_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            TopScrollViewer.ChangeView(null, 0, null);
            _topBackStack.Push(0);
        }

        private void XTopScrollBack_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PopBackStack(_topBackStack, _topForwardStack, TopScrollViewer);
        }


        private void PagePrev(ScrollViewer scroller)
        {
            DataVirtualizationSource pages;
            double annoWidth = 0;
            if (scroller.Equals(TopScrollViewer))
            {
                pages = TopPages;
                annoWidth = xTopAnnotationBox.Width;
            }
            else
            {
                pages = BottomPages;
                annoWidth = xBottomAnnotationBox.Width;
            }

            var sizes = pages.PageSizes;
            var currOffset = 0.0;
            foreach (var size in sizes)
            {
                var scale = (scroller.ViewportWidth - annoWidth) / size.Width;

                if (currOffset + (size.Height + 10) * scale - scroller.VerticalOffset >= -1)
                {
                    break;
                }

                currOffset += (size.Height + 10) * scale;
            }

            scroller.ChangeView(null, currOffset, 1);
            if (scroller.Equals(TopScrollViewer))
            {
                _topBackStack.Push(currOffset / TopScrollViewer.ExtentHeight);
            }
            else if (scroller.Equals(BottomScrollViewer))
            {
                _bottomBackStack.Push(currOffset / BottomScrollViewer.ExtentHeight);
            }

        }

        private void PageNext(ScrollViewer scroller)
        {
            DataVirtualizationSource pages;
            double annoWidth;
            if (scroller.Equals(TopScrollViewer))
            {

                pages = TopPages;
                annoWidth = xTopAnnotationBox.Width;

            }

            else
            {
                pages = BottomPages;
                annoWidth = xBottomAnnotationBox.Width;

            }

            var sizes = pages.PageSizes;
            var currOffset = 0.0;
            foreach (var size in sizes)
            {
                var scale = (scroller.ViewportWidth - annoWidth) / size.Width;
                currOffset += (size.Height + 10) * scale;
                if (currOffset - scroller.VerticalOffset > 1)
                {
                    break;
                }

            }

            scroller.ChangeView(null, currOffset, 1);
            if (scroller.Equals(TopScrollViewer))
            {
                _topBackStack.Push(currOffset / TopScrollViewer.ExtentHeight);
            }
            else if (scroller.Equals(BottomScrollViewer))
            {
                _bottomBackStack.Push(currOffset / BottomScrollViewer.ExtentHeight);
            }

        }

        public void GoToPage(double pageNum)
        {

            var sizes = BottomPages.PageSizes;
            var currOffset = 0.0;

            for (var i = 0; i < pageNum - 1; i++)
            {
                var scale = (BottomScrollViewer.ViewportWidth - xBottomAnnotationBox.Width) / sizes[i].Width;
                currOffset += (sizes[i].Height + 10) * scale;
            }

            BottomScrollViewer.ChangeView(null, currOffset, 1);
            TopScrollViewer.ChangeView(null, currOffset, 1);

        }

        private void AddToStack(Stack<double> stack, ScrollViewer viewer)
        {
            if (!stack.Count().Equals(0))
            {
                if (!stack.Peek().Equals(viewer.VerticalOffset / viewer.ExtentHeight))
                {
                    stack.Push(viewer.VerticalOffset / viewer.ExtentHeight);
                }
            }
            else
            {
                stack.Push(0);
                stack.Push(viewer.VerticalOffset / viewer.ExtentHeight);
            }
        }

        private void PopBackStack(Stack<double> backstack, Stack<double> forwardstack, ScrollViewer viewer)
        {
            if (backstack.Any())
            {
                var pop = backstack.Pop();
                if (backstack.Count > 0 && !backstack.Peek().Equals(Double.NaN) &&
                    !viewer.ExtentHeight.Equals(Double.NaN))
                {
                    viewer.ChangeView(null, backstack.Peek() * viewer.ExtentHeight, 1);
                }
                else
                {
                    viewer.ChangeView(null, 0, 1);
                }

                //viewer.ChangeView(null, backstack.Any() ? backstack.Peek() * viewer.ExtentHeight : 0, 1);
                forwardstack.Push(pop);
            }
        }

        private void XBottomControlsToggleButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (xToggleButtonStack2.Visibility.Equals(Visibility.Collapsed))
            {
                xToggleButtonStack2.Visibility = Visibility.Visible;
                xFadeAnimation2.Begin();
            }
            else
            {
                xToggleButtonStack2.Visibility = Visibility.Collapsed;
            }

        }

        private void XTopControlsToggleButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (xTopToggleButtonStack.Visibility.Equals(Visibility.Collapsed))
            {
                xTopToggleButtonStack.Visibility = Visibility.Visible;
                xFadeAnimation.Begin();
            }
            else
            {
                xTopToggleButtonStack.Visibility = Visibility.Collapsed;
            }
        }

        public void ShowRegions()
        {
            _topAnnotationOverlay.Visibility = Visibility.Visible;
            _bottomAnnotationOverlay.Visibility = Visibility.Visible;
        }

        public void HideRegions()
        {
            _topAnnotationOverlay.Visibility = Visibility.Collapsed;
            _bottomAnnotationOverlay.Visibility = Visibility.Collapsed;
        }

        public bool AreAnnotationsVisible()
        {
            //This makes the assumption that both overlays are kept in sync
            return _bottomAnnotationOverlay.Visibility == Visibility.Visible;
        }

        public LinkHandledResult HandleLink(DocumentController linkDoc, LinkDirection direction)
        {
            if (_bottomAnnotationOverlay.RegionDocsList.Contains(linkDoc.GetDataDocument()
                .GetField<DocumentController>(KeyStore.LinkSourceKey)))
            {
                var src = linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkSourceKey);
                ScrollToRegion(src);
            }

            var target = linkDoc.GetLinkedDocument(direction);
            if (_bottomAnnotationOverlay.RegionDocsList.Contains(target))
            {
                ScrollToRegion(target, linkDoc.GetLinkedDocument(direction, true));
                return LinkHandledResult.HandledClose;
            }

            return LinkHandledResult.Unhandled;
        }

        private void XTopScrollForward_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PopForwardStack(_topForwardStack, _topBackStack, TopScrollViewer);
        }

        private void XBottomScrollForward_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PopForwardStack(_bottomForwardStack, _bottomBackStack, BottomScrollViewer);
        }

        private void PopForwardStack(Stack<double> forwardstack, Stack<double> backstack, ScrollViewer viewer)
        {
            if (forwardstack.Any() && forwardstack.Peek() != double.NaN)
            {
                var pop = forwardstack.Pop();
                viewer.ChangeView(null, forwardstack.Any() ? pop * viewer.ExtentHeight : 0, 1);
                backstack.Push(pop);
            }
        }

        private void Test(object sender, KeyRoutedEventArgs e)
        {

        }



        public void HidePdfControls()
        {
            xTopButtonPanel.Visibility = Visibility.Collapsed;
            xBottomButtonPanel.Visibility = Visibility.Collapsed;
        }

        public void ShowPdfControls()
        {
            xTopButtonPanel.Visibility = Visibility.Visible;
            xBottomButtonPanel.Visibility = Visibility.Visible;

            xFadeAnimation.Begin();
            xFadeAnimation2.Begin();
        }

        private void SetUpToolTips()
        {
            var placementMode = PlacementMode.Bottom;
            const int offset = 0;

            var _controlsTop = new ToolTip()
            {
                Content = "Toggle controls",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xTopAnnotationsToggleButton, _controlsTop);

            var _controlsBottom = new ToolTip()
            {
                Content = "Toggle controls",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xBottomAnnotationsToggleButton, _controlsBottom);

            var _nextTop = new ToolTip()
            {
                Content = "Next page",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xTopNextPageButton, _nextTop);

            var _nextBottom = new ToolTip()
            {
                Content = "Next page",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xBottomNextPageButton, _nextBottom);

            var _prevTop = new ToolTip()
            {
                Content = "Previous page",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xTopPreviousPageButton, _prevTop);

            var _prevBottom = new ToolTip()
            {
                Content = "Previous page",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xBottomPreviousPageButton, _prevBottom);

            var _upTop = new ToolTip()
            {
                Content = "Scroll to top",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xTopScrollToTop, _upTop);

            var _upBottom = new ToolTip()
            {
                Content = "Scroll to top",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xBottomScrollToTop, _upBottom);

            var _backTop = new ToolTip()
            {
                Content = "Scroll backward",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xTopScrollBack, _backTop);

            var _backBottom = new ToolTip()
            {
                Content = "Scroll backward",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xBottomScrollBack, _backBottom);

            var _forwardTop = new ToolTip()
            {
                Content = "Scroll forward",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xTopScrollForward, _forwardTop);

            var _forwardBottom = new ToolTip()
            {
                Content = "Scroll forward",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xBottomScrollForward, _forwardBottom);



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

        public void SetAnnotationsVisibleOnScroll(bool status)
        {
            var allChildren = new List<UIElement>();
            allChildren.AddRange(_bottomAnnotationOverlay.XAnnotationCanvas.Children);
            //allChildren.AddRange(_topAnnotationOverlay.XAnnotationCanvas.Children);

            foreach (var child in allChildren.OfType<FrameworkElement>())
            {
                //get linked annotations
                if ((child.DataContext as AnchorableAnnotation.Selection)?.RegionDocument is DocumentController regionDoc)
                {
                    var allLinks = regionDoc.GetDataDocument().GetLinks(null);

                    //bool for checking whether child is currently in view of scrollviewer
                    bool inView = new Rect(0, 0, BottomScrollViewer.ActualWidth, BottomScrollViewer.ActualHeight).Contains(child.TransformToVisual(BottomScrollViewer).TransformPoint(new Point()));

                    foreach (DocumentController link in allLinks)
                    {
                        link.GetDataDocument().SetField<BoolController>(KeyStore.IsAnnotationScrollVisibleKey, status, true);

                        if (link.GetDataDocument().GetField<DocumentController>(KeyStore.LinkSourceKey, true) is DocumentController sourceDoc) sourceDoc.SetHidden(!status && !inView);
                        if (link.GetDataDocument().GetField<DocumentController>(KeyStore.LinkDestinationKey, true) is DocumentController destDoc) destDoc.SetHidden(!status && !inView);
                    }
                }
            }
        }

        private void xPdfDivider_Tapped(object sender, TappedRoutedEventArgs e) => xFirstPanelRow.Height = new GridLength(0);

        private void xToggleActivationButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            SetActivationMode(!LinkActivationManager.ActivatedDocs.Contains(this.GetFirstAncestorOfType<DocumentView>()));
        }

        public void SetActivationMode(bool onoff)
        {
            xActivationMode.Visibility = onoff ? Visibility.Visible : Visibility.Collapsed;
            if (onoff)
                LinkActivationManager.ActivateDoc(this.GetFirstAncestorOfType<DocumentView>());
            else
                LinkActivationManager.DeactivateDoc(this.GetFirstAncestorOfType<DocumentView>());
        }
    }
}


