﻿using System;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
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
        private CollectionFreeformView _freeformView;
        public double MinScale { get; set; } = .2;
        public double MaxScale { get; set; } = 5.0;
        public double ElementScale { get; set; } = 1.0;
        public PointerDeviceType BlockedInputType { get; set; }
        public bool FilterInput { get; set; }

        public delegate void OnManipulatorTranslatedHandler(TransformGroupData transformation, bool isAbsolute);
        public event OnManipulatorTranslatedHandler OnManipulatorTranslatedOrScaled;

        private bool IsMouseScrollOn => SettingsView.Instance.MouseScrollOn; 

        /// <summary>
        /// Created a manipulation control to move element
        /// NOTE: bounds checking is done relative to element.Parent so the element must be in an element with the proper size for bounds checking
        /// </summary>
        /// <param name="element">The element to add manipulation to</param>
        /// <param name="doesRespondToManipulationDelta"></param>
        /// <param name="doesRespondToPointerWheel"></param>
        /// <param name="borderRegions"></param>
        public ViewManipulationControls(CollectionFreeformView element)
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

            if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control) || !IsMouseScrollOn) // scale 
            {
                var point = e.GetCurrentPoint(_freeformView);

                // get the scale amount from the mousepoint in canvas space
                float scaleAmount = e.GetCurrentPoint(_freeformView).Properties.MouseWheelDelta > 0 ? 1.07f : 1 / 1.07f;

                //Clamp the scale factor 
                ElementScale *= scaleAmount;

                if (!ClampScale(scaleAmount))
                    OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(new Point(), new Point(scaleAmount, scaleAmount), point.Position), false);
            }
            else // scroll 
            {
                var scrollAmount = e.GetCurrentPoint(_freeformView).Properties.MouseWheelDelta / 3.0f;
                var x = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down) ? scrollAmount : 0;
                OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(new Point(x, scrollAmount-x), new Point(1,1)), false);
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
            if (Window.Current.CoreWindow.GetKeyState(VirtualKey.RightButton).HasFlag(CoreVirtualKeyStates.Down) ||
                Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
            {
                var pointerPosition = MainPage.Instance.TransformToVisual(_freeformView.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(new Point());
                var pointerPosition2 = MainPage.Instance.TransformToVisual(_freeformView.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(e.Delta.Translation);
                var delta = new Point(pointerPosition2.X - pointerPosition.X, pointerPosition2.Y - pointerPosition.Y);

                if (_processManipulation)
                {
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

        public void FitToParent()
        {
            var par = _freeformView.Parent as FrameworkElement;
            if (par != null)
            {
                var r = Rect.Empty;
                foreach (var dvm in _freeformView.ViewModel.DocumentViewModels)

                {
                    r.Union(dvm.Bounds);
                }
                if (r.Width != 0 && r.Height != 0)
                {
                    var rect     = new Rect(new Point(), new Point(par.ActualWidth, par.ActualHeight));
                    var scaleWidth = r.Width / r.Height > rect.Width / rect.Height;
                    var scaleAmt = scaleWidth ? rect.Width / r.Width : rect.Height / r.Height;
                    var scale    = new Point(scaleAmt, scaleAmt);
                    var trans    = new Point(-r.Left * scaleAmt, -r.Top * scaleAmt);

                    if (scaleAmt != 0)
                        OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(trans, scale), true);
                }
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
