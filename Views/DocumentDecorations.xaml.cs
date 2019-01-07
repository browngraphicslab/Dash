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
        private bool                           _parentIsFreeform = false;
        private bool                           _showPDFcontrols = false;
        private bool                           _doubleTapped = false;
        private Visibility                     _resizerVisibilityState = Visibility.Collapsed;
        private ToolTip                        _titleTip = new ToolTip { Placement = PlacementMode.Top };
        private IEnumerable<DocumentViewModel> _selectedDocViewModels => SelectionManager.SelectedDocViewModels;

        public bool                              ShowPDFControls
        {
            get => _showPDFcontrols;
            set => SetProperty(ref _showPDFcontrols, value);
        }
        public bool                              ParentIsFreeform
        {
            get => _parentIsFreeform;
            set => SetProperty(ref _parentIsFreeform, value);
        }
        public Visibility                        ResizerVisibilityState
        {
            get => _resizerVisibilityState;
            set => SetProperty(ref _resizerVisibilityState, value);
        }
        public List<LinkButton>                  LinkButtons    = new List<LinkButton>();
        public static KeyController              HeaderFieldKey = KeyStore.TitleKey;
        public event PropertyChangedEventHandler PropertyChanged;

        public DocumentDecorations()
        {
            DataContext = this;
            void keyHdlr(object sender, KeyRoutedEventArgs e)     { SetPositionAndSize(); }
            void ptrHdlr(object sender, PointerRoutedEventArgs e) { SetPositionAndSize(); }
            void tapHdlr(object sender, TappedRoutedEventArgs e)  { SetPositionAndSize(); }
            
            MainPage.Instance.xOuterGrid.AddHandler(PointerMovedEvent,        new PointerEventHandler(ptrHdlr), true);
            MainPage.Instance.xOuterGrid.AddHandler(PointerReleasedEvent,     new PointerEventHandler(ptrHdlr), true);
            MainPage.Instance.xOuterGrid.AddHandler(PointerWheelChangedEvent, new PointerEventHandler(ptrHdlr), true);
            MainPage.Instance.xOuterGrid.AddHandler(TappedEvent,              new TappedEventHandler(tapHdlr), true);
            MainPage.Instance.xOuterGrid.AddHandler(KeyDownEvent,             new KeyEventHandler(keyHdlr), true);
            MainPage.Instance.xOuterGrid.AddHandler(KeyUpEvent,               new KeyEventHandler(keyHdlr), true);
            InitializeComponent();
            _titleTip.Content = HeaderFieldKey.Name;
            ToolTipService.SetToolTip(xHeaderText, _titleTip);
            xHeaderText.PointerEntered += (s,e) => _titleTip.IsOpen = true;
            xHeaderText.PointerExited  += (s,e) => _titleTip.IsOpen = false;
            xHeaderText.GotFocus       += (s,e) => (xHeaderText.Text == "<empty>" ? xHeaderText : null)?.SelectAll();
            Loaded                     += (s,e) => SelectionManager.SelectionChanged += SelectionManager_SelectionChanged;
            // setup ResizeHandles
            void ResizeHandles_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
            {
                if (this.IsRightBtnPressed())
                {
                    SelectionManager.InitiateDragDrop(SelectionManager.SelectedDocViews.FirstOrDefault(), null);
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
                (xTopLeftResizeControl, SplitDirection.InPlace), (xTopResizeControl, SplitDirection.Up), (xTopRightResizeControl, SplitDirection.InPlace),
                (xLeftResizeControl, SplitDirection.Left), (xRightResizeControl, SplitDirection.Right),
                (xBottomLeftResizeControl, SplitDirection.InPlace), (xBottomRightResizeControl, SplitDirection.InPlace), (xBottomResizeControl, SplitDirection.Down) })
            {
                handle.Item1.ManipulationStarted += ResizeHandles_OnManipulationStarted;
                if (handle.Item2 != SplitDirection.InPlace)
                {
                    handle.Item1.DoubleTapped += (s, e) =>
                    {
                        _doubleTapped = true;
                        var vm = _selectedDocViewModels.FirstOrDefault();
                        var frame = MainPage.Instance.MainSplitter.GetFrameWithDoc(vm?.DocumentController, true);
                        if (frame != null)
                        {
                            frame.Delete();
                        }
                        else
                        {
                            // bcz: frame location should be determined by which part of the resize rectangle is tapped (e.g., left, right, top, bottom)
                            SplitFrame.OpenInInactiveFrame(vm.DocumentController, handle.Item2);
                        }
                    };
                    handle.Item1.Tapped += async (s, e) =>
                    {
                        _doubleTapped = false;
                        await System.Threading.Tasks.Task.Delay(100);
                        if (!_doubleTapped)
                        {
                        }
                    };
                }
            }
            SelectionManager.DragManipulationStarted += (s, e) => Visibility = Visibility.Collapsed;
        }

        public void SetPositionAndSize()
        {
            Visibility = _selectedDocViewModels.Any() ? Visibility.Visible : Visibility.Collapsed;
            try
            {
                var topLeft  = new Point(double.PositiveInfinity, double.PositiveInfinity);
                var botRight = new Point(double.NegativeInfinity, double.NegativeInfinity);
                foreach (var docView in SelectionManager.SelectedDocViews)
                {
                    var viewModelBounds = docView.TransformToVisual(MainPage.Instance.xCanvas).TransformBounds(new Rect(new Point(), new Size(docView.ActualWidth, docView.ActualHeight)));

                    topLeft.X  = Math.Min(viewModelBounds.Left, topLeft.X);
                    topLeft.Y  = Math.Min(viewModelBounds.Top, topLeft.Y);

                    botRight.X = Math.Max(viewModelBounds.Right, botRight.X);
                    botRight.Y = Math.Max(viewModelBounds.Bottom, botRight.Y);
                }
                if (SelectionManager.SelectedDocViews.Any())
                {
                    if (botRight.X > MainPage.Instance.ActualWidth - xAnnotationButtonsStack.ActualWidth - MainPage.Instance.xLeftGrid.ActualWidth)
                    {
                        botRight = new Point(MainPage.Instance.ActualWidth - xAnnotationButtonsStack.ActualWidth - MainPage.Instance.xLeftGrid.ActualWidth, botRight.Y);
                    }

                    RenderTransform     = new TranslateTransform { X = topLeft.X, Y = topLeft.Y };
                    ContentColumn.Width = new GridLength(Math.Max(0, botRight.X - topLeft.X));
                    ContentRow.Height   = new GridLength(botRight.Y - topLeft.Y);
                }
            }
            catch (Exception e) { Debug.WriteLine("Got Exception:" + e); }
        }
        public void SetSearchBoxFocus() { xSearchBox.Focus(FocusState.Programmatic); }
        /// <summary>
        /// adds a button for a link type to appear underneath the link button
        /// </summary>
        public void AddLinkTypeButton(string linkName)
        {
            linkName = linkName ?? "Annotation";
            if (_selectedDocViewModels.Any() && // add button if something's selected and we don't have this link type already
                !xButtonsPanel.Children.OfType<LinkButton>().Any(lb => lb.Text.Equals(linkName)))
            {
                var btnColorOrig = LinkMenu.GetTagColor(linkName); // set button color to tag color
                var btnColorFinal = btnColorOrig != null
                        ? Color.FromArgb(200, btnColorOrig.Value.R, btnColorOrig.Value.G, btnColorOrig.Value.B)
                        : Color.FromArgb(255, 64, 123, 177);

                var toolTip = new ToolTip { Content = linkName, HorizontalOffset = 5, Placement = PlacementMode.Right };
                var button  = new LinkButton(this, btnColorFinal, linkName, toolTip, SelectionManager.SelectedDocViews.FirstOrDefault());
                xButtonsPanel.Children.Add(button);
                LinkButtons.Add(button);
                ToolTipService.SetToolTip(button, toolTip);  // adds tooltip with link tag name inside
            }
        }
        public void OpenNewLinkMenu(string text, DocumentController linkDoc)
        {
            LinkButtons.ForEach(lb => (lb.Text.Equals(text ?? "Annotation") ? lb : null)?.OpenFlyout(lb,linkDoc));
        }
        /// <summary>
        /// Rebuilds the different link dots when the menu is refreshed or one is added
        /// </summary>
        public void RebuildMenu()
        {
            xButtonsPanel.Children.Clear();
            LinkButtons.Clear();
            //check each relevant tag name & create the tag graphic & button for it

            var theDoc = _selectedDocViewModels.FirstOrDefault()?.DataDocument;

            var allLinks   = theDoc?.GetLinks(null)?.ToList() ?? new List<DocumentController>();
            var allRegions = theDoc?.GetRegions()?.SelectMany(r => r.GetDataDocument().GetLinks(null)?.ToList() ?? new List<DocumentController>())
                                    ?? new List<DocumentController>();  
            allLinks.AddRange(allRegions);
            foreach (var link in allLinks)
            {
                AddLinkTypeButton(link?.GetDataDocument().GetField<TextController>(KeyStore.LinkTagKey)?.Data);
            }
            
            xButtonsCanvas.Height = xButtonsPanel.Children.Aggregate(xAnnotateEllipseBorder.ActualHeight, (hgt, child) => hgt += (child as FrameworkElement).Height);

            var uriString = theDoc?.GetDereferencedField<TextController>(KeyStore.SourceUriKey,null)?.Data;
            if (!string.IsNullOrEmpty(uriString))  
            {
                Uri uri = null;
                try
                {
                    uri = new Uri(uriString);
                }
                catch (Exception) { uri = new Uri("dash:" + uriString); }  // bcz: for backward compatibility

                var hyperlink = new Hyperlink();
                if (uri.Scheme == "dash")
                {
                    if (RESTClient.Instance.Fields.GetController<DocumentController>(uri.AbsolutePath) is DocumentController doc)
                    {
                        hyperlink.Click += (s, e) => Launcher.LaunchUriAsync(uri, new LauncherOptions() { LimitPickerToCurrentAppAndAppUriHandlers = false });
                        hyperlink.Inlines.Add(new Run() { Text = (doc.GetDataDocument().GetRegionDefinition() ?? doc).Title });
                    }
                }
                else
                {
                    hyperlink.NavigateUri = uri;
                    hyperlink.Inlines.Add(new Run { Text = " " + HtmlToDashUtil.GetTitlesUrl(uriString) });
                }
                xURISource.Text = "From:";     // add a hyperlink that points to the source webpage.
                xURISource.Inlines.Add(hyperlink);
            }
            else
            {
                var author = theDoc?.GetDereferencedField<TextController>(KeyStore.AuthorKey,null)?.Data;
                xURISource.Text = string.IsNullOrEmpty(author) ? "" : "Authored by: " + author; // add a hyperlink that points to the source webpage.
            }
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void SelectionManager_SelectionChanged(DocumentSelectionChangedEventArgs args)
        {
            RebuildMenu();
            SetPositionAndSize();
            ResetHeader(); // force header field to update

            xButtonsCanvas.Margin = new Thickness(_selectedDocViewModels.Any(dv => dv.InsetDecorations == true) ? -6 : 0, 
                                                  _selectedDocViewModels.Any(dv => dv.InsetDecorations == true) ?  6 : 0, 0, 0);
            ParentIsFreeform = true;
            ShowPDFControls  = false;
            try
            {
                foreach (var docView in SelectionManager.SelectedDocViews)
                {
                    if (docView.ViewModel.LayoutDocument.DocumentType.Equals(PdfBox.DocumentType) == true)
                    {
                        ShowPDFControls = true;
                    }
                    if (docView.GetFirstAncestorOfType<CollectionView>()?.CurrentView.ViewType != CollectionViewType.Freeform)
                    {
                        ParentIsFreeform = false;
                    }
                    xPinSymbol.Symbol = docView.ViewModel.LayoutDocument.GetAreContentsHitTestVisible() ? Symbol.Pin : Symbol.UnPin;
                }
            } catch (Exception e) {  }
            ResizerVisibilityState = _selectedDocViewModels.FirstOrDefault()?.ResizersVisible == true ? Visibility.Visible : Visibility.Collapsed;
            if (_selectedDocViewModels.Count() == 1)
            {
                xSearchBox.Text = _selectedDocViewModels.First().DocumentController.GetField<TextController>(KeyStore.SearchStringKey)?.Data ?? "";
            }
            Visibility = _selectedDocViewModels.Any() ? Visibility.Visible : Visibility.Collapsed;
        }
        private void xDelete_Tapped(object sender, TappedRoutedEventArgs e) { 
            SelectionManager.SelectedDocViews.ToList().ForEach((dv) => MainPage.Instance.ClearFloatingDoc(dv));
            SelectionManager.DeleteSelected();
        }

        private void xPinPanel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (xPinSymbol.Symbol == Symbol.Pin)
            {
                xPinSymbol.Symbol = Symbol.UnPin;
                SelectionManager.SelectedDocViewModels.ToList().ForEach(vm => vm.LayoutDocument.SetAreContentsHitTestVisible(false));
            }
            else
            {
                SelectionManager.SelectedDocViewModels.ToList().ForEach(vm => vm.LayoutDocument.SetAreContentsHitTestVisible(true));
                xPinSymbol.Symbol = Symbol.Pin;
            }
        }

        private async void XAnnotateEllipseBorder_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            _doubleTapped = false;
            await System.Threading.Tasks.Task.Delay(100);
            if (!_doubleTapped)
            {
                SelectionManager.SelectedDocViews.FirstOrDefault()?.ShowContextMenu(e.GetPosition(MainPage.Instance)); 
            }
        }
        private void xAnnotateEllipseBorder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            _doubleTapped = true;
            var kvp = _selectedDocViewModels.FirstOrDefault()?.DocumentController.GetKeyValueAlias(new Point());
            MainPage.Instance.AddFloatingDoc(kvp, new Point(500, 300), e.GetPosition(MainPage.Instance.xCanvas));
        }
        private void XAnnotateEllipseBorder_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var dragDocOffset  = args.GetPosition(sender);
            var relDocOffsets  = SelectionManager.SelectedDocViews.Select(args.GetPosition).Select(ro => new Point(ro.X - dragDocOffset.X, ro.Y - dragDocOffset.Y)).ToList();
            var parCollections = SelectionManager.SelectedDocViews.Select(dv => dv.GetFirstAncestorOfType<AnnotationOverlayEmbeddings>() == null ? dv.ParentViewModel : null).ToList();
            args.Data.SetDragModel(new DragDocumentModel(SelectionManager.SelectedDocViews, parCollections, relDocOffsets, dragDocOffset) { DraggingLinkButton = true });
            args.AllowedOperations =
                DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            args.Data.RequestedOperation =
                DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
            //touchActivated = false;
        }

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

        public void XPDFButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            SelectionManager.SelectedDocViews.SelectMany(v => new[] { v.GetFirstDescendantOfType<PdfView>() }.ToList()).ToList().ForEach(pv =>
            {
                if (sender == xNextPageButton) pv?.NextPage();
                if (sender == xPreviousPageButton) pv?.PrevPage();
                if (sender == xScrollBack) pv?.ScrollBack();
                if (sender == xScrollForward) pv?.ScrollForward();
            });
            e.Handled = true;
        }

        private void XTitleBorder_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            CapturePointer(e.Pointer);
            e.Handled = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
        }

        private void ResizeTLaspect       (object s, ManipulationDeltaRoutedEventArgs e) { resize(e, true,  true,  true); }
        private void ResizeRTaspect       (object s, ManipulationDeltaRoutedEventArgs e) { resize(e, true,  false, true); }
        private void ResizeBLaspect       (object s, ManipulationDeltaRoutedEventArgs e) { resize(e, false, true,  true); }
        private void ResizeBRaspect       (object s, ManipulationDeltaRoutedEventArgs e) { resize(e, false, false, true); }
        private void ResizeRTunconstrained(object s, ManipulationDeltaRoutedEventArgs e) { resize(e, true,  false, false); }
        private void ResizeBLunconstrained(object s, ManipulationDeltaRoutedEventArgs e) { resize(e, false, true,  false); }
        private void ResizeBRunconstrained(object s, ManipulationDeltaRoutedEventArgs e) { resize(e, false, false, false); }
        private void resize(ManipulationDeltaRoutedEventArgs e, bool shiftTop, bool shiftLeft, bool unconstrained)
        {
            SelectionManager.SelectedDocViews.ToList().ForEach(dv => dv.Resize(e, shiftTop, shiftLeft, unconstrained));
        }

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
                case VirtualKey.Up: ChooseNextHeaderKey(e.Key == VirtualKey.Up); break;
                default: xHeaderText.Foreground = new SolidColorBrush(Colors.Red); break;
            }
            e.Handled = true;
        }

        private void ChooseNextHeaderKey(bool prev = false)
        {
            var keys = new List<KeyController>();
            foreach (var d in _selectedDocViewModels.Select(sd => sd.DataDocument))
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
            foreach (var doc in _selectedDocViewModels.Select(sd => sd.DocumentController))
            {
                var targetDoc = doc.GetField<TextController>(HeaderFieldKey)?.Data != null ? doc : doc.GetDataDocument();

                targetDoc.SetField<TextController>(HeaderFieldKey, xHeaderText.Text, true);
            }
            xHeaderText.Background = new SolidColorBrush(Colors.LightBlue);
            ResetHeader();
        }
        private void ResetHeader(string newkey = null)
        {
            if (_selectedDocViewModels.Any())
            {
                if (newkey != null)
                {
                    HeaderFieldKey = KeyController.Get(newkey);
                }
                var layoutHeader = _selectedDocViewModels.FirstOrDefault()?.DocumentController.GetField<TextController>(HeaderFieldKey)?.Data;
                xHeaderText.Text = layoutHeader ?? _selectedDocViewModels.FirstOrDefault()?.DataDocument.GetDereferencedField<TextController>(HeaderFieldKey, null)?.Data ?? "<empty>";
                if (_selectedDocViewModels.Count() > 1)
                {
                    foreach (var d in _selectedDocViewModels.Select(sd => sd.DataDocument))
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

        private void TitleEllipse_StartDragging(UIElement sender, DragStartingEventArgs args)
        {
            var activeDoc = _selectedDocViewModels.FirstOrDefault()?.DocumentController;
            args.Data.SetDragModel(new DragFieldModel(new DocumentFieldReference(activeDoc.GetDataDocument(), HeaderFieldKey)));
            // args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
        }

        private void DocumentDecorations_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = e.DataView.AvailableFormats.Contains(StandardDataFormats.Text) ? DataPackageOperation.Copy : DataPackageOperation.None;
        }
        private async void DocumentDecorations_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            var xamlText = (await e.DataView.GetTextAsync()).Replace("\"", "'");
            using (UndoManager.GetBatchHandle())
            {
                _selectedDocViewModels.ToList().ForEach(dv => dv.DocumentController.SetXaml(xamlText));
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
            foreach (var doc in _selectedDocViewModels.Select(dv => dv.DocumentController))
            {
                var searchIndex = doc.GetField<NumberController>(KeyStore.SearchIndexKey)?.Data ?? -1;
                doc.SetField<NumberController>(KeyStore.SearchIndexKey, Math.Max(0, searchIndex + change), true);
            }
        }

        private bool updateSearchString()
        {
            foreach (var doc in _selectedDocViewModels.Select(dv => dv.DocumentController))
            {
                var searchString = doc.GetField<TextController>(KeyStore.SearchStringKey)?.Data ?? "";
                if (!searchString.Equals(xSearchBox.Text))
                {
                    doc.SetField<TextController>(KeyStore.SearchStringKey, xSearchBox.Text, true);
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


        /// <summary>
        ///  pulled from ViewModelBase
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storage"></param>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
