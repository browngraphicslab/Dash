using Microsoft.ProjectOxford.Vision.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using NewControls.Geometry;
using static Dash.NoteDocuments;
using Point = Windows.Foundation.Point;
using System.Collections.ObjectModel;

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

        public delegate void OnManipulatorTranslatedHandler(TransformGroupData transformationDelta);
        public event OnManipulatorTranslatedHandler OnManipulatorTranslatedOrScaled;

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
            element.AddHandler(UIElement.ManipulationCompletedEvent, new ManipulationCompletedEventHandler(ElementOnManipulationCompleted), true);
        }

        private void ElementOnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;

            if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control))
            {
                var point = e.GetCurrentPoint(_freeformView);

                // get the scale amount from the mousepoint in canvas space
                float scaleAmount = e.GetCurrentPoint(_freeformView).Properties.MouseWheelDelta > 0 ? 1.07f : 1 / 1.07f;

                //Clamp the scale factor 
                ElementScale *= scaleAmount;

                if (!ClampScale(scaleAmount))
                    OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(new Point(), new Point(scaleAmount, scaleAmount), point.Position));
            }
            else
            {
                var scrollAmount = e.GetCurrentPoint(_freeformView).Properties.MouseWheelDelta / 3.0f;
                var x = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down) ? scrollAmount : 0;
                OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(new Point(x, scrollAmount-x), new Point(1,1)));
            }
        }
        public void ElementOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (e != null && _freeformView.ManipulationMode == ManipulationModes.None)
            {
                e.Complete();
                return;
            }
            if (e != null && e.PointerDeviceType == BlockedInputType && FilterInput)
            {
                e.Complete();
                _processManipulation = false;
                e.Handled = true;
                return;
            }
            
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
                var containerScale = new Point(e.Delta.Translation.X == 0 ? 0 : (pointerPosition2.X - pointerPosition.X) / e.Delta.Translation.X, e.Delta.Translation.Y == 0 ? 0 : (pointerPosition2.Y - pointerPosition.Y) / e.Delta.Translation.Y);

                var translate = new Point(e.Delta.Translation.X * containerScale.X, e.Delta.Translation.Y * containerScale.Y);

                if (_processManipulation)
                {
                    ElementScale *= e.Delta.Scale;

                    //Clamp the scale factor 
                    if (!ClampScale(e.Delta.Scale))
                    {
                        // translate the entire group except for
                        var transformGroup = new TransformGroupData(translate, new Point(e.Delta.Scale, e.Delta.Scale), e.Position);
                        OnManipulatorTranslatedOrScaled?.Invoke(transformGroup);
                    }
                }
                e.Handled = true;
            }
        }
        public void ElementOnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs manipulationCompletedRoutedEventArgs)
        {
            manipulationCompletedRoutedEventArgs.Handled = true;
        }
        
        public void FitToParent()
        {
            var ff = _freeformView as CollectionFreeformView;
            var par = ff?.Parent as FrameworkElement;
            if (par == null || ff == null)
                return;

            var rect = new Rect(new Point(), new Point(par.ActualWidth, par.ActualHeight)); //  par.GetBoundingRect();

            
            var r = Rect.Empty;
            foreach (var dvm in ff.xItemsControl.ItemsPanelRoot.Children.Select((ic) => (ic as ContentPresenter)?.Content as DocumentViewModel))
            {
                r.Union(dvm?.Content?.GetBoundingRect(par) ?? r);
            }

            if (r != Rect.Empty)
            {
                var trans = new Point(-r.Left - r.Width / 2 + rect.Width / 2, -r.Top);
                var scaleAmt = new Point(rect.Width / r.Width, rect.Width / r.Width);
                if (rect.Width / rect.Height > r.Width / r.Height)
                {
                    scaleAmt = new Point(rect.Height / r.Height, rect.Height / r.Height);
                }
                else
                    trans = new Point(-r.Left + (rect.Width - r.Width) / 2, -r.Top + (rect.Height - r.Height) / 2);

                OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(trans, scaleAmt));
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
