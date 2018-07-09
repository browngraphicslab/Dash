using System;
using System.Collections.Generic;
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
    public sealed partial class PDFRegionMarker : UserControl
    {
        public DocumentController LinkTo;
        public double Offset;
        public Size Size;
        public Point Position;

        public PDFRegionMarker()
        {
            this.InitializeComponent();
        }

        // only used for regions added to the side as a sidebar marker; leave alone if this is for the actual PDF
        public void SetPosition(double scrollTarget, double totalOffset)
        {
            Grid.SetColumn(xRegion, 0);
            Grid.SetColumnSpan(xRegion, 3);
            var upHeight = scrollTarget - 5;
            if (upHeight < 0) upHeight = 0;
            var downHeight = totalOffset - scrollTarget - 5;
            xUp.Height = new GridLength(upHeight / totalOffset, GridUnitType.Star);
            xDown.Height = new GridLength(downHeight / totalOffset, GridUnitType.Star);
            xRegion.Width = 15;
        }

        // only used for regions physically on the page; leave alone if you're trying to add it to the side
        public void SetSize(Size size, Point position, Size totalSize)
        {
            var upRatio = position.Y / totalSize.Height;
            var regionHeightRatio = size.Height / totalSize.Height;
            var downRatio = 1 - upRatio - regionHeightRatio;
            var leftRatio = position.X / totalSize.Width;
            var regionWidthRatio = size.Width / totalSize.Width;
            var rightRatio = 1 - leftRatio - regionWidthRatio;
            Size = size;
            Position = position;

            xUp.Height = new GridLength(upRatio, GridUnitType.Star);
            xRegionRow.Height = new GridLength(regionHeightRatio, GridUnitType.Star);
            xDown.Height = new GridLength(downRatio, GridUnitType.Star);
            xLeft.Width = new GridLength(leftRatio, GridUnitType.Star);
            xRegionColumn.Width = new GridLength(regionWidthRatio, GridUnitType.Star);
            xRight.Width = new GridLength(rightRatio < 0 ? 1 : rightRatio, GridUnitType.Star);
        }

        public void SetColor(SolidColorBrush color)
        {
            xRegion.Fill = color;
        }
    }
}