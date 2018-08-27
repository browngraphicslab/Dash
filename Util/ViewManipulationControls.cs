using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Point = Windows.Foundation.Point;

namespace Dash
{
    

    /// <summary>
    /// Instantiations of this class in a UserControl element will give that
    /// control's selected UIElement the ability to be moved and zoomed based on
    /// interactions with its given handleControl grid.
    /// </summary>
    public class ViewManipulationControls : IDisposable
    {
        private bool _processManipulation;
        private readonly CollectionFreeformBase _freeformView;
        public double MinScale { get; set; } = .2;
        public double MaxScale { get; set; } = 5.0;

        private List<PointerPoint> _deltas = new List<PointerPoint>();

        public bool IsScaleDiscrete = false;
        private double _elementScale = 1.0;
        public double ElementScale
        {
            get => _elementScale;
            set
            {
                _elementScale = value;
                _freeformView.ViewModel.PrevScale = value;
            }
        }

        public PointerDeviceType BlockedInputType { get; set; }
        public bool FilterInput { get; set; }

        public delegate void OnManipulatorTranslatedHandler(TransformGroupData transformation, bool isAbsolute);
        public event OnManipulatorTranslatedHandler OnManipulatorTranslatedOrScaled;

        private bool IsMouseScrollOn => SettingsView.Instance.MouseScrollOn == SettingsView.MouseFuncMode.Scroll; 

        /// <summary>
        /// Created a manipulation control to move element
        /// NOTE: bounds checking is done relative to element.Parent so the element must be in an element with the proper size for bounds checking
        /// </summary>
        /// <param name="element">The element to add manipulation to</param>
        /// <param name="doesRespondToManipulationDelta"></param>
        /// <param name="doesRespondToPointerWheel"></param>
        /// <param name="borderRegions"></param>
        public ViewManipulationControls(CollectionFreeformBase element)
        {
            _freeformView = element;
            _processManipulation = true; 
            element.ManipulationDelta += ElementOnManipulationDelta;
            element.PointerWheelChanged += ElementOnPointerWheelChanged;
            element.ManipulationMode = ManipulationModes.All;
            element.ManipulationStarted += ElementOnManipulationStarted;
            element.ManipulationInertiaStarting += (sender, args) => args.TranslationBehavior.DesiredDeceleration = 0.02;
            element.ManipulationCompleted += (sender, args) => args.Handled = true;
        }

        private void ElementOnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;

            if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control) ^ IsMouseScrollOn) //scroll
            {
                var scrollAmount = e.GetCurrentPoint(_freeformView).Properties.MouseWheelDelta / 3.0f;
                var x = e.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift) ? scrollAmount  : 0;
                OnManipulatorTranslatedOrScaled?.Invoke(
                    new TransformGroupData(new Point(x, scrollAmount - x), new Point(1, 1)), false);
            }
            else //scale
            {
                PointerPoint point = e.GetCurrentPoint(_freeformView);

                // get the scale amount from the mousepoint in canvas space
                float scaleAmount = e.GetCurrentPoint(_freeformView).Properties.MouseWheelDelta >= 0 ? 1.07f : 1 / 1.07f;
                
                if (!IsScaleDiscrete)
                    //Clamp the scale factor 
                    ElementScale *= scaleAmount;

                if (!ClampScale(scaleAmount))
                    OnManipulatorTranslatedOrScaled?.Invoke(
                        new TransformGroupData(new Point(), new Point(scaleAmount, scaleAmount), point.Position),
                        false);

                foreach (var view in _freeformView.GetDescendantsOfType<DocumentView>())
                {
                    view.UpdateResizers();
                }
            }
        }

        public void ElementOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (_freeformView.ManipulationMode == ManipulationModes.None || (e.PointerDeviceType == BlockedInputType && FilterInput))
            {
                e.Complete();
                _processManipulation = false;
            } else
                _processManipulation = true;
            e.Handled = true;
        }

        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event.
        /// </summary>
        private void ElementOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
           
            if (MenuToolbar.Instance.GetMouseMode() == MenuToolbar.MouseMode.PanFast || _freeformView.IsRightBtnPressed() || _freeformView.IsCtrlPressed())
            {
                var pointerPosition = MainPage.Instance.TransformToVisual(_freeformView.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(new Point());
                var pointerPosition2 = MainPage.Instance.TransformToVisual(_freeformView.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(e.Delta.Translation);
                var delta = new Point(pointerPosition2.X - pointerPosition.X, pointerPosition2.Y - pointerPosition.Y);

                if (_processManipulation)
                {
                    if (!IsScaleDiscrete)
                        ElementScale *= e.Delta.Scale;
                    if (!ClampScale(e.Delta.Scale))
                    {
                        OnManipulatorTranslatedOrScaled?.Invoke(
                            new TransformGroupData(delta, new Point(e.Delta.Scale, e.Delta.Scale), e.Position), false);
                    }
                }
                e.Handled = true;
            }
        }
        
        public void Dispose()
        {
            _freeformView.ManipulationDelta -= ElementOnManipulationDelta;
            _freeformView.PointerWheelChanged -= ElementOnPointerWheelChanged;
        }
        private bool ClampScale(double scaleFactor)
        {
            if (ElementScale > MaxScale)
            {
                ElementScale = MaxScale;
                return scaleFactor > 1;
            }

            if (ElementScale < MinScale)
            {
                ElementScale = MinScale;
                return scaleFactor < 1;
            }
            return false;
        }
    }
}
