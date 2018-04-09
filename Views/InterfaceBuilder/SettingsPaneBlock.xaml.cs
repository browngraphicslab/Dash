using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class SettingsPaneBlock : UserControl
    {
        public static readonly DependencyProperty MainContentProperty =
            DependencyProperty.Register(
                "MainContent",
                typeof(object),
                typeof(SettingsPaneBlock),
                new PropertyMetadata(default(object)));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                "Title",
                typeof(string),
                typeof(SettingsPaneBlock),
                new PropertyMetadata(default(object)));

        public object MainContent
        {
            get { return (object) GetValue(MainContentProperty); }
            set { SetValue(MainContentProperty, value); }
        }

        public string Title
        {
            get { return (string) GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public SettingsPaneBlock()
        {
            this.InitializeComponent();
        }

        private void XTitle_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            xContentGrid.Visibility = xContentGrid.Visibility == Visibility.Collapsed
                ? Visibility.Visible
                : Visibility.Collapsed;
            xCollapseExpandButton.Text = xCollapseExpandButton.Text.Equals(Application.Current.Resources["ContractArrowIcon"].ToString()) ? Application.Current.Resources["ExpandArrowIcon"].ToString() : Application.Current.Resources["ContractArrowIcon"].ToString();

        }
    }
}
