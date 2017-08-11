using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class InkSelectionRect : UserControl
    {
        public CollectionFreeformView FreeformView;
        public InkStrokeContainer Strokes;
        public InkSelectionRect(CollectionFreeformView view, InkStrokeContainer strokes)
        {
            FreeformView = view;
            Strokes = strokes;
            this.InitializeComponent();
            Loaded += OnLoaded;

            ManipulationControls bottomRightControls = new ManipulationControls(BottomRightDragger, true, false);
            bottomRightControls.AddAllAndHandle();
            bottomRightControls.OnManipulatorTranslatedOrScaled += BottomRightControlsOnOnManipulatorTranslatedOrScaled;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            SizeChanged += Grid_OnSizeChanged;
        }

        private void BottomRightControlsOnOnManipulatorTranslatedOrScaled(TransformGroupData e)
        {
            var max = Math.Max(e.Translate.X, e.Translate.Y);
            Width += max;
            Height += max;
        }

        double RectStrokeThickness => 2 / FreeformView.CanvasScale;

        private void Grid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Matrix3x2 scale = Matrix3x2.CreateScale((float)(e.NewSize.Width / e.PreviousSize.Width), (float)(e.NewSize.Height / e.PreviousSize.Height));
            foreach (var stroke in Strokes.GetStrokes())
            {
                if (stroke.Selected)
                {
                    stroke.PointTransform *= scale;
                    stroke.DrawingAttributes.Size = new Size(stroke.DrawingAttributes.Size.Width, stroke.DrawingAttributes.Size.Height * scale) *= scale;
                }
               
            }
        }
    }
}
