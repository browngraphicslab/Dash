using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Annotations;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class DocumentDecorations : UserControl, INotifyPropertyChanged
    {
        private Visibility _resizerVisibilityState = Visibility.Collapsed;
        private Visibility _visibilityState;
        private List<DocumentView> _selectedDocs;

        public List<LinkButton> LinkButtons = new List<LinkButton>();
        
        public bool touchActivated;

        public Visibility VisibilityState
        {
            get => _visibilityState;
            set
            {
                if (value != _visibilityState && !_visibilityLock)
                {
                    _visibilityState = value;
                    OnPropertyChanged(nameof(VisibilityState));
                }
            }
        }
        public Visibility ResizerVisibilityState
        {
            get => _resizerVisibilityState;
            set
            {
                if (_resizerVisibilityState != value)
                {
                    _resizerVisibilityState = value;
                    if (value == Visibility.Visible)
                        SetPositionAndSize();
                    OnPropertyChanged(nameof(ResizerVisibilityState));
                }
            }
        }

        public double DocWidth
        {
            get => _docWidth;
            set => _docWidth = value;
        }
        private double _docWidth;
        private bool   _visibilityLock;

        public List<DocumentView> SelectedDocs
        {
            get => _selectedDocs.Where(s => s.IsInVisualTree()).ToList();
            set
            {
                foreach (var docView in _selectedDocs)
                {
                    docView.PointerEntered -= SelectedDocView_PointerEntered;
                    docView.PointerExited -= SelectedDocView_PointerExited;
                    docView.FadeOutBegin -= DocView_OnDeleted;
                }

                _visibilityLock = false;
                xButtonsCanvas.Margin = new Thickness(0, 0, 0, 0);
                foreach (var docView in value)
                {
                    if (docView.ViewModel?.Undecorated == true)
                    {
                        xButtonsCanvas.Margin = new Thickness(-20, 0, 0, 0);
                    }

                    docView.PointerEntered += SelectedDocView_PointerEntered;
                    docView.PointerExited += SelectedDocView_PointerExited;
                    docView.FadeOutBegin += DocView_OnDeleted;
                }

                _selectedDocs = value;
            }
        }
        private void DocView_OnDeleted()
        {
            VisibilityState = Visibility.Collapsed;
        }
        private void keyHdlr(object sender, KeyRoutedEventArgs e)
        {
            if (SelectedDocs.Count > 0)
            {
                SetPositionAndSize(false);
            }
        }

        private void ptrHdlr(object sender, PointerRoutedEventArgs e)
        {
            if (SelectedDocs.Count > 0)
            {
                SetPositionAndSize(false);
            }
        }
        private void tapHdlr(object sender, TappedRoutedEventArgs e)
        {
            if (SelectedDocs.Count > 0)
            {
                SetPositionAndSize(false);
            }
        }

        private object ptrhdlr, taphdlr, keyhdlr;

        private ToolTip _titleTip = new ToolTip { Placement = PlacementMode.Top };
        public DocumentDecorations()
        {
            if (ptrhdlr == null)
            {
                ptrhdlr = new PointerEventHandler(ptrHdlr);
                taphdlr = new TappedEventHandler(tapHdlr);
                keyhdlr = new KeyEventHandler(keyHdlr);
            }
            MainPage.Instance.xOuterGrid.RemoveHandler(PointerMovedEvent, ptrhdlr);
            MainPage.Instance.xOuterGrid.AddHandler(PointerMovedEvent, ptrhdlr, true);
            MainPage.Instance.xOuterGrid.RemoveHandler(PointerReleasedEvent, ptrhdlr);
            MainPage.Instance.xOuterGrid.AddHandler(PointerReleasedEvent, ptrhdlr, true);
            MainPage.Instance.xOuterGrid.RemoveHandler(PointerWheelChangedEvent, ptrhdlr);
            MainPage.Instance.xOuterGrid.AddHandler(PointerWheelChangedEvent, ptrhdlr, true);
            MainPage.Instance.xOuterGrid.RemoveHandler(TappedEvent, taphdlr);
            MainPage.Instance.xOuterGrid.AddHandler(TappedEvent, taphdlr, true);
            MainPage.Instance.xOuterGrid.RemoveHandler(KeyDownEvent, keyhdlr);
            MainPage.Instance.xOuterGrid.AddHandler(KeyDownEvent, keyhdlr, true);
            MainPage.Instance.xOuterGrid.RemoveHandler(KeyUpEvent, keyhdlr);
            MainPage.Instance.xOuterGrid.AddHandler(KeyUpEvent, keyhdlr, true);
            InitializeComponent();
            _visibilityState = Visibility.Collapsed;
            _selectedDocs = new List<DocumentView>();
            _titleTip.Content = HeaderFieldKey.Name;
            ToolTipService.SetToolTip(xHeaderText, _titleTip);
            xHeaderText.PointerEntered += (s, e) => _titleTip.IsOpen = true;
            xHeaderText.PointerExited += (s, e) => _titleTip.IsOpen = false;
            xHeaderText.GotFocus += (s, e) =>
            {
                if (xHeaderText.Text == "<empty>") xHeaderText.SelectAll();
            };
            Loaded += DocumentDecorations_Loaded;
            Unloaded += DocumentDecorations_Unloaded;
            // setup ResizeHandles
            void ResizeHandles_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
            {
                if (this.IsRightBtnPressed())
                {
                    SelectionManager.InitiateDragDrop(_selectedDocs.First(), null, null);
                }
                else
                {
                    (sender as FrameworkElement).ManipulationCompleted -= ResizeHandles_OnManipulationCompleted;
                    (sender as FrameworkElement).ManipulationCompleted += ResizeHandles_OnManipulationCompleted;

                    UndoManager.StartBatch();

                    MainPage.Instance.Focus(FocusState.Programmatic);
                    e.Handled = true;
                }
            }

            void ResizeHandles_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
            {
                UndoManager.EndBatch();
                (sender as FrameworkElement).ManipulationCompleted -= ResizeHandles_OnManipulationCompleted;
                e.Handled = true;
            }

            foreach (var handle in new[] {
                xTopLeftResizeControl, xTopResizeControl, xTopRightResizeControl,
                xLeftResizeControl, xRightResizeControl,
                xBottomLeftResizeControl, xBottomRightResizeControl, xBottomResizeControl })
            {
                handle.ManipulationStarted += ResizeHandles_OnManipulationStarted;
                handle.DoubleTapped += (s, e) =>
                {
                    _doubleTapped = true;
                    var vm = SelectedDocs.FirstOrDefault().ViewModel;
                    var frame = MainPage.Instance.MainSplitter.GetFrameWithDoc(vm.DocumentController, true);
                    if (frame != null)
                    {
                        frame.Delete();
                    }
                    else
                    {
                        // bcz: frame location should be determined by which part of the resize rectangle is tapped (e.g., left, right, top, bottom)
                        SplitFrame.OpenInInactiveFrame(vm.DocumentController);
                    }
                };
                handle.Tapped += async (s, e) =>
                {
                    _doubleTapped = false;
                    await System.Threading.Tasks.Task.Delay(100);
                    if (!_doubleTapped)
                    {
                    }
                };
            }
            SelectionManager.DragManipulationStarted += (s, e) => ResizerVisibilityState = Visibility.Collapsed;
            SelectionManager.DragManipulationCompleted += (s, e) =>
                 ResizerVisibilityState = _selectedDocs.FirstOrDefault()?.GetFirstAncestorOfType<CollectionFreeformView>() == null ? Visibility.Collapsed : Visibility.Visible;

        }

        private void DocumentDecorations_Unloaded(object sender, RoutedEventArgs e)
        {
            SelectionManager.SelectionChanged -= SelectionManager_SelectionChanged;
        }

        private void DocumentDecorations_Loaded(object sender, RoutedEventArgs e)
        {
            SelectionManager.SelectionChanged += SelectionManager_SelectionChanged;
        }

        private void SelectionManager_SelectionChanged(DocumentSelectionChangedEventArgs args)
        {
            SelectedDocs = SelectionManager.GetSelectedDocViews().ToList();
            xMultiSelectBorder.BorderThickness = new Thickness(SelectedDocs.Count > 1 ? 2 : 0);
            SetPositionAndSize();

            ResetHeader(); // force header field to update
            VisibilityState = (SelectedDocs.Any() && !this.IsRightBtnPressed()) ? Visibility.Visible : Visibility.Collapsed;

            if (SelectedDocs.Count == 1)
            {
                xSearchBox.Text = SelectedDocs.First().ViewModel.DocumentController
                    .GetField<TextController>(KeyStore.SearchStringKey)?.Data ?? "";
            }
        }

        public void SetPositionAndSize(bool rebuildMenu = true)
        {
            var topLeft  = new Point(double.PositiveInfinity, double.PositiveInfinity);
            var botRight = new Point(double.NegativeInfinity, double.NegativeInfinity);

            var parentIsFreeform = true;
            var showPDFControls = false;
            try
            {
                foreach (var doc in SelectedDocs)
                {
                    if (doc.ViewModel.LayoutDocument.DocumentType.Equals(PdfBox.DocumentType)==true)
                        showPDFControls = true;
                    if (doc.GetFirstAncestorOfType<CollectionView>()?.CurrentView.ViewType != CollectionViewType.Freeform)
                        parentIsFreeform = false;
                    var viewModelBounds = doc.TransformToVisual(MainPage.Instance.xCanvas).TransformBounds(new Rect(new Point(), new Size(doc.ActualWidth, doc.ActualHeight)));

                    topLeft.X = Math.Min(viewModelBounds.Left, topLeft.X);
                    topLeft.Y = Math.Min(viewModelBounds.Top, topLeft.Y);

                    botRight.X = Math.Max(viewModelBounds.Right, botRight.X);
                    botRight.Y = Math.Max(viewModelBounds.Bottom, botRight.Y);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Got Exception:" + e);
            }
            xHeaderText.Visibility = parentIsFreeform ? Visibility.Visible : Visibility.Collapsed;
            xURISource.Visibility  = parentIsFreeform ? Visibility.Visible : Visibility.Collapsed;
            xScrollNavStack.Visibility = showPDFControls ? Visibility.Visible : Visibility.Collapsed;
            xPageButtonStack.Visibility = showPDFControls ? Visibility.Visible : Visibility.Collapsed;
            xSearchStack.Visibility = showPDFControls ? Visibility.Visible : Visibility.Collapsed;

            ResizerVisibilityState = _selectedDocs.FirstOrDefault() != null && _selectedDocs.First().ViewModel?.ResizersVisible == true ? Visibility.Visible : Visibility.Collapsed;

            if (rebuildMenu)
            {
                rebuildMenuIfNeeded();
            }

            if (!double.IsPositiveInfinity(topLeft.X) && !double.IsPositiveInfinity(topLeft.Y) &&
                !double.IsNegativeInfinity(botRight.X) && !double.IsNegativeInfinity(botRight.Y))
            {
                if (botRight.X > MainPage.Instance.ActualWidth - xAnnotationButtonsStack.ActualWidth - MainPage.Instance.xLeftGrid.ActualWidth)
                {
                    botRight = new Point(MainPage.Instance.ActualWidth - xAnnotationButtonsStack.ActualWidth - MainPage.Instance.xLeftGrid.ActualWidth, botRight.Y);
                }

                RenderTransform = new TranslateTransform
                {
                    X = topLeft.X,
                    Y = topLeft.Y
                };

                ContentColumn.Width = new GridLength(Math.Max(0, botRight.X - topLeft.X));
                ContentRow.Height = new GridLength(botRight.Y - topLeft.Y);
            }
        }

        public void SetSearchBoxFocus()
        {
            this.xSearchBox.Focus(FocusState.Programmatic);
        }

        //adds a button for a link type to appear underneath the link button
        public void AddLinkTypeButton(string linkName)
        {
            if (linkName == null)
            {
                linkName = "Annotation";
            }
            

            //set button color to tag color
            var btnColorOrig = LinkMenu.GetTagColor(linkName);
                var btnColorFinal = btnColorOrig != null
                    ? Color.FromArgb(200, btnColorOrig.Value.R, btnColorOrig.Value.G, btnColorOrig.Value.B)
                    : Color.FromArgb(255, 64, 123, 177);

                var toolTip = new ToolTip
                {
                    Content = linkName,
                    HorizontalOffset = 5,
                    Placement = PlacementMode.Right
                };

                if (SelectedDocs.Count != 0)
                {
                    
                    bool unique = true;
                    foreach (var lb in xButtonsPanel.Children)
                    {
                        if ((lb as LinkButton).Text.Equals(linkName))
                        {
                            unique = false;
                        }
                    }

                    if (unique)
                    {
                        var button = new LinkButton(this, btnColorFinal, linkName, toolTip, SelectedDocs.FirstOrDefault());
                        xButtonsPanel.Children.Add(button);
                        LinkButtons.Add(button);
                        //adds tooltip with link tag name inside
                        ToolTipService.SetToolTip(button, toolTip);
                    }
                    
                }

        }

        public void OpenNewLinkMenu(string text, DocumentController linkDoc)
        {

            if (text == null)
            {
                text = "Annotation";
            }
            foreach (var lb in LinkButtons)
            {
                if (lb.Text.Equals(text))
                {
                    lb.OpenFlyout(lb, linkDoc);
                }
            }
        }

        
        static public KeyController HeaderFieldKey = KeyStore.TitleKey;
        //rebuilds the different link dots when the menu is refreshed or one is added
        public void rebuildMenuIfNeeded()
        {
            xButtonsPanel.Children.Clear();
            LinkButtons.Clear();
            //check each relevant tag name & create the tag graphic & button for it


            var allLinks = SelectedDocs.FirstOrDefault()?.ViewModel?.DataDocument.GetLinks(null)?.ToList() ?? new List<DocumentController>();
            var allRegions = SelectedDocs.FirstOrDefault()?.ViewModel.DataDocument.GetRegions()?.SelectMany((region) =>
                region.GetDataDocument().GetLinks(null)?.ToList() ?? new List<DocumentController>()
            ) ?? new List<DocumentController>();  
            allLinks.AddRange(allRegions);
            foreach (var link in allLinks)
            {
                AddLinkTypeButton(link?.GetDataDocument().GetField<TextController>(KeyStore.LinkTagKey)?.Data);
            }
            
            xButtonsCanvas.Height = xButtonsPanel.Children.Aggregate(xAnnotateEllipseBorder.ActualHeight, (hgt, child) => hgt += (child as FrameworkElement).Height);

            var htmlAddress = SelectedDocs.FirstOrDefault()?.ViewModel?.DataDocument.GetDereferencedField<TextController>(KeyStore.SourceUriKey,null)?.Data;
            if (!string.IsNullOrEmpty(htmlAddress))
            {// add a hyperlink that points to the source webpage.

                xURISource.Text = "From:";
                try
                {
                    var hyperlink = new Hyperlink { NavigateUri = new Uri(htmlAddress) };
                    hyperlink.Inlines.Add(new Run { Text = " " + HtmlToDashUtil.GetTitlesUrl(htmlAddress) });

                    xURISource.Inlines.Add(hyperlink);
                }
                catch (Exception)
                {
                    var theDoc = RESTClient.Instance.Fields.GetController<DocumentController>(htmlAddress);
                    if (theDoc != null)
                    {
                        var regDef = theDoc.GetDataDocument().GetRegionDefinition() ?? theDoc;
                        xURISource.Text += " " + regDef?.Title;
                        //var hyperlink = new Hyperlink() { NavigateUri = new System.Uri(htmlAddress) };
                        //hyperlink.Inlines.Add(new Run() { Text = " " + HtmlToDashUtil.GetTitlesUrl(htmlAddress) });

                        //xURISource.Inlines.Add(hyperlink);
                    }
                }
            }
            else
            {
                var author = SelectedDocs.FirstOrDefault()?.ViewModel?.DataDocument.GetDereferencedField<TextController>(KeyStore.AuthorKey,null)?.Data;
                if (!string.IsNullOrEmpty(author))
                {// add a hyperlink that points to the source webpage.

                    xURISource.Text = "Authored by: " + author;
                }
                else xURISource.Text = "";
            }
        }

        private void SelectedDocView_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var doc = sender as DocumentView;
            if (e.Pointer.PointerDeviceType.Equals(PointerDeviceType.Touch))
                touchActivated = true;
            if (doc.ViewModel != null)
            {
                VisibilityState = Visibility.Visible;
            }
        }

        private void SelectedDocView_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var doc = sender as DocumentView;
            if (e == null || (!e.IsRightPressed() && !e.IsRightPressed() && !e.Pointer.PointerDeviceType.Equals(PointerDeviceType.Touch)))
            {
                VisibilityState = Visibility.Collapsed;
            }

            touchActivated = false;
        }
        private void xDelete_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SelectionManager.DeleteSelected();
        }

        private async void XAnnotateEllipseBorder_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            _doubleTapped = false;
            await System.Threading.Tasks.Task.Delay(100);
            if (!_doubleTapped)
            {
                SelectedDocs.First().ShowContextMenu(e.GetPosition(MainPage.Instance)); 
            }
        }
        private bool _doubleTapped = false;
        private void xAnnotateEllipseBorder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            _doubleTapped = true;
            var kvp = SelectedDocs.First().ViewModel.DocumentController.GetKeyValueAlias(new Point());
            MainPage.Instance.AddFloatingDoc(kvp, new Point(500, 300), e.GetPosition(MainPage.Instance.xCanvas));
        }
        private void AllEllipses_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
        }

        private void XAnnotateEllipseBorder_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
        }

        private void XAnnotateEllipseBorder_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var dragDocOffset  = args.GetPosition(sender);
            var relDocOffsets  = SelectedDocs.Select(args.GetPosition).Select(ro => new Point(ro.X - dragDocOffset.X, ro.Y - dragDocOffset.Y)).ToList();
            var parCollections = SelectedDocs.Select(dv => dv.GetFirstAncestorOfType<AnnotationOverlayEmbeddings>() == null ? dv.ParentCollection?.ViewModel : null).ToList();
            args.Data.SetDragModel(new DragDocumentModel(SelectedDocs, parCollections, relDocOffsets, dragDocOffset) { DraggingLinkButton = true });
            args.AllowedOperations =
                DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            args.Data.RequestedOperation =
                DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
            //touchActivated = false;
        }

        //private void XTemplateEditorEllipseBorder_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        //{
        //    foreach (var doc in SelectedDocs)
        //    {
        //        doc.ManipulationMode = ManipulationModes.None;
        //        doc.ToggleTemplateEditor();
        //    }
        //}

        private void XOnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            //Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor =
            //    new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 1);
            if (sender is Grid button && ToolTipService.GetToolTip(button) is ToolTip tip)
            {
                tip.IsOpen = true;
            }
        }

        private void XOnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            //Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor =
            //    new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
            if (sender is Grid button && ToolTipService.GetToolTip(button) is ToolTip tip)
            {
                tip.IsOpen = false;
            }
        }

        public void XNextPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            SelectedDocs.SelectMany(v => new[] { v.GetFirstDescendantOfType<PdfView>() }.ToList()).ToList().ForEach(pv =>
             pv?.NextPage());
            e.Handled = true;
        }

        public void XPreviousPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            SelectedDocs.SelectMany(v => new[] { v.GetFirstDescendantOfType<PdfView>() }.ToList()).ToList().ForEach(pv =>
             pv?.PrevPage());
            e.Handled = true;
        }

        public void XScrollBack_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            SelectedDocs.SelectMany(v => new[] { v.GetFirstDescendantOfType<PdfView>() }.ToList()).ToList().ForEach(pv =>
             pv?.ScrollBack());
            e.Handled = true;
        }

        public void XScrollForward_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            SelectedDocs.SelectMany(v => new[] { v.GetFirstDescendantOfType<PdfView>() }.ToList()).ToList().ForEach(pv =>
             pv?.ScrollForward());
            e.Handled = true;
        }

        private void XTitleBorder_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            CapturePointer(e.Pointer);
            e.Handled = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
        }

        private void XTitleBorder_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            foreach (var doc in SelectedDocs)
            {
                doc.ShowContext();
                e.Handled = true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void DocumentDecorations_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            VisibilityState = Visibility.Visible;
        }

        private void DocumentDecorations_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!this.IsLeftBtnPressed() && touchActivated == false)
                ;// VisibilityState = Visibility.Collapsed;
        }
        
        void ResizeTLaspect(object sender, ManipulationDeltaRoutedEventArgs e) { _selectedDocs.ForEach(dv => dv.Resize(sender as FrameworkElement, e, true, true, true)); }
        void ResizeRTaspect(object sender, ManipulationDeltaRoutedEventArgs e) { _selectedDocs.ForEach(dv => dv.Resize(sender as FrameworkElement, e, true, false, true)); }
        void ResizeBLaspect(object sender, ManipulationDeltaRoutedEventArgs e) { _selectedDocs.ForEach(dv => dv.Resize(sender as FrameworkElement, e, false, true, true)); }
        void ResizeBRaspect(object sender, ManipulationDeltaRoutedEventArgs e) { _selectedDocs.ForEach(dv => dv.Resize(sender as FrameworkElement, e, false, false, true)); }
        void ResizeRTunconstrained(object sender, ManipulationDeltaRoutedEventArgs e) { _selectedDocs.ForEach(dv => dv.Resize(sender as FrameworkElement, e, true, false, false)); }
        void ResizeBLunconstrained(object sender, ManipulationDeltaRoutedEventArgs e) { _selectedDocs.ForEach(dv => dv.Resize(sender as FrameworkElement, e, false, true, false)); }
        void ResizeBRunconstrained(object sender, ManipulationDeltaRoutedEventArgs e) { _selectedDocs.ForEach(dv => dv.Resize(sender as FrameworkElement, e, false, false, false)); }

        private void xTitle_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
            case VirtualKey.Enter:
                if (xHeaderText.Text.StartsWith("#"))
                {
                    ResetHeader(xHeaderText.Text.Substring(1));
                }
                else
                {
                    CommitHeaderText();
                }
                break;
            case VirtualKey.Down:
            case VirtualKey.Up:
                ChooseNextHeaderKey(e.Key == VirtualKey.Up);
                break;
            default:
                xHeaderText.Foreground = new SolidColorBrush(Colors.Red);
                break;
            }
            e.Handled = true;
        }

        private void ChooseNextHeaderKey(bool prev = false)
        {
            var keys = new List<KeyController>();
            foreach (var d in SelectedDocs.Select(sd => sd.ViewModel?.DataDocument))
            {
                keys.AddRange(d.EnumDisplayableFields().Select(pair => pair.Key));
            }
            keys = keys.ToHashSet().ToList();
            keys.Sort((dv1, dv2) => string.Compare(dv1.Name, dv2.Name));
            var ind = keys.IndexOf(HeaderFieldKey);
            do
            {
                ind = prev ? (ind > 0 ? ind - 1 : keys.Count - 1) : (ind < keys.Count - 1 ? ind + 1 : 0);
                ResetHeader(keys[ind].Name);
            } while (xHeaderText.Text == "<empty>");
        }

        private void CommitHeaderText()
        {
            foreach (var doc in SelectedDocs.Select(sd => sd.ViewModel?.DocumentController))
            {
                var targetDoc = doc.GetField<TextController>(HeaderFieldKey)?.Data != null ? doc : doc.GetDataDocument();

                targetDoc.SetField<TextController>(HeaderFieldKey, xHeaderText.Text, true);
            }
            xHeaderText.Background = new SolidColorBrush(Colors.LightBlue);
            ResetHeader();
        }

        private void ResetHeader(string newkey = null)
        {
            if (SelectedDocs.Count > 0)
            {
                if (newkey != null)
                {
                    HeaderFieldKey = KeyController.Get(newkey);
                }
                var layoutHeader = SelectedDocs.First().ViewModel?.DocumentController.GetField<TextController>(HeaderFieldKey)?.Data;
                xHeaderText.Text = layoutHeader ?? SelectedDocs.First().ViewModel?.DataDocument.GetDereferencedField<TextController>(HeaderFieldKey, null)?.Data ?? "<empty>";
                if (SelectedDocs.Count > 1)
                {
                    foreach (var d in SelectedDocs.Where(sd => sd.ViewModel != null).Select(sd => sd.ViewModel.DataDocument))
                    {
                        var dvalue = d?.GetDereferencedField<TextController>(HeaderFieldKey, null)?.Data ?? "<empty>";
                        if (dvalue != xHeaderText.Text)
                        {
                            xHeaderText.Text = "...";
                            break;
                        }
                    }
                }
                xHeaderText.Foreground = new SolidColorBrush(Colors.Black);
                _titleTip.Content = HeaderFieldKey.Name;
                xHeaderText.Background = new SolidColorBrush(xHeaderText.Text == "<empty>" ? Colors.Pink : Colors.LightBlue);
            }
        }

        private void Ellipse_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var activeDoc = SelectedDocs.FirstOrDefault()?.ViewModel.DocumentController;
            args.Data.SetDragModel(new DragFieldModel(new DocumentFieldReference(activeDoc.GetDataDocument(), HeaderFieldKey)));
            // args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
        }

        private async void UserControl_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            var txt = await e.DataView.GetTextAsync();
            using (UndoManager.GetBatchHandle())
            {
                foreach (var d in SelectedDocs)
                {
                    var xml = txt.Replace("\"", "'");
                    d.ViewModel.DocumentController.SetField<TextController>(KeyStore.XamlKey, xml, true);
                }
            }
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (!updateSearchString())
            {
                changeSearchIndex(1);
            }
        }
        private void XPrevOccur_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            updateSearchString();
            changeSearchIndex(-1);
        }
        private void changeSearchIndex(int change)
        {
            foreach (var documentView in SelectedDocs)
            {
                var searchIndex = documentView.ViewModel.DocumentController.GetField<NumberController>(KeyStore.SearchIndexKey)?.Data ?? -1;
                documentView.ViewModel.DocumentController.SetField<NumberController>(KeyStore.SearchIndexKey, Math.Max(0, searchIndex + change), true);
            }
        }
        

        private bool updateSearchString()
        {
            foreach (var documentView in SelectedDocs)
            {
                var searchString = documentView.ViewModel.DocumentController.GetField<TextController>(KeyStore.SearchStringKey)?.Data ?? "";
                if (!searchString.Equals(xSearchBox.Text))
                {
                    documentView.ViewModel.DocumentController.SetField<TextController>(KeyStore.SearchStringKey, xSearchBox.Text, true);
                    return true;
                }
            }
            return false;
        }

        // try dropping the Xaml style below onto the blue frame of one or more selected text documents:
        /* -- restyles a text note 
        <Grid
            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:dash="using:Dash"
            xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"></RowDefinition>
                <RowDefinition Height="*"></RowDefinition>
                <RowDefinition Height="Auto"></RowDefinition>
            </Grid.RowDefinitions>
                <Border BorderThickness="2" BorderBrush="CadetBlue" Background="White">
                    <TextBlock x:Name="xTextFieldTitle" Text="DOC TITLE" HorizontalAlignment="Stretch" Height="25" VerticalAlignment="Top"/>
                </Border>
                <Border Grid.Row="1" Background="CadetBlue" >
                    <dash:RichEditView x:Name="xRichTextFieldData" Foreground="White" HorizontalAlignment="Stretch" Grid.Row="1" VerticalAlignment="Top" />
                </Border>
            <StackPanel Orientation="Horizontal"  Grid.Row="2" Height="30" Background="White" >
                <TextBlock Text="Author:" HorizontalAlignment="Stretch" FontStyle="Italic" FontSize="9" VerticalAlignment="Center" Margin="0 5 0 0" Padding="0 0 5 0" />
                <dash:EditableTextBlock x:Name="xTextFieldAuthor" Text="author" HorizontalAlignment="Stretch" VerticalAlignment="Center" Padding="0 0 5 0" />
                <TextBlock Text="Created: " HorizontalAlignment="Stretch" FontStyle="Italic" FontSize="9" VerticalAlignment="Center" Margin="0 5 0 0" Padding="0 0 5 0" />
                <TextBlock x:Name="xTextFieldDateCreated" Text="created" HorizontalAlignment="Stretch" VerticalAlignment="Center" />
            </StackPanel>
        </Grid>  
        -- restyles an image to have a caption
<Grid
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:dash="using:Dash"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"></RowDefinition>
        <RowDefinition Height="Auto"></RowDefinition>
    </Grid.RowDefinitions>
        <Border Grid.Row="0" Background="CadetBlue" >
            <dash:EditableImage x:Name="xImageFieldData" Foreground="White" HorizontalAlignment="Stretch" Grid.Row="1" VerticalAlignment="Top" />
        </Border>
        <Border Grid.Row="1" Background="CadetBlue" MinHeight="30">
            <dash:RichEditView x:Name="xRichTextFieldCaption" TextWrapping="Wrap" Foreground="White" HorizontalAlignment="Stretch" Grid.Row="1" VerticalAlignment="Top" />
        </Border>
</Grid>
        <Grid
            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:dash="using:Dash"
            xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"></RowDefinition>
                <RowDefinition Height="*"></RowDefinition>
                <RowDefinition Height="Auto"></RowDefinition>
            </Grid.RowDefinitions>
                <Border BorderThickness="2" BorderBrush="CadetBlue" Background="White">
                    <TextBlock x:Name="xTextFieldTitle" Text="DOC TITLE" HorizontalAlignment="Stretch" Height="25" VerticalAlignment="Top"/>
                </Border>
                <Border Grid.Row="1" Background="CadetBlue" >
                    <dash:PdfView x:Name="xPdfFieldData" Foreground="White" HorizontalAlignment="Stretch" Grid.Row="1" VerticalAlignment="Top" />
                </Border>
            <StackPanel Orientation="Horizontal"  Grid.Row="2" Height="30" Background="White" >
                <TextBlock Text="Author:" HorizontalAlignment="Stretch" FontStyle="Italic" FontSize="9" VerticalAlignment="Center" Margin="0 5 0 0" Padding="0 0 5 0" />
                <dash:EditableTextBlock x:Name="xTextFieldAuthor" Text="author" HorizontalAlignment="Stretch" VerticalAlignment="Center" Padding="0 0 5 0" />
                <TextBlock Text="Created: " HorizontalAlignment="Stretch" FontStyle="Italic" FontSize="9" VerticalAlignment="Center" Margin="0 5 0 0" Padding="0 0 5 0" />
                <TextBlock x:Name="xTextFieldDateCreated" Text="created" HorizontalAlignment="Stretch" VerticalAlignment="Center" />
            </StackPanel>
        </Grid>
          <Grid
            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:dash="using:Dash"
            xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"></RowDefinition>
                    <RowDefinition Height="*"></RowDefinition>
                    <RowDefinition Height="Auto"></RowDefinition>
                </Grid.RowDefinitions>
                    <Border Grid.Row="1" Background="CadetBlue">
                        <StackPanel Orientation="Horizontal">
                            <dash:PdfView x:Name="xPdfFieldData" Foreground="White" HorizontalAlignment="Left" Width="1000" VerticalAlignment="Top" />
                            <dash:CollectionView x:Name="xCollectionFieldAnnotations" Width="5000" Background="Yellow" HorizontalAlignment="Left" VerticalAlignment="Stretch"/>
                        </StackPanel>
                    </Border>
                    <StackPanel Orientation="Horizontal"  Grid.Row="2" Height="30" Background="White">
                        <TextBlock Text="Author:" HorizontalAlignment="Stretch" FontStyle="Italic" FontSize="9" VerticalAlignment="Center" Margin="0 5 0 0" Padding="0 0 5 0"/>
                        <TextBlock x:Name="xTextFieldAuthor" Text="author" HorizontalAlignment="Stretch" VerticalAlignment="Center" Padding="0 0 5 0"/>
                        <TextBlock Text="Created: " HorizontalAlignment="Stretch" FontStyle="Italic" FontSize="9" VerticalAlignment="Center" Margin="0 5 0 0" Padding="0 0 5 0"/>
                        <TextBlock x:Name="xTextFieldDateCreated" Text="created" HorizontalAlignment="Stretch" VerticalAlignment="Center"/>
                    </StackPanel>
            </Grid>
            <Grid IsHitTestVisible="False" Opacity="0.3" HorizontalAlignment="Right" Margin="0 0 -85 0">
                <Grid.RenderTransform>
                    <RotateTransform Angle="45" CenterX="55" CenterY="50"/>
                </Grid.RenderTransform>
                 <Border BorderThickness="2" BorderBrush="CadetBlue" Background="White" Width="195" Height="25" HorizontalAlignment="Left" VerticalAlignment="Top">
                    <TextBlock x:Name="xTextFieldTitle" Text="DOC TITLE" HorizontalAlignment="Stretch" Padding="23 0 0 0" Height="25" VerticalAlignment="Top" />
                </Border>
            </Grid>
        </Grid>
        */


        private void UserControl_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = e.DataView.AvailableFormats.Contains(StandardDataFormats.Text) ? DataPackageOperation.Copy : DataPackageOperation.None;
        }

        
    }
}
