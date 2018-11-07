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
        public AnnotationOverlay                  AnnotationOverlay => _annotationOverlay;
        public DocumentController                 DataDocument => (DataContext as DocumentViewModel).DataDocument;
        public DocumentController                 LayoutDocument => (DataContext as DocumentViewModel).LayoutDocument;
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
        
        public PdfAnnotationView()
        {
            InitializeComponent();
            Pages = new DataVirtualizationSource(this, ScrollViewer, PageItemsControl);

            Loaded += PdfAnnotationView_Loaded;
            Unloaded += PdfAnnotationView_Unloaded;

            _BackStack = new Stack<double>();
            _BackStack.Push(0);
            _ForwardStack = new Stack<double>();

            ScrollViewer.ViewChanged += ScrollViewer_ViewChanged;

            Canvas.SetZIndex(xButtonPanel, 999);

            _scrollTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 500) };
            _scrollTimer.Tick += (s, e) =>
            {
                _scrollTimer.Stop();
                AddToStack(_BackStack, ScrollViewer);
            };

            _scrollTimer.Start();
        }
        ~PdfAnnotationView()
        {
            //Debug.WriteLine("FINALIZING PdfAnnotationView");
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
                var scale = xPdfCol.ActualWidth / size.Width;
                currOffset += (size.Height + 10) * scale;
            }

            ScrollViewer.ChangeView(null, currOffset, 1);

        }

        public void ScrollToPosition(double pos)
        {
            var sizes = Pages.PageSizes;
            var botOffset = 0.0;
            var annoWidth = xCollectionView.ActualWidth;
            foreach (var size in sizes)
            {
                var scale = (ScrollViewer.ViewportWidth - annoWidth) / size.Width;
                if (botOffset + (size.Height * scale) - pos > 1)
                {
                    break;
                }

                botOffset += (size.Height * scale) + 15;
            }

            ScrollViewer.ChangeView(null, botOffset, null);
        }
        

        public void SetAnnotationsVisibleOnScroll(bool? visibleOnScroll)
        {
            foreach (var annotation in AnnotationOverlay.XAnnotationCanvas.Children.OfType<AnchorableAnnotation>())
            {
                //get linked annotations
                var regionDoc = (annotation.DataContext as AnchorableAnnotation.Selection)?.RegionDocument;
                if (regionDoc != null)
                {
                    //bool for checking whether child is currently in view of scrollviewer
                    var inView = annotation.IsInView(ScrollViewer.GetBoundingRect(annotation));

                    foreach (var link in regionDoc.GetDataDocument().GetLinks(null))
                    {
                        bool pinned = link.GetDataDocument().GetField<BoolController>(KeyStore.IsAnnotationScrollVisibleKey)?.Data ??
                                        !MainPage.Instance.xToolbar.xPdfToolbar.xAnnotationsVisibleOnScroll.IsChecked ?? false;
                        if (visibleOnScroll.HasValue)
                        {
                            link.GetDataDocument().SetField<BoolController>(KeyStore.IsAnnotationScrollVisibleKey, visibleOnScroll.Value, true);
                            pinned = visibleOnScroll.Value;
                        }

                        if (link.GetDataDocument().GetField<DocumentController>(KeyStore.LinkSourceKey, true) is DocumentController sourceDoc) sourceDoc.SetHidden(!pinned && !inView);
                        if (link.GetDataDocument().GetField<DocumentController>(KeyStore.LinkDestinationKey, true) is DocumentController destDoc) destDoc.SetHidden(!pinned && !inView);
                    }
                }
            }
        }

        /// <summary>
        /// This creates a region document at a Point specified in the coordinates of the containing DocumentView
        /// </summary>
        /// <param name="docViewPoint"></param>
        /// <returns></returns>
        public DocumentController GetRegionDocument(Point? docViewPoint = null)
        {
            var regionDoc = AnnotationOverlay.CreateRegionFromPreviewOrSelection();
            if (regionDoc == null)
            {
                if (docViewPoint != null)
                {

                    //else, make a new push pin region closest to given point
                    var OverlayPoint = Util.PointTransformFromVisual(docViewPoint.Value, this.GetFirstAncestorOfType<DocumentView>(), AnnotationOverlay);
                    var newPoint = calculateClosestPointOnPDF(OverlayPoint);

                    regionDoc = AnnotationOverlay.CreatePinRegion(newPoint);
                }
                else
                    regionDoc = LayoutDocument;
            }
            AnnotationOverlay.RegionDocsList.Add(regionDoc);
            return regionDoc;
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
                if (args.Key == VirtualKey.PageDown)
                {
                    PageNext();
                    args.Handled = true;
                }
                if (args.Key == VirtualKey.PageUp)
                {
                    PagePrev();
                    args.Handled = true;
                }
            }
            if (this.IsCtrlPressed())
            {
                var TextAnnos = AnnotationOverlay.CurrentAnchorableAnnotations.OfType<TextAnnotation>();
                var Selections = TextAnnos.Select(i => new KeyValuePair<int, int>(i.StartIndex, i.EndIndex));
                var ClipRects = TextAnnos.Select(i => i.ClipRect);

                var selections = new List<List<SelRange>>
                {
                    Selections.Zip(ClipRects, (map, clip) => new SelRange() {Range = map, ClipRect = clip}).ToList()
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
                            var ele = _annotationOverlay.TextSelectableElements[i];
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
                        var nchar = ((string)selectableElement.Contents).First();
                        if (prevIndex > 0 && sb.Length > 0 &&
                            (nchar > 128 || char.IsUpper(nchar) ||
                             (!char.IsWhiteSpace(sb[sb.Length - 1]) && !char.IsPunctuation(sb[sb.Length - 1]) &&
                              !char.IsLower(sb[sb.Length - 1]))) &&
                            _annotationOverlay.TextSelectableElements[prevIndex].Bounds.Bottom <
                            _annotationOverlay.TextSelectableElements[index].Bounds.Top)
                        {
                            sb.Append("\\par}\\pard{\\sa120 \\fs" + 2 * currentFontSize);
                        }
                        var font = selectableElement.TextData.GetFont().GetFontProgram().GetFontNames()
                            .GetFontName();
                        if (selectableElement.Type == SelectableElement.ElementType.Text)
                        {
                            var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                            var fontSize = (int)(selectableElement.Bounds.Height * 72 / dpi);
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

        private void PdfAnnotationView_Loaded(object sender, RoutedEventArgs routedEventArgs)
        {
            (xCollectionView.CurrentView as CollectionFreeformView)?.ViewManipulationControls.SetDisableScrollWheel(true);
            Pages.ScrollViewerContentWidth = ActualWidth;
            KeyDown += PdfAnnotationView_KeyDown;
            SelectionManager.SelectionChanged += SelectionManagerOnSelectionChanged;
            if (AnnotationOverlay == null)
            {
                _annotationOverlay = new AnnotationOverlay(LayoutDocument, RegionGetter);
                xPdfGrid.Children.Add(AnnotationOverlay);
                
                _annotationOverlay.CurrentAnnotationType =  AnnotationType.Region;
            }
            xCollectionView.DataContext = new CollectionViewModel(DataDocument, KeyController.Get("PDFSideAnnotations"));
            xInkToolbar.TargetInkCanvas = AnnotationOverlay.XInkCanvas;
            if (Pages.PageSizes.Count != 0)
                Pages.Initialize();
        }

        private void PdfAnnotationView_Unloaded(object sender, RoutedEventArgs e)
        {
            _annotationOverlay.TextSelectableElements?.Clear();
            SelectionManager.SelectionChanged -= SelectionManagerOnSelectionChanged;
        }

        private void SelectionManagerOnSelectionChanged(DocumentSelectionChangedEventArgs args)
        {
            var docview = this.GetFirstAncestorOfType<DocumentView>();
            xButtonPanel.Visibility = SelectionManager.IsSelected(docview) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void xPdfGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (PdfMaxWidth > 0)
            {
                Pages.ScrollViewerContentWidth = xPdfCol.ActualWidth;
                var ratio = xPdfCol.ActualWidth / PdfMaxWidth;
                xCollectionView.RenderTransform = new Windows.UI.Xaml.Media.ScaleTransform() { ScaleX = ratio, ScaleY = ratio };
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
            TouchInteractions.NumFingers--;
            if (TouchInteractions.NumFingers == 0)
            {
                TouchInteractions.CurrInteraction = TouchInteractions.TouchInteraction.None;
            }

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

                this.Focus(FocusState.Pointer);
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
                _annotationOverlay.StartAnnotation(AnnotationOverlay.CurrentAnnotationType, e.GetCurrentPoint(_annotationOverlay).Position);
                (sender as FrameworkElement).PointerMoved -= XPdfGrid_PointerMoved;
                (sender as FrameworkElement).PointerMoved += XPdfGrid_PointerMoved;
            } else if (currentPoint.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
            {
                this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = ManipulationModes.All;
            }
            e.Handled = true;

            if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
            {
                e.Handled = true;
                TouchInteractions.CurrInteraction = TouchInteractions.TouchInteraction.DocumentManipulation;
            }
        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (ScrollViewer.ExtentHeight != 0)
            {
                _scrollTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
                _scrollTimer.Start();
            }

            SetAnnotationsVisibleOnScroll(null);
        }

        private void ScrollViewer_OnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            _scrollRatio = e.FinalView.VerticalOffset / ScrollViewer.ExtentHeight;
        }

        private void XNextPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PageNext();
        }

        private void XPreviousPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PagePrev();
        }

        private void XScrollBack_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PopBackStack(_BackStack, _ForwardStack, ScrollViewer);
        }

        private void XScrollForward_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PopForwardStack(_ForwardStack, _BackStack, ScrollViewer);
        }

        private void PagePrev()
        {
            var sizes = Pages.PageSizes;
            var currOffset = 0.0;
            foreach (var size in sizes)
            {
                var scale = xPdfCol.ActualWidth / size.Width;
                if (currOffset + (size.Height + 10) * scale - ScrollViewer.VerticalOffset >= -1)
                {
                    break;
                }
                currOffset += (size.Height + 10) * scale;
            }

            ScrollViewer.ChangeView(null, currOffset, 1);
            _BackStack.Push(currOffset / ScrollViewer.ExtentHeight);
        }

        private void PageNext()
        {
            var sizes      = Pages.PageSizes;
            var currOffset = 0.0;
            foreach (var size in sizes)
            {
                var scale = xPdfCol.ActualWidth / size.Width;
                currOffset += (size.Height + 10) * scale;
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

        private void PopForwardStack(Stack<double> forwardstack, Stack<double> backstack, ScrollViewer viewer)
        {
            if (forwardstack.Any() && forwardstack.Peek() != double.NaN)
            {
                var pop = forwardstack.Pop();
                viewer.ChangeView(null, forwardstack.Any() ? pop * viewer.ExtentHeight : 0, 1);
                backstack.Push(pop);
            }
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
    }
}


