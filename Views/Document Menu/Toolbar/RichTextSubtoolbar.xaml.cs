using System;
using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarButtons;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarFormats;
using Windows.UI;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Shapes;
using Dash.Annotations;
using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarFormats.RichText;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /// <summary>
    /// The subtoolbar that allows users to edit and style their text. Visible only when a richeditbox is selected.
    /// </summary>
    public sealed partial class RichTextSubtoolbar : INotifyPropertyChanged
    {
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation",
            typeof(Orientation), typeof(RichTextSubtoolbar), new PropertyMetadata(default(Orientation)));

        public Orientation Orientation
        {
            get => (Orientation) GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        private RichEditBox _currBox;
        private DocumentView _documentView;
        private Dictionary<string, Button> _buttons;
        private Windows.UI.Color _currentColor;

        private sealed class MyFormatter : RichTextFormatter
        {
            public MyFormatter(TextToolbar model, ResourceDictionary xTextGridResources) : base(model)
            {
                var tbStyle = (Style) xTextGridResources["TextBlockStyle"];

                DefaultButtons = new ButtonMap();
                base.DefaultButtons.Where((v, i) => i != 3 && i != 4).ToList().ForEach(DefaultButtons.Add);
                const PlacementMode placementMode = PlacementMode.Bottom;
                const int offset = 5;

                // BOLD
                var bold = (ToolbarButton)DefaultButtons[0];
                bold.Loaded += (sender, args) => bold.Icon.GetDescendantsOfType<TextBlock>().ToList().ForEach(tb => tb.Style = tbStyle);
                var margin = bold.Margin;
                margin.Top = -4;
                bold.Margin = margin;
                ToolTipService.SetToolTip(bold, null);
                var boldTip = new ToolTip()
                {
                    Content = "Bold (Ctrl + B)",
                    Placement = placementMode,
                    VerticalOffset = offset
                };
                ToolTipService.SetToolTip(bold, boldTip);
                bold.PointerEntered += (sender, args) => { if (sender is ToolbarButton b && ToolTipService.GetToolTip(b) is ToolTip tip) tip.IsOpen = true; };
                bold.PointerExited += (sender, args) => { if (sender is ToolbarButton b && ToolTipService.GetToolTip(b) is ToolTip tip) tip.IsOpen = false; };

                // ITALICS
                var italics = (ToolbarButton)DefaultButtons[1];
                italics.Loaded += (sender, args) => italics.Icon.GetDescendantsOfType<TextBlock>().ToList().ForEach(tb => tb.Style = tbStyle);
                margin = italics.Margin;
                margin.Top = -4;
                italics.Margin = margin;
                ToolTipService.SetToolTip(italics, null);
                var italicTip = new ToolTip()
                {
                    Content = "Italics (Ctrl + I)",
                    Placement = placementMode,
                    VerticalOffset = offset
                };
                ToolTipService.SetToolTip(italics, italicTip);
                italics.PointerEntered += (sender, args) => { if (sender is ToolbarButton b && ToolTipService.GetToolTip(b) is ToolTip tip) tip.IsOpen = true; };
                italics.PointerExited += (sender, args) => { if (sender is ToolbarButton b && ToolTipService.GetToolTip(b) is ToolTip tip) tip.IsOpen = false; };

                // UNDERLINE
                var underline = (ToolbarButton)DefaultButtons[2];
                underline.Loaded += (sender, args) => underline.Icon.GetDescendantsOfType<TextBlock>().ToList().ForEach(tb => tb.Style = tbStyle);
                margin = underline.Margin;
                margin.Top = -3;
                underline.Margin = margin;
                ToolTipService.SetToolTip(underline, null);
                var underlineTip = new ToolTip()
                {
                    Content = "Underline (Ctrl + U)",
                    Placement = placementMode,
                    VerticalOffset = offset
                };
                ToolTipService.SetToolTip(underline, underlineTip);
                underline.PointerEntered += (sender, args) => { if (sender is ToolbarButton b && ToolTipService.GetToolTip(b) is ToolTip tip) tip.IsOpen = true; };
                underline.PointerExited += (sender, args) => { if (sender is ToolbarButton b && ToolTipService.GetToolTip(b) is ToolTip tip) tip.IsOpen = false; };

                // STRIKETHROUGH
                var strikethrough = (ToolbarButton)DefaultButtons[3];
                strikethrough.Loaded += (sender, args) => strikethrough.Icon.GetDescendantsOfType<TextBlock>().ToList().ForEach(tb => tb.Style = tbStyle);
                margin = strikethrough.Margin;
                margin.Top = -4;
                strikethrough.Margin = margin;
                strikethrough.Width = 65;
                ToolTipService.SetToolTip(strikethrough, null);
                var strikethroughTip = new ToolTip()
                {
                    Content = "Strikethrough (Ctrl + -)",
                    Placement = placementMode,
                    VerticalOffset = offset
                };
                ToolTipService.SetToolTip(strikethrough, strikethroughTip);
                strikethrough.PointerEntered += (sender, args) => { if (sender is ToolbarButton b && ToolTipService.GetToolTip(b) is ToolTip tip) tip.IsOpen = true; };
                strikethrough.PointerExited += (sender, args) => { if (sender is ToolbarButton b && ToolTipService.GetToolTip(b) is ToolTip tip) tip.IsOpen = false; };

                // SEPARATOR ONE
                var sepOne = (ToolbarSeparator)DefaultButtons[4];
                sepOne.Loaded += (sender, args) => sepOne.GetDescendantsOfType<Rectangle>().ToList().ForEach(rect => rect.Fill = new SolidColorBrush(Colors.White));
                margin = sepOne.Margin;
                margin.Top = -7;
                margin.Left = 12;
                margin.Right = 12;
                sepOne.Margin = margin;

                // UNORDERED LIST
                var list = (ToolbarButton)DefaultButtons[5];
                list.Loaded += (sender, args) =>
                {
                    list.GetDescendantsOfType<TextBlock>().ToList().ForEach(tb => tb.Foreground = new SolidColorBrush(Colors.White));
                    var attach = list.GetDescendants().Where(dob => (dob as FrameworkElement)?.Name == "Attach").ToList();
                    ((Grid) attach.First()).Background = new SolidColorBrush(Colors.White);
                };
                margin = list.Margin;
                margin.Top = -3;
                list.Margin = margin;
                ToolTipService.SetToolTip(list, null);
                var ulTip = new ToolTip()
                {
                    Content = "List",
                    Placement = placementMode,
                    VerticalOffset = offset
                };
                ToolTipService.SetToolTip(list, ulTip);
                list.PointerEntered += (sender, args) => {
                    if (sender is ToolbarButton b && ToolTipService.GetToolTip(b) is ToolTip tip)
                    {
                        tip.IsOpen = true;
                        tip.VerticalOffset = 5;
                        tip.Placement = PlacementMode.Bottom;
                    }
                };
                list.PointerExited += (sender, args) => {
                    if (sender is ToolbarButton b && ToolTipService.GetToolTip(b) is ToolTip tip)
                    {
                        tip.IsOpen = false;
                        tip.VerticalOffset = 5;
                        tip.Placement = PlacementMode.Bottom;
                    }
                };

                // ORDERED LIST
                var orderedList = (ToolbarButton)DefaultButtons[6];
                orderedList.Loaded += (sender, args) =>
                {
                    orderedList.GetDescendantsOfType<TextBlock>().ToList().ForEach(tb => tb.Foreground = new SolidColorBrush(Colors.White));
                    var attach = orderedList.GetDescendants().Where(dob => (dob as FrameworkElement)?.Name == "Attach").ToList();
                    ((Grid) attach.First()).Background = new SolidColorBrush(Colors.White);
                };
                margin = orderedList.Margin;
                margin.Top = -3;
                orderedList.Margin = margin;
                list.Margin = margin;
                ToolTipService.SetToolTip(orderedList, null);
                var olTip = new ToolTip()
                {
                    Content = "Ordered List",
                    Placement = placementMode,
                    VerticalOffset = offset
                };
                ToolTipService.SetToolTip(orderedList, olTip);
                orderedList.PointerEntered += (sender, args) => { if (sender is ToolbarButton b && ToolTipService.GetToolTip(b) is ToolTip tip) tip.IsOpen = true; };
                orderedList.PointerExited += (sender, args) => { if (sender is ToolbarButton b && ToolTipService.GetToolTip(b) is ToolTip tip) tip.IsOpen = false; };
            }

            public override ButtonMap DefaultButtons { get; }
        }

        public SolidColorBrush SelectedFontColor
        {
            get => _selectedFontColor;
            set
            {
                _selectedFontColor = value;
                OnPropertyChanged();
            }
        }

        public SolidColorBrush SelectedHighlightColor
        {
            get => _selectedHighlightColor;
            set
            {
                _selectedHighlightColor = value;
                OnPropertyChanged();
            }
        }

        public RichTextSubtoolbar()
        {
            InitializeComponent();
            _buttons = new Dictionary<string, Button>();

            SelectedFontColor = new SolidColorBrush(Colors.White);
            SelectedHighlightColor = new SolidColorBrush(Colors.White);

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
            };
            _formatter = new MyFormatter(xDashTextSubtoolbar, xTextGridResources);
            SetUpToolTips();
        }

        private Formatter _formatter;
        private SolidColorBrush _selectedFontColor;
        private SolidColorBrush _selectedHighlightColor;

        /**
		 * Binds the text toolbar with the most recently selected text box for editing purposes.
		 */
        public void SetMenuToolBarBinding(RichEditBox selection)
        {
            xDashTextSubtoolbar.Editor = selection;
            xDashTextSubtoolbar.Visibility = Visibility.Visible;
            xDashTextSubtoolbar.GetFirstDescendantOfType<StackPanel>().Orientation = Orientation;
            xDashTextSubtoolbar.Formatter = _formatter;
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
        public void SetSelectedDocumentView(DocumentView doc)
        {
            _documentView = doc;
            xMenuView?.SetRichTextBinding(doc.GetFirstDescendantOfType<RichEditView>()); // bcz: weird ... the selected view is a RichTextView, but it's not always in the visual tree (eg when in it's in CollectionStackingView) so we can't use this seemingly reasonable code: _docs.ViewModel.Content.GetFirstDescendantOfType<RichTextView>() ?? _docs.ViewModel.Content as RichTextView);
            xBackgroundColorPicker.SelectedColor = _documentView.ViewModel.DocumentController.GetBackgroundColor() ?? Colors.Transparent;
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
            _documentView?.ViewModel?.LayoutDocument?.SetBackgroundColor(e);
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
                xMenuView?.SetRichTextBinding(_documentView.GetFirstDescendantOfType<RichEditView>());

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

        private void xHighlightColorPicker_SelectedColorChanged(object sender, Color e)
        {
            xMenuView.xHighlightColorPicker_SelectedColorChanged(sender, e);
            SelectedHighlightColor = new SolidColorBrush(xHighlightColorPicker.SelectedColor);
        }

        private void xForegroundColorPicker_SelectedColorChanged(object sender, Color e)
        {
            xMenuView.xForegroundColorPicker_SelectedColorChanged(sender, e);
            SelectedFontColor = new SolidColorBrush(xForegroundColorPicker.SelectedColor);
        }

        private async void XAddSpeechButton_OnClick(object sender, RoutedEventArgs e)
        {
            string newText = await CollectionView.getSpokenText();
            if (!String.IsNullOrEmpty(newText))
            {
                string oldText = string.Empty;
                _currBox.Document.GetText(Windows.UI.Text.TextGetOptions.AdjustCrlf, out oldText);
                _currBox.Document.SetText(Windows.UI.Text.TextSetOptions.None, oldText + " " + newText);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ToolTip _fontColor;
        private ToolTip _highlightColor;
        private ToolTip _subscript;
        private ToolTip _superScript;
        private ToolTip _backgroundColor;
        private ToolTip _addSpeech;

        private void SetUpToolTips()
        {
            const PlacementMode placementMode = PlacementMode.Bottom;
            const int offset = 5;

            _fontColor = new ToolTip()
            {
                Content = "Text Color",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xFontColor, _fontColor);

            _highlightColor = new ToolTip()
            {
                Content = "Highlight Color",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xHighlightColor, _highlightColor);

            _subscript = new ToolTip()
            {
                Content = "Subscript",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xSubscript, _subscript);

            _superScript = new ToolTip()
            {
                Content = "Superscript",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xSuperscript, _superScript);

            _backgroundColor = new ToolTip()
            {
                Content = "Background Color",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xBackgroundColorButton, _backgroundColor);

            _addSpeech = new ToolTip()
            {
                Content = "Speech to Text",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xAddSpeechButton, _addSpeech);
        }

        private void ShowAppBarToolTip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is AppBarButton button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = true;
            else if (sender is AppBarToggleButton toggleButton && ToolTipService.GetToolTip(toggleButton) is ToolTip toggleTip) toggleTip.IsOpen = true;
        }

        private void HideAppBarToolTip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is AppBarButton button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = false;
            else if (sender is AppBarToggleButton toggleButton && ToolTipService.GetToolTip(toggleButton) is ToolTip toggleTip) toggleTip.IsOpen = false;
        }
    }
}
