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
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;      
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
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
    public sealed partial class PdfAnnotationView : UserControl
    {
        private class SelRange
        {
            public KeyValuePair<int, int> Range;
            public Rect ClipRect;
        }
        public const double       InterPageSpacing = 10;
        private Stack<double>     _BackStack;
        private Stack<double>     _ForwardStack;
        private Point             _downPt = new Point();
        private double            _scrollRatio;// ScrollViewers don't deal well with being resized so we have to manually track the scroll ratio and restore it on SizeChanged
        private DispatcherTimer   _scrollTimer;
        private AnnotationOverlay _annotationOverlay;
        private double _leftMargin, _rightMargin;
        private double viewboxScaling => (ActualWidth - RightMargin - LeftMargin) / PdfMaxWidth;
        public AnnotationOverlay                  AnnotationOverlay => _annotationOverlay;
        public DocumentViewModel                  ViewModel => DataContext as DocumentViewModel;
        public DocumentController                 DataDocument => ViewModel?.DataDocument;
        public DocumentController                 LayoutDocument => ViewModel?.LayoutDocument;
        public DataVirtualizationSource           Pages { get; set; }
        public WPdf.PdfDocument                   PDFdoc { get; set; }
        //public InkCanvas XInkCanvas;
        public double                             PdfMaxWidth { get; set; } = 1;
        public double                             PdfTotalHeight{ get; set; }
        public bool                               ActiveView { get; set; }
        public double                             RightMargin { get => _rightMargin; set { _rightMargin = value; UpdateMargins(); } }
        public double                             LeftMargin { get => _leftMargin; set { _leftMargin = value; UpdateMargins(); } }

        public bool                               CanScrollPDF
        {
            get => ScrollViewer.VerticalScrollMode == ScrollMode.Enabled;
            set => ScrollViewer.VerticalScrollMode = value ? ScrollMode.Enabled : ScrollMode.Disabled;
        }

        public PdfAnnotationView()
        {
            InitializeComponent();
            Pages = new DataVirtualizationSource(this, ScrollViewer, PageItemsControl);

            Loaded += PdfAnnotationView_Loaded;

            _BackStack = new Stack<double>();
            _ForwardStack = new Stack<double>();
            _ForwardStack.Push(0);

            ScrollViewer.ViewChanged += ScrollViewer_ViewChanged;

            AddHandler(PointerReleasedEvent, new PointerEventHandler(XPdfGrid_PointerReleased), true);

            _scrollTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 500) };
            _scrollTimer.Tick += (s, e) =>
            {
                _scrollTimer.Stop();
                var offset = ScrollViewer.VerticalOffset / ScrollViewer.ExtentHeight;
                if (_ForwardStack.Peek() != offset)
                {
                    AddToStack(_BackStack, _ForwardStack.Pop());
                    _ForwardStack.Clear();
                    AddToStack(_ForwardStack, offset);
                }
            };
            PointerEntered += (s, e) => ActiveView = true;
            PointerExited += (s, e) =>
            {
                if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse && this.GetDocumentView().NumFingersUsed < 2)
                    ScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
                ActiveView = false;
            };
            _scrollTimer.Start();
        }
        ~PdfAnnotationView()
        {
            Debug.WriteLine("FINALIZING PdfAnnotationView");
        }

        public void   GoToPage(double pageNum)
        {
            var currOffset = 0.0;
            for (int page = 0; page < pageNum-1; page++) { 
                currOffset += Pages.PageSizes[page].Height + InterPageSpacing;
            }
            ScrollViewer.ChangeView(null, currOffset * viewboxScaling, 1);
        }
        public int    PageNum()
        {
            var currOffset = 0.0;
            for (int page = 0; page < Pages.PageSizes.Count; page++)
            {
                if (currOffset > ScrollViewer.VerticalOffset + ScrollViewer.ActualHeight/2)
                {
                    return page;
                }
                currOffset += Pages.PageSizes[page].Height * viewboxScaling + InterPageSpacing * viewboxScaling;
            }
            return Pages.PageSizes.Count;
        }
        public void   InitializeRegions(List<SelectableElement> textElements, List<int> pageEndIndices)
        {
            AnnotationOverlay.TextSelectableElements = textElements;
            AnnotationOverlay.PageEndIndices = pageEndIndices;
            AnnotationOverlay.InitializeRegions();
         }
        public void   ScrollToPosition(double pos, bool center = true)
        {
            var offset = Math.Max(pos * viewboxScaling - (center ? ScrollViewer.ViewportHeight / 2 :0), 0);
            ScrollViewer.ChangeView(null, Math.Max(offset, 0), null);
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
        public void   PopBackStack()
        {
            if (_BackStack.Any())
            {
                var pop = _BackStack.Pop();
                ScrollViewer.ChangeView(null, pop * ScrollViewer.ExtentHeight, 1);
                AddToStack(_ForwardStack, pop);
            }
        }
        public void   PopForwardStack()
        {
            if (_ForwardStack.Count > 1)
            {
                AddToStack(_BackStack, _ForwardStack.Pop());
                ScrollViewer.ChangeView(null, _ForwardStack.Peek() * ScrollViewer.ExtentHeight, 1);
            }
        }
        public void   PagePrev() { GoToPage(Math.Max(0, PageNum() - 1)); }
        public void   PageNext() { GoToPage(Math.Min(Pages.PageSizes.Count, PageNum()+1)); }
        public void   SetRegionVisibility(Visibility state)
        {
            AnnotationOverlay.Visibility = state;
            xPdfGridWithEmbeddings.Visibility = state;
        }
        /// <summary>
        /// This creates a region document at a Point specified in the coordinates of the containing DocumentView
        /// </summary>
        /// <param name="pointInAnnotationOverlayCoords"></param>
        /// <returns></returns>
        public async Task<DocumentController> GetRegionDocument(Point? pointInAnnotationOverlayCoords = null)
        {
            Point calculateClosestPointOnPDF(Point p)
            {
                return new Point(p.X < 0 ? 30 : p.X > xPdfGrid.ActualWidth ? xPdfGrid.ActualWidth - 30 : p.X,
                                 p.Y < 0 ? 30 : p.Y > xPdfGrid.ActualHeight ? xPdfGrid.ActualHeight - 30 : p.Y);
            }
            var regionDoc = await AnnotationOverlay.CreateRegionFromPreviewOrSelection();
            if (regionDoc == null && pointInAnnotationOverlayCoords != null)
            {
                regionDoc = AnnotationOverlay.CreatePinRegion(calculateClosestPointOnPDF(pointInAnnotationOverlayCoords.Value));
            }
            return regionDoc ?? LayoutDocument;
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
            KeyDown -= PdfAnnotationView_KeyDown;
            KeyDown += PdfAnnotationView_KeyDown;

            if (AnnotationOverlay == null)
            {
                _annotationOverlay = new AnnotationOverlay(LayoutDocument, true);
                xPdfGrid.Children.Add(AnnotationOverlay);
               
                xPdfGridWithEmbeddings.Children.Add(_annotationOverlay.AnnotationOverlayEmbeddings);

                //XInkCanvas = new InkCanvas();
                //XParentPdfGrid.Children.Add(XInkCanvas);
                //_annotationOverlay.BindInkCanvas();
                //xPdfGridWithEmbeddings.Children.Add(XInkCanvas);


                _annotationOverlay.CurrentAnnotationType =  AnnotationType.Region;
            }

            //xInkToolbar.TargetInkCanvas = XInkCanvas;

            if (Pages.PageSizes.Count != 0)
            {
                Pages.Initialize();
            }
        }

        private void XPdfGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            AnnotationOverlay.AnnotationOverlayDoubleTapped(sender, e);
        }
        private void XPdfGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
            {
                CanScrollPDF = true;
                TouchInteractions.CurrInteraction = TouchInteractions.TouchInteraction.None;
              //  if (TouchInteractions.HeldDocument == this.GetFirstAncestorOfType<DocumentView>())
              //      TouchInteractions.HeldDocument = null;
            }
            //xPdfGrid.ReleasePointerCapture(e.Pointer);
            xPdfGrid.PointerMoved -= XPdfGrid_PointerMoved;
            var currentPoint = e.GetCurrentPoint(PageItemsControl);
            if (currentPoint.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased || e.Pointer.PointerDeviceType == PointerDeviceType.Pen)
            {
                _annotationOverlay.EndAnnotation(e.GetCurrentPoint(_annotationOverlay).Position);
                e.Handled = true;

                Focus(FocusState.Pointer);
            }

        }
        private void XPdfGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            _annotationOverlay.UpdateAnnotation(e.GetCurrentPoint(_annotationOverlay).Position);
        }
        private void XPdfGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            CanScrollPDF = this.GetDocumentView().NumFingersUsed > 0;
            _downPt = e.GetCurrentPoint(this).Position;
            var currentPoint = e.GetCurrentPoint(PageItemsControl);
            if (this.GetDocumentView().AreContentsActive && (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed  || e.Pointer.PointerDeviceType == PointerDeviceType.Pen) && 
                e.Pointer.PointerDeviceType != PointerDeviceType.Touch)
            {
                var annotationOverlayPt = e.GetCurrentPoint(_annotationOverlay).Position;
                _annotationOverlay.StartAnnotation(AnnotationOverlay.CurrentAnnotationType, annotationOverlayPt);
                xPdfGrid.PointerMoved -= XPdfGrid_PointerMoved;
                xPdfGrid.PointerMoved += XPdfGrid_PointerMoved;
                xPdfGrid.CapturePointer(e.Pointer);
                e.Handled = true;
            }

            //if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
            //{
            //    e.Handled = false;

            //    TouchInteractions.CurrInteraction = TouchInteractions.TouchInteraction.DocumentManipulation;
            //    if (TouchInteractions.HeldDocument == null) TouchInteractions.HeldDocument = this.GetFirstAncestorOfType<DocumentView>();
            //}
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

        private void AddToStack(Stack<double> stack, double offset)
        {
            if (!stack.Any() || stack.Peek() != offset)
            {
                stack.Push(offset);
            }
        }
        private void UpdateMargins()
        {
            var scrollPos = ScrollViewer.VerticalOffset / viewboxScaling;
            xPdfGrid.Padding = new Thickness(LeftMargin / viewboxScaling, 0, RightMargin / viewboxScaling, 0);
            xPdfGridWithEmbeddings.RenderTransform = new TranslateTransform() { X = LeftMargin / viewboxScaling };
            UpdateLayout();
            ScrollToPosition(scrollPos, false);
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = e.DataView.HasDragModel() ? e.AcceptedOperation | DataPackageOperation.Copy : DataPackageOperation.None;
        }
        private void OnDrop(object sender, DragEventArgs e)
        {
            AnnotationOverlay.OnDrop(sender, e);
        }
        

        /// <summary>
        /// Enable scrolling only when 2 fingers are on the pdf
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void ScrollViewer_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
            {
              //  if (TouchInteractions.HeldDocument == null)
                //    TouchInteractions.HeldDocument = this.GetFirstAncestorOfType<DocumentView>();

                e.Handled = false;
            }
        }

        public void PdfOnDrop()
        {
           //if (TouchInteractions.HeldDocument == this.GetFirstAncestorOfType<DocumentView>())
            //   TouchInteractions.HeldDocument = null;
        }

        private void ScrollViewer_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
        }

        private void PdfAnnotationView_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {

        }

        private void XParentPdfGrid_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            ScrollViewer.VerticalScrollMode = ScrollMode.Enabled;

        }
    }
}


