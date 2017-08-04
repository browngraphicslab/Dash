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
    public partial class EditableImage
    {
        public Image Image { get { return xImage; } }

        public Rect ClipRect { get; set; } = new Rect(0, 0, 0, 0);

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

                if (value)
                {
                    xImageGrid.ManipulationDelta += xImageGrid_ManipulationDelta;
                    _imageManipulator.OnManipulatorTranslatedOrScaled += ImageManipulator_OnManipulatorTranslatedOrScaled;
                    var rect = new Rect(0, 0, Image.ActualWidth, Image.ActualHeight);
                    Image.Clip = new RectangleGeometry { Rect = rect };
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

            UpdateClipRect(0, 0, 200, 200);                                                                     // TODO get a better way to set up image width and height 

            // set up cliprect draggers 
            SetUpDraggersHelper(xCLIPBottomLeftDragger, xCLIPBottomRightDragger, xCLIPTopLeftDragger, xCLIPTopRightDragger);

            // set up image draggers 
            SetUpDraggersHelper(xBottomLeftDragger, xBottomRightDragger, xTopLeftDragger, xTopRightDragger);

            SetUpEvents();
        }

        private void SetUpDraggersHelper(Ellipse bottomLeft, Ellipse bottomRight, Ellipse topLeft, Ellipse topRight)
        {
            // set up cliprect draggers 
            Canvas.SetLeft(bottomLeft, ClipRect.X - 10);
            Canvas.SetTop(bottomLeft, ClipRect.Y + ClipRect.Height - 10);

            Canvas.SetLeft(bottomRight, ClipRect.X + ClipRect.Width - 10);
            Canvas.SetTop(bottomRight, ClipRect.Y + ClipRect.Height - 10);

            Canvas.SetLeft(topLeft, ClipRect.X - 10);
            Canvas.SetTop(topLeft, ClipRect.Y - 10);

            Canvas.SetLeft(topRight, ClipRect.X + ClipRect.Width - 10);
            Canvas.SetTop(topRight, ClipRect.Y - 10);
        }

        private void SetUpEvents()
        {
            //image draggers
            var bottomLeftManipulator = new ManipulationControls(xBottomLeftDragger);
            bottomLeftManipulator.OnManipulatorTranslatedOrScaled += BottomLeftManipulator_OnManipulatorTranslated;
            var bottomRightManipulator = new ManipulationControls(xBottomRightDragger);
            bottomRightManipulator.OnManipulatorTranslatedOrScaled += BottomRightManipulator_OnManipulatorTranslatedOrScaled;
            var topLeftManipulator = new ManipulationControls(xTopLeftDragger);
            topLeftManipulator.OnManipulatorTranslatedOrScaled += TopLeftManipulator_OnManipulatorTranslated;
            var topRightManipulator = new ManipulationControls(xTopRightDragger);
            topRightManipulator.OnManipulatorTranslatedOrScaled += TopRightManipulator_OnManipulatorTranslated;

            // clip rect draggers 
            var m1 = new ManipulationControls(xCLIPBottomLeftDragger);
            m1.OnManipulatorTranslatedOrScaled += CLIPBottomLeftManipulator_OnManipulatorTranslated;
            var m2 = new ManipulationControls(xCLIPBottomRightDragger);
            m2.OnManipulatorTranslatedOrScaled += CLIPBottomRightManipulator_OnManipulatorTranslatedOrScaled;
            var m3 = new ManipulationControls(xCLIPTopLeftDragger);
            m3.OnManipulatorTranslatedOrScaled += CLIPTopLeftManipulator_OnManipulatorTranslated;
            var m4 = new ManipulationControls(xCLIPTopRightDragger);
            m4.OnManipulatorTranslatedOrScaled += CLIPTopRightManipulator_OnManipulatorTranslated;
        }
        
        private void BottomRightManipulator_OnManipulatorTranslatedOrScaled(TransformGroupData e)
        {
            //if (!UpdateClipRect(0, 0, e.Translate.X, e.Translate.Y)) return;
            UpdateImage(0, 0, e.Translate.X, e.Translate.Y); 

            TranslateHelper(e.Translate.X, e.Translate.Y, xBottomRightDragger);
            TranslateHelper(0, e.Translate.Y, xBottomLeftDragger);
            TranslateHelper(e.Translate.X, 0, xTopRightDragger);
        }

        private void TopRightManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            //if (!UpdateClipRect(0, e.Translate.Y, e.Translate.X, -e.Translate.Y)) return;
            UpdateImage(0, e.Translate.Y, e.Translate.X, -e.Translate.Y);

            //move draggers 
            TranslateHelper(e.Translate.X, e.Translate.Y, xTopRightDragger);
            TranslateHelper(0, e.Translate.Y, xTopLeftDragger);
            TranslateHelper(e.Translate.X, 0, xBottomRightDragger);
        }

        private void TopLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            //if (!UpdateClipRect(e.Translate.X, e.Translate.Y, -e.Translate.X, -e.Translate.Y)) return;
            UpdateImage(e.Translate.X, e.Translate.Y, -e.Translate.X, -e.Translate.Y);

            //move draggers  
            TranslateHelper(e.Translate.X, e.Translate.Y, xTopLeftDragger);
            TranslateHelper(0, e.Translate.Y, xTopRightDragger);
            TranslateHelper(e.Translate.X, 0, xBottomLeftDragger);
        }

        private void BottomLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            //if (!UpdateClipRect(e.Translate.X, 0, -e.Translate.X, e.Translate.Y)) return;
            UpdateImage(e.Translate.X, 0, -e.Translate.X, e.Translate.Y);

            //move draggers 
            TranslateHelper(e.Translate.X, e.Translate.Y, xBottomLeftDragger);
            TranslateHelper(0, e.Translate.Y, xBottomRightDragger);
            TranslateHelper(e.Translate.X, 0, xTopLeftDragger);
        }


        private void ImageManipulator_OnManipulatorTranslatedOrScaled(TransformGroupData e)// TODO must update position and width height controllers!!!??????????? 
        {
            var bottomLeft1 = Util.PointTransformFromVisual(new Point(0, Image.ActualHeight), Image, xGrid);
            var bottomRight1 = Util.PointTransformFromVisual(new Point(Image.ActualWidth, Image.ActualHeight), Image, xGrid);
            var topLeft1 = Util.PointTransformFromVisual(new Point(0, 0), Image, xGrid);
            var topRight1 = Util.PointTransformFromVisual(new Point(Image.ActualWidth, 0), Image, xGrid);

            ScaleHelper(e.ScaleCenter, e.ScaleAmount, Image);
            TranslateHelper(e.Translate.X, e.Translate.Y, Image);

            var bottomLeft2 = Util.PointTransformFromVisual(new Point(0, Image.ActualHeight), Image, xGrid);
            var bottomRight2 = Util.PointTransformFromVisual(new Point(Image.ActualWidth, Image.ActualHeight), Image, xGrid);
            var topLeft2 = Util.PointTransformFromVisual(new Point(0, 0), Image, xGrid);
            var topRight2 = Util.PointTransformFromVisual(new Point(Image.ActualWidth, 0), Image, xGrid);

            TranslateHelper(bottomLeft2.X - bottomLeft1.X, bottomLeft2.Y - bottomLeft1.Y, xBottomLeftDragger);
            TranslateHelper(bottomRight2.X - bottomRight1.X, bottomRight2.Y - bottomRight1.Y, xBottomRightDragger);
            TranslateHelper(topLeft2.X - topLeft1.X, topLeft2.Y - topLeft1.Y, xTopLeftDragger);
            TranslateHelper(topRight2.X - topRight1.X, topRight2.Y - topRight1.Y, xTopRightDragger);
        }

        private void UpdateImage(double deltaX, double deltaY, double deltaW, double deltaH)
        {
            Image.Width += deltaW;
            Image.Height += deltaH; 
            TranslateHelper(deltaX, deltaY, Image);
            // TODO must update position and width height controllers!!!??????????? 
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

        private void CLIPBottomRightManipulator_OnManipulatorTranslatedOrScaled(TransformGroupData e)
        {
            if (!UpdateClipRect(0, 0, e.Translate.X, e.Translate.Y)) return;

            //move draggers 
            TranslateHelper(e.Translate.X, e.Translate.Y, xCLIPBottomRightDragger);
            TranslateHelper(0, e.Translate.Y, xCLIPBottomLeftDragger);
            TranslateHelper(e.Translate.X, 0, xCLIPTopRightDragger);
        }

        private void CLIPTopRightManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            if (!UpdateClipRect(0, e.Translate.Y, e.Translate.X, -e.Translate.Y)) return;

            //move draggers 
            TranslateHelper(e.Translate.X, e.Translate.Y, xCLIPTopRightDragger);
            TranslateHelper(0, e.Translate.Y, xCLIPTopLeftDragger);
            TranslateHelper(e.Translate.X, 0, xCLIPBottomRightDragger);
        }

        private void CLIPTopLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            if (!UpdateClipRect(e.Translate.X, e.Translate.Y, -e.Translate.X, -e.Translate.Y)) return;

            //move draggers  
            TranslateHelper(e.Translate.X, e.Translate.Y, xCLIPTopLeftDragger);
            TranslateHelper(0, e.Translate.Y, xCLIPTopRightDragger);
            TranslateHelper(e.Translate.X, 0, xCLIPBottomLeftDragger);
        }

        private void CLIPBottomLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            if (!UpdateClipRect(e.Translate.X, 0, -e.Translate.X, e.Translate.Y)) return;

            //move draggers 
            TranslateHelper(e.Translate.X, e.Translate.Y, xCLIPBottomLeftDragger);
            TranslateHelper(0, e.Translate.Y, xCLIPBottomRightDragger);
            TranslateHelper(e.Translate.X, 0, xCLIPTopLeftDragger);
        }


        /// <summary>
        /// Updates ClipRect and the visual rectangle (xClipRectangle) using the change from manipulation. 
        /// </summary>
        /// <returns> Return true if the manipulation is feasible, false if not </returns>
        public bool UpdateClipRect(double deltaX, double deltaY, double deltaW, double deltaH)
        {
            var width = ClipRect.Width + deltaW;
            var height = ClipRect.Height + deltaH;
            if (width < 0 || height < 0) return false;

            ClipRect = new Rect { X = ClipRect.X + deltaX, Y = ClipRect.Y + deltaY, Width = width, Height = height };
            xClipRectangle.Width = ClipRect.Width;
            xClipRectangle.Height = ClipRect.Height;
            Canvas.SetLeft(xClipRectangle, ClipRect.X);
            Canvas.SetTop(xClipRectangle, ClipRect.Y);
            return true;
        }

        /// <summary>
        /// Brings up the editorview upon doubleclick 
        /// </summary>
        private void xImage_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            IsEditorModeOn = true;
            IsClipRectVisible = true; 
        }

        private void DoneButton_Tapped(object sender, TappedRoutedEventArgs e)                                                          // TODO gotta bind it to the ... the ... the clipcontroller thingy ... 
        {
            IsEditorModeOn = false;
            IsImageDraggerVisible = false;
            IsClipRectVisible = false;

            // accounts for image's position changing  
            var imageLeftTop = Util.PointTransformFromVisual(new Point(0, 0), Image, xGrid);
            Rect clip = new Rect { X = ClipRect.X - imageLeftTop.X, Y = ClipRect.Y - imageLeftTop.Y, Width = ClipRect.Width, Height = ClipRect.Height };

            Image.Clip = new RectangleGeometry { Rect = clip };


        }

        /// <summary>
        /// Toggles Visibility of Image's draggerEllipses 
        /// </summary>
        private void xImageButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (xImageButton.Background == new SolidColorBrush(Colors.SteelBlue)) return;
            xImageButton.Background = new SolidColorBrush(Colors.SteelBlue);
            xClipButton.Background = new SolidColorBrush(Colors.Gray);

            IsImageDraggerVisible = true;
            IsClipRectVisible = false;
        }

        /// <summary>
        /// Toggles Visibility of ClipRect's draggerEllipses 
        /// </summary>
        private void xClipbutton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (xClipButton.Background == new SolidColorBrush(Colors.SteelBlue)) return;
            xClipButton.Background = new SolidColorBrush(Colors.SteelBlue);
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
