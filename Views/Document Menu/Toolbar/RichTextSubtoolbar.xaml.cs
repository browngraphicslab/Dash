using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarButtons;
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
using Dash.Views.Document_Menu.Toolbar;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarFormats;
using Windows.UI.Xaml.Documents;
using Windows.UI;
using Windows.UI.Text;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /// <summary>
    /// The subtoolbar that allows users to edit and style their text. Visible only when a richeditbox is selected.
    /// </summary>
    public sealed partial class RichTextSubtoolbar : UserControl
    {
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation",
            typeof(Orientation), typeof(RichTextSubtoolbar), new PropertyMetadata(default(Orientation)));

        public Orientation Orientation
        {
            get { return (Orientation) GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        private RichEditBox _currBox;
        private DocumentView _docs;
        private Dictionary<string, Button> _buttons;
        private DocumentController _currentDocController;
        private Windows.UI.Color _currentColor;

        public RichTextSubtoolbar()
        {
            this.InitializeComponent();
            _buttons = new Dictionary<string, Button>();

            _currBox = null;
            //xBackgroundColorPicker.ParentFlyout = xColorPickerFlyout;

            //add an additional sub-toolbar for further operations
            /*
            var addButton = this.AddButton("Font", Symbol.Add, 0, (sender, args) =>
            {
                /**
                 * When the Font Button is clicked, the font menu visibility is toggled, giving user access to additional editing operations like font style, etc.
                 
                if (_currBox != null && xMenuView == null)
                {
                    //create a formatting menu and bind it to the currently selected richEditBox's view
                    xMenuView = new FormattingMenuView(this)
                    {
                        richTextView = _docs.GetFirstDescendantOfType<RichTextView>(),
                        xRichEditBox = _currBox
                    };
                    //add the menu to the stack panel
                    xStack.Children.Add(xMenuView);
                    //collapse other text menu
                    xDashTextSubtoolbar.Visibility = Visibility.Collapsed;
	                xFontColor.Visibility = Visibility.Collapsed;
	                xOpacitySlider.Visibility = Visibility.Collapsed;
                    _buttons.TryGetValue("Font", out var fontButton);
	                //xBackgroundColorButton.Visibility = Visibility.Collapsed;
                    
                    if (fontButton != null)
                    {
                        //Width meant to be 67 to match actual rendered width of main toolbar collapse button
                        fontButton.Width = 67;
                    }
					
                }
                //Width meant to be 67 to match actual rendered width of main toolbar collapse button
            }, 67, 100);
		*/
            //binds orientation of toolbar to the orientation of the main toolbar
            xDashTextSubtoolbar.Loaded += delegate
            {
                var sp = xDashTextSubtoolbar.GetFirstDescendantOfType<StackPanel>();
                sp.SetBinding(StackPanel.OrientationProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath(nameof(Orientation)),
                    Mode = BindingMode.OneWay
                });
                Visibility = Visibility.Collapsed;


                xHighlightColorPicker.ParentFlyout = xHighlightColorFlyout;
                xForegroundColorPicker.ParentFlyout = xForegroundColorFlyout;
                foreach (var toolbarButton in xDashTextSubtoolbar.GetDescendantsOfType<ToolbarButton>())
                {
                    toolbarButton.Style = xToolbarButtonStyler;
                }
            };
        }

        /**
		 * Binds the text toolbar with the most recently selected text box for editing purposes.
		 */
        public void SetMenuToolBarBinding(RichEditBox selection)
        {
            xDashTextSubtoolbar.Editor = selection;
            xDashTextSubtoolbar.Visibility = Visibility.Visible;
            xDashTextSubtoolbar.GetFirstDescendantOfType<StackPanel>().Orientation = Orientation;
        }

        /**
		 * Helper method for adding custom buttons.
		 */
        public Button AddButton(string name, Symbol icon, int position, TappedEventHandler onTapped, int width = 40,
            int height = 40, bool includeSeparator = false)
        {
            //instantiate ToolbarButton & set properties
            var button = new ToolbarButton
            {
                Name = name,
                Icon = new SymbolIcon(icon),
                Position = position,
                Foreground = new SolidColorBrush(Colors.White),
                Width = width,
                Height = height,
            }; //add to toolbar
            button.Height = height;
            xDashTextSubtoolbar.CustomButtons.Add(button);
            //assign event handler to button on tapped
            button.Tapped += onTapped;
            ////add small separation between other buttons
            //if (includeSeparator) xDashTextSubtoolbar.CustomButtons.Add(new ToolbarSeparator {Position = position + 1});
            //add button to dictionary for accessibility
            _buttons.Add(name, button);
            return button;
        }

        /**
		 *  Sets the current text box used for editing
		 */
        public void SetCurrTextBox(RichEditBox box)
        {
            _currBox = box;
            //if (xMenuView != null) xMenuView.xRichEditBox = _currBox;
        }

        /**
		 * Setter for the documnentview of the richedittextbox, used for accessing text edit methods
		 */
        public void SetDocs(DocumentView docs)
        {
            _docs = docs;
            _currentDocController = docs.ViewModel.DocumentController;
            xMenuView?.SetRichTextBinding(_docs.GetFirstDescendantOfType<RichTextView>());
            Color ccol = _currentDocController.GetBackgroundColor() ?? Colors.Transparent;
            //xOpacitySlider.Value = ccol.A / 255.0 * xOpacitySlider.Maximum;
            xBackgroundColorPicker.SelectedColor = ccol;
        }


        /**
		 * Used to toggle between text sub-menus
		 */
        public void CloseSubMenu()
        {
            //restore other menu
            //xDashTextSubtoolbar.Visibility = Visibility.Visible;
            //xBackgroundColorButton.Visibility = Visibility.Visible;

            xMoreButton.Icon = new SymbolIcon(Symbol.Add);
            xInitialGrid.Visibility = Visibility.Visible;
            xMenuView.Visibility = Visibility.Collapsed;
        }

        private void XBackgroundColorPicker_OnSelectedColorChanged(object sender, Color e)
        {
            _docs?.SetBackgroundColor(e);
        }

        #region Old Opacity/Color Code No Longer In Use

        /*
		/*
         * Runs the current ARGB color through the "filter" of the current opacity slider value by replacing default alpha prefix with the desired substitution
         
		private Windows.UI.Color GetColorWithUpdatedOpacity()
        {
            if (_currentColor == null)
                return Windows.UI.Color.FromArgb(0x80, 0x00, 0x00, 0x00); //A fallback during startup (edge case) where current color string is null
            var alpha = (byte)(xOpacitySlider.Value / xOpacitySlider.Maximum * 255); //Ratio of current value to maximum determines the relative desired opacity
            return Windows.UI.Color.FromArgb(alpha, _currentColor.R, _currentColor.G, _currentColor.B);
        }

        private void XBackgroundColorPicker_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _currentColor = xBackgroundColorPicker.SelectedColor;
            UpdateColor();
        }
        /*
         * Ensures current color reflects desired opacity and then updates the appropriate bindings for...
         
        private void UpdateColor()
        {
            _currentColor = GetColorWithUpdatedOpacity();
            //TODO we don't actually need to store the opacity slider value as it is stored in the color as well
            //...shape's background color
            _currentDocController?.SetBackgroundColor(_currentColor);
        }
		*/
        /* NO LONGER NEED OPACITY SLIDER
        private void XOpacitySlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e) => UpdateColor();

        private void XOpacitySlider_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            xOpacitySlider.Value = 128;
            UpdateColor();
        }
		*/

        #endregion

        //prevents the color from seeing invisible
        private void XBackgroundColorButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (xBackgroundColorPicker.SelectedColor.A.Equals(0)) xBackgroundColorPicker.SetOpacity(150);
        }

        private void XMoreButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (xInitialGrid.Visibility == Visibility.Visible)
            {
                //create a formatting menu and bind it to the currently selected richEditBox's view
                xInitialGrid.Visibility = Visibility.Collapsed;
                xMenuView.Visibility = Visibility.Visible;
                xMoreIcon.Visibility = Visibility.Collapsed;
                xMoreIconBack.Visibility = Visibility.Visible;
                xMenuView?.SetRichTextBinding(_docs.GetFirstDescendantOfType<RichTextView>());

                //_buttons.TryGetValue("Font", out var fontButton);
                //if (fontButton != null)
                //{
                //	//Width meant to be 67 to match actual rendered width of main toolbar collapse button
                //	fontButton.Width = 67;
                //}

            }
            else
            {
                xInitialGrid.Visibility = Visibility.Visible;
                xMenuView.Visibility = Visibility.Collapsed;
                xMoreIcon.Visibility = Visibility.Visible;
                xMoreIconBack.Visibility = Visibility.Collapsed;

            }
        }

        private void SubscriptButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xMenuView.SubscriptButton_Tapped(sender, e);
        }

        private void SuperscriptButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xMenuView.SuperscriptButton_Tapped(sender, e);
        }

        private void xBackgroundColorPicker_SelectedColorChanged(object sender, Color e)
        {
            xMenuView.xBackgroundColorPicker_SelectedColorChanged(sender, e);
        }

        private void xForegroundColorPicker_SelectedColorChanged(object sender, Color e)
        {
            xMenuView.xForegroundColorPicker_SelectedColorChanged(sender, e);
        }
    }
}