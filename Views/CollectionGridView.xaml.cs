using System;
using System.Collections.Generic;
using System.Diagnostics;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionGridView : UserControl
    {
        public CollectionGridView(CollectionView view)
        {
            this.InitializeComponent();
            xGridView.DragItemsStarting += view.xGridView_OnDragItemsStarting;
            xGridView.DragItemsCompleted += view.xGridView_OnDragItemsCompleted;
            DataContextChanged += OnDataContextChanged;
            Binding selectionBinding = new Binding
            {
                Source = view.ViewModel,
                Path = new PropertyPath(nameof(view.ViewModel.ItemSelectionMode)),
                Mode = BindingMode.OneWay,
            };
            xGridView.SetBinding(ListViewBase.SelectionModeProperty, selectionBinding);
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            CollectionViewModel vm = DataContext as CollectionViewModel;
            xGridView.SelectionChanged += vm.SelectionChanged;
        }

        private void xGridView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void XGridView_OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            args.Handled = true;
            if (args.Phase != 0) throw new Exception("Please start in stage 0");
            var rootGrid = (Grid) args.ItemContainer.ContentTemplateRoot;
            var backdrop = (DocumentView) rootGrid?.FindName("Backdrop");
            var border = (Border) rootGrid?.FindName("xBorder");
            Debug.Assert(backdrop != null, "backdrop != null");
            backdrop.Visibility = Visibility.Visible;
            Debug.Assert(border != null, "border != null");
            border.Visibility = Visibility.Collapsed;
            args.RegisterUpdateCallback(RenderDocumentPhaseOne);
        }

        private void RenderDocumentPhaseOne(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase != 1) throw new Exception("Please start in phase 1");
            var rootGrid = (Grid) args.ItemContainer.ContentTemplateRoot;
            var backdrop = (DocumentView) rootGrid.FindName("Backdrop");
            var border = (Border) rootGrid.FindName("xBorder");
            var canvas = (Canvas) border.FindName("xDocumentCanvas");
            var document = (DocumentView) canvas.FindName("xDocumentDisplay");
            backdrop.Visibility = Visibility.Collapsed;
            border.Visibility = Visibility.Visible;
            document.IsHitTestVisible = false;
            document.DataContext = ((CollectionViewModel) DataContext).DataBindingSource[args.ItemIndex];
        }
    }
}
