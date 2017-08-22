using System;
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

        public string Text
        {
            get { return (string)Block.Text; }
            set { Block.SetValue(TextBlock.TextProperty, value); }
        }
        public string Formula
        {
            get { return (string)Box.Text; }
            set { Box.SetValue(TextBox.TextProperty, value); }
        }
#endregion
        public class TextToFormulaConverter : IValueConverter
        {
            EditableTextBlock et;
            public TextToFormulaConverter(EditableTextBlock e) { et = e;  }
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                if (et.Box.Text.Trim(' ').StartsWith("="))
                    return et.Box.Text;
                return value is string ? (string)value : "";
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language)
            {
                return value is string ? (string)value : "";
            }
        }

    public EditableTextBlock()
        {
            InitializeComponent();

            //events 
            Box.PointerWheelChanged += (s, e) => e.Handled = true;
            Box.ManipulationDelta += (s, e) => e.Handled = true;

            // bindings 
            var formulaBinding = new Binding
            {
                Source = Block,
                Path   = new PropertyPath(nameof(Text)),
                Mode   = BindingMode.TwoWay,
                Converter = new TextToFormulaConverter(this)
            };
            Box.SetBinding(TextBox.TextProperty, formulaBinding);

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
