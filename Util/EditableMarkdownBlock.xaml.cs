using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class EditableMarkdownBlock : INotifyPropertyChanged
    {
        #region BINDING PROPERTIES 

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(EditableMarkdownBlock), new PropertyMetadata(default(string)));

        public string Text
        {
            get => GetValue(TextProperty)?.ToString();
            set => SetValue(TextProperty, value);
        }

        #endregion
        

        private bool _markdownBoxLoaded = false;

        public bool MarkdownBoxLoaded
        {
            get => _markdownBoxLoaded;
            set
            {
                if (!value)
                {
                    XMarkdownBox?.RemoveHandler(KeyDownEvent, _markdownBoxKeyDownHandler);
                }
                _markdownBoxLoaded = value;
                OnPropertyChanged();
                if (value)
                {
                    XMarkdownBox.AddHandler(KeyDownEvent, _markdownBoxKeyDownHandler, true);
                }
            }
        }

        public Grid TextBackground => xBackground;

        public FieldControllerBase TargetFieldController { get; set; }
        public Context TargetDocContext { get; set; }

        private KeyEventHandler _markdownBoxKeyDownHandler;

        public EditableMarkdownBlock()
        {
            InitializeComponent();
            RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);

            XMarkdownBlock.AddHandler(TappedEvent, new TappedEventHandler(XMarkdownBlock_Tapped), true);
            XMarkdownBlock.AddHandler(DoubleTappedEvent, new DoubleTappedEventHandler(XMarkdownBlock_DoubleTapped), true);

            _markdownBoxKeyDownHandler = new KeyEventHandler(xMarkdownBox_KeyDown);

            this.Loaded += EditableMarkdownBlock_Loaded;
        }

        private void EditableMarkdownBlock_Loaded(object sender, RoutedEventArgs e)
        {
            if (CollectionFreeformView.ForceFocusPoint != null && this.GetBoundingRect(MainPage.Instance).Contains((Windows.Foundation.Point)CollectionFreeformView.ForceFocusPoint))
            {
                MakeEditable();
            }
        }

        private void XMarkdownBlock_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            e.Handled = false;
            MarkdownBoxLoaded = true;
        }
        
        private void XMarkdownBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = false;
            MarkdownBoxLoaded = true;
        }

        public void MakeEditable()
        {
            SetExpression(Text);
            MarkdownBoxLoaded = true;
        }
        private void TextChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            //TODO this gets called more than it should
            if (MarkdownBoxLoaded)
            {
                SetExpression(XMarkdownBox.Text);
            }
        }

        private void XMarkdownBox_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void XMarkdownBox_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void XMarkdownBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (CollectionFreeformView.ForceFocusPoint != null && this.GetBoundingRect(MainPage.Instance).Contains((Windows.Foundation.Point)CollectionFreeformView.ForceFocusPoint))
            {
                CollectionFreeformView.ClearForceFocus();
                XMarkdownBox.Focus(FocusState.Programmatic);
            }
            XMarkdownBlock.Visibility = Visibility.Collapsed;
            XMarkdownBox.Focus(FocusState.Programmatic);
            XMarkdownBox.Text = GetExpression() ?? XMarkdownBlock.Text;

            //this makes typing continue at end
            if (XMarkdownBox.Text.Length > 0)
            {
                XMarkdownBox.SelectionStart = XMarkdownBox.Text.Length;
                XMarkdownBox.SelectionLength = 0;
            }
        }

        private string GetExpression()
        {
            var reference = TargetFieldController?.Dereference(TargetDocContext);
            if (reference is DocumentReferenceController dref && (dref.ReferenceFieldModel as DocumentReferenceModel).CopyOnWrite)
                return XMarkdownBlock.Text;
            return reference?.GetValue()?.ToString();
        }

        private void SetExpression(string expression)
        {
            Text = expression;
            MarkdownBoxLoaded = false;
            XMarkdownBlock.Visibility = Visibility.Visible;
        }

        private void XMarkdownBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (MarkdownBoxLoaded)
            {
                SetExpression(XMarkdownBox.Text);
            }
        }

        private void XMarkdownBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void xMarkdownBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && Window.Current.CoreWindow.GetKeyState(VirtualKey.Control)
                    .HasFlag(CoreVirtualKeyStates.Down))
            {
                XMarkdownBox.Text = XMarkdownBox.Text.Remove(XMarkdownBox.SelectionStart - 1, 1);
                SetExpression(XMarkdownBox.Text);
                e.Handled = true;
            }

            if (e.Key.Equals(VirtualKey.Escape))
            {
                var tab = XMarkdownBlock.IsTabStop;
                XMarkdownBlock.IsTabStop = false;
                XMarkdownBlock.IsEnabled = false;
                XMarkdownBlock.IsEnabled = true;
                XMarkdownBlock.IsTabStop = tab;
            }
        }

        private async void XMarkdownBlock_LinkClicked(object sender, Microsoft.Toolkit.Uwp.UI.Controls.LinkClickedEventArgs e)
        {
            var linkE = e.Link;
            if (!linkE.Contains("http"))
            {
                linkE = "https://" + linkE;
            }
            if (Uri.TryCreate(linkE, UriKind.Absolute, out Uri link))
            {
                await Launcher.LaunchUriAsync(link);
            }
        }
    }
}
