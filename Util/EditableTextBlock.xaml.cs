using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class EditableTextBlock : INotifyPropertyChanged
    {
        #region BINDING PROPERTIES 

        public static readonly DependencyProperty TextProperty = 
            DependencyProperty.Register("Text", typeof(string), typeof(EditableTextBlock), new PropertyMetadata(default(string)));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
            "TextAlignment", typeof(TextAlignment), typeof(EditableTextBlock), new PropertyMetadata(default(TextAlignment)));

        public TextAlignment TextAlignment
        {
            get { return (TextAlignment) GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

#endregion

        private bool _textBoxLoaded = false;

        private bool TextBoxLoaded
        {
            get => _textBoxLoaded;
            set
            {
                _textBoxLoaded = value;
                OnPropertyChanged();
            }
        }

        private bool Not(bool b)
        {
            return b != true;
        }

        public EditableTextBlock()
        {
            InitializeComponent();
        }

        private void xTextBlock_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            e.Handled = true;
            TextBoxLoaded = true;
            xTextBlock.Visibility = Visibility.Collapsed;
            
        }

        private void xTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBoxLoaded = false;
            xTextBlock.Visibility = Visibility.Visible;
        }

        private void XTextBox_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void XTextBox_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void XTextBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            xTextBox.Focus(FocusState.Programmatic);
            xTextBox.SelectAll();
        }
    }
}
