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
using Microsoft.Toolkit.Uwp.UI.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TextSubtoolbar : UserControl
    {
        public TextSubtoolbar()
        {
            this.InitializeComponent();
           
        }


        public void SetMenuToolBarBinding(RichEditBox selection)
        {
            xDashTextSubtoolbar.Editor = selection;
        }

        public void AddButton(String name, Symbol icon, int position)
        {
            ToolbarButton button = xDashTextSubtoolbar.GetDefaultButton(ButtonType.Headers);
            button.Visibility = Visibility.Collapsed;
            xDashTextSubtoolbar.CustomButtons.Add(new ToolbarButton
            {
                Name = name,
                Icon = new SymbolIcon(icon),
                Position = position
            }
                );
            xDashTextSubtoolbar.CustomButtons.Add(new ToolbarSeparator { Position = position + 1 });
        }
    }
}
