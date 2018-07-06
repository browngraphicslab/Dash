using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class PresentationViewTextBox : UserControl
    {
        public bool HasBeenCustomRenamed = false;

        public PresentationViewTextBox()
        {
            this.InitializeComponent();
        }

        private void Textblock_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            TriggerEdit();
        }

        private void Textbox_OnLostFocus(object sender, RoutedEventArgs e)
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
            if (args.NewValue is DocumentViewModel dvm)
            {
                SetTitleBinding(dvm);
            }
        }

        public void ResetTitle()
        {
            HasBeenCustomRenamed = false;
            SetTitleBinding((DocumentViewModel) DataContext);
        }

        private void SetTitleBinding(DocumentViewModel dvm)
        {
            var doc = dvm.DocumentController;
            var binding = new FieldBinding<TextController>
            {
                Document = doc,
                Key = KeyStore.TitleKey,
                Mode = BindingMode.OneWay
            };
            Textbox.AddFieldBinding(TextBox.TextProperty, binding);
            Textblock.AddFieldBinding(TextBlock.TextProperty, binding);
        }
    }
}
