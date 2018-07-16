using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
    /// This goes over the view, in the same grid. Having one is required to instantiate a VisualAnnotationManager.
    /// </summary>
    public sealed partial class AnnotationOverlay : UserControl
    {

        public GridLength PostColumn1Width
        {
            get => xRegionPostManipulationPreview.Column1.Width;
            set => xRegionPostManipulationPreview.Column1.Width = value;
        }
        public GridLength PostColumn2Width
        {
            get => xRegionPostManipulationPreview.Column2.Width;
            set => xRegionPostManipulationPreview.Column2.Width = value;
        }
        public GridLength PostColumn3Width
        {
            get => xRegionPostManipulationPreview.Column3.Width;
            set => xRegionPostManipulationPreview.Column3.Width = value;
        }
        public GridLength PostRow1Height
        {
            get => xRegionPostManipulationPreview.Row1.Height;
            set => xRegionPostManipulationPreview.Row1.Height = value;
        }
        public GridLength PostRow2Height
        {
            get => xRegionPostManipulationPreview.Row2.Height;
            set => xRegionPostManipulationPreview.Row2.Height = value;
        }
        public GridLength PostRow3Height
        {
            get => xRegionPostManipulationPreview.Row3.Height;
            set => xRegionPostManipulationPreview.Row3.Height = value;
        }

        public Thickness DuringMargin {
            get => xRegionDuringManipulationPreview.Margin;
            set => xRegionDuringManipulationPreview.Margin = value;
        }

        public Visibility DuringVisibility
        {
            get => xRegionDuringManipulationPreview.Visibility;
            set => xRegionDuringManipulationPreview.Visibility = value;
        }

        public Visibility PostVisibility
        {
            get => xRegionPostManipulationPreview.Visibility;
            set => xRegionPostManipulationPreview.Visibility = value;
        }

        public AnnotationOverlay()
        {
            this.InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            //UI for preview boxes
            xRegionPostManipulationPreview.xRegionBoxFill.Color = Colors.AntiqueWhite;
            xRegionPostManipulationPreview.xRegionBox.Stroke = new SolidColorBrush(Colors.SaddleBrown);
            xRegionPostManipulationPreview.xRegionBox.StrokeDashArray = new DoubleCollection() { 1, 0 };

            xRegionPostManipulationPreview.xRegionBox.StrokeThickness = 2;
            xRegionPostManipulationPreview.xRegionBoxFill.Opacity = 0.4;
        }

        public Point GetTopLeftPercentile()
        {
            return xRegionPostManipulationPreview.TopLeftPercentile;
		}

	    public Point GetBottomRightPercentile()
	    {
		    return xRegionPostManipulationPreview.BottomRightPercentile;
	    }

		public void AddRegion(RegionBox box)
        {
            xRegionsGrid.Children.Add(box);
        }

        public void RemoveRegion(RegionBox box)
        {
            xRegionsGrid.Children.Remove(box);
        }

        public void SetDuringPreviewSize(Size size)
        {
            xRegionDuringManipulationPreview.Width = size.Width;
            xRegionDuringManipulationPreview.Height = size.Height;
        }

        public void SetPostPreviewSize(Size size)
        {
            xRegionPostManipulationPreview.Width = size.Width;
            xRegionPostManipulationPreview.Height = size.Height;
        }

        public Size GetDuringPreviewSize()
        {
            return new Size(xRegionDuringManipulationPreview.Width, xRegionDuringManipulationPreview.Height);
        }

        public Size GetPostPreviewSize()
        {
            return new Size(xRegionPostManipulationPreview.Width, xRegionPostManipulationPreview.Height);
        }

        public Size GetDuringPreviewActualSize()
        {
            return new Size(xRegionDuringManipulationPreview.ActualWidth, xRegionDuringManipulationPreview.ActualHeight);
        }

        public Size GetPostPreviewActualSize()
        {
            return new Size(xRegionPostManipulationPreview.ActualWidth, xRegionPostManipulationPreview.ActualHeight);
        }

        public void SetRegionBoxPosition(Size totalSize)
        {
            xRegionPostManipulationPreview.SetPosition(
                new Point(xRegionDuringManipulationPreview.Margin.Left, xRegionDuringManipulationPreview.Margin.Top),
                new Size(xRegionDuringManipulationPreview.ActualWidth, xRegionDuringManipulationPreview.ActualHeight),
                totalSize
            );
        }
    }
}
