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

    public sealed partial class TextSubtoolbar : UserControl
    {
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(TextSubtoolbar), new PropertyMetadata(default(Orientation)));

        public Orientation Orientation
        {
            get { return (Orientation) GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

	    private RichEditBox _currBox;
        private FormattingMenuView _menuView = null;
        private DocumentView _docs;
        private Dictionary<string, Button> _buttons;

        public TextSubtoolbar()
        {
            this.InitializeComponent();
            _buttons = new Dictionary<string, Button>();
			//instantiate formatter to create custom buttons
			Formatter customButtonFormatter = new CustomButtonFormatter(xDashTextSubtoolbar);
	        _currBox = null;
			//add an additional sub-toolbar for further operations
            this.AddButton("Font", Symbol.Add, 0, (sender, args) =>
            {
                /**
                 * When the Font Button is clicked, the font menu visibility is toggled, giving user access to additional editing operations like font style, etc.
                 */
                if (_currBox != null && _menuView == null)
                {
                    //create a formatting menu and bind it to the currently selected richEditBox's view
                    _menuView = new FormattingMenuView(this)
                    {
                        richTextView = _docs.GetFirstDescendantOfType<RichTextView>(),
                        xRichEditBox = _currBox
                    };
                    //add the menu to the stack panel
                    xStack.Children.Add(_menuView);
                    //collapse other text menu
                    xDashTextSubtoolbar.Visibility = Visibility.Collapsed;
                    _buttons.TryGetValue("Font", out var fontButton);
                    if (fontButton != null)
                    {
                        fontButton.Width = 67;
                    }
                }
            }, 67);

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
            };
        }

		/**
		 * Binds the text toolbar with the most recently selected text box for editing purposes.
		 */
        public void SetMenuToolBarBinding(RichEditBox selection)
        {
            xDashTextSubtoolbar.Editor = selection;
        }

		/**
		 * Helper method for adding custom buttons.
		 */
        public Button AddButton(string name, Symbol icon, int position, TappedEventHandler onTapped, int width = 70, bool includeSeparator = false)
        {
           //instantiate ToolbarButton & set properties
	        var button = new ToolbarButton
	        {
		        Name = name,
		        Icon = new SymbolIcon(icon),
		        Position = position,
				Background = new SolidColorBrush(Colors.LightSlateGray),
				Width = width,
	        }; //add to toolbar
	        xDashTextSubtoolbar.CustomButtons.Add(button);
            //assign event handler to button on tapped
            button.Tapped += onTapped;        
			//add small separation between other buttons
            if (includeSeparator) xDashTextSubtoolbar.CustomButtons.Add(new ToolbarSeparator { Position = position + 1 });
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
	    }

		/**
		 * Setter for the documnentview of the richedittextbox, used for accessing text edit methods
		 */
	    public void SetDocs(DocumentView docs)
	    {
		    _docs = docs;
	    }

		/**
		 * Used to toggle between text sub-menus
		 */
	    public void CloseSubMenu()
	    {
			xStack.Children.Remove(_menuView);
		    _menuView = null;
			//restore other menu
		    xDashTextSubtoolbar.Visibility = Visibility.Visible;

		}

	}
}
