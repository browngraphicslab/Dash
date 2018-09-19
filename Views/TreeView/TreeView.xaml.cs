using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public sealed partial class TreeView : UserControl
    {
        public CollectionViewModel ViewModel => DataContext as CollectionViewModel;

        public static readonly DependencyProperty FilterFuncProperty = DependencyProperty.Register(
            "FilterFunc", typeof(Func<DocumentController, bool>), typeof(TreeView), new PropertyMetadata(default(Func<DocumentController, bool>)));

        public Func<DocumentController, bool> FilterFunc
        {
            get => (Func<DocumentController, bool>)GetValue(FilterFuncProperty);
            set => SetValue(FilterFuncProperty, value);
        }

        public TreeView()
        {
            InitializeComponent();
        }

    }
}
