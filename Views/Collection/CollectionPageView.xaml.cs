using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionPageView : ICollectionView
    {
        public UserControl UserControl => this;
        public CollectionViewModel ViewModel { get => DataContext as CollectionViewModel; }
        public CollectionViewModel OldViewModel = null;
        public ObservableCollection<DocumentViewModel> PageDocumentViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();
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
                if (xThumbs.Items.Count > 0)
                    xThumbs.SelectedIndex = 0;
            };
            xThumbs.SizeChanged += (sender, e) =>
            {
                if (CurPage?.Content is CollectionView cview)
                {
                    cview.ViewModel.ContainerDocument.SetActualSize(new Windows.Foundation.Point(xDocContainer.ActualWidth, xDocContainer.ActualHeight));
                }
            };
            Unloaded += (sender, e) =>
            {
                if (ViewModel != null)
                    ViewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;
                if (OldViewModel != null)
                    OldViewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;
                OldViewModel = null;
            };

            AddHandler(KeyDownEvent, new KeyEventHandler(SelectionElement_KeyDown), true);
            xDocContainer.AddHandler(PointerReleasedEvent, new PointerEventHandler(xDocContainer_PointerReleased), true);
            LosingFocus += CollectionPageView_LosingFocus;
        }

        private void EnterPressed(KeyRoutedEventArgs obj)
        {
            if (!MainPage.Instance.IsShiftPressed())
            {
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

                if (obj != null)
                    obj.Handled = true;
            }
        }

        private void DocumentViewModels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            PageDocumentViewModels = new ObservableCollection<DocumentViewModel>(ViewModel.DocumentViewModels.Select((vm) => new DocumentViewModel(vm.DocumentController) { Undecorated = true, IsDimensionless = true }));
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
        

        private void CollectionPageView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args != null)
                args.Handled = true;
            if (ViewModel != null && ViewModel != OldViewModel)
            {
                ViewModel.DocumentViewModels.CollectionChanged += DocumentViewModels_CollectionChanged;
                DocumentViewModels_CollectionChanged(null, null);
                OldViewModel = ViewModel;
            }
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

        public void SetHackCaptionText(FieldControllerBase caption)
        {
            XDocDisplay.Content = caption is DocumentController ?
                new DocumentView() { ViewModel = new DocumentViewModel(caption as DocumentController) { Undecorated= true, IsDimensionless = true} } :
                DataBox.MakeView(new DataBox(caption).Document, null);
        }
        public DocumentViewModel CurPage
        {
            get { return (xThumbs.SelectedIndex < PageDocumentViewModels.Count && xThumbs.SelectedIndex >= 0) ? PageDocumentViewModels[xThumbs.SelectedIndex] : PageDocumentViewModels.FirstOrDefault(); }
            set
            {
                _scope = new OuterReplScope();
                _scope.DeclareVariable("this", value?.DocumentController);
                _dsl = new DSL(_scope);
                
                EnterPressed(null);
            }
        }
        

        public void SetDropIndicationFill(Brush fill)
        {
        }

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

        private void xThumbs_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            var collectionField = ViewModel.ContainerDocument.GetDataDocument().GetField(ViewModel.CollectionKey);
            if (collectionField is ListController<DocumentController> && args.DropResult == DataPackageOperation.Move)
            {
                var docList = ViewModel.DocumentViewModels.Select((dvm) => dvm.DocumentController).ToList();
                if (xThumbs.IsPointerOver())
                {
                    ViewModel.ContainerDocument.GetDataDocument().SetField(ViewModel.CollectionKey, new ListController<DocumentController>(docList), true);
                }
                else if (args.Items.FirstOrDefault() is DocumentViewModel draggedDoc)
                {
                    docList.Remove(draggedDoc.DocumentController);
                    ViewModel.ContainerDocument.GetDataDocument().SetField(ViewModel.CollectionKey, new ListController<DocumentController>(docList), true);
                }
            }
        }
        private void XThumbs_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = ManipulationModes.None;
            foreach (object m in e.Items)
            {
                int ind = ViewModel.DocumentViewModels.IndexOf(m as DocumentViewModel);
                e.Data.SetDragModel(new DragDocumentModel(PageDocumentViewModels[ind].DocumentController));
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
            if (e.DataView.GetDragModel() is DragFieldModel field)
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
            args.Data.SetDragModel(new DragDocumentModel(new CollectionNote(new Point(0, 0), CollectionView.CollectionViewType.Grid, 500, 300, docs).Document));
            // args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
        }
    }
}
