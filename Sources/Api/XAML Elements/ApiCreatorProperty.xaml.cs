using System;
using System.Collections;
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
        }

        private void XKey_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var k = (KeyController) ((DictionaryEntry) DataContext).Key;
            KeyChanged?.Invoke(k, xKey.Text);
        }

        private void XValue_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var k = (KeyController) ((DictionaryEntry) DataContext).Key;
            ValueChanged?.Invoke(k, xValue.Text);
        }

        public delegate void ValueChangedHandler(KeyController key, string newValue);
        public event ValueChangedHandler KeyChanged;
        public event ValueChangedHandler ValueChanged;
    }
}
