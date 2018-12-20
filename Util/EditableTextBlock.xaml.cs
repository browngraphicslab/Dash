using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

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
            get => GetValue(TextProperty)?.ToString();
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
            "TextAlignment", typeof(TextAlignment), typeof(EditableTextBlock), new PropertyMetadata(default(TextAlignment)));

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        #endregion

        private bool _textBoxLoaded = false;

        public bool TextBoxLoaded
        {
            get => _textBoxLoaded;
            set
            {
                _textBoxLoaded = value;
                OnPropertyChanged();
            }
        }

        public Grid TextBackground => xBackground;

        public FieldControllerBase TargetFieldController { get; set; }
        public Context TargetDocContext { get; set; }

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

        public void MakeEditable()
        {
            SetExpression(Text);
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

            if (MainPage.Instance.ForceFocusPoint != null && this.GetBoundingRect(MainPage.Instance).Contains((Windows.Foundation.Point)MainPage.Instance.ForceFocusPoint))
            {
                MainPage.Instance.ClearForceFocus();
                MakeEditable();
            }
        }
        private string GetExpression()
        {
            var reference = TargetFieldController?.Dereference(TargetDocContext);
            if (reference is DocumentReferenceController dref && (dref.ReferenceFieldModel as DocumentReferenceModel).CopyOnWrite)
                return XTextBlock.Text;
            return reference?.GetValue()?.ToString();
        }

        private void SetExpression(string expression)
        {
            //TypeTimer.typeEvent();

            Text = expression;
            TextBoxLoaded = false;
            XTextBlock.Visibility = Visibility.Visible;
            //if (TargetFieldReference?.SetValue(Tuple.Create(TargetDocContext, expression)) == false)
            //    Text = GetExpression() ?? XTextBlock.Text;
            //TargetFieldReference?.Dereference(TargetDocContext)?.SetValue(expression);
        }

        private void XTextBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            

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
