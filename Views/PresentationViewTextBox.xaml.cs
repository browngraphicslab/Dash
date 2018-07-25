using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class PresentationViewTextBox : UserControl
    {
        public bool HasBeenCustomRenamed;

        public PresentationViewTextBox()
        {
            InitializeComponent();
            KeyDown += (sender, args) => { if (args.Key == VirtualKey.Enter) UpdateName(); };
        }

        private void Textblock_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => TriggerEdit();

        private void Textbox_OnLostFocus(object sender, RoutedEventArgs e) => UpdateName();

        private void UpdateName()
        {
            Textblock.Text = Textbox.Text;
            if (!HasBeenCustomRenamed)
            {
                HasBeenCustomRenamed = true;
                Textblock.AddFieldBinding(TextBlock.TextProperty, null);
                Textbox.AddFieldBinding(TextBox.TextProperty, null);
            }
            Textblock.Visibility = Visibility.Visible;
            Textbox.Visibility = Visibility.Collapsed;
        }

        public void TriggerEdit()
        {
            Textblock.Visibility = Visibility.Collapsed;
            Textbox.Visibility = Visibility.Visible;
            Textbox.Focus(FocusState.Programmatic);
        }
        
        // binding to the title of the corresponding document
        private void FrameworkElement_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue is DocumentController dc)
            {
                string currentTitle = dc.GetDereferencedField<TextController>(KeyStore.TitleKey, null).Data;
                if (string.IsNullOrEmpty(currentTitle))
                    dc.SetField(KeyStore.TitleKey, new TextController("<untitled>"), true);
                SetTitleBinding(dc);
            }
        }

        public void ResetTitle()
        {
            HasBeenCustomRenamed = false;
            SetTitleBinding((DocumentController) DataContext);
        }

        private void SetTitleBinding(DocumentController dc)
        {
            var binding = new FieldBinding<TextController>
            {
                Document = dc,
                Key = KeyStore.TitleKey,
                Mode = BindingMode.OneWay
            };
            Textbox.AddFieldBinding(TextBox.TextProperty, binding);
            Textblock.AddFieldBinding(TextBlock.TextProperty, binding);
        }
    }
}
