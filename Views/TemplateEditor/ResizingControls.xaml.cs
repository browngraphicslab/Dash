using System;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class ResizingControls : UserControl
    {
        private TemplateEditorView _parent;
        private bool _isLeft;

        public ResizingControls()
        {
        }

        public ResizingControls(TemplateEditorView tev)
        {
            this.InitializeComponent();
            _parent = tev;
        }

        // initializes the cropping guides and cropping box
        private void ResizingControl_Loaded(object sender, RoutedEventArgs e)
        {
            var doc = _parent.LayoutDocument.GetField<DocumentController>(KeyStore.DataKey);
            var layout = doc?.GetField<DocumentController>(KeyStore.ActiveLayoutKey);
            double width = _parent.xWorkspace.ActualWidth;
            double height = _parent.xWorkspace.ActualHeight;
            if (layout?.DocumentType.Equals(TemplateBox.DocumentType) ?? false)
            {
                width = doc.GetWidthField().Data < 500 ? doc.GetWidthField().Data : 500;
                height = doc.GetHeightField().Data < 500 ? doc.GetHeightField().Data : 500;
            }
            Canvas.SetLeft(xRight, width - xRight.Width);
            Canvas.SetTop(xRight, (height - xRight.Height) / 2);
            Canvas.SetLeft(xLeft, 0);
            Canvas.SetTop(xLeft, (height - xLeft.Height) / 2);
            Canvas.SetLeft(xTop, (width - xTop.Width) / 2);
            Canvas.SetTop(xTop, 0);
            Canvas.SetLeft(xBottom, (width - xBottom.Width) / 2);
            Canvas.SetTop(xBottom, height - xBottom.Height);

            UpdateRect();
        }

        #region Manipulation Delta for Cropping Guides

        private void XLeft_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;

            var left = Canvas.GetLeft(xRight);
            left -= Util.DeltaTransformFromVisual(e.Delta.Translation, this).X;
            if (left + xRight.Width > 500 ||
                Math.Abs(left - Canvas.GetLeft(xLeft)) <= _parent.Bounds.Width) return;
            Canvas.SetLeft(xRight, left);

            var leftBounds = Canvas.GetLeft(xLeft);
            var rightBounds = Canvas.GetLeft(xRight);

            if (rightBounds > 490)
            {
                Canvas.SetLeft(xRight, 490);
            }

            if (rightBounds - leftBounds <= _parent.Bounds.Width)
            {
                Canvas.SetLeft(xRight, leftBounds + _parent.Bounds.Width);
            }

            UpdateRect();
            //// e.handled is required for manipulation delta to work
            //e.Handled = true;

            //// calculates the new left boundary
            //var left = Canvas.GetLeft(xLeft);
            //left += Util.DeltaTransformFromVisual(e.Delta.Translation, this).X;
            //// checks for validity in new left boundaries
            //if (Canvas.GetLeft(xLeft) < 0 || Math.Abs(left - Canvas.GetLeft(xRight)) <= 70) return;
            //Canvas.SetLeft(xLeft, left);

            //var leftBounds = Canvas.GetLeft(xLeft);
            //var rightBounds = Canvas.GetLeft(xRight);

            //if (leftBounds < 0)
            //{
            //    Canvas.SetLeft(xLeft, 0);
            //}

            //if (rightBounds - leftBounds <= 70)
            //{
            //    Canvas.SetLeft(xLeft, rightBounds - 70);
            //}

            //UpdateRect();
        }

        private void XBottom_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;

            var top = Canvas.GetTop(xBottom);
            top += Util.DeltaTransformFromVisual(e.Delta.Translation, this).Y;
            if (Canvas.GetTop(xBottom) + xBottom.Height > 500 ||
                Math.Abs(top - Canvas.GetTop(xTop)) <= _parent.Bounds.Height) return;
            Canvas.SetTop(xBottom, top);

            var topBounds = Canvas.GetTop(xTop);
            var bottomBounds = Canvas.GetTop(xBottom);

            if (bottomBounds > 490)
            {
                Canvas.SetTop(xBottom, 490);
            }

            if (bottomBounds - topBounds <= _parent.Bounds.Height)
            {
                Canvas.SetTop(xBottom, topBounds + _parent.Bounds.Height);
            }

            UpdateRect();
        }

        private void XRight_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;

            var left = Canvas.GetLeft(xRight);
            left += Util.DeltaTransformFromVisual(e.Delta.Translation, this).X;
            if (left + xRight.Width > 500 ||
                Math.Abs(left - Canvas.GetLeft(xLeft)) <= _parent.Bounds.Width) return;
            Canvas.SetLeft(xRight, left);

            var leftBounds = Canvas.GetLeft(xLeft);
            var rightBounds = Canvas.GetLeft(xRight);

            if (rightBounds > 490)
            {
                Canvas.SetLeft(xRight, 490);
            }

            if (rightBounds - leftBounds <= _parent.Bounds.Width)
            {
                Canvas.SetLeft(xRight, leftBounds + _parent.Bounds.Width);
            }

            UpdateRect();
        }

        private void XTop_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;

            var bottom = Canvas.GetTop(xBottom);
            bottom -= Util.DeltaTransformFromVisual(e.Delta.Translation, this).Y;
            if (Canvas.GetTop(xBottom) + xBottom.Height > 500 ||
                Math.Abs(bottom - Canvas.GetTop(xTop)) <= _parent.Bounds.Height) return;
            Canvas.SetTop(xBottom, bottom);

            var topBounds = Canvas.GetTop(xTop);
            var bottomBounds = Canvas.GetTop(xBottom);

            if (bottomBounds > 490)
            {
                Canvas.SetTop(xBottom, 490);
            }

            if (bottomBounds - topBounds <= _parent.Bounds.Height)
            {
                Canvas.SetTop(xBottom, topBounds + _parent.Bounds.Height);
            }

            UpdateRect();
            //e.Handled = true;

            //var top = Canvas.GetTop(xTop);
            //top += Util.DeltaTransformFromVisual(e.Delta.Translation, this).Y;
            //if (Canvas.GetTop(xTop) < 0 || Math.Abs(top - Canvas.GetTop(xBottom)) <= 70) return;
            //Canvas.SetTop(xTop, top);

            //var topBounds = Canvas.GetTop(xTop);
            //var bottomBounds = Canvas.GetTop(xBottom);

            //if (topBounds < 0)
            //{
            //    Canvas.SetTop(xTop, 0);
            //}

            //if (bottomBounds - topBounds <= 70)
            //{
            //    Canvas.SetTop(xTop, bottomBounds - 70);
            //}

            //UpdateRect();
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
            Canvas.SetLeft(xBounds, Canvas.GetLeft(xLeft));
            Canvas.SetTop(xBounds, Canvas.GetTop(xTop));

            _parent.ResizeCanvas(new Size(xBounds.Width, xBounds.Height));
            _parent.PositionEllipses(_parent.xWorkspace.ActualWidth, _parent.xWorkspace.ActualHeight);
        }

        #endregion

        // required for all manipulation deltas to start
        private void OnAllManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (_isLeft)
            {
                e.Handled = true;
            }
        }

        private void LeftRightPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeWestEast, 0);
        }

        private void AllPointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        private void TopBottomPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeNorthSouth, 0);
        }

        private void OnAllManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (_isLeft)
            {
                e.Handled = true;
            }
           
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint((UIElement)sender).Properties.IsLeftButtonPressed)
            {
                _isLeft = true;
            }
            else
            {
                _isLeft = false;
            }

            e.Handled = true;
        }

        private void Grid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;
        }
    }
}
