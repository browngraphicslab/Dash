using Dash.Sources.Api.XAML_Elements;
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

namespace Dash.Sources.Api {
    public sealed partial class ApiSourceDisplay : UserControl {
        public DocumentController DocModel;

        // == CONSTRUCTORS ==
        public ApiSourceDisplay() {
            this.InitializeComponent();
            //new ManipulationControls(this);
            Debug.WriteLine("hello!");
        }

        public ApiSourceDisplay(DocumentController docModel) {
            this.InitializeComponent();
            DocModel = docModel;
        }

        // == MEMBERS ==
        public ListView PropertiesListView { get { return xListView; } set { xListView = value; } }

        // == METHODS ==

        /// <summary>
        /// Adds an ApiProperty to our ListView.
        /// </summary>
        /// <param name="property">ApiProperty to add</param>
        /// <param name="index">(optional) position to insert into</param>
        public void addToListView(ApiProperty property, int index = -1) {
            Debug.WriteLine("awwww");
            if (index == -1) {
                xListView.Items.Add(property);
            } else {
                xListView.Items.Insert(index, property);
            }
        }

        /// <summary>
        /// Removes an ApiProperty to our ListView.
        /// </summary>
        /// <param name="property">ApiProperty to add</param>
        /// <param name="index">(optional) position to insert into</param>
        public void removeFromListView(int index) {
            xListView.Items.RemoveAt(index);
        }

        /// <summary>
        /// Adds a given event handler to our query button.
        /// </summary>
        /// <param name="r">event handler to add</param>
        public void addButtonEventHandler(TappedEventHandler r) {
            xQueryBtn.Tapped += r;
        }

        private void xEditBtn_Tapped(object sender, TappedRoutedEventArgs e) {
            this.Visibility = Visibility.Collapsed;
        }
    }
}
