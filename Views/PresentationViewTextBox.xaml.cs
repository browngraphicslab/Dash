using System;
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
        public bool HasBeenCustomRenamed
        {
            get => ((DocumentController)DataContext).GetField<BoolController>(KeyStore.PresTextRenamedKey)?.Data ?? false;
            set => ((DocumentController)DataContext).SetField<BoolController>(KeyStore.PresTextRenamedKey, value, true);
        }

        public PresentationViewTextBox()
        {
            InitializeComponent();
            KeyDown += (sender, args) =>
            {
                if (args.Key == VirtualKey.Enter)
                {
                    UpdateName();
                    args.Handled = true;
                }
            };
            Loaded += AddPresTitleListener;
            Unloaded -= AddPresTitleListener;
        }

        private void AddPresTitleListener(object sender, RoutedEventArgs e)
        {
            ((DocumentController) DataContext)?.AddFieldUpdatedListener(KeyStore.PresentationTitleKey, OnPresTitleKeyUpdated);
        }

        private void OnPresTitleKeyUpdated(DocumentController dc, DocumentController.DocumentFieldUpdatedEventArgs dArgs, Context context)
        {
            if (dArgs.OldValue == null && dArgs.NewValue != null) SetCustomTitleBinding(dc);
        }

        private void Textblock_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => TriggerEdit();

        private void Textbox_OnLostFocus(object sender, RoutedEventArgs e) => UpdateName();

        private void UpdateName()
        {
            if (Textblock.Text.Equals(Textbox.Text))
            {
                Textblock.Visibility = Visibility.Visible;
                Textbox.Visibility = Visibility.Collapsed;
                return;
            }

            Textblock.Text = Textbox.Text;
            if (!HasBeenCustomRenamed)
            {
                HasBeenCustomRenamed = true;
                var dc = (DocumentController) DataContext;
                dc.SetField<TextController>(KeyStore.PresentationTitleKey, Textbox.Text, true);
                SetCustomTitleBinding(dc);
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
            if (!(args.NewValue is DocumentController dc)) return;

            string currentTitle = dc.GetDereferencedField(KeyStore.TitleKey, null).GetValue(null).ToString();

            if (string.IsNullOrEmpty(currentTitle))
            {
                dc.SetField(KeyStore.TitleKey, new TextController("<untitled>"), true);
            }

            if (HasBeenCustomRenamed || dc.GetField(KeyStore.PresentationTitleKey) != null) SetCustomTitleBinding(dc);
            else SetTitleBinding(dc);
        }

        public void ResetTitle()
        {
            HasBeenCustomRenamed = false;
            SetTitleBinding((DocumentController) DataContext);
        }

        private void SetTitleBinding(DocumentController dc)
        {
            dc.SetField(KeyStore.PresentationTitleKey, null, true);
            var initialBinding = new FieldBinding<TextController>
            {
                Document = dc,
                Key = KeyStore.TitleKey,
                Mode = BindingMode.OneWay
            };
            Textbox.AddFieldBinding(TextBox.TextProperty, initialBinding);
            Textblock.AddFieldBinding(TextBlock.TextProperty, initialBinding);
        }

        private void SetCustomTitleBinding(DocumentController dc)
        {
            var renamedBinding = new FieldBinding<TextController>
            {
                Document = dc,
                Key = KeyStore.PresentationTitleKey,
                Mode = BindingMode.TwoWay
            };
            Textblock.AddFieldBinding(TextBlock.TextProperty, renamedBinding);
            Textbox.AddFieldBinding(TextBox.TextProperty, renamedBinding);
        }
    }
}
