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
    public sealed partial class TextSubtoolbar : UserControl
    {

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            "Orientation", typeof(Orientation), typeof(TextSubtoolbar), new PropertyMetadata(default(Orientation)));

        public Orientation Orientation
        {
            get { return (Orientation) GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

	    private RichEditBox _currBox;
	    private Color _highlightColor;

        public TextSubtoolbar()
        {
            this.InitializeComponent();
			//instantiate formatter to create custom buttons
			Formatter customButtonFormatter = new CustomButtonFormatter(xDashTextSubtoolbar);
	        _currBox = null;
	        this.AddCustomButtons();
        }


        public void SetMenuToolBarBinding(RichEditBox selection)
        {
            xDashTextSubtoolbar.Editor = selection;
        }

	    public void AddCustomButtons()
	    {
			this.AddButton("Highlight", Symbol.Highlight, 4);
		    this.AddButton("Color", Symbol.FontColor, 5);
		    this.AddButton("Font", Symbol.Font, 6);
		    this.AddButton("+", Symbol.FontIncrease, 7);
		    this.AddButton("-", Symbol.FontDecrease, 8);
		}

        public void AddButton(String name, Symbol icon, int position)
        {
            //ToolbarButton button = xDashTextSubtoolbar.GetDefaultButton(ButtonType.Headers);
            //button.Visibility = Visibility.Collapsed;
	        ToolbarButton button = new ToolbarButton
	        {
		        Name = name,
		        Icon = new SymbolIcon(icon),
		        Position = position,
	        };
	        xDashTextSubtoolbar.CustomButtons.Add(button);
	        switch (button.Name)
	        {
				case "Highlight":
					button.Tapped += (sender, args) =>
					{
						this.HighlightOnTapped();
					};
					break;

				case "Color":
					button.Tapped += (sender, args) =>
					{
						this.ColorOnTapped();
					};
					break;

				default:
					break;

	        }
			//add small separation between other buttons
            xDashTextSubtoolbar.CustomButtons.Add(new ToolbarSeparator { Position = position + 1 });
        }

	    public void SetCurrTextBox(RichEditBox box)
	    {
		    _currBox = box;
	    }

	    private void HighlightOnTapped()
		{ 
		    if (_currBox != null)
		    {
				//if text is highlighted already, un-highlight
			    if (_currBox.Document.Selection.CharacterFormat.BackgroundColor == Colors.Yellow)
			    {
				    _currBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.White;
			    }
			    else
			    {
					//set background color of selected text to current highlight color
				    _currBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.Yellow;
				}
				//unselect selected text
			    _currBox.Document.Selection.SetRange(0, 0);
		    }
		
		}

	    private void ColorOnTapped()
	    {
		    if (_currBox != null)
		    {
				//set font color of selected text to current color
				_currBox.Document.Selection.CharacterFormat.ForegroundColor = Colors.Red;
			    //unselect selected text
			    _currBox.Document.Selection.SetRange(0, 0);
		    }

	    }



	}
}
