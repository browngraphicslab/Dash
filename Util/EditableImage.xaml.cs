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
        #region FIELDS 
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

                if (value)
                {
                    xImageButton.Background = new SolidColorBrush(Colors.SteelBlue);
                    ClipController.Data = new Rect(0, 0, Image.ActualWidth, Image.ActualHeight); 
                }
                else xImageButton.Background = new SolidColorBrush(Colors.Gray);
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

                if (value) xClipButton.Background = new SolidColorBrush(Colors.SteelBlue);
                else xClipButton.Background = new SolidColorBrush(Colors.Gray);
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

                if (value)
                {
                    _imageManipulator.AddAllAndHandle();
                    _imageManipulator.OnManipulatorTranslatedOrScaled += ImageManipulator_OnManipulatorTranslatedOrScaled;
                    // show the entire image 
                    ClipController.Data = new Rect(0, 0, Image.ActualWidth, Image.ActualHeight);
                }
                else
                {
                    _imageManipulator.OnManipulatorTranslatedOrScaled -= ImageManipulator_OnManipulatorTranslatedOrScaled;
                    _imageManipulator.RemoveAllAndDontHandle(); 
                }
            }
        }

        public DocumentController DocController { get; }
        public Context Context { get; }

        public RectFieldModelController ClipController
        {
            get { return DocController.GetDereferencedField(ImageBox.ClipKey, Context) as RectFieldModelController; }
        }
#endregion

        private ManipulationControls _imageManipulator;

        public EditableImage(DocumentController docController, Context context)
        {
            InitializeComponent();
            DocController = docController;
            Context = context; 
            _imageManipulator = new ManipulationControls(Image);

            IsClipRectVisible = false;
            IsImageDraggerVisible = false;
            IsEditorModeOn = false;

            double width = 200;
            double height = 200;
            var container = this.GetFirstAncestorOfType<SelectableContainer>(); 
            if (container != null)
            {
                width = container.Width;
                height = container.Height;
            }
            UpdateClipRect(0, 0, width, height);                                                                     // TODO get a better way to set up image width and height 

            // set up cliprect draggers 
            SetUpDraggersHelper(xCLIPBottomLeftDragger, xCLIPBottomRightDragger, xCLIPTopLeftDragger, xCLIPTopRightDragger);
            // set up image draggers 
            SetUpDraggersHelper(xBottomLeftDragger, xBottomRightDragger, xTopLeftDragger, xTopRightDragger);

            SetUpEvents();
        }

        #region SETUP
        private void SetUpDraggersHelper(Ellipse bottomLeft, Ellipse bottomRight, Ellipse topLeft, Ellipse topRight)
        {
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
        #endregion 

        #region IMAGE TRANSFORMATIONS  
        private void BottomRightManipulator_OnManipulatorTranslatedOrScaled(TransformGroupData e)
        {
            if (!UpdateImage(0, 0, e.Translate.X, e.Translate.Y)) return;

            TranslateHelper(e.Translate.X, e.Translate.Y, xBottomRightDragger);
            TranslateHelper(0, e.Translate.Y, xBottomLeftDragger);
            TranslateHelper(e.Translate.X, 0, xTopRightDragger);
        }

        private void TopRightManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            if (!UpdateImage(0, e.Translate.Y, e.Translate.X, -e.Translate.Y)) return;

            //move draggers 
            TranslateHelper(e.Translate.X, e.Translate.Y, xTopRightDragger);
            TranslateHelper(0, e.Translate.Y, xTopLeftDragger);
            TranslateHelper(e.Translate.X, 0, xBottomRightDragger);
        }

        private void TopLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            if (!UpdateImage(e.Translate.X, e.Translate.Y, -e.Translate.X, -e.Translate.Y)) return;

            //move draggers  
            TranslateHelper(e.Translate.X, e.Translate.Y, xTopLeftDragger);
            TranslateHelper(0, e.Translate.Y, xTopRightDragger);
            TranslateHelper(e.Translate.X, 0, xBottomLeftDragger);
        }

        private void BottomLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            if (!UpdateImage(e.Translate.X, 0, -e.Translate.X, e.Translate.Y)) return;

            //move draggers 
            TranslateHelper(e.Translate.X, e.Translate.Y, xBottomLeftDragger);
            TranslateHelper(0, e.Translate.Y, xBottomRightDragger);
            TranslateHelper(e.Translate.X, 0, xTopLeftDragger);
        }

        /// <summary>
        /// Moves the image and updates the position of image draggers 
        /// </summary>
        private void ImageManipulator_OnManipulatorTranslatedOrScaled(TransformGroupData e)                                         // TODO must update position and width height controllers? 
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

        /// <summary>
        /// Translates the image by deltaX and deltaY; resizes the image by deltaW and deltaH 
        /// </summary>
        private bool UpdateImage(double deltaX, double deltaY, double deltaW, double deltaH)                                            // TODO must update position and width height controllers? 
        {
            var width = Image.ActualWidth + deltaW;
            var height = Image.ActualHeight + deltaH;
            if (width < 0 || height < 0) return false; 

            Image.Width = width;
            Image.Height = height; 
            TranslateHelper(deltaX, deltaY, Image);
            return true; 
        }
#endregion

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

        #region CLIP TRANSFORMATIONS 
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
        #endregion


        /// <summary>
        /// Brings up the editorview upon doubleclick 
        /// </summary>
        private void xImage_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            IsEditorModeOn = true;
            IsClipRectVisible = true; 
        }

        private void DoneButton_Tapped(object sender, TappedRoutedEventArgs e)                                                                                                     
        {
            IsEditorModeOn = false;
            IsImageDraggerVisible = false;
            IsClipRectVisible = false;

            // accounts for image's position changing  
            var imageLeftTop = Util.PointTransformFromVisual(new Point(0, 0), Image, xGrid);
            Rect clip = new Rect { X = NormalizeWidth(ClipRect.X - imageLeftTop.X), Y = NormalizeHeight(ClipRect.Y - imageLeftTop.Y), Width = NormalizeWidth(ClipRect.Width), Height = NormalizeHeight(ClipRect.Height) };

            // updates controllers 
            ClipController.Data = clip;
            var positionController = DocController.GetDereferencedField(DashShared.DashConstants.KeyStore.PositionFieldKey, Context) as PointFieldModelController;
            positionController.Data = new Point(imageLeftTop.X, imageLeftTop.Y); 
            //Image.Clip = new RectangleGeometry { Rect = clip };
        }

        private double NormalizeWidth(double num)
        {
            return (num / Image.ActualWidth) * 100; 
        }

        private double NormalizeHeight(double num)
        {
            return (num / Image.ActualHeight) * 100;
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
    }
}
