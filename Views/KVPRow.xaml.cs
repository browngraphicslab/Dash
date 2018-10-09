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
    }
}
