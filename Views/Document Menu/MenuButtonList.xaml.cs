using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public class MenuButtonItem
    {
        public Symbol Symbol { get; set; }
        public string Name { get; set; }
        public Action Action { get; set; }

        public MenuButtonItem(Symbol symbol, string name, Action action)
        {
            Symbol = symbol;
            Name = name;
            Action = action;
        }

        public void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            Action?.Invoke();
        }
    }

    public sealed partial class MenuButtonList : UserControl
    {

        private List<MenuButtonItem> _menuButtonItems;

        public List<MenuButtonItem> MenuButtonItems
        {
            get => _menuButtonItems;
            set
            {
                _menuButtonItems = value;
                XMenuListView.ItemsSource = value;
            }
        }

        public static readonly DependencyProperty ShowNameProperty = DependencyProperty.Register(
            "ShowName", typeof(bool), typeof(MenuButtonList), new PropertyMetadata(true));

        public bool ShowName
        {
            get { return (bool) GetValue(ShowNameProperty); }
            set { SetValue(ShowNameProperty, value); }
        }

        public MenuButtonList()
        {
            this.InitializeComponent();
        }
    }
}
