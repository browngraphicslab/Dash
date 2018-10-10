using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionPageView : ICollectionView
    {
        private bool templateMode = false;
        public UserControl UserControl => this;
        public CollectionViewModel ViewModel { get => DataContext as CollectionViewModel; }
        public CollectionViewModel OldViewModel = null;
        private DSL _dsl;
        private OuterReplScope _scope;
        private DocumentController _newDoc;

        public CollectionPageView()
        {
            this.InitializeComponent();
            xTextBox.AddKeyHandler(VirtualKey.Enter, EnterPressed);
            xThumbs.Loaded += (sender, e) =>
            {
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
                //if (ViewModel != null)
                //{
                //    ViewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;
                //}

                //if (OldViewModel != null)
                //    OldViewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;
                OldViewModel = null;
            };

            AddHandler(KeyDownEvent, new KeyEventHandler(SelectionElement_KeyDown), true);
            xDocContainer.AddHandler(PointerReleasedEvent, new PointerEventHandler(xDocContainer_PointerReleased), true);
            LosingFocus += CollectionPageView_LosingFocus;
            //Debug.WriteLine(ViewModel.DocumentViewModels.Select((dvm) => dvm.DocumentController).ToList().Count);
        }
        public void SetupContextMenu(MenuFlyout contextMenu)
        {

        }

        private async void EnterPressed(KeyRoutedEventArgs obj)
        {
            if (!MainPage.Instance.IsShiftPressed())
            {
                var keyString = xTextBox.Text;
                if (templateMode)
                {
                    if (CurPage != null)
                    {
                        _newDoc.SetField(KeyStore.DocumentContextKey, CurPage.DataDocument, true);
                    }
                }
                else
                {
                    if (keyString?.StartsWith("=") ?? false)
                    {
                        try
                        {
                            var result = await _dsl.Run(keyString.Substring(1));
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
                Debug.WriteLine("Data Context changing...");
                CurPage = xThumbs.SelectedItem as DocumentViewModel;
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

            get
            {
                return (xThumbs.SelectedIndex < ViewModel.DocumentViewModels.Count && xThumbs.SelectedIndex >= 0)
                    ? ViewModel.DocumentViewModels[xThumbs.SelectedIndex]
                    : ViewModel.DocumentViewModels.FirstOrDefault();
            }
            //get
            //{
            //    return xThumbs.SelectedItem as DocumentViewModel;
            //}
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
                var ind = ViewModel.DocumentViewModels.IndexOf(CurPage);
                xThumbs.SelectedIndex = Math.Max(0, ind - 1);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurPage != null)
            {
                var ind = ViewModel.DocumentViewModels.IndexOf(CurPage);
                xThumbs.SelectedIndex = Math.Min(ViewModel.DocumentViewModels.Count - 1, ind + 1);
            }
        }

        private void xThumbs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int ind = xThumbs.SelectedIndex;
            Debug.WriteLine("selected index:" + ind);
            if (ViewModel != null && ViewModel.DocumentViewModels.Count > 0)
            {
                CurPage = xThumbs.SelectedItem as DocumentViewModel;
                _scope = new OuterReplScope();
                _scope.DeclareVariable("this", CurPage.DocumentController);
                _dsl = new DSL(_scope);
            }

            if (xThumbs.ItemsPanelRoot != null && ind >= 0 && ind < xThumbs.ItemsPanelRoot.Children.Count)
            {
                var x = xThumbs.ItemsPanelRoot.Children[ind].GetFirstDescendantOfType<Control>();
                if (x != null)
                {
                    try
                    {
                        x.Focus(FocusState.Keyboard);
                        x.Focus(FocusState.Pointer);
                    }
                    catch (Exception)
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
            }
            catch (Exception)
            {

            }
        }

        private void xThumbs_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
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
            if (xThumbs.IsPointerOver() && args.DropResult == DataPackageOperation.Move)
            {
                var ind = ViewModel.DocumentViewModels.IndexOf(_dragDoc);
                ViewModel.RemoveDocument(_dragDoc.DocumentController);
                ViewModel.InsertDocument(_dragDoc.DocumentController, ind);
            }
        }
        private DocumentViewModel _dragDoc;
        private void XThumbs_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = ManipulationModes.None;
            foreach (object m in e.Items)
            {
                var startInd = ViewModel.DocumentViewModels.IndexOf(m as DocumentViewModel);
                _dragDoc = m as DocumentViewModel;
                var dm = new DragDocumentModel(_dragDoc.DocumentController);
                dm.DraggedDocCollectionViews = new List<CollectionViewModel>(new CollectionViewModel[] { ViewModel });
                e.Data.SetDragModel(dm);
            }
        }

        private void xThumbs_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = e.GetCurrentPoint(this).Properties.IsRightButtonPressed ? ManipulationModes.All : ManipulationModes.None;
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                _originalSender = (FrameworkElement)sender;
                var mF = new MenuFlyout();
                MenuFlyoutItem rename = new MenuFlyoutItem();
                MenuFlyoutItem delete = new MenuFlyoutItem();
                rename.Text = "Rename";
                delete.Text = "Delete";
                mF.Items.Add(rename);
                mF.Items.Add(delete);
                mF.ShowAt(_originalSender);
                rename.Click += rename_OnClicked;
                delete.Click += delete_OnClicked;
            }
        }

        private void delete_OnClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveDocument(CurPage.DocumentController);
        }

        private FrameworkElement _originalSender;
        private TextBox renameBox;
        private Flyout flyout;

        private void rename_OnClicked(object sender, RoutedEventArgs e)
        {
            flyout = new Flyout();
            renameBox = new TextBox();
            renameBox.GotFocus += XRenameBox_OnGotFocus;
            renameBox.LostFocus += XRenameBox_OnLostFocus;
            renameBox.KeyDown += XRenameBox_OnKeyDown;
            flyout.Content = renameBox;
            flyout.ShowAt(_originalSender);
        }

        private void CommitEdit()
        {
            using (UndoManager.GetBatchHandle())
            {
                if (CurPage.DocumentController.GetField<TextController>(KeyStore.TitleKey) != null)
                    CurPage.DocumentController.SetTitle(renameBox.Text);
                else CurPage.DataDocument.SetTitle(renameBox.Text);
            }
            flyout.Hide();
        }

        private void XRenameBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            renameBox.Text = CurPage.DocumentController.Title ?? CurPage.DataDocument.Title;
            renameBox.SelectAll();
        }

        private void XRenameBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            CommitEdit();
        }

        private void XRenameBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                CommitEdit();
            }
            else if (e.Key == VirtualKey.Escape)
            {
                CancelEdit();
            }
        }

        private void CancelEdit()
        {
            flyout.Hide();
            // prevents CommitEdit() from being called when esc is pressed
            renameBox.LostFocus -= XRenameBox_OnLostFocus;

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

        private async void ApplyScript_OnDragStarting(UIElement sender, DragStartingEventArgs args)
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
                        var result = await _dsl.Run(keyString.Substring(1));
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

        private void TemplateButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!templateMode && CurPage!=null)
            {
                templateMode = true;
                templateButton.Content = "Remove Template";
                CreateTemplate();
            }
            else
            {
                templateMode = false;
                templateButton.Content = "Generate Template";
                RemoveTemplate();
            }
        }

        private void RemoveTemplate()
        {
            var ind = xThumbs.SelectedIndex;
            if (ViewModel.DocumentViewModels.Count > 0)
            {
                //CurPage = ViewModel.DocumentViewModels[ind];
                CurPage = xThumbs.SelectedItem as DocumentViewModel;
            }
        }

        private void CreateTemplate()
        {
            var docView = this.GetFirstAncestorOfType<DocumentView>();
            var parentCollection = docView.ParentCollection;
            if (parentCollection != null)
            {
                var viewModel = docView.ViewModel;
                var where = viewModel.Position.X + viewModel.Width + 70;
                var point = new Point(where, viewModel.Position.Y);
                parentCollection.ViewModel.AddDocument(CurPage.DocumentController.GetKeyValueAlias(point));
            }
            _newDoc = ViewModel.ContainerDocument.GetDataDocument().GetDereferencedField<DocumentController>(KeyStore.CollectionItemLayoutPrototypeKey, null);
            if (_newDoc == null)
            {
                var cnote = new CollectionNote(new Point(), CollectionView.CollectionViewType.Freeform, double.NaN, double.NaN);
                cnote.Document.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                cnote.Document.SetVerticalAlignment(VerticalAlignment.Stretch);
                _newDoc = cnote.Document;
                _newDoc.SetFitToParent(true);
            }
            XDocDisplay.Content = new DocumentView() {DataContext = new DocumentViewModel(_newDoc) { IsDimensionless = true } };

        }

        private void FrameworkElement_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue is DocumentViewModel dvm)
            {
                var binding = new FieldBinding<TextController>
                {
                    Mode = BindingMode.OneWay,
                    Document = dvm.DocumentController,
                    Key = KeyStore.TitleKey,
                };
                sender.AddFieldBinding(TextBlock.TextProperty, binding);
            }
        }

        private void ScriptToggle_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
            }
        }
    }
}
