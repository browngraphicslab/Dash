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

    /// <summary>
    /// This class contains the visual display for an ApiSourceCreator connection
    /// property, here representing by a key textbox, a value textbox, and check buttons
    /// for additional display options.
    /// </summary>
    public sealed partial class ApiCreatorProperty : UserControl {

        // == MEMBERS ==
        public String PropertyName { get { return xKey.Text; } }
        public String PropertyValue { get { return xValue.Text; } }
        public bool ToDisplay { get { return (bool)xDisplay.IsChecked; } }
        public bool Required { get { return (bool)xRequired.IsChecked; } }

        // == CONSTRUCTORS == 
        public ApiCreatorProperty() {
            DataContext = this;
            this.InitializeComponent();
        }

        // == METHODS ==
        /// <summary>
        /// On click, removes this property from the ListView it is contained in. If
        /// the node is not parented by a ListView (should never happen), this method 
        /// fails and sends an error.
        /// </summary>
        /// <param name="sender">sending obj (the delete button)</param>
        /// <param name="e">event arg</param>
        private void xDelete_Tapped(object sender, TappedRoutedEventArgs e) {

            // TODO: I could have this throw an error. If you get an error here, you're not
            // using this thing correctly.
            if (this.Parent.GetType() == typeof(ListView)) {
                ListView listView = (ListView)XApiCreatorProperty.Parent;
                listView.Items.RemoveAt(listView.Items.IndexOf(XApiCreatorProperty));
                if (listView.Items.Count == 0)
                    listView.Visibility = Visibility.Collapsed;
                else
                    listView.Visibility = Visibility.Visible;
            } 
        }
    }
}
