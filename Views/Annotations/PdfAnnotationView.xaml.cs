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
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class PdfAnnotationView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler DocumentLoaded;
        private class SelRange
        {
            public KeyValuePair<int, int> Range;
            public Rect ClipRect;
        }
        public void OnDocumentLoaded(DocumentController layoutDocument) { DocumentLoaded?.Invoke(layoutDocument, new EventArgs()); }
        private ObservableCollection<DocumentView> _annotationList = new ObservableCollection<DocumentView>();
        private Stack<double>     _BackStack;
        private Stack<double>     _ForwardStack;
        private double            _pdfMaxWidth = -1;
        private double            _pdfTotalHeight;
        private Point             _downPt = new Point();
        private double            _scrollRatio;// ScrollViewers don't deal well with being resized so we have to manually track the scroll ratio and restore it on SizeChanged
        private DispatcherTimer   _scrollTimer;
        private AnnotationOverlay _annotationOverlay;
        private double            _pageScaling => (xPdfGrid.ActualWidth + RightMargin + LeftMargin) / xPdfGrid.ActualWidth;
        public AnnotationOverlay                  AnnotationOverlay => _annotationOverlay;
        public DocumentViewModel                  ViewModel => DataContext as DocumentViewModel;
        public DocumentController                 DataDocument => ViewModel?.DataDocument;
        public DocumentController                 LayoutDocument => ViewModel?.LayoutDocument;
        public DataVirtualizationSource           Pages { get; set; }
        public WPdf.PdfDocument                   PDFdoc { get; set; }
        public ObservableCollection<DocumentView> Annotations
        {
            get => _annotationList;
            set
            {
                _annotationList = value;
                OnPropertyChanged();
            }
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
        public bool                               ActiveView { get; set; }

        public PdfAnnotationView()
        {
            InitializeComponent();
            Pages = new DataVirtualizationSource(this, ScrollViewer, PageItemsControl);

            Loaded += PdfAnnotationView_Loaded;

            _BackStack = new Stack<double>();
            _BackStack.Push(0);
            _ForwardStack = new Stack<double>();

            ScrollViewer.ViewChanged += ScrollViewer_ViewChanged;

            _scrollTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 500) };
            _scrollTimer.Tick += (s, e) =>
            {
                _scrollTimer.Stop();
                AddToStack(_BackStack, ScrollViewer);
            };
            PointerEntered += (s, e) => ActiveView = true;
            PointerExited += (s, e) => ActiveView = false;
            _scrollTimer.Start();
        }
        ~PdfAnnotationView()
        {
            Debug.WriteLine("FINALIZING PdfAnnotationView");
        }
        public void GoToPage(double pageNum)
        {
            var sizes = Pages.PageSizes;
            var currOffset = 0.0;

            int page = 1;
            foreach (var size in sizes)
            {
                if (page++ >= pageNum)
                    break;
                currOffset += (size.Height + 10) * _pageScaling;
            }

            ScrollViewer.ChangeView(null, currOffset, 1);
        }

        public int    PageNum()
        {
            var sizes = Pages.PageSizes;
            var currOffset = 0.0;

            int page = 1;
            foreach (var size in sizes)
            {
                currOffset += size.Height * _pageScaling;
                if (currOffset > ScrollViewer.VerticalOffset)
                    break;
                currOffset += 10 * _pageScaling;
                page++;
            }
            return page;
        }
        public void   InitializeRegions(List<SelectableElement> textElements, List<int> pageEndIndices)
        {
            AnnotationOverlay.TextSelectableElements = textElements;
            AnnotationOverlay.PageEndIndices = pageEndIndices;
            AnnotationOverlay.InitializeRegions();
         }
        public void   ScrollToPosition(double pos, bool center = true)
        {
            var offset = pos / _pageScaling * ActualWidth / (PdfMaxWidth - RightMargin - LeftMargin);
            var botOffset = Math.Max(offset - (ScrollViewer.ViewportHeight / 2), 0);
            if (!double.IsNaN(botOffset))
            {
                ScrollViewer.ChangeView(null, Math.Max(botOffset, 0), null);
            }
        }
        public void   UpdateMatchedSearch(string searchString, int index)
        {
            AnnotationOverlay.ClearSelection();
            ScrollToPosition(AnnotationOverlay.TextSelectableElements[index].Bounds.Top);
            for (int j = 0; j < searchString.Length; j++)
            {
                AnnotationOverlay.SelectIndex(index + j, null, new SolidColorBrush(Windows.UI.Color.FromArgb(120, 0x00, 0xFF, 0xFF)));
            }
        }
        public double RightMargin { get; set; }
        public double LeftMargin { get; set; }
        public void   SetRightMargin(double margin)
        {
            PdfMaxWidth += margin - RightMargin;
            RightMargin = margin;
            UpdateMargins();
        }

        public void   SetLeftMargin(double margin)
        {
            PdfMaxWidth += margin - LeftMargin;
            LeftMargin = margin;
            UpdateMargins();
        }
        public void UpdateMargins()
        {
            if (Pages.PageSizes.Count() > 0 && ScrollViewer.ExtentHeight > 0)
            {
                var viewboxScaling = PdfTotalHeight / ScrollViewer.ExtentHeight;
                xPdfGrid.Padding = new Thickness(LeftMargin * viewboxScaling, 0, RightMargin * viewboxScaling, 0);
                xPdfGridWithEmbeddings.RenderTransform = new TranslateTransform() { X = LeftMargin * viewboxScaling };
            }
        }

        /// <summary>
        /// This creates a region document at a Point specified in the coordinates of the containing DocumentView
        /// </summary>
        /// <param name="pointInAnnotationOverlayCoords"></param>
        /// <returns></returns>
        public async Task<DocumentController> GetRegionDocument(Point? pointInAnnotationOverlayCoords = null)
        {
            var regionDoc = await AnnotationOverlay.CreateRegionFromPreviewOrSelection();
            if (regionDoc == null && pointInAnnotationOverlayCoords != null)
            {
                regionDoc = AnnotationOverlay.CreatePinRegion(calculateClosestPointOnPDF(pointInAnnotationOverlayCoords.Value));
            }
            if (regionDoc != null)
            {
                return regionDoc;
            }
            return LayoutDocument;
        }

        private Point calculateClosestPointOnPDF(Point p)
        {
            return new Point(p.X < 0 ? 30 : p.X > xPdfGrid.ActualWidth ? xPdfGrid.ActualWidth - 30 : p.X,
                             p.Y < 0 ? 30 : p.Y > xPdfGrid.ActualHeight ? xPdfGrid.ActualHeight - 30 : p.Y);
        }

        private static DocumentController RegionGetter(AnnotationType type)
        {
            return new RichTextNote().Document;
        }

        private void PdfAnnotationView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScrollViewer.ChangeView(null, _scrollRatio * ScrollViewer.ExtentHeight, null, true);
        }

        private void PdfAnnotationView_KeyDown(object sender, KeyRoutedEventArgs args)
        {
            if (args.Key == VirtualKey.Space)
                MainPage.Instance.xToolbar.xPdfToolbar.Update(
                    AnnotationOverlay.CurrentAnnotationType == AnnotationType.Region ? AnnotationType.Selection : AnnotationType.Region);
            if (!MainPage.Instance.IsShiftPressed())
            {
                switch (args.Key) {
                    case VirtualKey.PageDown: PageNext();
                                              args.Handled = true;
                                              break;
                    case VirtualKey.PageUp:   PagePrev();
                                              args.Handled = true;
                                              break;
                    case VirtualKey.Down:     ScrollViewer.ChangeView(null, ScrollViewer.VerticalOffset + 20, null);
                                              args.Handled = true;
                                              break;
                    case VirtualKey.Up:       ScrollViewer.ChangeView(null, ScrollViewer.VerticalOffset - 20, null);
                                              args.Handled = true;
                                              break;
                }
            }
            if (this.IsCtrlPressed())
            {
                var TextAnnos  = AnnotationOverlay.CurrentAnchorableAnnotations.OfType<TextAnnotation>();
                var Selections = TextAnnos.Select(i => new KeyValuePair<int, int>(i.StartIndex, i.EndIndex));
                var ClipRects  = TextAnnos.Select(i => i.ClipRect);
                var selections = new List<List<SelRange>>
                {
                    Selections.Zip(ClipRects, (map, clip) => new SelRange() {Range = map, ClipRect = clip}).ToList()
                };
                var allSelections = selections.SelectMany(s => s.ToList()).ToList();
                if (args.Key == VirtualKey.F)
                {
                    MainPage.Instance.XDocumentDecorations.SetSearchBoxFocus();
                    args.Handled = true;
                }
                if (args.Key == VirtualKey.C && allSelections.Count > 0 && allSelections.Last().Range.Key != -1)
                {
                    Debug.Assert(allSelections.Last().Range.Value != -1);
                    Debug.Assert(allSelections.Last().Range.Value >= allSelections.Last().Range.Key);
                    var fontStringBuilder = new StringBuilder("\\fonttbl ");
                    var fontMap = new Dictionary<string, int>();
                    int fontNum = 0;
                    foreach (var selection in allSelections)
                    {
                        for (var i = selection.Range.Key; i <= selection.Range.Value; i++)
                        {
                            var ele = _annotationOverlay.TextSelectableElements[i];
                            var fontFamily = ele.FontFamily;
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


                    var sb = new StringBuilder();
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
                                var eleBounds = _annotationOverlay.TextSelectableElements[i].Bounds;
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

                        var selectableElement = _annotationOverlay.TextSelectableElements[index];
                        var font = selectableElement.FontFamily;
                        if (selectableElement.Type == SelectableElement.ElementType.Text)
                        {
                            var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                            var fontSize = (int)(selectableElement.Bounds.Height * 72 / dpi);
                            if (fontSize != currentFontSize)
                            {
                                sb.Append("\\fs" + 2 * fontSize);
                                currentFontSize = fontSize;
                            }

                            if (!isBold && selectableElement.Bounds.Width > 1.05 * selectableElement.AvgWidth)
                            {
                                sb.Append("{\\b");
                                isBold = true;
                            }
                            else if (isBold && selectableElement.Bounds.Width <
                                     1.05 * selectableElement.AvgWidth)
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
                            if (contents.Equals("\n"))
                            {
                                sb.Append("\\par}\\pard{\\sa120 \\fs" + 2 * currentFontSize);
                            }
                            else if (char.IsWhiteSpace(contents, 0))
                            {
                                sb.Append(contents);
                            }
                            else if (contents.Equals("-") || contents.Equals("—") || contents.Equals("--"))
                            {
                                sb.Append("\\_");
                            }
                            else if (char.IsNumber(contents.First()))
                            {
                                sb.Append("\\" + contents);
                            }
                            else
                            {
                                sb.Append(contents);
                            }
                        }

                        prevIndex = index;
                    }

                    //sb.Append("}");

                    var dataPackage = new DataPackage();
                    dataPackage.SetRtf(sb.ToString());
                    dataPackage.Properties[nameof(DocumentController)] = LayoutDocument;
                    Clipboard.SetContent(dataPackage);
                    args.Handled = true;
                }
            }
        }
        private void PdfAnnotationView_Loaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Pages.ScrollViewerContentWidth = ActualWidth;
            KeyDown -= PdfAnnotationView_KeyDown;
            KeyDown += PdfAnnotationView_KeyDown;
            if (AnnotationOverlay == null)
            {
                _annotationOverlay = new AnnotationOverlay(LayoutDocument, RegionGetter, true);
                xPdfGrid.Children.Add(AnnotationOverlay);
                xPdfGridWithEmbeddings.Children.Add(_annotationOverlay.AnnotationOverlayEmbeddings);
                _annotationOverlay.CurrentAnnotationType =  AnnotationType.Region;
            }
            if (Pages.PageSizes.Count != 0)
            {
                Pages.Initialize();
            }
        }

        private void xPdfGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (PdfMaxWidth > 0)
            {
                Pages.ScrollViewerContentWidth = ScrollViewer.ActualWidth - LeftMargin - RightMargin;
            }
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void XPdfGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (_annotationOverlay.CurrentAnnotationType == AnnotationType.Region)
            {
                using (UndoManager.GetBatchHandle())
                {
                    _annotationOverlay.EmbedDocumentWithPin(e.GetPosition(_annotationOverlay));
                }
            }
        }

        private void XPdfGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            (sender as FrameworkElement).PointerMoved -= XPdfGrid_PointerMoved;
            var currentPoint = e.GetCurrentPoint(PageItemsControl);
            if (currentPoint.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                _annotationOverlay.EndAnnotation(e.GetCurrentPoint(_annotationOverlay).Position);
                e.Handled = true;
                var curPt = e.GetCurrentPoint(this).Position;
                var delta = new Point(curPt.X - _downPt.X, curPt.Y - _downPt.Y);
                var dist = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
                if (!SelectionManager.IsSelected(this.GetFirstAncestorOfType<DocumentView>()) && dist > 10)
                {
                    SelectionManager.Select(this.GetFirstAncestorOfType<DocumentView>(), this.IsShiftPressed());
                }

                Focus(FocusState.Pointer);
            }
        }

        private void XPdfGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(PageItemsControl);
            if (currentPoint.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                _annotationOverlay.EndAnnotation(e.GetCurrentPoint(_annotationOverlay).Position);
            }
            else if (currentPoint.Properties.IsLeftButtonPressed)
            {
                _annotationOverlay.UpdateAnnotation(e.GetCurrentPoint(_annotationOverlay).Position);
            }
        }

        private void XPdfGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _downPt = e.GetCurrentPoint(this).Position;
            var currentPoint = e.GetCurrentPoint(PageItemsControl);
            if (currentPoint.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = ManipulationModes.None;
                var annotationOverlayPt = e.GetCurrentPoint(_annotationOverlay).Position;
                _annotationOverlay.StartAnnotation(AnnotationOverlay.CurrentAnnotationType, annotationOverlayPt);
                (sender as FrameworkElement).PointerMoved -= XPdfGrid_PointerMoved;
                (sender as FrameworkElement).PointerMoved += XPdfGrid_PointerMoved;
            }
            else if (currentPoint.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
            {
                this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = ManipulationModes.All;
            }
            e.Handled = true;
        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (ScrollViewer.ExtentHeight != 0)
            {
                _scrollTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
                _scrollTimer.Start();
            }
        }

        private void ScrollViewer_OnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            _scrollRatio = e.FinalView.VerticalOffset / ScrollViewer.ExtentHeight;
        }

        public void PagePrev()
        {
            var sizes = Pages.PageSizes;
            var currOffset = 0.0;
            foreach (var size in sizes)
            {
                if (currOffset + (size.Height + 10) * _pageScaling - ScrollViewer.VerticalOffset >= -1)
                {
                    break;
                }
                currOffset += (size.Height + 10) * _pageScaling;
            }

            ScrollViewer.ChangeView(null, currOffset, 1);
            _BackStack.Push(currOffset / ScrollViewer.ExtentHeight);
        }
        public void PageNext()
        {
            var sizes      = Pages.PageSizes;
            var currOffset = 0.0;
            foreach (var size in sizes)
            {
                currOffset += (size.Height + 10) * _pageScaling;
                if (currOffset - ScrollViewer.VerticalOffset > 1)
                {
                    break;
                }
            }

            ScrollViewer.ChangeView(null, currOffset, 1);
            _BackStack.Push(currOffset / ScrollViewer.ExtentHeight);
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

        public void PopBackStack()
        {
            if (_BackStack.Any())
            {
                var pop = _BackStack.Pop();
                if (_BackStack.Count > 0 && !_BackStack.Peek().Equals(double.NaN) &&
                    !ScrollViewer.ExtentHeight.Equals(double.NaN))
                {
                    ScrollViewer.ChangeView(null, _BackStack.Peek() * ScrollViewer.ExtentHeight, 1);
                }
                else
                {
                    ScrollViewer.ChangeView(null, 0, 1);
                }
                
                _ForwardStack.Push(pop);
            }
        }
        public void PopForwardStack()
        {
            if (_ForwardStack.Any() && _ForwardStack.Peek() != double.NaN)
            {
                var pop = _ForwardStack.Pop();
                ScrollViewer.ChangeView(null, _ForwardStack.Any() ? pop * ScrollViewer.ExtentHeight : 0, 1);
                _BackStack.Push(pop);
            }
        }

        public void OnDragEnter(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = e.DataView.HasDragModel() ? e.AcceptedOperation | DataPackageOperation.Copy : DataPackageOperation.None;
        }
        public void OnDrop(object sender, DragEventArgs e)
        {
            AnnotationOverlay.OnDrop(sender, e);
        }
    }
}


