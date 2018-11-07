using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Media.SpeechRecognition;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views.Collection
{
    public sealed partial class MarqueeInfo : UserControl
    {
        private Rectangle _marquee;


        public double Height
        {
            get => _marquee.Height;
            set => _marquee.Height = value;
        }
        public double Width
        {
            get => _marquee.Width;
            set => _marquee.Width = value;
        }


        public MarqueeInfo(CollectionFreeformBase col)
        {
            this.InitializeComponent();
            _marquee = new Rectangle()
            {
                Stroke = new SolidColorBrush(Color.FromArgb(200, 66, 66, 66)),
                StrokeThickness = 1.5 / col.Zoom,
                StrokeDashArray = new DoubleCollection { 4, 1 },
                CompositeMode = ElementCompositeMode.SourceOver
            };

            _marquee.AllowFocusOnInteraction = true;
       
            xMarqueeGrid?.Children.Add(_marquee);
        }

        private void Collection_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var collection = this.GetFirstAncestorOfType<CollectionFreeformBase>();
            collection?.TriggerActionFromSelection(VirtualKey.C, true);
            e.Handled = true;
        }

        private async void Group_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var collection = this.GetFirstAncestorOfType<CollectionFreeformBase>();
            collection?.TriggerActionFromSelection(VirtualKey.G, true);
            e.Handled = true;
        }

        private void Collection_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }

        public void AdjustMarquee(double newWidth, double newHeight)
        {
            _marquee.Width = newWidth;
            _marquee.Height = newHeight;
        }
    }
}
