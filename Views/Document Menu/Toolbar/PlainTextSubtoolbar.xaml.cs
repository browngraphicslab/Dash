using System;
using System.Collections.Generic;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Dash.Converters;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /// <summary>
    /// The subtoolbar that allows users to edit and style their text. Visible only when a richeditbox is selected.
    /// </summary>
    public sealed partial class PlainTextSubtoolbar : UserControl
    {
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(RichTextSubtoolbar), new PropertyMetadata(default(Orientation)));

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        private TextBox _currBox;
        private FormattingMenuView _menuView = null;
        private DocumentView _docs;
        private Dictionary<string, Button> _buttons;
        private DocumentController _currentDocController;
        private Windows.UI.Color _currentColor;

        public PlainTextSubtoolbar()
        {
            this.InitializeComponent();
            _buttons = new Dictionary<string, Button>();

            _currBox = null;
			Visibility = Visibility.Collapsed;
		}

        /**
		 * Binds the text toolbar with the most recently selected text box for editing purposes.
		 */
        public void SetMenuToolBarBinding(TextBox selection)
        {
        }

        /**
		 *  Sets the current text box used for editing
		 */
        public void SetCurrTextBox(TextBox box)
        {
            _currBox = box;
        }

        /**
		 * Setter for the documnentview of the richedittextbox, used for accessing text edit methods
		 */
        public void SetDocs(DocumentView docs)
        {
            _docs = docs;
            _currentDocController = docs.ViewModel.DocumentController;

            var selectedWeight = xNormalWeight;
            var weight = _currentDocController.GetField<NumberController>(KeyStore.FontWeightKey)?.Data ?? 400;
            switch (weight)
            {
                case 300:
                    selectedWeight = xLightWeight;
                    break;
                case 700:
                    selectedWeight = xBoldWeight;
                    break;
                case 900:
                    selectedWeight = xBlackWeight;
                    break;
            }
            xFontWeightOptionsDropdown.SelectedItem = selectedWeight;
            
            var fontSize = _currentDocController.GetField<NumberController>(KeyStore.FontSizeKey)?.Data ?? (double)App.Instance.Resources["DefaultFontSize"];
            if (fontSize >= 6 && fontSize <= 200)
            {
                xFontSizeSlider.Value = fontSize;
            }
        }


        /**
		 * Used to toggle between text sub-menus
		 */
        public void CloseSubMenu()
        {
            xStack.Children.Remove(_menuView);
            _menuView = null;
            //restore other menu
        }

        private void XBackgroundColorPicker_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _currentColor = xBackgroundColorPicker.SelectedColor;
            UpdateColor();
        }

        /*
         * Ensures current color reflects desired opacity and then updates the appropriate bindings for...
         */
        private void UpdateColor()
        {
            _currentColor = GetColorWithUpdatedOpacity();
            //TODO we don't actually need to store the opacity slider value as it is stored in the color as well
            //...shape's background color
            _currentDocController?.SetBackgroundColor(_currentColor);
        }

        private Windows.UI.Color GetColorWithUpdatedOpacity()
        {
            if (_currentColor == null)
                return Windows.UI.Color.FromArgb(0x80, 0x00, 0x00, 0x00); //A fallback during startup (edge case) where current color string is null
            var alpha = (byte)(xOpacitySlider.Value / xOpacitySlider.Maximum * 255); //Ratio of current value to maximum determines the relative desired opacity
            return Windows.UI.Color.FromArgb(alpha, _currentColor.R, _currentColor.G, _currentColor.B);
        }

        private void XOpacitySlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e) => UpdateColor();

        private void XOpacitySlider_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            xOpacitySlider.Value = 128;
            UpdateColor();
        }

        private void XFontWeightOptionsDropdown_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currBox == null) return;
            var fontWeight = FontWeights.Normal;
            switch (((sender as ComboBox).SelectedItem as ComboBoxItem).Content.ToString())
            {
                case "Light":
                    fontWeight = FontWeights.Light;
                    break;
                case "Bold":
                    fontWeight = FontWeights.Bold;
                    break;
                case "Black":
                    fontWeight = FontWeights.Black;
                    break;
            }
            _currBox.FontWeight = fontWeight;
            _currentDocController.SetField(KeyStore.FontWeightKey, new NumberController(fontWeight.Weight), true);
        }

        private void xFontSizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_currBox == null) return;
            _currBox.FontSize = (sender as Slider).Value;
            _currentDocController.SetField(KeyStore.FontSizeKey, new NumberController((sender as Slider).Value), true);
        }
    }
}
