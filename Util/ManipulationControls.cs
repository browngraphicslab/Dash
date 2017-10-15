using System;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Dash
{

    /// <summary>
    /// Instantiations of this class in a UserControl element will give that
    /// control's selected UIElement the ability to be moved and zoomed based on
    /// interactions with its given handleControl grid.
    /// </summary>
    public class ManipulationControls : IDisposable
    {

        // == MEMBERS ==


        public double MinScale { get; set; } = .5;
        public double MaxScale { get; set; } = 2.0;
        private bool _disabled;
        private FrameworkElement _element;
        private readonly bool _doesRespondToManipulationDelta;
        private readonly bool _doesRespondToPointerWheel;
        private bool _handle;
        public double ElementScale = 1.0;


        public delegate void OnManipulatorTranslatedHandler(TransformGroupData transformationDelta);
        public event OnManipulatorTranslatedHandler OnManipulatorTranslatedOrScaled;

        public PointerDeviceType BlockedInputType;
        public bool FilterInput;
        private bool _processManipulation;

        /// <summary>
        /// Ensure pointerwheel only changes size of documents when it's selected 
        /// </summary>
        private bool PointerWheelEnabled
        {
            get
            {
                var docView = _element as DocumentView;
                if (docView != null) return docView.IsLowestSelected;
                var colView = _element as CollectionFreeformView;
                return colView == null || colView.IsLowestSelected;
            }
        }

        /// <summary>
        /// Created a manipulation control to move element
        /// NOTE: bounds checking is done relative to element.Parent so the element must be in an element with the proper size for bounds checking
        /// </summary>
        /// <param name="element">The element to add manipulation to</param>
        /// <param name="doesRespondToManipulationDelta"></param>
        /// <param name="doesRespondToPointerWheel"></param>
        public ManipulationControls(FrameworkElement element, bool doesRespondToManipulationDelta, bool doesRespondToPointerWheel)
        {
            _element = element;
            _doesRespondToManipulationDelta = doesRespondToManipulationDelta;
            _doesRespondToPointerWheel = doesRespondToPointerWheel;
            _processManipulation = true;

            if (_doesRespondToManipulationDelta)
            {
                element.ManipulationDelta += ManipulateDeltaMoveAndScale;
            }
            if (_doesRespondToPointerWheel)
            {
                element.PointerWheelChanged += PointerWheelMoveAndScale;
            }
            element.ManipulationMode = ManipulationModes.All;
            element.ManipulationStarted += ElementOnManipulationStarted;
        }

        private void ElementOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (e.PointerDeviceType == BlockedInputType && FilterInput)
            {
                _processManipulation = false;
                e.Handled = true;
                return;
            }
            _processManipulation = true;
        }

        public void AddAllAndHandle()
        {
            if (!_disabled) return;

            if (_doesRespondToManipulationDelta)
            {
                _element.ManipulationDelta -= EmptyManipulationDelta;
                _element.ManipulationDelta += ManipulateDeltaMoveAndScale;
            }

            if (_doesRespondToPointerWheel)
            {
                _element.PointerWheelChanged -= EmptyPointerWheelChanged;
                _element.PointerWheelChanged += PointerWheelMoveAndScale;
            }
            _disabled = false;
        }

        public void RemoveAllButHandle()
        {
            RemoveAllSetHandle(true);
        }

        public void RemoveAllAndDontHandle()
        {
            RemoveAllSetHandle(false);
        }

        private void RemoveAllSetHandle(bool handle)
        {
            if (_disabled) return;

            if (_doesRespondToManipulationDelta)
            {
                _element.ManipulationDelta -= ManipulateDeltaMoveAndScale;
                _element.ManipulationDelta += EmptyManipulationDelta;
            }
            if (_doesRespondToPointerWheel)
            {
                _element.PointerWheelChanged -= PointerWheelMoveAndScale;
                _element.PointerWheelChanged += EmptyPointerWheelChanged;
            }
            _handle = handle;
            _disabled = true;
        }

        // == METHODS ==

        private void PointerWheelMoveAndScale(object sender, PointerRoutedEventArgs e)
        {
            if (PointerWheelEnabled)
            {
                _processManipulation = true;
                TranslateAndScale(e);
            }

        }

        private void EmptyManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = _handle;
        }

        private void EmptyPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = _handle;
        }

        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event.
        /// </summary>
        private void ManipulateDeltaMoveAndScale(object sender, ManipulationDeltaRoutedEventArgs e)
        {

            TranslateAndScale(e);
        }

        private void TranslateAndScale(PointerRoutedEventArgs e)
        {
            if (!_processManipulation) return;
            e.Handled = true;

            //Get mousepoint in canvas space 
            var point = e.GetCurrentPoint(_element);

            // get the scale amount
            float scaleAmount = (float)Math.Pow(1 + 0.15 * Math.Sign(point.Properties.MouseWheelDelta),
                Math.Abs(point.Properties.MouseWheelDelta) / 120.0f);
            scaleAmount = Math.Max(Math.Min(scaleAmount, 1.7f), 0.4f);

            //Clamp the scale factor 
            var newScale = ElementScale * scaleAmount;
            ClampScale(newScale, ref scaleAmount);

            OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(new Point(),
                point.Position, new Point(scaleAmount, scaleAmount)));
        }

        public void FitToParent()
        {
            var par = _element.Parent as ContentControl;
            var ff = _element as CollectionFreeformView;

            var rect = par.GetBoundingRect();
            Rect r = Rect.Empty;
            foreach (var i in ff.xItemsControl.ItemsPanelRoot.Children.Select((ic) => ic as ContentPresenter))
            {
                if (i != null)
                    r.Union((i.Content as DocumentViewModel).Content.GetBoundingRect(par));
            }
            if (r != Rect.Empty)
            {
                var trans = new Point(-r.Left + (rect.Width - r.Width) / 2, -r.Top);
                var scaleAmt = new Point(rect.Width / r.Width, rect.Width / r.Width);
                if (rect.Width / rect.Height > r.Width / r.Height)
                    scaleAmt = new Point(rect.Height / r.Height, rect.Height / r.Height);
                else
                    trans = new Point(-r.Left, -r.Top + (rect.Height - r.Height) / 2);

                OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(trans, new Point(r.Left, r.Top), scaleAmt));
            }
        }

        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event.
        /// </summary>
        /// <param name="canTranslate">Are translate controls allowed?</param>
        /// <param name="canScale">Are scale controls allows?</param>
        /// <param name="e">passed in frm routed event args</param>
        private void TranslateAndScale(ManipulationDeltaRoutedEventArgs e)
        {
            if (!_processManipulation) return;
            var handleControl = VisualTreeHelper.GetParent(_element) as FrameworkElement;
            e.Handled = true;

            // set up translation transform
            var translate = Util.TranslateInCanvasSpace(e.Delta.Translation, handleControl);

            //Clamp the scale factor 
            var scaleFactor = e.Delta.Scale;
            var newScale = ElementScale * scaleFactor;
            ClampScale(newScale, ref scaleFactor);

            // TODO we may need to take into account the _element's render transform here with regards to scale
            OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(new Point(translate.X, translate.Y),
                e.Position, new Point(scaleFactor, scaleFactor)));
        }

        public void Dispose()
        {
            _element.ManipulationDelta -= ManipulateDeltaMoveAndScale;
            _element.ManipulationDelta -= EmptyManipulationDelta;
            _element.PointerWheelChanged -= PointerWheelMoveAndScale;
            _element.PointerWheelChanged -= EmptyPointerWheelChanged;
        }

        private void ClampScale(double newScale, ref float scale)
        {
            if (newScale > MaxScale)
            {
                scale = (float)(MaxScale / ElementScale);
                ElementScale = MaxScale;
            }
            else if (newScale < MinScale)
            {
                scale = (float)(MinScale / ElementScale);
                ElementScale = MinScale;
            }
            else
            {
                ElementScale = newScale;
            }
        }
    }
}
