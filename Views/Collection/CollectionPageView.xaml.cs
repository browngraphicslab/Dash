using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using static Dash.DataTransferTypeInfo;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionPageView : ICollectionView
    {
        public UserControl UserControl => this;
        public CollectionViewModel ViewModel { get => DataContext as CollectionViewModel; }
        public CollectionViewModel OldViewModel = null;
        private DSL _dsl;
        private OuterReplScope _scope;

        public CollectionPageView()
        {
            this.InitializeComponent();
            xTextBox.AddKeyHandler(VirtualKey.Enter, EnterPressed);
            xThumbs.Loaded += (sender, e) =>
            {
                DataContextChanged -= CollectionPageView_DataContextChanged;
                DataContextChanged += CollectionPageView_DataContextChanged;
                if (ViewModel != null)
                    CollectionPageView_DataContextChanged(null, null);
                foreach (var t in ViewModel.ThumbDocumentViewModels)
                    t.Width = xThumbs.ActualWidth;
                if (xThumbs.Items.Count > 0)
                    xThumbs.SelectedIndex = 0;
            };
            xThumbs.SizeChanged += (sender, e) =>
            {
                if (CurPage?.Content is CollectionView cview)
                    cview.ViewModel.FitContents(cview);
                foreach (var t in ViewModel.ThumbDocumentViewModels)
                    t.Width = xThumbs.ActualWidth;
            };
            Unloaded += (sender, e) =>
            {
                if (ViewModel != null)
                    ViewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;
                if (OldViewModel != null)
                    OldViewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;
                OldViewModel = null;
            };

            this.AddHandler(KeyDownEvent, new KeyEventHandler(SelectionElement_KeyDown), true);
            this.xDocContainer.AddHandler(PointerReleasedEvent, new PointerEventHandler(xDocContainer_PointerReleased), true);
            this.GotFocus += CollectionPageView_GotFocus;
            this.LosingFocus += CollectionPageView_LosingFocus;
        }

        private void EnterPressed(KeyRoutedEventArgs obj)
        {
            if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
            {
                return;
            }

            var keyString = xTextBox.Text;
            if (keyString?.StartsWith("=") ?? false)
            {
                try
                {
                    var result = _dsl.Run(keyString.Substring(1));
                    SetHackCaptionText(result == null
                        ? new TextController(
                            "Field not found, make sure the key name is correct and that you're accessing the right document!")
                        : result);
                }
                catch (DSLException)
                {
                    SetHackCaptionText(new TextController(keyString));
                }
            }
            //_scope = new OuterReplScope();
            //_scope.DeclareVariable("this", CurPage.DocumentController);
            //var reference = DSL.InterpretUserInput(keyString, true, _scope);
            //SetHackCaptionText(reference);

            if (obj != null) obj.Handled = true;
        }

        private void DocumentViewModels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            PageDocumentViewModels.Clear();
            ViewModel.ThumbDocumentViewModels.Clear();
            foreach (var pageDoc in ViewModel.DocumentViewModels.Select((vm) => vm.DocumentController))
            {
                var pageViewDoc = pageDoc.GetViewCopy();
                pageViewDoc.SetLayoutDimensions(double.NaN, double.NaN);

                PageDocumentViewModels.Add(new DocumentViewModel(pageViewDoc) { Undecorated = true });

                DocumentController thumbnailImageViewDoc = null;
                if (!string.IsNullOrEmpty(pageDoc.Title))
                {
                    thumbnailImageViewDoc = new PostitNote(pageDoc.Title.Substring(0, Math.Min(100, pageDoc.Title.Length))).Document;
                    thumbnailImageViewDoc.GetDataDocument().SetField(KeyStore.DataKey, new DocumentReferenceController(pageDoc.GetDataDocument(), KeyStore.TitleKey), true);
                    thumbnailImageViewDoc.SetField<NumberController>(TextingBox.TextAlignmentKey,
                        (double) TextAlignment.Left, true);
                }
                else
                {
                    thumbnailImageViewDoc = (pageDoc.GetDereferencedField(KeyStore.ThumbnailFieldKey, null) as DocumentController ?? pageDoc).GetViewCopy();
                }
                thumbnailImageViewDoc.SetLayoutDimensions(double.NaN, double.NaN);
                thumbnailImageViewDoc.SetBackgroundColor(Colors.Transparent);
                ViewModel.ThumbDocumentViewModels.Add(new DocumentViewModel(thumbnailImageViewDoc) { Undecorated = true });
            }
            CurPage = PageDocumentViewModels.LastOrDefault();
        }

        private void CollectionPageView_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (args.FocusState == FocusState.Pointer)
            {
                if (this.GetFirstDescendantOfType<ScrollViewer>() == args.OldFocusedElement)
                    args.Handled = args.Cancel = true;
                if (this.GetFirstDescendantOfType<Microsoft.Toolkit.Uwp.UI.Controls.GridSplitter>() == args.OldFocusedElement)
                {
                    var xx = this.GetFirstDescendantOfType<Microsoft.Toolkit.Uwp.UI.Controls.GridSplitter>();
                    args.Handled = args.Cancel = true;
                }
            }
            else if (args.FocusState == FocusState.Keyboard)
            {
                //if (this.GetDescendantsOfType<RichEditBox>().Contains(args.OldFocusedElement))
                    args.Handled = args.Cancel = true;
            }
        }

        private void CollectionPageView_GotFocus(object sender, RoutedEventArgs e)
        {
        }

        public ObservableCollection<DocumentViewModel> PageDocumentViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();


        private void CollectionPageView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args != null)
                args.Handled = true;
            if (ViewModel != null && ViewModel != OldViewModel)
            {
                if (OldViewModel != null)
                    OldViewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged1;
                ViewModel.DocumentViewModels.CollectionChanged += DocumentViewModels_CollectionChanged;
                DocumentViewModels_CollectionChanged(null, null);
                OldViewModel = ViewModel;
            }
        }

        private void DocumentViewModels_CollectionChanged1(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void xThumbs_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (xThumbs.Items.Count > 0)
                xThumbs.SelectedIndex = 0;
        }
        private void xThumbs_Loaded(object sender, RoutedEventArgs e)
        {
            if (xThumbs.Items.Count > 0)
                xThumbs.SelectedIndex = 0;
        }

        KeyController CaptionKey = null;
        KeyController DisplayKey = null;
        string DisplayString = "";

        public void SetHackCaptionText(FieldControllerBase caption)
        {
            var textBox = xTextBox;
            xDocContainer.Children.Clear();
            xDocContainer.Children.Add(textBox);
            var dataBox = new DataBox(caption);
            var docView = dataBox.Document.MakeViewUI(null);
            Grid.SetRow(docView, 0);
            xDocContainer.Children.Add(docView);
            //captionKey = captionKey ?? KeyStore.TitleKey;
            //if (captionKey != null && CurPage != null)
            //{
            //    var bodyDoc = CurPage.DataDocument.GetDereferencedField<DocumentController>(DisplayKey, null)?.GetDataDocument();
            //    xDocTitle.Visibility = Windows.UI.Xaml.Visibility.Visible;
            //    CaptionKey = captionKey;

            //    var currPageBinding = new FieldBinding<TextController>()
            //    {
            //        Mode = BindingMode.TwoWay,
            //        Document = CurPage.DataDocument,
            //        Key = CaptionKey,
            //        FieldAssignmentDereferenceLevel = XamlDereferenceLevel.DontDereference,
            //        Converter = new ObjectToStringConverter()
            //    };
            //    xDocTitle.AddFieldBinding(TextBox.TextProperty, currPageBinding);

            //    //if (bodyDoc?.Equals(CurPage.DataDocument) == false)
            //    //    bodyDoc?.SetField(CaptionKey,
            //    //        new DocumentReferenceController(CurPage.DataDocument.GetId(),
            //    //            CaptionKey), true);

            //    xDocTitle.Height = 50;
            //    xDocCaptionRow.Height = new GridLength(50);
            //}
        }
        public void SetHackBodyDoc(KeyController documentKey, string keyasgn)
        {
            documentKey = documentKey ?? KeyStore.DataKey;
            if (documentKey != null && CurPage != null)
            {
                DisplayString = keyasgn;
                DisplayKey = documentKey;
                //xDocView.Visibility = Windows.UI.Xaml.Visibility.Visible;
                var data = CurPage.DataDocument.GetDereferencedField(DisplayKey,null);
                if (!string.IsNullOrEmpty(DisplayString))
                {
                    var keysToReplace = new Regex("#[a-z0-9A-Z_]*").Matches(DisplayString);
                    var replacedString = DisplayString;
                    foreach (var keyToReplace in keysToReplace)
                    {
                        var k = new KeyController(keyToReplace.ToString().Substring(1));
                        var value = CurPage.DataDocument.GetDereferencedField<TextController>(k, null)?.Data;
                        if (value != null)
                            replacedString = replacedString.Replace(keyToReplace.ToString(), value);
                    }

                    var img = replacedString == "this" ? CurPage.DocumentController : MainPage.Instance.xMainSearchBox.SearchForFirstMatchingDocument(replacedString, CurPage.DataDocument);
                    if (img != null && (!(data is DocumentController) || !img.GetDataDocument().Equals((data as DocumentController).GetDataDocument())))
                    {
                        var imgView = img.GetViewCopy();
                        imgView.GetWidthField().NumberFieldModel.Data = double.NaN;
                        imgView.GetHeightField().NumberFieldModel.Data = double.NaN;
                        data = imgView;
                    }
                }
                if (data != null)
                {
                    CurPage.DataDocument.SetField(DisplayKey, data, true);
                    var db = new DataBox(data,0, 0, double.NaN, double.NaN); // CurPage.DocumentController.GetDataDocument(null).GetField(DisplayKey));
                    
                    xDocView.DataContext = new DocumentViewModel(db.Document) { Undecorated = true };
                    _scope = new OuterReplScope();
                    _scope.DeclareVariable("this", db.Document);
                    _dsl = new DSL(_scope);
                }
            }
        }
        public DocumentViewModel CurPage
        {
            get { return (xThumbs.SelectedIndex < PageDocumentViewModels.Count && xThumbs.SelectedIndex >= 0) ? PageDocumentViewModels[xThumbs.SelectedIndex] : xDocView.DataContext as DocumentViewModel; }
            set
            {
                xDocView.DataContext = value;
                _scope = new OuterReplScope();
                _scope.DeclareVariable("this", value?.DocumentController);
                _dsl = new DSL(_scope);

                //SetHackBodyDoc(DisplayKey, DisplayString); // TODO order of these maters cause of writing body doc
                //SetHackCaptionText(value.DocumentController);

                
                if (value?.Content is CollectionView cview)
                {
                    void Cview_Loaded(object sender, RoutedEventArgs e)
                    {
                        void ContainerDocument_FieldModelUpdated(FieldControllerBase s, FieldUpdatedEventArgs args, Context context)
                        {
                            cview.ViewModel.FitContents(cview);
                        }
                        cview.ViewModel.ContainerDocument.FieldModelUpdated -= ContainerDocument_FieldModelUpdated;
                        cview.ViewModel.ContainerDocument.FieldModelUpdated += ContainerDocument_FieldModelUpdated;
                        cview.ViewModel.FitContents(cview);
                    }
                    cview.ViewModel.ContainerDocument.SetActualSize(new Windows.Foundation.Point(xDocView.ActualWidth, xDocView.ActualHeight));
                    cview.Loaded -= Cview_Loaded;
                    cview.Loaded += Cview_Loaded;
                    cview.ViewModel.FitContents(cview);
                }
                EnterPressed(null);
            }
        }
        
        #region DragAndDrop

        //private void CollectionViewOnDragLeave(object sender, DragEventArgs e)
        //{
        //    this.xDockSpots.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        //}

        //private void CollectionViewOnDragOver(object sender, DragEventArgs e)
        //{
        //    this.xDockSpots.Visibility = Windows.UI.Xaml.Visibility.Visible;
        //}

        //private void Top_Drop(object sender, DragEventArgs e)
        //{
        //    this.xDockSpots.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        //    if (!e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
        //        return;
        //    var dragModel = e.DataView.Properties[nameof(DragDocumentModel)] as DragDocumentModel;
        //    var keyString = dragModel.DraggedDocument.GetDataDocument()?.GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null)?.Data;
        //    if (keyString?.StartsWith("#") == true)
        //    {
        //        var key = keyString.Substring(1);
        //        var splits = key.Split("=");
        //        var keyName = splits.Length > 0 ? splits[0] : key;
        //        var keyasgn = splits.Length > 1 ? splits[1] : "";

        //        SetHackBodyDoc(new KeyController(keyName), keyasgn);
                
        //        e.AcceptedOperation = DataPackageOperation.Copy;
        //    }
        //    e.Handled = true;
        //}

        //private void Bottom_Drop(object sender, DragEventArgs e)
        //{
        //    this.xDockSpots.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        //    if (!e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
        //        return;
        //    var dragModel = e.DataView.Properties[nameof(DragDocumentModel)] as DragDocumentModel;
        //    if (dragModel.DraggedKey != null)
        //    {
        //        SetHackCaptionText(dragModel.DraggedDocument);
        //        e.AcceptedOperation = DataPackageOperation.Copy;
        //    }
        //    else
        //    {
        //        var keyString = dragModel.DraggedDocument.GetDataDocument()?.GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null)?.Data;
        //        if (keyString?.StartsWith("=") ?? false)
        //        {
        //            try
        //            {
        //                var result = _dsl.Run(keyString.Substring(1));
        //                SetHackCaptionText(result);
        //            }
        //            catch (DSLException)
        //            {
        //                SetHackCaptionText(new TextController(keyString));
        //            }

        //            e.AcceptedOperation = DataPackageOperation.Copy;
        //        }
        //    }
        //    e.Handled = true;
        //}

        public void SetDropIndicationFill(Brush fill)
        {
        }
        #endregion

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurPage != null)
            {
                var ind = PageDocumentViewModels.IndexOf(CurPage);
                xThumbs.SelectedIndex = Math.Max(0, ind - 1);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurPage != null)
            {
                var ind = PageDocumentViewModels.IndexOf(CurPage);
                xThumbs.SelectedIndex = Math.Min(PageDocumentViewModels.Count - 1, ind + 1);
            }
        }
        
        private void xThumbs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var ind = xThumbs.SelectedIndex;
            if (PageDocumentViewModels.Count > 0)
            {
                CurPage = PageDocumentViewModels[Math.Max(0, Math.Min(PageDocumentViewModels.Count - 1, ind))];
                _scope = new OuterReplScope();
                _scope.DeclareVariable("this", CurPage.DocumentController);
                _dsl = new DSL(_scope);
            }
            if (xThumbs.ItemsPanelRoot != null &&  ind >= 0 && ind < xThumbs.ItemsPanelRoot.Children.Count)
            {
                var x = xThumbs.ItemsPanelRoot.Children[ind].GetFirstDescendantOfType<Control>();
                if (x != null)
                {

                    try
                    {
                        x.Focus(FocusState.Keyboard);
                        x.Focus(FocusState.Pointer);
                    } catch (Exception)
                    {

                    }
                }
            }
        }

        private void XDrag_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Handled = true;
            e.Complete();
        }

        private void XDragContainer_DragStarting(UIElement sender, DragStartingEventArgs e)
        {
            e.Data.RequestedOperation = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            e.AllowedOperations       = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            e.Data.Properties.Add("Width", xDocView.ActualWidth);
            e.Data.Properties.Add("Height", xDocView.ActualHeight);
            e.Data.AddDragModel(new DragDocumentModel(CurPage.DocumentController, true));
        }

        private void SelectionElement_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Handled)
                return;
            if (e.Key == Windows.System.VirtualKey.PageDown || e.Key == Windows.System.VirtualKey.Down)
            {
                NextButton_Click(sender, e);
                e.Handled = true;
            }
            if (e.Key == Windows.System.VirtualKey.PageUp || e.Key == Windows.System.VirtualKey.Up)
            {
                PrevButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void TextBlock_GettingFocus(UIElement sender, GettingFocusEventArgs args)
        {
            try
            {
                args.Cancel = true;
            } catch (Exception)
            {

            }
        }

        private void xThumbs_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try { 
                xThumbs.Focus(FocusState.Pointer);
            }
            catch (Exception)
            {

            }
        }
        private void xDocContainer_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!(FocusManager.GetFocusedElement() is FrameworkElement focus) || 
                focus.GetFirstAncestorOfType<CollectionPageView>() != this || 
                xThumbs.GetDescendants().Contains(focus))
            {
                xThumbs.Focus(FocusState.Pointer);
                e.Handled = true;
            }
        }

        private void XThumbs_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = ManipulationModes.None;
            foreach (object m in e.Items)
            {
                int ind = ViewModel.ThumbDocumentViewModels.IndexOf(m as DocumentViewModel);
                e.Data.AddDragModel(new DragDocumentModel(PageDocumentViewModels[ind].DocumentController, true));
            }
        }

        private void xThumbs_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = e.GetCurrentPoint(this).Properties.IsRightButtonPressed ? ManipulationModes.All : ManipulationModes.None;
        }

        /// <summary>
        /// When left-dragging, we need to "handle" the manipulation since the splitter doesn't do that and the manipulation will 
        /// propagate to the ManipulationControls which will start moving the parent document.
        /// When right-dragging, we want to terminate the manipulation and let the parent document use its ManipulationControlHelper to drag the document.
        /// The helper is setup in the CollectionView's PointerPressed handler;
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xSplitter_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (!this.IsRightBtnPressed())
                e.Handled = true;
            else e.Complete();
         }

        /// <summary>
        /// when we're left-dragging the splitter, we don't want to let events fall through to the ManipulationControls which would cancel the manipulation.
        /// Since the splitter doesn't handle it's own drag events, we do it here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xSplitter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true; 
        }

        private void xSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void XTextBox_OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.TryGetLoneDragModel(out DragModelBase dragModel) && dragModel is DragFieldModel field)
            {
                KeyController fieldKey = field.DraggedRefs.First().FieldKey;

                if (xTextBox.Text.Length == 0) xTextBox.Text = "=this";

                xTextBox.Text += "." + fieldKey.Name.RemoveWhitespace();

                e.Handled = true;
            }
        }

        private void ApplyScript_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var docs = new List<DocumentController>();
            int i = 0;
            foreach (var docViewModel in ViewModel.DocumentViewModels)
            {
                var doc = docViewModel.DocumentController;
                _scope = new OuterReplScope();
                _scope.DeclareVariable("this", doc);
                _dsl = new DSL(_scope);
                var keyString = xTextBox.Text;
                if (keyString?.StartsWith("=") ?? false)
                {
                    try
                    {
                        var result = _dsl.Run(keyString.Substring(1));
                        var db = new DataBox(result, i * 50, i * 50);
                        docs.Add(db.Document);
                    }
                    catch (DSLException)
                    {
                        continue;
                    }
                }

                i++;
            }
            args.Data.AddDragModel(new DragDocumentModel(new CollectionNote(new Point(0, 0), CollectionView.CollectionViewType.Grid, 500, 300, docs).Document, true));
            // args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
        }
    }
}
