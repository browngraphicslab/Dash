using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /// <summary>
    /// This class is the little yellow indicator to the sides of PDFs that shows where annotations have been made.
    /// </summary>
    public sealed partial class PDFRegionMarker : UserControl
    {
        public DocumentController LinkTo;
        public double Offset;
        public Size   Size;
        public Point  Position;

        public PDFRegionMarker()
        {
            this.InitializeComponent();
        }

        public void SetScrollPosition(double scrollTarget, double totalOffset)
        {
            ////Grid.SetColumn(xRegion, 0);
            ////Grid.SetColumnSpan(xRegion, 3);
            var upHeight = scrollTarget - 5;
            if (upHeight < 0)
                upHeight = 0;
            var downHeight = totalOffset - scrollTarget - 5;
            xUp.Height = new GridLength(upHeight / totalOffset, GridUnitType.Star);
            xDown.Height = new GridLength(downHeight / totalOffset, GridUnitType.Star);
            //xRegion.Width = 15;
        }
    }
}
