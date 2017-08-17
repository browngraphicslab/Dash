using Windows.UI.Text;
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
        }

        public TextBlock Block
        {
            get { return xTextBlock; }
        }

        #region BINDING PROPERTIES 
        public static readonly DependencyProperty TextProperty = 
            DependencyProperty.Register("Text", typeof(string), typeof(EditableTextBlock), new PropertyMetadata(false));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty ColorDependency =
            DependencyProperty.Register("Foreground", typeof(SolidColorBrush), typeof(EditableTextBlock), new PropertyMetadata(false));

        public SolidColorBrush Foreground
        {
            get { return (SolidColorBrush)GetValue(ColorDependency); }
            set { SetValue(ColorDependency, value); }
        }

        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
            "FontSize", typeof(double), typeof(EditableTextBlock), new PropertyMetadata(default(double)));

        public double FontSize
        {
            get { return (double) GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public static readonly DependencyProperty FontWeightPropertyProperty = DependencyProperty.Register(
            "FontWeightProperty", typeof(FontWeight), typeof(EditableTextBlock), new PropertyMetadata(default(FontWeight)));

        public FontWeight FontWeightProperty
        {
            get { return (FontWeight) GetValue(FontWeightPropertyProperty); }
            set { SetValue(FontWeightPropertyProperty, value); }
        }

        public static readonly DependencyProperty TextAlignmentPropertyProperty = DependencyProperty.Register(
            "TextAlignmentProperty", typeof(TextAlignment), typeof(EditableTextBlock), new PropertyMetadata(default(TextAlignment)));

        public TextAlignment TextAlignmentProperty
        {
            get { return (TextAlignment) GetValue(TextAlignmentPropertyProperty); }
            set { SetValue(TextAlignmentPropertyProperty, value); }
        }

#endregion

        public EditableTextBlock()
        {
            InitializeComponent();

            //events 
            Box.PointerWheelChanged += (s, e) => e.Handled = true;
            Box.ManipulationDelta += (s, e) => e.Handled = true;

            // bindings 
            var textBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(Text)),
                Mode = BindingMode.TwoWay
            };
            Block.SetBinding(TextBlock.TextProperty, textBinding);
            Box.SetBinding(TextBox.TextProperty, textBinding);

            //var colorBinding = new Binding
            //{
            //    Source = this,
            //    Path = new PropertyPath(nameof(Foreground)),
            //    Mode = BindingMode.TwoWay
            //};
            //Block.SetBinding(TextBlock.ForegroundProperty, colorBinding);
            //Box.SetBinding(TextBox.ForegroundProperty, colorBinding);
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
