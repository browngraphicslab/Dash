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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash {

    public sealed partial class CollectionSubtoolbar : UserControl, ICommandBarBased
    {
        public CollectionSubtoolbar()
        {
            this.InitializeComponent();
            xCollectionCommandbar.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Collapsed;
        }

        private void BreakGroup_OnClick(object sender, RoutedEventArgs e)
        {
            //TODO: Dismantle current selection (which must be a collection if the collection bar is showing)
            Debug.WriteLine("COLLECTION DISMANTLED/BROKEN!");
        }

        private void ViewModesDropdown_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateView();
        }

        private void UpdateView()
        {
            switch (xViewModesDropdown.SelectedIndex)
            {
                case 0:
                    Debug.WriteLine("Freeform View selected");
                    break;
                case 1:
                    Debug.WriteLine("Grid View selected");
                    break;
                case 2:
                    Debug.WriteLine("Page View selected");
                    break;
                case 3:
                    Debug.WriteLine("Database View selected");
                    break;
                case 4:
                    Debug.WriteLine("Schema View selected");
                    break;
                case 5:
                    Debug.WriteLine("Tree View selected");
                    break;
                case 6:
                    Debug.WriteLine("Timeline View selected");
                    break;
            }
        }

        public CommandBar GetCommandBar()
        {
            return xCollectionCommandbar;
        }
    }
}
