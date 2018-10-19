using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Dash;
using Windows.ApplicationModel.DataTransfer;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{

    public sealed partial class KVPRow : UserControl, INotifyPropertyChanged
    {
        public EditableScriptViewModel ViewModel => DataContext as EditableScriptViewModel;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (value == _isEditing) return;
                _isEditing = value;
                OnPropertyChanged();
                xEditBox.Focus(FocusState.Programmatic);
            }
        }

        private bool _isEditing;

        private Visibility Not(bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }

        public KVPRow()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;
        }

        private EditableScriptViewModel _oldViewModel;
        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (ViewModel == _oldViewModel)
            {
                return;
            }

            _oldViewModel = ViewModel;

            if (ViewModel == null)
            {
                return;
            }

            var document = ViewModel.Document;
            var key = ViewModel.Key;

            TableBox.BindContent(XValuePresenter, document, key, null);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Delete_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.Document.RemoveField(ViewModel.Key);
        }

        private void Edit_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            IsEditing = true;

            if (xEditBox.Height > 50)
            {
                xEditBox.Height = XValuePresenter.ActualHeight;
            }
            else
            {
                xEditBox.Height = 50;
            }
            
        }

        private void XEditBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            xEditBox.Text = DSL.GetScriptForField(ViewModel.Document.GetField(ViewModel.Key));
            xEditBox.SelectAll();
        }

        private void XEditBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            CancelEdit();
        }

        private async void XEditBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                await CommitEdit();
            }
            else if (e.Key == VirtualKey.Escape)
            {
                CancelEdit();
            }
        }

        private async Task CommitEdit()
        {
            using (UndoManager.GetBatchHandle())
            {
                var field = await DSL.InterpretUserInput(xEditBox.Text, true);
                if (field != null)
                {
                    ViewModel.Document.SetField(ViewModel.Key, field, true);
                }

                IsEditing = false;
            }
        }

        private void CancelEdit()
        {
            IsEditing = false;
        }

        private async void UserControl_Drop(object sender, DragEventArgs e)
        {
            var fromFileSystem = e.DataView.Contains(StandardDataFormats.StorageItems);

            var dragModel        = e.DataView.GetDragModel();
            var dragDocModel     = dragModel as DragDocumentModel;
            var internalMove     = !MainPage.Instance.IsShiftPressed() && !MainPage.Instance.IsAltPressed() && !MainPage.Instance.IsCtrlPressed() && !fromFileSystem;
            var isLinking        = e.AllowedOperations.HasFlag(DataPackageOperation.Link) && internalMove && dragDocModel?.DraggingLinkButton == true;
            var isMoving         = e.AllowedOperations.HasFlag(DataPackageOperation.Move) && internalMove && dragDocModel?.DraggingLinkButton != true;
            var isCopying        = e.AllowedOperations.HasFlag(DataPackageOperation.Copy) && (fromFileSystem || MainPage.Instance.IsShiftPressed());
            var isSettingContext = MainPage.Instance.IsAltPressed() && !fromFileSystem;

            e.AcceptedOperation = isSettingContext ? DataPackageOperation.None :
                                  isLinking ? DataPackageOperation.Link :
                                  isMoving ? DataPackageOperation.Move :
                                  isCopying ? DataPackageOperation.Copy :
                                  DataPackageOperation.None;
            
            var docsToAdd = await e.DataView.GetDroppableDocumentsForDataOfType(DataTransferTypeInfo.Any, sender as FrameworkElement);
            var docs = await CollectionViewModel.AddDroppedDocuments(sender, docsToAdd, dragModel, isMoving, null);

            e.DataView.ReportOperationCompleted(e.AcceptedOperation);
            ViewModel.Document.SetField(ViewModel.Key, docs.Count() == 1 ? (FieldControllerBase) docs.First() : new ListController<DocumentController>(docs), true);
            e.Handled = true;
        }
    }
}
