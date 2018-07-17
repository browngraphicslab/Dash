using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
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

			//TODO: REMOVE MOUSE INPUT - ONLY DID THIS FOR TESTING PURPOSES
            xInkCanvas.InkPresenter.InputDeviceTypes =
                CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;
            xInkCanvas.InkPresenter.StrokesCollected += (sender, args) => UpdateStrokes();
            xInkCanvas.InkPresenter.StrokesErased += (sender, args) => UpdateStrokes();
            SetInkEnabled(false);
        }

        public event EventHandler<IEnumerable<InkStroke>> InkUpdated;

        public void InitStrokes(IEnumerable<InkStroke> strokes)
        {
            xInkCanvas.InkPresenter.StrokeContainer.AddStrokes(strokes);
        }

        private void UpdateStrokes()
        {
            InkUpdated?.Invoke(this, xInkCanvas.InkPresenter.StrokeContainer.GetStrokes());
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            //UI for preview boxes
	        xRegionPostManipulationPreview.ToggleSelectionState(RegionSelectionState.Select);
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

        public void AddCanvasRegion(FrameworkElement element)
        {
            xRegionsCanvas.Children.Add(element);
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
                totalSize);
        }

        public void SetInkEnabled(bool value)
        {
            xInkCanvas.IsHitTestVisible = value;
            xInkCanvas.InkPresenter.IsInputEnabled = value;
        }
    }
}
