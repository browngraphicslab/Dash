using System;
using System.Collections.Generic;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views.Document_Menu
{
    public sealed partial class MenuButtonContainer : UserControl
    {
        public MenuButtonContainer()
        {
            this.InitializeComponent();
        }

        public MenuButtonContainer(Symbol symbol, String label)
        {
            this.InitializeComponent();
            xSymbol.Symbol = symbol;
            xLabel.Text = label.ToLower();
        }
        public TextBlock Label { get { return xLabel; } }
        public Button Button { get { return xButton; } }
        public Border Border { get { return xBorder; } set { xBorder = value; } }
    }
}
