
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views.Collection
{
    public sealed partial class MarqueeInfo : UserControl
    {
        public MarqueeInfo()
        {
            this.InitializeComponent();
        }

        private void Collection_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var collection = this.GetFirstAncestorOfType<CollectionFreeformBase>();
            collection?.TriggerActionFromSelection(VirtualKey.C, true);
            e.Handled = true;
        }

        private void Group_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var collection = this.GetFirstAncestorOfType<CollectionFreeformBase>();
            collection?.TriggerActionFromSelection(VirtualKey.G, true);
            e.Handled = true;
        }

        private void Collection_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }
    }
}
