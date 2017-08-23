using Dash.Converters;
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
        #region BINDING PROPERTIES 

        public string Text
        {
            get { return (string)xTextBlock.Text; }
            set { xTextBlock.SetValue(TextBlock.TextProperty, value); }
        }

        #endregion
        public ReferenceFieldModelController TargetFieldReference = null;
        public Context                       TargetDocContext = null;

        public EditableTextBlock()
        {
            InitializeComponent();

            //events 
            xTextBox.PointerWheelChanged += (s, e) => e.Handled = true;
            xTextBox.ManipulationDelta += (s, e) => e.Handled = true;
            xTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                    xTextBox_LostFocus(s, null);
            };

            //var colorBinding = new Binding
            //{
            //    Source = this,
            //    Path = new PropertyPath(nameof(Foreground)),
            //    Mode = BindingMode.TwoWay
            //};
            //Block.SetBinding(TextBlock.ForegroundProperty, colorBinding);
            //Box.SetBinding(TextBox.ForegroundProperty, colorBinding);

            xTextBox.Text = xTextBlock.Text;
        }

        private void xTextBlock_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            e.Handled = true;

            this.xTextBox.Text = TargetFieldReference?.Dereference(TargetDocContext)?.GetValue(TargetDocContext)?.ToString() ?? xTextBlock.Text;

            xTextBlock.Visibility = Visibility.Collapsed;
            xTextBox.Visibility = Visibility.Visible;
            xTextBox.Focus(FocusState.Programmatic);
            xTextBox.SelectAll();
        }

        private void xTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (xTextBox.Visibility == Visibility.Visible)
            {
                xTextBox.Visibility = Visibility.Collapsed;
                xTextBlock.Visibility = Visibility.Visible;

                // if textBox specifies a field reference, then TextBlock's bindings will convert it to a ReferenceField 
                // which will then trigger the TextBlock to display the dereferenced value
                xTextBlock.Text = xTextBox.Text; 
            }
        }
    }
}
