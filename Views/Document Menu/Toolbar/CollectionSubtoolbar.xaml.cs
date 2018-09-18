using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{

    /// <summary>
    /// The subtoolbar that is activated when a collection is selected on click. Implements ICommandBarBased because it uses a CommandBar as the UI.
    /// </summary>
    public sealed partial class CollectionSubtoolbar : UserControl, ICommandBarBased
    {
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            "Orientation", typeof(Orientation), typeof(CollectionSubtoolbar), new PropertyMetadata(default(Orientation)));
		
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /*
         * Determines whether or not to hide or display the combo box: in context, this applies only to toggling rotation which is not currently supported
         */
        public void SetComboBoxVisibility(Visibility visibility) => xViewModesDropdown.Visibility = visibility;

        private CollectionView _collection;
	    private DocumentController _docController;

        public CollectionSubtoolbar()
        {
            this.InitializeComponent();
            //FormatDropdownMenu();

            xCollectionCommandbar.Loaded += delegate
            {
                var sp = xCollectionCommandbar;
                sp?.SetBinding(StackPanel.OrientationProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath(nameof(Orientation)),
                    Mode = BindingMode.OneWay
                });

                Visibility = Visibility.Collapsed;
                xViewModesDropdown.ItemsSource = Enum.GetValues(typeof(CollectionView.CollectionViewType));
            };

			xBackgroundColorPicker.SetOpacity(200);
	        xBackgroundColorPicker.ParentFlyout = xColorFlyout;

            SetUpToolTips();
        }

        /// <summary>
        /// Formats the combo box according to Toolbar Constants.
        /// </summary>
        //private void FormatDropdownMenu()
        //{
        //    xViewModesDropdown.Width = ToolbarConstants.ComboBoxWidth;
        //    xViewModesDropdown.Height = ToolbarConstants.ComboBoxHeight;
        //}

        /// <summary>
        /// When the Break button is clicked, the selected group should separate.
        /// </summary>
        private void BreakGroup_OnClick(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                //get list of doc views in the collection
                var mainPageCollectionView = _collection.GetFirstAncestorOfType<CollectionView>();
                if (mainPageCollectionView == null)
                {
                    return;
                }
                var vms = _collection.ViewModel.DocumentViewModels.ToList();

	            var offsetX = _collection.GetFirstAncestorOfType<DocumentView>()?.ViewModel?.XPos ?? 0;
	            var offsetY = _collection.GetFirstAncestorOfType<DocumentView>()?.ViewModel?.YPos ?? 0;

				DocumentViewModel mostTopLeft = vms.First();

	            foreach (DocumentViewModel vm in vms)
	            {
		            if (vm.XPos < mostTopLeft.XPos && vm.YPos < mostTopLeft.YPos)
			            mostTopLeft = vm;
	            }

				//add them each to the main canvas
				foreach (DocumentViewModel vm in vms)
                {
		            vm.XPos = offsetX + (vm.XPos - mostTopLeft.XPos) * vm.Scale.X;
		            vm.YPos = offsetY + (vm.YPos - mostTopLeft.YPos) * vm.Scale.Y;
					
                    mainPageCollectionView.ViewModel.AddDocument(vm.DocumentController);
                }

                //delete the sellected collection
                foreach (DocumentView d in SelectionManager.GetSelectedDocs())
                {
                    d.DeleteDocument();
                }
            }
        }

        /// <summary>
        /// Binds the drop down selection of view options with the view of the collection.
        /// </summary>
        private void ViewModesDropdown_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (xViewModesDropdown.SelectedItem != null &&  _collection != null)
            {
                using (UndoManager.GetBatchHandle())
                {
                    _collection.SetView((CollectionView.CollectionViewType) xViewModesDropdown.SelectedItem);
                }
            }

            CommandBarOpen(true);
        }

        /// <summary>
        /// Toggles the open/closed state of the command bar. 
        /// </summary>
        public void CommandBarOpen(bool status)
        {
            xCollectionCommandbar.Visibility = Visibility.Visible;
            //xViewModesDropdown.Margin = status ? new Thickness(ToolbarConstants.ComboBoxMarginOpen) : new Thickness(ToolbarConstants.ComboBoxMarginClosed);
        }

        public void SetCollectionBinding(CollectionView thisCollection, DocumentController docController)
        {
            _collection = thisCollection;
            xViewModesDropdown.SelectedIndex = Array.IndexOf(Enum.GetValues(typeof(CollectionView.CollectionViewType)), _collection.ViewModel.ViewType);
	        _docController = docController;
        }

	    private void XBackgroundColorPicker_OnSelectedColorChanged(object sender, Color e)
	    {
	            _collection?.GetFirstAncestorOfType<DocumentView>().ViewModel?.LayoutDocument?.SetBackgroundColor(e);
	    }

        private ToolTip _break;
        private ToolTip _color;

        private void SetUpToolTips()
        {
            var placementMode = PlacementMode.Bottom;
            const int offset = 5;

            _break = new ToolTip()
            {
                Content = "Break Collection",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xBreakGroup, _break);

            _color = new ToolTip()
            {
                Content = "Background Color",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xBackgroundColor, _color);
        }

        private void ShowAppBarToolTip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is AppBarButton button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = true;
            else if (sender is AppBarToggleButton toggleButton && ToolTipService.GetToolTip(toggleButton) is ToolTip toggleTip) toggleTip.IsOpen = true;
        }

        private void HideAppBarToolTip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is AppBarButton button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = false;
            else if (sender is AppBarToggleButton toggleButton && ToolTipService.GetToolTip(toggleButton) is ToolTip toggleTip) toggleTip.IsOpen = false;
        }

    }
}
