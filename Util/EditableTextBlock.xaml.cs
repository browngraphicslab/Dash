using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class EditableTextBlock
    {
        public TextBox Box
        {
            get { return xTextBox; }
            set { xTextBox = value; }
        }

        public TextBlock Block
        {
            get { return xTextBlock; }
            set { xTextBlock = value; }
        }

        public static readonly DependencyProperty TextDependency = 
            DependencyProperty.Register("Text", typeof(string), typeof(EditableTextBlock), new PropertyMetadata(false));

        public string Text
        {
            get { return (string)GetValue(TextDependency); }
            set { SetValue(TextDependency, value); }
        }

        public static readonly DependencyProperty ColorDependency =
            DependencyProperty.Register("Foreground", typeof(SolidColorBrush), typeof(EditableTextBlock), new PropertyMetadata(false));

        public SolidColorBrush Foreground
        {
            get { return (SolidColorBrush)GetValue(ColorDependency); }
            set { SetValue(ColorDependency, value); }
        }

        public EditableTextBlock()
        {
            InitializeComponent();

            Box.PointerWheelChanged += (s, e) => e.Handled = true;
            Box.ManipulationDelta += (s, e) => e.Handled = true;

            var textBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(Text)),
                Mode = BindingMode.TwoWay
            };
            Block.SetBinding(TextBlock.TextProperty, textBinding);
            Box.SetBinding(TextBox.TextProperty, textBinding);

            var colorBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(Foreground)),
                Mode = BindingMode.TwoWay
            };
            Block.SetBinding(TextBlock.ForegroundProperty, colorBinding);
            Box.SetBinding(TextBox.ForegroundProperty, colorBinding);
        }

        private void xTextBlock_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            e.Handled = true;
            Block.Visibility = Visibility.Collapsed;
            Box.Visibility = Visibility.Visible;
            Box.Focus(FocusState.Programmatic);
            Box.SelectAll();
        }

        private void xTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Box.Visibility = Visibility.Collapsed;
            Block.Visibility = Visibility.Visible;
        }
    }
}
