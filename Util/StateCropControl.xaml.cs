using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Annotations;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class StateCropControl : UserControl, INotifyPropertyChanged
    {
        #region Constructors

        public StateCropControl()
        {
        }

        public StateCropControl(DocumentController docCtrl, EditableImage editableImage)
        {
            InitializeComponent();
            ImageBase = editableImage;
            DocController = docCtrl;
            Loaded += StateCropControl_Loaded;
        }

        #endregion

        #region variables
        
        private EditableImage _imageBase;

        public Brush Fill { get; } = new SolidColorBrush(Color.FromArgb(55, 255, 255, 255));

        public EditableImage ImageBase
        {
            get => _imageBase;
            set
            {
                _imageBase = value;
                OnPropertyChanged();
            }
        }

        public DocumentController DocController { get; set; }

        public double ImageCenterY => (ImageBase.ActualHeight - xLeft.Height) / 2;

        public double ImageCenterX => (ImageBase.ActualWidth - xBottom.Width) / 2;

        public double ImageMaxY => ImageBase.ActualHeight - xTop.Height;

        public double ImageMaxX => ImageBase.ActualWidth - xRight.Width;

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        // called by editable image to get the rectangle geometry when performing the crop
        public Rect GetBounds()
        {
            return new Rect(Canvas.GetLeft(xLeft), Canvas.GetTop(xTop), xBounds.Width, xBounds.Height);
        }

        // initializes the cropping guides and cropping box
        private void StateCropControl_Loaded(object sender, RoutedEventArgs e)
        {
            // TODO: stop using canvas as method for x and y positions
            Canvas.SetLeft(xRight, ImageMaxX);
            Canvas.SetTop(xRight, ImageCenterY);
            Canvas.SetLeft(xLeft, 0);
            Canvas.SetTop(xLeft, ImageCenterY);
            Canvas.SetLeft(xTop, ImageCenterX);
            Canvas.SetTop(xTop, 0);
            Canvas.SetLeft(xBottom, ImageCenterX);
            Canvas.SetTop(xBottom, ImageMaxY);

            UpdateRect();
        }

        // updates the cropping boundaries 
        private void UpdateRect()
        {
            // xBounds represents the geometry that we are actually going to crop. logically most important
            xBounds.Width = Canvas.GetLeft(xRight) + xRight.Width - Canvas.GetLeft(xLeft);
            xBounds.Height = Canvas.GetTop(xBottom) + xBottom.Height - Canvas.GetTop(xTop);
            xBounds.RenderTransform = new TranslateTransform
            {
                X = Canvas.GetLeft(xLeft),
                Y = Canvas.GetTop(xTop)
            };
            // xBase represents the shape that represents the geometry we are going to crop. different than xBounds
            Canvas.SetLeft(xBase, Canvas.GetLeft(xLeft));
            Canvas.SetTop(xBase, Canvas.GetTop(xTop));
        }

        #region Manipulation Delta for Cropping Guides
        
        private void XLeft_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // e.handled is required for manipulation delta to work
            e.Handled = true;

            // calculates the new left boundary
            var left = Canvas.GetLeft(xLeft);
            left += e.Delta.Translation.X;
            // checks for validity in new left boundaries
            if (Canvas.GetLeft(xLeft) < 0 || Math.Abs(left - Canvas.GetLeft(xRight)) <= 50) return;
            Canvas.SetLeft(xLeft, left);
            UpdateRect();
        }

        private void XBottom_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;

            var top = Canvas.GetTop(xBottom);
            top += e.Delta.Translation.Y;
            if (Canvas.GetTop(xBottom) + xBottom.Height > ImageBase.Image.ActualHeight ||
                Math.Abs(top - Canvas.GetTop(xTop)) <= 50) return;
            Canvas.SetTop(xBottom, top);
            UpdateRect();
        }

        private void XRight_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;

            var left = Canvas.GetLeft(xRight);
            left += e.Delta.Translation.X;
            if (left + xRight.Width > ImageBase.Image.ActualWidth ||
                Math.Abs(left - Canvas.GetLeft(xLeft)) <= 50) return;
            Canvas.SetLeft(xRight, left);
            UpdateRect();
        }

        private void XTop_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;

            var top = Canvas.GetTop(xTop);
            top += e.Delta.Translation.Y;
            if (Canvas.GetTop(xTop) < 0 || Math.Abs(top - Canvas.GetTop(xBottom)) <= 50) return;
            Canvas.SetTop(xTop, top);
            UpdateRect();
        }

        #endregion

        /// <summary>
        ///     functionality for user to use directional arrow keys to move the
        ///     cropping boundary / cropping box pixel by pixel
        /// </summary>
        public void OnKeyDown(KeyRoutedEventArgs e)
        {
            var dist = 1;
            if ((CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift) & CoreVirtualKeyStates.Down) ==
                CoreVirtualKeyStates.Down)
                dist = 20;
            switch (e.Key)
            {
                case VirtualKey.Left:
                    // check validity before moving
                    if (Canvas.GetLeft(xLeft) - dist > 0)
                    {
                        // update cropping guide information
                        Canvas.SetLeft(xLeft, Canvas.GetLeft(xLeft) - dist);
                        Canvas.SetLeft(xRight, Canvas.GetLeft(xRight) - dist);
                    }

                    break;
                case VirtualKey.Right:
                    if (Canvas.GetLeft(xRight) + xRight.Width + dist < ImageBase.Image.ActualWidth)
                    {
                        Canvas.SetLeft(xLeft, Canvas.GetLeft(xLeft) + dist);
                        Canvas.SetLeft(xRight, Canvas.GetLeft(xRight) + dist);
                    }

                    break;
                case VirtualKey.Up:
                    if (Canvas.GetTop(xTop) - dist > 0)
                    {
                        Canvas.SetTop(xTop, Canvas.GetTop(xTop) - dist);
                        Canvas.SetTop(xBottom, Canvas.GetTop(xBottom) - dist);
                    }

                    break;
                case VirtualKey.Down:
                    if (Canvas.GetTop(xBottom) + xBottom.Height + dist < ImageBase.Image.ActualHeight)
                    {
                        Canvas.SetTop(xTop, Canvas.GetTop(xTop) + dist);
                        Canvas.SetTop(xBottom, Canvas.GetTop(xBottom) + dist);
                    }

                    break;
            }

            // update the rectangle
            UpdateRect();
        }

        // required for all manipulation deltas to start
        private void OnAllManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {

            e.Handled = true;
        }

        // once manipulation completes, re-check the validity of the bounding box
        private void OnAllManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            // TODO: fix bug with hyperextending boundaries when moving fast as heck
            //checks validity on minimum crop
            if (Canvas.GetLeft(xRight) + xRight.Width - Canvas.GetLeft(xLeft) <= 50)
                Canvas.SetLeft(xLeft, Canvas.GetLeft(xRight) - 40);

            if (Canvas.GetTop(xBottom) + xBottom.Height - Canvas.GetTop(xTop) <= 50)
                Canvas.SetTop(xBottom, Canvas.GetTop(xTop) + 40);

            if (Canvas.GetLeft(xLeft) + 40 >= Canvas.GetLeft(xRight))
                Canvas.SetLeft(xRight, Canvas.GetLeft(xLeft) + 40);

            if (Canvas.GetTop(xTop) + 40 >= Canvas.GetTop(xBottom)) Canvas.SetTop(xTop, Canvas.GetTop(xBottom) - 40);

            //checks validity on maximum crop

            if (Canvas.GetLeft(xLeft) < 0) Canvas.SetLeft(xLeft, 0);

            if (Canvas.GetTop(xTop) < 0) Canvas.SetTop(xTop, 0);

            if (Canvas.GetLeft(xRight) + xRight.Width > ImageBase.Image.ActualWidth)
                Canvas.SetLeft(xRight, ImageBase.Image.ActualWidth - xRight.Width);

            if (Canvas.GetTop(xBottom) + xBottom.Height > ImageBase.Image.ActualHeight)
                Canvas.SetTop(xBottom, ImageBase.Image.ActualHeight - xBottom.Height);

            UpdateRect();
        }

        // used to click and drag the cropping box around the image
        private void XBase_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // required :/
            e.Handled = true;

            // calculates the position to move the bounding box to
            var top = Canvas.GetTop(xBase);
            var left = Canvas.GetLeft(xBase);
            top += e.Delta.Translation.Y;
            left += e.Delta.Translation.X;
            // check validity of each side
            if (left < 0 || top < 0 || left + xBounds.Width > ImageBase.Image.ActualWidth ||
                top + xBounds.Height > ImageBase.Image.ActualHeight) return;
            Canvas.SetLeft(xBase, left);
            Canvas.SetTop(xBase, top);

            xBounds.RenderTransform = new TranslateTransform
            {
                X = left,
                Y = top
            };
        }

        // when bounding box is moved, guide positions are not, this updates them.
        private void XBase_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            Canvas.SetLeft(xLeft, Canvas.GetLeft(xBase));
            Canvas.SetLeft(xRight, Canvas.GetLeft(xBase) + xBounds.Width - xRight.Width);
            Canvas.SetTop(xTop, Canvas.GetTop(xBase));
            Canvas.SetTop(xBottom, Canvas.GetTop(xBase) + xBounds.Height - xBottom.Height);
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}