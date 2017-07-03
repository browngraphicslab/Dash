using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash {

    /// <summary>
    /// This class contains the visual display for an ApiSourceCreator connection
    /// property, here representing by a key textbox, a value textbox, and check buttons
    /// for additional display options.
    /// </summary>
    public sealed partial class ApiCreatorProperty : UserControl {
        ApiCreatorPropertyGenerator parent;

        // == MEMBERS ==
        public String PropertyName { get { return xKey.Text; } }
        public String PropertyValue { get { return xValue.Text; } }
        public bool ToDisplay { get { return (bool)xDisplay.IsChecked; } }
        public bool Required { get { return (bool)xRequired.IsChecked; } }


        public TextBox XPropertyName { get { return xKey; } }
        public TextBox XPropertyValue { get { return xValue; } }
        public CheckBox XToDisplay { get { return xDisplay; } }
        public CheckBox XRequired { get { return xRequired; } }

        public DocumentController docModelRef;

        // == CONSTRUCTORS == 
        public ApiCreatorProperty(ApiCreatorPropertyGenerator parent) {
            DataContext = this;
            this.InitializeComponent();
            this.parent = parent;
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
            if (this.Parent.GetType() == typeof(ListView)) {

                // fetch containing list view
                ListView listView = (ListView)XApiCreatorProperty.Parent;
                int index = listView.Items.IndexOf(XApiCreatorProperty);
                listView.Items.RemoveAt(index);

                // update rendered source result to reflect the deleted field
                parent.SourceDisplay.removeFromListView(index);

                // propagate changes to the document model
                CourtesyDocuments.ApiDocumentModel.removeParameter(parent.DocModel,docModelRef,parent.parameterCollectionKey,parent.SourceDisplay);

                if (listView.Items.Count == 0)
                    listView.Visibility = Visibility.Collapsed;
                else
                    listView.Visibility = Visibility.Visible;
            } 
        }

        private void xDisplay_Checked(object sender, RoutedEventArgs e) {
           
        }

        private void xDisplay_Unchecked(object sender, RoutedEventArgs e) {

        }
    }
}
