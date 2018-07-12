using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Printing3D;
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
    /// <summary>
    /// A generic class for box-like annotations/regions that go over controls.
    /// </summary>
    public sealed partial class RegionBox : UserControl
    {
        public DocumentController LinkTo;
	    public VisualAnnotationManager Manager;
        public Point TopLeftPercentile;
        public Point BottomRightPercentile;

        public RegionBox()
        {
            this.InitializeComponent();
        }

		//sets position of region box in center square of 3x3 grid (the entire grid is the size of the image)
        public void SetPosition(Point topLeftPoint, Size size, Size imageSize)
        {
            var row1 = topLeftPoint.Y / imageSize.Height;
            var row2 = size.Height / imageSize.Height;

			//TODO: fix issue regarding why row1 + row2 > 1
	     
            var row3 = 1 - row1 - row2;
            var column1 = topLeftPoint.X / imageSize.Width;
            var column2 = size.Width / imageSize.Width;
            var column3 = 1 - column1 - column2;

            if (row3 < 0 || row3 > 1 || double.IsNaN(row3))
                row3 = 0;
            if (column1 < 0 || column1 > 1 || double.IsNaN(column1))
                column1 = 0;
            if (column2 < 0 || column2 > 1 || double.IsNaN(column2))
                column2 = 0;
            if (column3 < 0 || column3 > 1 || double.IsNaN(column3))
                column3 = 0;

            TopLeftPercentile = new Point(column1, row1);
            BottomRightPercentile = new Point(column2 + column1, row2 + row1);

            Column1.Width = new GridLength(column1 * 100, GridUnitType.Star);
            Column2.Width = new GridLength(column2 * 100, GridUnitType.Star);
            Column3.Width = new GridLength(column3 * 100, GridUnitType.Star);
            Row1.Height = new GridLength(row1 * 100, GridUnitType.Star);
            Row2.Height = new GridLength(row2 * 100, GridUnitType.Star);
            Row3.Height = new GridLength(row3 * 100, GridUnitType.Star);
        }

        internal void SetPosition(Point topLeft, Point bottomRight)
        {
            var row1 = topLeft.Y;
            var row3 = 1 - bottomRight.Y;
            var row2 = 1 - row1 - row3;
            var column1 = topLeft.X;
            var column3 = 1 - bottomRight.X;
            var column2 = 1 - column1 - column3;

            TopLeftPercentile = new Point(column1, row1);
            BottomRightPercentile = new Point(column2 + column1, row2 + row1);

            Column1.Width = new GridLength(column1 * 100, GridUnitType.Star);
            Column2.Width = new GridLength(column2 * 100, GridUnitType.Star);
            Column3.Width = new GridLength(column3 * 100, GridUnitType.Star);
            Row1.Height = new GridLength(row1 * 100, GridUnitType.Star);
            Row2.Height = new GridLength(row2 * 100, GridUnitType.Star);
            Row3.Height = new GridLength(row3 * 100, GridUnitType.Star);
        }

        // TODO rewrite this (would need to write DeleteRegion into VisualAnnotationManager)
        private void XCloseRegionButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
	    {
			//deletes the selected region (if the XClose button is pressed, the selected region will always be the desired one)
		    //AnnotationManager?.DeleteRegion(this);
	    }

	    public void Hide()
	    {
		    xRegionBox.Opacity = 0;
	    }

	    public void Show()
	    {
		    xRegionBox.Opacity = 1;
		    xRegionBoxFill.Opacity = 0.1;
	    }
    }
}
