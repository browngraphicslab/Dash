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

        public CollectionSubtoolbar()
        {
            this.InitializeComponent();
            FormatDropdownMenu();

            xCollectionCommandbar.Loaded += delegate
            {
                var sp = xCollectionCommandbar.GetFirstDescendantOfType<StackPanel>();
                sp.SetBinding(StackPanel.OrientationProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath(nameof(Orientation)),
                    Mode = BindingMode.OneWay
                });
                Visibility = Visibility.Collapsed;
            };
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
            //TODO: Dismantle current selection (which must be a collection if the collection bar is showing)
            Debug.WriteLine("COLLECTION DISMANTLED/BROKEN!");
            xCollectionCommandbar.IsOpen = true;
            xCollectionCommandbar.IsEnabled = true;

            //get list of doc views in the collection
            var mainPageCollectionView =
                           MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>();
            ObservableCollection<DocumentViewModel> vms = _collection.ViewModel.DocumentViewModels;

            //add them each to the main canvas
            foreach (DocumentViewModel vm in vms)
            {
                mainPageCollectionView.ViewModel.AddDocument(vm.DocumentController);
            }

            //delete the sellected collection
            var tempDocs = MainPage.Instance.GetSelectedDocuments().ToList<DocumentView>();
            foreach (DocumentView d in tempDocs)
            {
                d.DeleteDocument();
            }
        }

        /// <summary>
        /// Binds the drop down selection of view otions with the view of the collection.
        /// </summary>
        private void ViewModesDropdown_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateView();
        }

        /// <summary>
        /// Updates the view of the selected collection. 
        /// </summary>
        private void UpdateView()
        {
            if (_collection != null)
            {
                switch (xViewModesDropdown.SelectedIndex)
                {
                    case 0:
                        _collection.SetView(CollectionView.CollectionViewType.Freeform);
                        break;

                    case 1:
                        _collection.SetView(CollectionView.CollectionViewType.Grid);
                        break;

                    case 2:
                        _collection.SetView(CollectionView.CollectionViewType.Page);
                        break;

                    case 3:
                        _collection.SetView(CollectionView.CollectionViewType.DB);
                        break;

                    case 4:
                        _collection.SetView(CollectionView.CollectionViewType.Freeform);
                        break;

                    case 5:
                        _collection.SetView(CollectionView.CollectionViewType.TreeView);
                        break;

                    case 6:
                        _collection.SetView(CollectionView.CollectionViewType.Timeline);
                        break;
                }

            }
        }

        /// <summary>
        /// Toggles the open/closed state of the command bar. 
        /// </summary>
        public void CommandBarOpen(bool status)
        {
            xCollectionCommandbar.IsOpen = status;
            xCollectionCommandbar.IsEnabled = true;
            xCollectionCommandbar.Visibility = Visibility.Visible;
            xViewModesDropdown.Margin = status ? new Thickness(ToolbarConstants.ComboBoxMarginOpen) : new Thickness(ToolbarConstants.ComboBoxMarginClosed);
        }

        public void SetCollectionBinding(CollectionView thisCollection)
        {
            _collection = thisCollection;
        }
    }
}
