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
	    private Color _highlightColor;
        private FormattingMenuView _menuView = null;
        private DocumentView _docs;

        public TextSubtoolbar()
        {
            this.InitializeComponent();
			//instantiate formatter to create custom buttons
			Formatter customButtonFormatter = new CustomButtonFormatter(xDashTextSubtoolbar);
	        _currBox = null;
			//add an additional sub-toolbar for further operations
	        this.AddButton("Font", Symbol.Add, 0);

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
        public void AddButton(String name, Symbol icon, int position)
        {
           //instantiate ToolbarButton & set properties
	        ToolbarButton button = new ToolbarButton
	        {
		        Name = name,
		        Icon = new SymbolIcon(icon),
		        Position = position,
				Background = new SolidColorBrush(Colors.LightSlateGray),
				Width = 70,
	        }; //add to toolbar
	        xDashTextSubtoolbar.CustomButtons.Add(button);
	        
			//add appropriate handlers
	        switch (button.Name)
	        {
				case "Font":
					button.Tapped += (sender, args) =>
					{
						this.FontOnTapped();
					};
					break;
				
					//add more buttons here!

				default:
					break;

	        }
			//add small separation between other buttons
            xDashTextSubtoolbar.CustomButtons.Add(new ToolbarSeparator { Position = position + 1 });
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
		 * When the Font Button is clicked, the font menu visibility is toggled, giving user access to additional editing operations like font style, etc.
		 */
	    private void FontOnTapped()
		{ 
		    if (_currBox != null)
		    {
			    if (_menuView == null)
			    {
					//create a formatting menu and bind it to the currently selected richEditBox's view
				    _menuView = new FormattingMenuView(this);
				    _menuView.richTextView = VisualTreeHelperExtensions.GetFirstDescendantOfType<RichTextView>(_docs);
				    _menuView.xRichEditBox = _currBox;
					//add the menu to the stack panel
				    xStack.Children.Add(_menuView);
					//collapse other text menu
				    xDashTextSubtoolbar.Visibility = Visibility.Collapsed;
			    }
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
		    xDashTextSubtoolbar.Visibility = Visibility.Visible;

		}

	}
}
