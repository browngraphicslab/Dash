using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using DashShared;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace Dash
{
    // Define different parts of the template, this is a convention!
    [TemplatePart(Name = EditableContentName, Type = typeof(ContentControl))]
    [TemplatePart(Name = ContainerName, Type = typeof(UIElement))]
    [TemplatePart(Name = OverlayCanvasName, Type = typeof(Canvas))]
    public class EditableFieldFrame : Control
    {
        /// <summary>
        /// The id associated with this <see cref="EditableFieldFrame"/>
        /// </summary>
        public string DocumentId { get; private set; }


        public delegate void PositionChangedHandler(object sender, double deltaX, double deltaY);
        public event PositionChangedHandler FieldPositionChanged;
        public delegate void SizeChangedHandler(object sender, double newWidth, double newHeight);
        public event SizeChangedHandler FieldSizeChanged;


        // variable names for accessing parts from xaml!
        private const string EditableContentName = "PART_EditableContent";
        private const string ContainerName = "PART_Container";
        private const string OverlayCanvasName = "PART_OverlayCanvas";

        /// <summary>
        /// The container contains the resize handles (<see cref="Thumb"/>s) but is separated from the content in <see cref="EditableContent"/>
        /// so <see cref="TranslateTransform"/> applied to this will only be applied to the resize handles.
        /// </summary>
        public FrameworkElement Container;

        /// <summary>
        /// The overlay canvas used to display the thumbs
        /// </summary>
        private Canvas _overlayCanvas;

        /// <summary>
        /// Whether or not the editable field frame is currently selected. SHOULD ONLY BE SET THROUGH THE PUBLIC IsSelected boolean.
        /// </summary>
        private bool _isSelected;

        private Shape _centerResizeHandle;

        /// <summary>
        /// Whether or not the editable field frame is currently selected, if it is, then it can display UI for editing
        /// otherwise that UI is hidden
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    UpdateUiToMatchSelectionValue(); // if the value changes we update the UI to match the selection value
                }
            }
        }

        //TODO move these backing colors to App.xaml
        private readonly Color _visibleBorderColor = Colors.CornflowerBlue;
        private readonly Color _hiddenBordercolor = Colors.Transparent;
        private readonly Thickness _borderThickness = new Thickness(1);

        /// <summary>
        /// Describe the various possible positions for <see cref="Thumb"/>s on the <see cref="EditableFieldFrame"/>
        /// </summary>
        private enum ResizeHandlePositions
        {
            LeftLower,
            LeftUpper,
            RightLower,
            RightUpper,
            Center
        }

        /// <summary>
        /// Dictionary of <see cref="Thumb"/>'s (resize handle UI) to positions (<see cref="ResizeHandlePositions"/>)
        /// </summary>
        private Dictionary<Shape, ResizeHandlePositions> _resizeHandleToPosition = new Dictionary<Shape, ResizeHandlePositions>();

        /// <summary>
        /// The inner content of the editable field frame can be anything!
        /// </summary>
        public object EditableContent
        {
            get { return (object)GetValue(EditableContentProperty); }
            set { SetValue(EditableContentProperty, value); }
        }

        /// <summary>
        /// Dependency property for the inner content (<see cref="EditableContent"/>) of the editable field frame
        /// </summary>
        public static readonly DependencyProperty EditableContentProperty = DependencyProperty.Register(
            "EditableContent", typeof(object), typeof(EditableFieldFrame), new PropertyMetadata(default(object)));

        /// <summary>
        /// Create a new <see cref="EditableFieldFrame"/> for the UI described by the passed in <paramref name="documentId"/>
        /// </summary>
        /// <param name="documentId"></param>
        public EditableFieldFrame(string documentId)
        {
            DocumentId = documentId;
            DefaultStyleKey = typeof(EditableFieldFrame);
        }

        /// <summary>
        /// On apply template we add events and get parts from xaml
        /// </summary>
        protected override void OnApplyTemplate()
        {
            // get the container private variable
            Container = GetTemplateChild(ContainerName) as FrameworkElement;
            Debug.Assert(Container != null);

            // get the overlay canvas which the thumbs are placed on top of
            _overlayCanvas = GetTemplateChild(OverlayCanvasName) as Canvas;
            Debug.Assert(_overlayCanvas != null);

            // Create all the thumbs, this doesn't position them
            InstantiateResizeHandles();

            // Add an event to layout the thumbs when the container changes size.
            Container.SizeChanged += _container_SizeChanged;
        }

        /// <summary>
        /// Called whenever the <see cref="Container"/> changes size, lays out all the resize handles (<see cref="Thumb"/>s)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _container_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            LayoutResizeHandles();
        }

        /// <summary>
        /// Creates all the resize handles, stores references to them, but doesn't position them
        /// </summary>
        private void InstantiateResizeHandles()
        {
            var leftLowerResizeHandle = InstantiateThumb(18,18,1);
            _overlayCanvas.Children.Add(leftLowerResizeHandle);
            _resizeHandleToPosition.Add(leftLowerResizeHandle, ResizeHandlePositions.LeftLower);

            var leftUpperResizeHandle = InstantiateThumb(18,18,1);
            _overlayCanvas.Children.Add(leftUpperResizeHandle);
            _resizeHandleToPosition.Add(leftUpperResizeHandle, ResizeHandlePositions.LeftUpper);

            var rightLowerResizeHandle = InstantiateThumb(18,18,1);
            _overlayCanvas.Children.Add(rightLowerResizeHandle);
            _resizeHandleToPosition.Add(rightLowerResizeHandle, ResizeHandlePositions.RightLower);

            var rightUpperResizeHandle = InstantiateThumb(18,18,1);
            _overlayCanvas.Children.Add(rightUpperResizeHandle);
            _resizeHandleToPosition.Add(rightUpperResizeHandle, ResizeHandlePositions.RightUpper);

            _centerResizeHandle = InstantiateThumb(Width,Height,0.2);
            _overlayCanvas.Children.Add(_centerResizeHandle);
            _resizeHandleToPosition.Add(_centerResizeHandle, ResizeHandlePositions.Center);

        }

        /// <summary>
        /// Lays out all the resize handles, should be called whenever the size of the frame changes
        /// </summary>
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
                        Canvas.SetTop(handle, Container.ActualHeight);
                        break;
                    case ResizeHandlePositions.LeftUpper:
                        Canvas.SetLeft(handle, 0 - handle.Width);
                        Canvas.SetTop(handle, 0 - handle.Height);
                        break;
                    case ResizeHandlePositions.RightLower:
                        Canvas.SetLeft(handle, Container.ActualWidth);
                        Canvas.SetTop(handle, Container.ActualHeight);
                        break;
                    case ResizeHandlePositions.RightUpper:
                        Canvas.SetLeft(handle, Container.ActualWidth);
                        Canvas.SetTop(handle, 0 - handle.Height);
                        break;
                    case ResizeHandlePositions.Center:
//                        Canvas.SetLeft(handle, Container.ActualWidth / 2 - handle.Width / 2);
//                        Canvas.SetTop(handle, Container.ActualHeight / 2 - handle.Height / 2);
                        Canvas.SetLeft(handle,0);
                        Canvas.SetTop(handle,0);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Updates the UI to match whether or not the <see cref="EditableFieldFrame"/> is selected
        /// </summary>
        private void UpdateUiToMatchSelectionValue()
        {
            DisplayResizeHandles();
            DisplayBorder();
        }

        /// <summary>
        /// Displays or hides the border around the <see cref="EditableFieldFrame"/> based on current state (selection)
        /// </summary>
        private void DisplayBorder()
        {
            BorderBrush = IsSelected
                ? new SolidColorBrush(_visibleBorderColor)
                : new SolidColorBrush(_hiddenBordercolor);
            BorderThickness = _borderThickness;
        }

        /// <summary>
        /// Displays or hides the resize handles around the <see cref="EditableFieldFrame"/> based on current state (selection)
        /// </summary>
        private void DisplayResizeHandles()
        {
            foreach (var kvp in _resizeHandleToPosition)
            {
                kvp.Key.Visibility = IsSelected ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Create a new <see cref="Thumb"/> to be used as a resize handle, using some xaml properties
        /// </summary>
        /// <returns></returns>
        private Shape InstantiateThumb(double width, double height, double opacity)
        {
            // TODO this could be extracted to app.xaml
            Shape thumb = null;
            if (opacity == 1)
            {
                thumb = new Ellipse()
                {
                    Height = height,
                    Width = width,
                    Opacity = opacity,
                    Fill = new SolidColorBrush(Colors.AliceBlue),
                    Stroke = new SolidColorBrush(_visibleBorderColor),
                    Visibility = Windows.UI.Xaml.Visibility.Collapsed
                };
            }
            else
            {
                thumb = new Rectangle()
                {
                    Height = height,
                    Width = width,
                    Opacity = opacity,
                    Fill = new SolidColorBrush(Colors.AliceBlue),
                    Stroke = new SolidColorBrush(_visibleBorderColor),
                    Visibility = Windows.UI.Xaml.Visibility.Collapsed
                };
            }
            thumb.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            thumb.ManipulationDelta += ResizeHandleOnManipulationDelta;

            return thumb;
        }

        /// <summary>
        /// Called whenever a thumb is manipulated, calculates the position and size deltas, and applies them to the frame.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResizeHandleOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {    
            var handle = sender as Shape;
            Debug.Assert(handle != null);
            var position = _resizeHandleToPosition[handle];

            // hold position and size deltas in variables
            double widthDelta = 0;
            double heightDelta = 0;
            double transXDelta = 0;
            double transYDelta = 0;

            // calculate position and size deltas based on the resize handle that was manipulated
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

            // apply the position delta to the Container's render transform
            var currentTranslateTransform = Container.RenderTransform as TranslateTransform;
            Debug.Assert(currentTranslateTransform != null, "we assume the render transform is a translate transform, if that assumption is false we need to change this code.");

            Container.RenderTransform = new TranslateTransform()
            {
                X = currentTranslateTransform.X + transXDelta,
                Y = currentTranslateTransform.Y + transYDelta,
            };

            // apply the size delta to the entire Width and Height of this editable field frame and the center thumb
            //TODO provide minimum width and height
            // restricts min size to 5x5
            // issue: handles other than the lower right handle are moving the frame when min size is reached
            Width = ActualWidth + widthDelta > 5 ? ActualWidth + widthDelta : 5;
            Height = ActualHeight + heightDelta > 5 ? ActualHeight + heightDelta : 5;
//            Width = ActualWidth + widthDelta;
//            Height = ActualHeight + heightDelta;
            // center handle doesn't respond to size changes made using the settings pane
            _centerResizeHandle.Width = Width;
            _centerResizeHandle.Height = Height;

            // invoke events
            // TODO these could probably be removed...
            FieldPositionChanged?.Invoke(this, transXDelta, transYDelta);
            FieldSizeChanged?.Invoke(this, Width, Height);

            e.Handled = true;
        }
    }
}
