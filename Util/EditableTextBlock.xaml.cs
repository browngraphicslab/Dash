using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Text;
using Dash.Converters;
using System;
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

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(EditableTextBlock), new PropertyMetadata(default(string)));

        public string Text
        {
            get { return GetValue(TextProperty)?.ToString(); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
            "TextAlignment", typeof(TextAlignment), typeof(EditableTextBlock), new PropertyMetadata(default(TextAlignment)));

        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
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

        public ReferenceController TargetFieldReference = null;
        public Context TargetDocContext = null;

        public EditableTextBlock()
        {
            InitializeComponent();

            RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);
        }

        private void XTextBlock_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            e.Handled = true;
            TextBoxLoaded = true;
        }

        private void TextChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            if (TextBoxLoaded)
            {
                SetExpression(XTextBox.Text);
            }
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
            XTextBlock.Visibility = Visibility.Collapsed;
            XTextBox.Focus(FocusState.Programmatic);
            XTextBox.Text = GetExpression() ?? XTextBlock.Text;
            XTextBox.SelectAll();
        }

        private string GetExpression()
        {
            return TargetFieldReference?.Dereference(TargetDocContext)?.GetValue(TargetDocContext)?.ToString();
        }

        private void SetExpression(string expression)
        {
            Text = expression;
            TextBoxLoaded = false;
            XTextBlock.Visibility = Visibility.Visible;
            //if (TargetFieldReference?.SetValue(Tuple.Create(TargetDocContext, expression)) == false)
            //    Text = GetExpression() ?? XTextBlock.Text;
            //TargetFieldReference?.Dereference(TargetDocContext)?.SetValue(expression);
        }

        private void XTextBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                SetExpression(XTextBox.Text);
        }

        private void XTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (TextBoxLoaded)
            {
                SetExpression(XTextBox.Text);
            }
        }
    }
}
