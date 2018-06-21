﻿using System;
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
    public sealed partial class ImageRegionBox : UserControl
    {
        public DocumentController LinkTo;
	    public EditableImage _image;

        public ImageRegionBox()
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

            Column1.Width = new GridLength(column1 * 100, GridUnitType.Star);
            Column2.Width = new GridLength(column2 * 100, GridUnitType.Star);
            Column3.Width = new GridLength(column3 * 100, GridUnitType.Star);
            Row1.Height = new GridLength(row1 * 100, GridUnitType.Star);
            Row2.Height = new GridLength(row2 * 100, GridUnitType.Star);
            Row3.Height = new GridLength(row3 * 100, GridUnitType.Star);
        }

	    private void XCloseRegionButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
	    {
			//deletes the selected region (if the XClose button is pressed, the selected region will always be the desired one)
		    _image?.DeleteRegion(_image._selectedRegion);
			Debug.WriteLine("CLOSING");
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