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

        public Rectangle Marquee
        {
            get => _marquee;
            set => _marquee = value;
        }

        public MarqueeInfo(CollectionFreeformView col)
        {
            this.InitializeComponent();
            _marquee = new Rectangle()
            {
                Stroke = new SolidColorBrush(Color.FromArgb(200, 66, 66, 66)),
                //StrokeThickness = 1.5 / col.Zoom,
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 4, 1 },
                CompositeMode = ElementCompositeMode.SourceOver
            };

            _marquee.AllowFocusOnInteraction = true;
            //only show collection/group buttons when size > 220
            _marquee.SizeChanged += (sender, args) =>
            {
                if (args.NewSize.Height > 30 && args.NewSize.Width > 220)
                {
                    Collection.Visibility = Visibility.Visible;
                    Group.Visibility = Visibility.Visible;
                }
                else
                {
                    Collection.Visibility = Visibility.Collapsed;
                    Group.Visibility = Visibility.Collapsed;
                }
            };

            _marquee.Height = 0;
            _marquee.Width = 0;
            Collection.Visibility = Visibility.Collapsed;
            Group.Visibility = Visibility.Collapsed;
            xMarqueeGrid?.Children.Add(_marquee);
        }

        private void Collection_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var collection = this.GetFirstAncestorOfType<CollectionFreeformView>();
            collection?.TriggerActionFromSelection(VirtualKey.C, true);
            e.Handled = true;
        }

        private async void Group_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var collection = this.GetFirstAncestorOfType<CollectionFreeformView>();
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
