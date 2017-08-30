using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    
    public sealed partial class ApiCreatorPropertyGenerator : UserControl {

        public KeyController parameterCollectionKey;
        public ApiSourceDisplay SourceDisplay;

        public delegate void OnParametersChangedEventHandler(ApiCreatorPropertyGenerator generator, ApiCreatorProperty property);

        public event OnParametersChangedEventHandler OnParametersChanged;

        public DocumentController Document { get; set; }

        public ApiCreatorPropertyGenerator() {
            InitializeComponent();
            xListView.Visibility = Visibility.Collapsed;
        }

        public ApiCreatorPropertyGenerator(KeyController key) {
            InitializeComponent();
            xListView.Visibility = Visibility.Collapsed;
            parameterCollectionKey = key;

        }

        private DocumentController _operatorDocument;
        private ApiOperatorController _operatorController;

        public ApiOperatorController ApiController
        {
            get { return _operatorController; }
            protected set { _operatorController = value; }
        }

        public Dictionary<KeyController, string> Keys = new Dictionary<KeyController, string>();
        public Dictionary<KeyController, string> Values = new Dictionary<KeyController, string>();

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
                xEditButton.Visibility = Visibility.Collapsed;
                addParameterItem.Visibility = Visibility.Collapsed;
                xCollapseButtonText.Text = "5";
            } else {
                xCollapseStackPanel.Visibility = Visibility.Visible;
                if (xListView.GetDescendantsOfType<ApiCreatorProperty>().Count() != 0)
                {
                    xEditButton.Visibility = Visibility.Visible;
                }
                addParameterItem.Visibility = Visibility.Visible;
                xCollapseButtonText.Text = "6";
            }
        }

        /// <summary>
        /// Adds an ApiCreatorProperty to the ListView on button click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addParameterItem_Click(object sender, RoutedEventArgs e) {
            if (TitleTag == "HEADERS")
            {
                _operatorController.AddHeader(new ApiParameter(false, false));
            }
            else if (TitleTag == "PARAMETERS")
            {
                _operatorController.AddParameter(new ApiParameter(true, false));
            } else if (TitleTag == "AUTHENTICATION HEADERS")
            {
                _operatorController.AddAuthHeader(new ApiParameter(true, true));
            } else if (TitleTag == "AUTHENTICATION PARAMETERS") 
            {
                _operatorController.AddAuthParameter(new ApiParameter(false, true));
            }
            xEditButton.Visibility = Visibility.Visible;
            //var stackPanel = new ApiCreatorProperty(this);

            // make listview visible
            //xListView.Items.Add(stackPanel);
            xListView.Visibility = Visibility.Visible;
            //xListView.ScrollIntoView(stackPanel);

            //// make panel visible
            //xCollapseStackPanel.Visibility = Visibility.Visible;
            //xCollapseButtonText.Text = "-";

            //Debug.Assert(SourceDisplay != null);
            //DocumentController c = ApiDocumentModel.addParameter(
            //    Document, stackPanel.XPropertyName, stackPanel.XPropertyValue, stackPanel.XToDisplay,
            //    stackPanel.XRequired, parameterCollectionKey, SourceDisplay);
            //stackPanel.docModelRef = c; // update to contain ref to docmodel generated

            //OnParametersChanged?.Invoke(this, stackPanel);
        }

        private void ApiCreatorPropertyGenerator_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var reference = args.NewValue as FieldReference;
            _operatorDocument = reference.GetDocumentController(null);
            _operatorController = _operatorDocument.GetField(reference.FieldKey) as ApiOperatorController;
            if (TitleTag == "HEADERS")
            {
                xListView.ItemsSource = _operatorController.Headers;
            }
            else if(TitleTag == "PARAMETERS")
            {
                xListView.ItemsSource = _operatorController.Parameters;
            } else if (TitleTag == "AUTHENTICATION HEADERS")
            {
                xListView.ItemsSource = _operatorController.AuthHeaders;
            } else if (TitleTag == "AUTHENTICATION PARAMETERS")
            {
                xListView.ItemsSource = _operatorController.AuthParameters;
            }
            XTitleBlock.Text = TitleTag;
        }

        private void ApiCreatorProperty_OnKeyChanged(KeyController key, string newValue)
        {
            Keys[key] = newValue;
        }

        private void ApiCreatorProperty_OnValueChanged(KeyController key, string newValue)
        {
            Values[key] = newValue;
        }

        private void XEditButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var symbol = (xEditButton.Content as SymbolIcon)?.Symbol;
            var items = xListView.GetDescendantsOfType<ApiCreatorProperty>();
            if (symbol == Symbol.Edit)
            {
                xEditButton.Content = new SymbolIcon(Symbol.Accept);
                if (items != null)
                    foreach (var item in items)
                    {
                         item?.EnterEditMode();
                    }
                xDeleteButtonColumn.Width = new GridLength(30);
            }
            else
            {
                xEditButton.Content = new SymbolIcon(Symbol.Edit);
                if (items != null)
                    foreach (var item in items)
                    {
                        item?.ExitEditMode();
                    }
                xDeleteButtonColumn.Width = new GridLength(0);
            }
        }
    }
}
