using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    
    public sealed partial class ApiCreatorPropertyGenerator : UserControl {

        public DashShared.Key parameterCollectionKey;
        public ApiSourceDisplay SourceDisplay;

        public delegate void OnParametersChangedEventHandler(ApiCreatorPropertyGenerator generator, ApiCreatorProperty property);

        public event OnParametersChangedEventHandler OnParametersChanged;

        private DocumentController docModel;
        public DocumentController DocModel {
            get { return this.docModel; }
            set { this.docModel = value; }
        }

        public ApiCreatorPropertyGenerator() {
            DataContext = this;
            InitializeComponent();
            xListView.Visibility = Visibility.Collapsed;
        }

        public ApiCreatorPropertyGenerator(DashShared.Key key) {
            DataContext = this;
            InitializeComponent();
            xListView.Visibility = Visibility.Collapsed;
            docModel = null;
            parameterCollectionKey = key;

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
            var stackPanel = new ApiCreatorProperty(this);

            // make listview visible
            xListView.Items.Add(stackPanel);
            xListView.Visibility = Visibility.Visible;
            xListView.ScrollIntoView(stackPanel);

            // make panel visible
            xCollapseStackPanel.Visibility = Visibility.Visible;
            xCollapseButtonText.Text = "-";

            Debug.Assert(SourceDisplay != null);
            DocumentController c = ApiDocumentModel.addParameter(
                docModel, stackPanel.XPropertyName, stackPanel.XPropertyValue, stackPanel.XToDisplay,
                stackPanel.XRequired, parameterCollectionKey, SourceDisplay);
            stackPanel.docModelRef = c; // update to contain ref to docmodel generated

            OnParametersChanged?.Invoke(this, stackPanel);
        }
    }
}
