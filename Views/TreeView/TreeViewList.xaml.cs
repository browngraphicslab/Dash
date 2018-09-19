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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views.TreeView
{
    public sealed partial class TreeViewList : UserControl
    {
        public CollectionViewModel ViewModel => DataContext as CollectionViewModel;

        public static readonly DependencyProperty FilterFuncProperty = DependencyProperty.Register(
            "FilterFunc", typeof(Func<DocumentController, bool>), typeof(TreeViewList), new PropertyMetadata(default(Func<DocumentController, bool>)));

        public Func<DocumentController, bool> FilterFunc
        {
            get { return (Func<DocumentController, bool>)GetValue(FilterFuncProperty); }
            set { SetValue(FilterFuncProperty, value); }
        }

        public TreeViewList()
        {
            InitializeComponent();
        }

        private CollectionViewModel _oldViewModel;
        private void TreeView_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (ViewModel == _oldViewModel)
            {
                return;
            }

            bool wasLoaded = _oldViewModel?.IsLoaded ?? true;
            if (wasLoaded)
            {
                _oldViewModel?.Loaded(false);
            }

            _oldViewModel = ViewModel;

            if (wasLoaded)
            {
                _oldViewModel?.Loaded(true);
            }
        }


        private void TreeView_OnLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel?.Loaded(true);
        }

        private void TreeView_OnUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel?.Loaded(false);
        }
    }
}
