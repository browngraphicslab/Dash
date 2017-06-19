using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DashShared;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace Dash
{

    [TemplatePart(Name = EditableContentName, Type = typeof(ContentControl))]
    [TemplatePart(Name = ContainerName, Type = typeof(UIElement))]
    [TemplatePart(Name = OverlayCanvasName, Type = typeof(Canvas))]
    public class EditableFieldFrame : Control
    {
        /// <summary>
        /// The key associated with this editable field frame
        /// </summary>
        public Key Key { get; private set; }


        public delegate void PositionChangedHandler(object sender, double deltaX, double deltaY);
        public event PositionChangedHandler PositionChanged;


        // variable names for accessing parts from xaml!
        private const string EditableContentName = "PART_EditableContent";
        private const string ContainerName = "PART_Container";
        private const string OverlayCanvasName = "PART_OverlayCanvas";

        /// <summary>
        /// Private variable to get the container which determines the size of the window
        /// so we don't have to look for it on manipulation delta
        /// </summary>
        private FrameworkElement _container;

        /// <summary>
        /// The overlay canvas used to display the thumbs
        /// </summary>
        private Canvas _overlayCanvas;

        /// <summary>
        /// Whether or not the editable field frame is currently editing
        /// </summary>
        private bool _isEditing;

        private readonly Color _visibleBorderColor = Colors.CornflowerBlue;
        private readonly Color _hiddenBordercolor = Colors.Transparent;
        private readonly Thickness _borderThickness = new Thickness(1);


        private enum ResizeHandlePositions
        {
            LeftLower,
            LeftUpper,
            RightLower,
            RightUpper,
            Center
        }

        private Dictionary<Thumb, ResizeHandlePositions> _resizeHandleToPosition = new Dictionary<Thumb, ResizeHandlePositions>();

        /// <summary>
        /// The inner content of the window can be anything!
        /// </summary>
        public object EditableContent
        {
            get { return GetValue(EditableContentProperty); }
            set { SetValue(EditableContentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EditableContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EditableContentProperty =
            DependencyProperty.Register("EditableContent", typeof(object), typeof(EditableFieldFrame), new PropertyMetadata(null));

        public EditableFieldFrame(Key key)
        {
            Key = key;
            DefaultStyleKey = typeof(EditableFieldFrame);
        }

        /// <summary>
        /// On apply template we add events and get parts from xaml
        /// </summary>
        protected override void OnApplyTemplate()
        {
            // get the container private variable
            _container = GetTemplateChild(ContainerName) as FrameworkElement;
            Debug.Assert(_container != null);

            _overlayCanvas = GetTemplateChild(OverlayCanvasName) as Canvas;
            Debug.Assert(_overlayCanvas != null);

            InstantiateResizeHandles();

            _container.SizeChanged += _container_SizeChanged;

            _container.Tapped += _container_Tapped;
        }

        private void _container_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            LayoutResizeHandles();
        }

        private void InstantiateResizeHandles()
        {
            var leftLowerResizeHandle = InstantiateThumb();
            _overlayCanvas.Children.Add(leftLowerResizeHandle);
            _resizeHandleToPosition.Add(leftLowerResizeHandle, ResizeHandlePositions.LeftLower);

            var leftUpperResizeHandle = InstantiateThumb();
            _overlayCanvas.Children.Add(leftUpperResizeHandle);
            _resizeHandleToPosition.Add(leftUpperResizeHandle, ResizeHandlePositions.LeftUpper);

            var rightLowerResizeHandle = InstantiateThumb();
            _overlayCanvas.Children.Add(rightLowerResizeHandle);
            _resizeHandleToPosition.Add(rightLowerResizeHandle, ResizeHandlePositions.RightLower);

            var rightUpperResizeHandle = InstantiateThumb();
            _overlayCanvas.Children.Add(rightUpperResizeHandle);
            _resizeHandleToPosition.Add(rightUpperResizeHandle, ResizeHandlePositions.RightUpper);

            var centerResizeHandle = InstantiateThumb();
            _overlayCanvas.Children.Add(centerResizeHandle);
            _resizeHandleToPosition.Add(centerResizeHandle, ResizeHandlePositions.Center);

        }

        private void LayoutResizeHandles()
        {
            foreach (var kvp in _resizeHandleToPosition)
            {
                var handle = kvp.Key;
                var position = kvp.Value;
                switch (position)
                {
                    case ResizeHandlePositions.LeftLower:
                        Canvas.SetLeft(handle, 0 - handle.Width);
                        Canvas.SetTop(handle, _container.ActualHeight);
                        break;
                    case ResizeHandlePositions.LeftUpper:
                        Canvas.SetLeft(handle, 0 - handle.Width);
                        Canvas.SetTop(handle, 0 - handle.Height);
                        break;
                    case ResizeHandlePositions.RightLower:
                        Canvas.SetLeft(handle, _container.ActualWidth);
                        Canvas.SetTop(handle, _container.ActualHeight);
                        break;
                    case ResizeHandlePositions.RightUpper:
                        Canvas.SetLeft(handle, _container.ActualWidth);
                        Canvas.SetTop(handle, 0 - handle.Height);
                        break;
                    case ResizeHandlePositions.Center:
                        Canvas.SetLeft(handle, _container.ActualWidth / 2 - handle.Width / 2);
                        Canvas.SetTop(handle, _container.ActualHeight / 2 - handle.Height / 2);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void _container_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _isEditing = !_isEditing;
            DisplayResizeHandles(_isEditing);
            DisplayBorder(_isEditing);
        }

        private void DisplayBorder(bool isVisible)
        {
            BorderBrush = _isEditing
                ? new SolidColorBrush(_visibleBorderColor)
                : new SolidColorBrush(_hiddenBordercolor);
            BorderThickness = _borderThickness;
        }

        private void DisplayResizeHandles(bool isVisible)
        {
            foreach (var kvp in _resizeHandleToPosition)
            {
                kvp.Key.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private Thumb InstantiateThumb()
        {
            var thumb = new Thumb
            {
                Height = 10,
                Width = 10,
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(_visibleBorderColor),
                Visibility = Visibility.Collapsed
            };

            thumb.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            thumb.ManipulationDelta += ResizeHandleOnManipulationDelta;

            return thumb;
        }

        private void ResizeHandleOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {    
            var handle = sender as Thumb;
            Debug.Assert(handle != null);
            var position = _resizeHandleToPosition[handle];

            double widthDelta = 0;
            double heightDelta = 0;
            double transXDelta = 0;
            double transYDelta = 0;

            switch (position)
            {
                case ResizeHandlePositions.LeftLower:
                    widthDelta = -e.Delta.Translation.X;
                    transXDelta = e.Delta.Translation.X;
                    heightDelta = e.Delta.Translation.Y;
                    break;
                case ResizeHandlePositions.LeftUpper:
                    widthDelta = -e.Delta.Translation.X;
                    transXDelta = e.Delta.Translation.X;
                    heightDelta = -e.Delta.Translation.Y;
                    transYDelta = e.Delta.Translation.Y;
                    break;
                case ResizeHandlePositions.RightLower:
                    widthDelta = e.Delta.Translation.X;
                    heightDelta = e.Delta.Translation.Y;
                    break;
                case ResizeHandlePositions.RightUpper:
                    widthDelta = e.Delta.Translation.X;
                    heightDelta = -e.Delta.Translation.Y;
                    transYDelta = e.Delta.Translation.Y;
                    break;
                case ResizeHandlePositions.Center:
                    transXDelta = e.Delta.Translation.X;
                    transYDelta = e.Delta.Translation.Y;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            Canvas.SetLeft(this, Canvas.GetLeft(this) + transXDelta);
            Canvas.SetTop(this, Canvas.GetTop(this) + transYDelta);

            //TODO provide minimum width and height
            Width = ActualWidth + widthDelta;
            Height = ActualHeight + heightDelta;

            PositionChanged?.Invoke(this, transXDelta, transYDelta);

            e.Handled = true;
        }
    }
}
