using System;
using System.Collections;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

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
        public ApiCreatorProperty()
        {
            this.InitializeComponent();
        }

        // == METHODS ==
        /// <summary>
        /// On click, removes this property from the ListView it is contained in. If
        /// the node is not parented by a ListView (should never happen), this method fails and sends an error.
        /// </summary>
        /// <param name="sender">sending obj (the delete button)</param>
        /// <param name="e">event arg</param>
        private void xDelete_Tapped(object sender, TappedRoutedEventArgs e) {
            var generator = this.GetFirstAncestorOfType<ApiCreatorPropertyGenerator>(); 
            generator?.ApiController.RemoveParameter((ApiParameter)((DictionaryEntry) DataContext).Value);
            generator?.ApiController.RemoveHeader((ApiParameter) ((DictionaryEntry) DataContext).Value);
            generator?.ApiController.RemoveAuthParameter((ApiParameter)((DictionaryEntry)DataContext).Value);
            generator?.ApiController.RemoveAuthHeader((ApiParameter)((DictionaryEntry)DataContext).Value);
            this.ExitEditMode();
        }

        private void XKey_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var k = (KeyController) ((DictionaryEntry) DataContext).Key;
            KeyChanged?.Invoke(k, xKey.Text);
        }

        private void XKey_OnLostFocus(object sender, RoutedEventArgs e)
        {
            xKeyText.Text = xKey.Text;
            if (!xKey.Text.Equals(string.Empty))
            {
                xKey.Visibility = Visibility.Collapsed;
                xKeyText.Visibility = Visibility.Visible;
            }
            else
            {
                xKey.Visibility = Visibility.Visible;
                xKeyText.Visibility = Visibility.Collapsed;
            }
        }

        private void XValue_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var k = (KeyController) ((DictionaryEntry) DataContext).Key;
            ValueChanged?.Invoke(k, xValue.Text);
        }

        private void XKeyText_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            xKeyText.Visibility = Visibility.Collapsed;
            xKey.Visibility = Visibility.Visible;
            xKey.Focus(FocusState.Programmatic);
        }

        public void EnterEditMode()
        {
            xDeleteButtonColumn.Width = new GridLength(30);
            xKey.Visibility = Visibility.Visible;
            xKeyText.Visibility = Visibility.Collapsed;
        }

        public void ExitEditMode()
        {
            xDeleteButtonColumn.Width = new GridLength(0);
            if (!xKey.Text.Equals(string.Empty))
            {
                xKey.Visibility = Visibility.Collapsed;
                xKeyText.Visibility = Visibility.Visible;
            }
            else
            {
                xKey.Visibility = Visibility.Visible;
                xKeyText.Visibility = Visibility.Collapsed;
            }
        }

        public delegate void ValueChangedHandler(KeyController key, string newValue);
        public event ValueChangedHandler KeyChanged;
        public event ValueChangedHandler ValueChanged;
    }
}
