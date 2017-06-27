using Dash.Sources.Api.XAML_Elements;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Sources.Api.XAML_Elements {


    public sealed partial class ApiCreatorPropertyGenerator : UserControl {
        public ApiCreatorPropertyGenerator() {
            DataContext = this;
            InitializeComponent();
            xListView.Visibility = Visibility.Collapsed;
        }

        // == DEPENDENCY MEMBERS ==
        public String TitleTag { get; set; }
        public ListView ItemListView{ get { return xListView; } }

        // == METHODS ==

        /// <summary>
        /// Collapses/uncollapses parameters panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xCollapseButton_OnTapped(object sender, TappedRoutedEventArgs e) {
            if (xCollapseStackPanel.Visibility == Visibility.Visible) {
                xCollapseStackPanel.Visibility = Visibility.Collapsed;
                xCollapseButtonText.Text = "+";
            } else {
                xCollapseStackPanel.Visibility = Visibility.Visible;
                xCollapseButtonText.Text = "-";
            }
        }

        /// <summary>
        /// Adds an ApiCreatorProperty to the ListView on button click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addParameterItem_Click(object sender, RoutedEventArgs e) {
            var stackPanel = new ApiCreatorProperty();

            // make listview visible
            xListView.Items.Add(stackPanel);
            xListView.Visibility = Visibility.Visible;
            xListView.ScrollIntoView(stackPanel);

            // make panel visible
            xCollapseStackPanel.Visibility = Visibility.Visible;
            xCollapseButtonText.Text = "-";
        }
    }
}
