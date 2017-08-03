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
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    /// <summary>
    /// An image wrapper class that enables image editing (resize, crop) upon double click 
    /// </summary>
    public sealed partial class EditableImage
    {
        public Image Image { get { return xImage; } }

        public Rect ClipRect { get; } = new Rect(30, 30, 100, 100);

        private bool _isImageDraggerVisible;
        public bool IsImageDraggerVisible
        {
            get { return _isImageDraggerVisible; }
            set
            {
                _isImageDraggerVisible = value;
                Visibility visibility = value ? Visibility.Visible : Visibility.Collapsed;

                xBottomLeftDragger.Visibility = visibility;
                xBottomRightDragger.Visibility = visibility;
                xTopLeftDragger.Visibility = visibility;
                xTopRightDragger.Visibility = visibility;
            }
        }

        private bool _isClipRectVisible;
        public bool IsClipRectVisible
        {
            get { return _isClipRectVisible; }
            set
            {
                _isClipRectVisible = value;
                Visibility visibility = value ? Visibility.Visible : Visibility.Collapsed;

                xCLIPBottomLeftDragger.Visibility = visibility;
                xCLIPBottomRightDragger.Visibility = visibility;
                xCLIPTopLeftDragger.Visibility = visibility;
                xCLIPTopRightDragger.Visibility = visibility;
            }
        }

        private bool _isEditorModeOn;
        public bool IsEditorModeOn
        {
            get { return _isEditorModeOn; }
            set
            {
                _isEditorModeOn = value;
                Visibility visibility = value ? Visibility.Visible : Visibility.Collapsed;

                xClipRectangle.Visibility = visibility;
                xEditStackPanel.Visibility = visibility;
                //xShadeRectangle.Visibility = visibility;

                if (value) xImageGrid.ManipulationDelta += xImageGrid_ManipulationDelta; 
                else xImageGrid.ManipulationDelta -= xImageGrid_ManipulationDelta;

            }
        }

        public EditableImage()
        {
            InitializeComponent();

            IsClipRectVisible = false;
            IsImageDraggerVisible = false;
            IsEditorModeOn = false; 

            SetUpBindings();
        }


        private void SetUpBindings()
        {
            // bind the rectangle dimensions to ClipRect 
            var widthBinding = new Binding
            {
                Source = ClipRect,
                Path = new PropertyPath("Width")
            };
            xClipRectangle.SetBinding(WidthProperty, widthBinding);

            var heightBinding = new Binding
            {
                Source = ClipRect,
                Path = new PropertyPath("Height")
            };
            xClipRectangle.SetBinding(HeightProperty, heightBinding);
        }

        private void SetUpEvents()
        {
            //image resize 
            var bottomLeftManipulator = new ManipulationControls(xBottomLeftDragger);
            bottomLeftManipulator.OnManipulatorTranslatedOrScaled += BottomLeftManipulator_OnManipulatorTranslated;
            var bottomRightManipulator = new ManipulationControls(xBottomRightDragger);
            bottomRightManipulator.OnManipulatorTranslatedOrScaled += (e) => ChangeSize(e.Translate.X, e.Translate.Y);
            var topLeftManipulator = new ManipulationControls(xTopLeftDragger);
            topLeftManipulator.OnManipulatorTranslatedOrScaled += TopLeftManipulator_OnManipulatorTranslated;
            var topRightManipulator = new ManipulationControls(xTopRightDragger);
            topRightManipulator.OnManipulatorTranslatedOrScaled += TopRightManipulator_OnManipulatorTranslated;

            // rectangle resize 

        }

        private void TopRightManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            var sizeChange = ChangeSize(e.Translate.X, -e.Translate.Y);
            ChangePosition(0, -sizeChange.Y);
        }

        private void TopLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            var sizeChange = ChangeSize(-e.Translate.X, -e.Translate.Y);
            ChangePosition(-sizeChange.X, -sizeChange.Y);
        }

        private void BottomLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            var sizeChange = ChangeSize(-e.Translate.X, e.Translate.Y);
            ChangePosition(-sizeChange.X, 0);
        }
        
        /// <summary>
        /// Update position controller BUT HOWWWWWW????????????????????????  
        /// </summary>
        private Point ChangePosition(double deltaX, double deltaY)                                                         // TODO holy shit 
        {
            return new Point(); 
        }

        /// <summary>
        /// Update width and height controller 
        /// </summary>
        private Point ChangeSize(double v, double y)                                                                   // TODO holy shit 
        {
            return new Point();
        }

        /// <summary>
        /// Brings up the editorview upon doubleclick 
        /// </summary>
        private void xImage_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            IsEditorModeOn = true; 
        }

        private void DoneButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            IsEditorModeOn = false; 
            IsImageDraggerVisible = false;
            IsClipRectVisible = false;

            // take care of the actual clipping lmaooooo 
            // but how??? gotta bind it to the ... the ... the clipcontroller thingy ... 
        }

        /// <summary>
        /// Toggles Visibility of Image's draggerEllipses 
        /// </summary>
        private void xImageButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (xImageButton.Background == new SolidColorBrush(Colors.Blue)) return;
            xImageButton.Background = new SolidColorBrush(Colors.Blue);
            xClipButton.Background = new SolidColorBrush(Colors.Gray);

            IsImageDraggerVisible = true;
            IsClipRectVisible = false;
        }

        /// <summary>
        /// Toggles Visibility of ClipRect's draggerEllipses 
        /// </summary>
        private void xClipbutton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (xClipButton.Background == new SolidColorBrush(Colors.Blue)) return;
            xClipButton.Background = new SolidColorBrush(Colors.Blue);
            xImageButton.Background = new SolidColorBrush(Colors.Gray);

            IsClipRectVisible = true;
            IsImageDraggerVisible = false;
        }

        private void xImageGrid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true; 
        }

        //    private class RectToGeometryConverter : IValueConverter
        //    {
        //        public object Convert(object value, Type targetType, object parameter, string language)
        //        {
        //            var gg = new GeometryGroup();
        //            gg.Children.Add(new RectangleGeometry { Rect = new Rect(-2000, -2000, 4000, 4000) });
        //            gg.Children.Add(new RectangleGeometry { Rect = (Rect)value });
        //            return gg;
        //        }

        //        public object ConvertBack(object value, Type targetType, object parameter, string language)
        //        {
        //            throw new NotImplementedException();
        //        }
        //    }
    }
}
