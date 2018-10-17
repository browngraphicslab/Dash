using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Annotations;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionPageView : ICollectionView
    {
        public CollectionViewModel ViewModel => DataContext as CollectionViewModel;

        private bool _templateMode;
        private DocumentController _templateDocument;

        public CollectionPageView()
        {
            this.InitializeComponent();
            xTextBox.AddKeyHandler(VirtualKey.Enter, EnterPressed);

            KeyDown += SelectionElement_KeyDown;
        }

        private async void EnterPressed(KeyRoutedEventArgs obj)
        {
            obj.Handled = await UpdateContentFromScript(true);
        }

        private async Task<bool> UpdateContentFromScript(bool catchErrors)
        {
            var text = xTextBox.Text;
            var field = await DSL.InterpretUserInput(text, catchErrors, Scope.CreateStateWithThisDocument(CurrentPage.LayoutDocument));
            if(field == null) { return false; }

            XDocDisplay.DataContext =
                new DocumentViewModel(field is DocumentController doc ? doc : new DataBox(field).Document){IsDimensionless = true};
            return true;
        }

        public DocumentViewModel CurrentPage
        {
            get => (DocumentViewModel)xThumbs.SelectedItem;
            set => xThumbs.SelectedItem = value;
        }

        #region ICollectionView Implementation

        public void SetDropIndicationFill(Brush fill)
        {
        }
        public UserControl UserControl => this;
        public void SetupContextMenu(MenuFlyout contextMenu)
        {
        }

        #endregion

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (xThumbs.Items != null && xThumbs.Items.Count > 0)
            {
                xThumbs.SelectedIndex = Math.Max(xThumbs.SelectedIndex - 1, 0);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (xThumbs.Items != null && xThumbs.Items.Count > 0)
            {
                xThumbs.SelectedIndex = Math.Min(xThumbs.SelectedIndex + 1, xThumbs.Items.Count - 1);
            }
        }

        private async void xThumbs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var page = CurrentPage;
            if (page == null)
            {
                return;
            }

            if (_templateMode)
            {
                _templateDocument.SetField(KeyStore.DocumentContextKey, page.DataDocument, true);
            }
            else
            {
                var element = await UpdateContentFromScript(false);
                if (!element)
                {
                    XDocDisplay.DataContext = page;
                }
            }
        }

        private void SelectionElement_KeyDown(object sender, KeyRoutedEventArgs e)
        {
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
            //TODO This is not correct if there are multiple items, as there can only be one drag model
            foreach (object m in e.Items)
            {
                _dragDoc = (DocumentViewModel) m;
                var dm = new DragDocumentModel(_dragDoc.DocumentController)
                {
                    DraggedDocCollectionViews = new List<CollectionViewModel>(new CollectionViewModel[] {ViewModel})
                };
                e.Data.SetDragModel(dm);
            }
        }

        private void Delete_OnClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveDocument(CurrentPage.DocumentController);
        }

        private TextBox _renameBox;
        private Flyout _flyout;

        private void Rename_OnClicked(object sender, RoutedEventArgs e)
        {
            _flyout = new Flyout();
            _renameBox = new TextBox();
            _renameBox.GotFocus += XRenameBox_OnGotFocus;
            _renameBox.LostFocus += XRenameBox_OnLostFocus;
            _renameBox.KeyDown += XRenameBox_OnKeyDown;
            _flyout.Content = _renameBox;
            _flyout.ShowAt(sender as FrameworkElement);
        }

        private void CommitEdit()
        {
            using (UndoManager.GetBatchHandle())
            {
                if (CurrentPage.DocumentController.GetField<TextController>(KeyStore.TitleKey) != null)
                    CurrentPage.DocumentController.SetTitle(_renameBox.Text);
                else CurrentPage.DataDocument.SetTitle(_renameBox.Text);
            }
            _flyout.Hide();
        }

        private void XRenameBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            _renameBox.Text = CurrentPage.DocumentController.Title ?? CurrentPage.DataDocument.Title;
            _renameBox.SelectAll();
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
            _flyout.Hide();
            // prevents CommitEdit() from being called when esc is pressed
            _renameBox.LostFocus -= XRenameBox_OnLostFocus;

        }

        private async void TemplateButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_templateMode && CurrentPage !=null)
            {
                _templateMode = true;
                templateButton.Content = "Remove Template";
                CreateTemplate();
            }
            else
            {
                _templateMode = false;
                templateButton.Content = "Generate Template";
                await RemoveTemplate();
            }
        }

        private async Task RemoveTemplate()
        {
            if (ViewModel.DocumentViewModels.Count > 0)
            {
                await UpdateContentFromScript(true);
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
                parentCollection.ViewModel.AddDocument(CurrentPage.DocumentController.GetKeyValueAlias(point));
            }
            _templateDocument = ViewModel.ContainerDocument.GetDataDocument().GetDereferencedField<DocumentController>(KeyStore.CollectionItemLayoutPrototypeKey, null);
            if (_templateDocument == null)
            {
                var cnote = new CollectionNote(new Point(), CollectionView.CollectionViewType.Freeform, double.NaN, double.NaN);
                _templateDocument = cnote.Document;
                _templateDocument.SetFitToParent(true);
            }
            XDocDisplay.DataContext = new DocumentViewModel(_templateDocument) { IsDimensionless = true };

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
            if (xTextBox.Visibility == Visibility.Collapsed)
            {
                ScriptToggle.Content = "Hide Script";
                xTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                ScriptToggle.Content = "Show Script";
                xTextBox.Visibility = Visibility.Collapsed;
            }
        }
    }
}
