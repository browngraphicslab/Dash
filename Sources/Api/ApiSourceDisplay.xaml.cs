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

namespace Dash.Sources.Api {
    public sealed partial class ApiSourceDisplay : UserControl {

        // == CONSTRUCTORS ==
        public ApiSourceDisplay() {
            this.InitializeComponent();
        }

        // == MEMBERS ==
        public ListView PropertiesListView { get { return xListView;  } }

        // == METHODS ==

        /// <summary>
        /// Adds an ApiProperty to our ListView.
        /// </summary>
        /// <param name="property">ApiProperty to add</param>
        /// <param name="index">(optional) position to insert into</param>
        public void addToListView(ApiProperty property, int index = -1) {
            if (index == -1)
                xListView.Items.Add(property);
            else
                xListView.Items.Insert(index, property);
        }

        /// <summary>
        /// Adds a given event handler to our query button.
        /// </summary>
        /// <param name="r">event handler to add</param>
        public void addButtonEventHandler(TappedEventHandler r) {
            xQueryBtn.Tapped += r;
        }
    }
}
