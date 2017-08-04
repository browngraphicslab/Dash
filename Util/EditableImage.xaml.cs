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

        public Rect ClipRect { get; set; } = new Rect(0, 0, 100, 100);

        private bool _isImageDraggerVisible;
        public bool IsImageDraggerVisible
        {
            get { return _isImageDraggerVisible; }
            set
            {
                _isImageDraggerVisible = value;
                Visibility visibility = value ? Visibility.Visible : Visibility.Collapsed;

                //xBottomLeftDragger.Visibility = visibility;
                //xBottomRightDragger.Visibility = visibility;
                //xTopLeftDragger.Visibility = visibility;
                //xTopRightDragger.Visibility = visibility;
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

                if (value)
                {
                    xImageGrid.ManipulationDelta += xImageGrid_ManipulationDelta;
                    _imageManipulator.OnManipulatorTranslatedOrScaled += ImageManipulator_OnManipulatorTranslatedOrScaled;
                }
                else
                {
                    xImageGrid.ManipulationDelta -= xImageGrid_ManipulationDelta;
                    _imageManipulator.OnManipulatorTranslatedOrScaled -= ImageManipulator_OnManipulatorTranslatedOrScaled;
                }
            }
        }

        private ManipulationControls _imageManipulator; 

        public EditableImage()
        {
            InitializeComponent();
            _imageManipulator = new ManipulationControls(Image);

            IsClipRectVisible = false;
            IsImageDraggerVisible = false;
            IsEditorModeOn = false;

            SetUpBindings();
            SetUpEvents();
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
            //image draggers
            //var bottomLeftManipulator = new ManipulationControls(xBottomLeftDragger);
            //bottomLeftManipulator.OnManipulatorTranslatedOrScaled += BottomLeftManipulator_OnManipulatorTranslated;
            //var bottomRightManipulator = new ManipulationControls(xBottomRightDragger);
            //bottomRightManipulator.OnManipulatorTranslatedOrScaled += BottomRightManipulator_OnManipulatorTranslatedOrScaled;
            //var topLeftManipulator = new ManipulationControls(xTopLeftDragger);
            //topLeftManipulator.OnManipulatorTranslatedOrScaled += TopLeftManipulator_OnManipulatorTranslated;
            //var topRightManipulator = new ManipulationControls(xTopRightDragger);
            //topRightManipulator.OnManipulatorTranslatedOrScaled += TopRightManipulator_OnManipulatorTranslated;

            // clip rect draggers 
            var m1 = new ManipulationControls(xCLIPBottomLeftDragger);
            m1.OnManipulatorTranslatedOrScaled += BottomLeftManipulator_OnManipulatorTranslated;
            var m2 = new ManipulationControls(xCLIPBottomRightDragger);
            m2.OnManipulatorTranslatedOrScaled += BottomRightManipulator_OnManipulatorTranslatedOrScaled;
            var m3 = new ManipulationControls(xCLIPTopLeftDragger);
            m3.OnManipulatorTranslatedOrScaled += TopLeftManipulator_OnManipulatorTranslated;
            var m4 = new ManipulationControls(xCLIPTopRightDragger);
            m4.OnManipulatorTranslatedOrScaled += TopRightManipulator_OnManipulatorTranslated;
        }

        private void ImageManipulator_OnManipulatorTranslatedOrScaled(TransformGroupData e)
        {
            ScaleHelper(e.ScaleCenter, e.ScaleAmount, Image); 
            TranslateHelper(e.Translate.X, e.Translate.Y, Image);
        }

        private void ScaleHelper(Point scaleCenter, Point scaleAmount, FrameworkElement element)
        {
            ScaleTransform scale = new ScaleTransform
            {
                CenterX = scaleCenter.X,
                CenterY = scaleCenter.Y,
                ScaleX = scaleAmount.X,
                ScaleY = scaleAmount.Y
            };

            var group = new TransformGroup();
            group.Children.Add(scale);
            group.Children.Add(element.RenderTransform);

            element.RenderTransform = new MatrixTransform { Matrix = group.Value };
        }

        private void TranslateHelper(double deltaX, double deltaY, FrameworkElement element)
        {
            var translate = new TranslateTransform { X = deltaX, Y = deltaY };
            var group = new TransformGroup();
            group.Children.Add(element.RenderTransform);
            group.Children.Add(translate);

            element.RenderTransform = new MatrixTransform { Matrix = group.Value };
        }

        private void BottomRightManipulator_OnManipulatorTranslatedOrScaled(TransformGroupData e)
        {
            //move draggers 
            TranslateHelper(e.Translate.X, e.Translate.Y, xCLIPBottomRightDragger);
            TranslateHelper(0, e.Translate.Y, xCLIPBottomLeftDragger);
            TranslateHelper(e.Translate.X, 0, xCLIPTopRightDragger);


            //ChangeSize(e.Translate.X, e.Translate.Y); 
        }

        private void TopRightManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            //move draggers 
            TranslateHelper(e.Translate.X, e.Translate.Y, xCLIPTopRightDragger);
            TranslateHelper(0, e.Translate.Y, xCLIPTopLeftDragger);
            TranslateHelper(e.Translate.X, 0, xCLIPBottomRightDragger);

            ChangeImagePosition(0, -e.Translate.Y);
            //var sizeChange = ChangeSize(e.Translate.X, -e.Translate.Y);
        }

        private void TopLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            //move draggers  
            TranslateHelper(e.Translate.X, e.Translate.Y, xCLIPTopLeftDragger);
            TranslateHelper(0, e.Translate.Y, xCLIPTopRightDragger);
            TranslateHelper(e.Translate.X, 0, xCLIPBottomLeftDragger);

            //var sizeChange = ChangeSize(-e.Translate.X, -e.Translate.Y);

            //ChangeImagePosition(-sizeChange.X, -sizeChange.Y);
        }

        private void BottomLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            //move draggers 
            TranslateHelper(e.Translate.X, e.Translate.Y, xCLIPBottomLeftDragger);
            TranslateHelper(0, e.Translate.Y, xCLIPBottomRightDragger);
            TranslateHelper(e.Translate.X, 0, xCLIPTopLeftDragger);

            //var sizeChange = ChangeSize(-e.Translate.X, e.Translate.Y);

            //ChangeImagePosition(-sizeChange.X, 0);
        }

        /// <summary>
        /// Update position controller; changes the position of the image   
        /// </summary>
        private void ChangeImagePosition(double deltaX, double deltaY)                                                         // TODO holy shit 
        {

            //return new Point(); 
        }

        /// <summary>
        /// Update width and height controller; changes the dimensions of the image 
        /// </summary>
        private Point ChangeSize(double v, double y)                                                                   // TODO holy shit 
        {
            return new Point();
        }

        private void UpdateClipRect(double deltaX, double deltaY, double deltaW, double deltaH)
        {
            ClipRect = new Rect { X = ClipRect.X + deltaX, Y = ClipRect.Y + deltaY, Width = ClipRect.Width + deltaW, Height = ClipRect.Height + deltaH };
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
