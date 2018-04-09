using System;
using Windows.UI.Xaml.Controls;

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
        public Symbol Symbol { get { return xSymbol.Symbol; } set { xSymbol.Symbol = value; } }
        public TextBlock Label { get { return xLabel; } }
        public Button Button { get { return xButton; } }
        // background binding to themeresource in menubutton makes buttons circular
        public Border Border { get { return xBorder; } set { xBorder = value; } }
    }
}