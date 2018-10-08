using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

    public sealed partial class KVPRow : UserControl
    {
        public EditableScriptViewModel ViewModel => DataContext as EditableScriptViewModel;

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
    }
}
