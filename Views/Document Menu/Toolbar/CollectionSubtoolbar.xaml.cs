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
using Dash.Views.Document_Menu.Toolbar;
using System.Collections.ObjectModel;
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
            FormatDropdownMenu();

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

			xBackgroundColorPicker.SetOpacity(75);
	        xBackgroundColorPicker.ParentFlyout = xColorFlyout;
        }

        /// <summary>
        /// Formats the combo box according to Toolbar Constants.
        /// </summary>
        private void FormatDropdownMenu()
        {
            xViewModesDropdown.Width = ToolbarConstants.ComboBoxWidth;
            xViewModesDropdown.Height = ToolbarConstants.ComboBoxHeight;
            xViewModesDropdown.Margin = new Thickness(ToolbarConstants.ComboBoxMarginOpen);
        }

        /// <summary>
        /// When the Break button is clicked, the selected group should separate.
        /// </summary>
        private void BreakGroup_OnClick(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                //get list of doc views in the collection
                var mainPageCollectionView = MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>();
                var vms = _collection.ViewModel.DocumentViewModels.ToList();

                //add them each to the main canvas
                foreach (DocumentViewModel vm in vms)
                {
                    mainPageCollectionView.ViewModel.AddDocument(vm.DocumentController);
                }

                //delete the sellected collection
                foreach (DocumentView d in SelectionManager.SelectedDocs)
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
        }

        /// <summary>
        /// Toggles the open/closed state of the command bar. 
        /// </summary>
        public void CommandBarOpen(bool status)
        {
            xCollectionCommandbar.Visibility = Visibility.Visible;
            xViewModesDropdown.Margin = status ? new Thickness(ToolbarConstants.ComboBoxMarginOpen) : new Thickness(ToolbarConstants.ComboBoxMarginClosed);
        }

        public void SetCollectionBinding(CollectionView thisCollection, DocumentController docController)
        {
            _collection = thisCollection;
            xViewModesDropdown.SelectedIndex = Array.IndexOf(Enum.GetValues(typeof(CollectionView.CollectionViewType)), _collection.ViewModel.ViewType);
	        _docController = docController;
        }

	    private void XBackgroundColorPicker_OnSelectedColorChanged(object sender, Color e)
	    {
		    //_docController?.GetDataDocument().SetField(KeyStore.BackgroundColorKey, new TextController(e.ToString()), false);
		    _collection?.GetFirstAncestorOfType<DocumentView>().SetBackgroundColor(e);
		    //if (_collection?.CurrentView is CollectionFreeformView) (_collection.CurrentView as CollectionFreeformView).xOuterGrid.Background = new SolidColorBrush(e);
		    //_docController?.SetField(KeyStore.BackgroundColorKey, new TextController(e.ToString()), false);
	    }
    }
}
