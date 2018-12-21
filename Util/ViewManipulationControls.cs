using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Frame = Microsoft.Office.Interop.Word.Frame;
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
        private readonly CollectionFreeformView _freeformView;
        private bool DraggingDoc;
        private bool IsMouseScrollOn => SettingsView.Instance.MouseScrollOn == SettingsView.MouseFuncMode.Scroll;

        public double MinScale     { get; set; } = .2;
        public double MaxScale     { get; set; } = 5.0;
        public double ElementScale { get; set; } = 1.0;
        public bool   FilterInput  { get; set; }
        public PointerDeviceType BlockedInputType { get; set; }

        public static bool IsPanning;
        public delegate void OnManipulatorTranslatedHandler(TransformGroupData transformation, bool isAbsolute);
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
            element.ManipulationMode             = ManipulationModes.All;
            element.ManipulationDelta           += ElementOnManipulationDelta;
            element.PointerWheelChanged         += ElementOnPointerWheelChanged;
            element.ManipulationStarted         += ElementOnManipulationStarted;
            element.PointerPressed              += Element_PointerPressed;
            element.ManipulationInertiaStarting += (sender, args) => args.TranslationBehavior.DesiredDeceleration = 0.02;
            element.ManipulationCompleted       += (sender, args) => _freeformView.GetDocumentView().ViewModel.DragAllowed = true;
        }

        private void Element_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var docView = _freeformView.GetDocumentView();
            if (docView.AreContentsActive &&
                      !docView.IsShiftPressed() &&
                      !docView.IsCtrlPressed() &&
                      !docView.IsAltPressed() &&
                      !(e.GetCurrentPoint(docView).PointerDevice.PointerDeviceType == BlockedInputType && FilterInput) &&
                      !_freeformView.ParentDocument.ViewModel.LayoutDocument.GetFitToParent())
            {
                docView.ViewModel.DragAllowed = false;
            }
        }

        private void ElementOnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            // bcz: don't zoom the contents of collections when FitToParent is set -- instead, it would be better if the container document size changed...
            if (!_freeformView.ParentDocument.ViewModel.LayoutDocument.GetFitToParent())
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
                    var point = e.GetCurrentPoint(_freeformView);
                    // get the scale amount from the mousepoint in canvas space
                    var scaleAmount = point.Properties.MouseWheelDelta >= 0 ? 1.07f : 1 / 1.07f; 

                    ElementScale *= scaleAmount;
                    
                    OnManipulatorTranslatedOrScaled?.Invoke(
                        new TransformGroupData(new Point(), new Point(scaleAmount, scaleAmount), point.Position), false);
                }
            }
        }
        
        private void DragManipCompletedTouch(object sender, EventArgs e)
        {
            CollectionFreeformView.NumFingers--;
            DraggingDoc = false;
            SelectionManager.DragManipulationCompleted -= DragManipCompletedTouch;
        }

        public void ElementOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            var docView = _freeformView.GetDocumentView();
            if (docView != null && CollectionFreeformView.NumFingers == 1 && e.PointerDeviceType == PointerDeviceType.Touch && !SplitManager.IsRoot(docView.ViewModel) && !DraggingDoc)
            {
                //drag document 
                if (!SelectionManager.IsSelected(docView.ViewModel))
                {
                    SelectionManager.Select(docView, false);
                    SelectionManager.DragManipulationCompleted += DragManipCompletedTouch;
                    DraggingDoc = true;
                    e.Handled = true;
                    e.Complete();
                    SelectionManager.InitiateDragDrop(docView, null);
                }
            }
            e.Handled = true;

        }

        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event.
        /// </summary>
        private void ElementOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (!_freeformView.GetDocumentView().ViewModel.DragAllowed)  // only try to manipulate doc contents if it can't be dragged
            {
                if (_freeformView.IsRightBtnPressed() ||
                    (e.PointerDeviceType == PointerDeviceType.Touch && CollectionFreeformView.NumFingers == 2) || 
                    (e.PointerDeviceType == PointerDeviceType.Touch && CollectionFreeformView.NumFingers == 1 && IsPanning))
                {
                    var pointerPosition = MainPage.Instance.TransformToVisual(_freeformView.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(new Point());
                    var pointerPosition2 = MainPage.Instance.TransformToVisual(_freeformView.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(e.Delta.Translation);
                    var delta = new Point(pointerPosition2.X - pointerPosition.X, pointerPosition2.Y - pointerPosition.Y);

                    ElementScale *= e.Delta.Scale;
                    OnManipulatorTranslatedOrScaled?.Invoke(
                        new TransformGroupData(delta, new Point(e.Delta.Scale, e.Delta.Scale), e.Position), false);
                    
                    IsPanning = true;
                    e.Handled = true;
                }
                else if (e.PointerDeviceType == PointerDeviceType.Touch && CollectionFreeformView.NumFingers == 1 &&
                           SplitManager.IsRoot(_freeformView.GetDocumentView()?.ViewModel))
                {
                    var point = _freeformView.TransformToVisual(_freeformView.SelectionCanvas).TransformPoint(e.Position);
                    //gets funky with nested collections, but otherwise works
                    ////handle touch interactions with just one finger - equivalent to drag without ctr
                    if (_freeformView.StartMarquee(point))
                    {
                        e.Handled = true;
                    }
                }
            }
        }

        public void Dispose()
        {
            _freeformView.ManipulationDelta -= ElementOnManipulationDelta;
            _freeformView.PointerWheelChanged -= ElementOnPointerWheelChanged;
        }
    }
}
