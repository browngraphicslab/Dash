using System;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
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
        public double MinScale { get; set; } = .2;
        public double MaxScale { get; set; } = 5.0;
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
        private bool _isManipulating;

        /// <summary>
        /// Ensure pointerwheel only changes size of documents when it's selected 
        /// </summary>
        private bool PointerWheelEnabled
        {
            get
            {
                // if we're on the lowest selecting document view then we can resize it with pointer wheel
                var docView = _element as DocumentView;
                if (docView != null) return docView.IsLowestSelected;

                /*
                var colView = _element as CollectionFreeformView;

                // hack to see if we're in the interface builder or in the compound operator editor
                // these are outside of the normal selection hierarchy so we always return true
                if (colView?.ViewModel is SimpleCollectionViewModel) return true;

                // if the collection view is a free form view, or it is the lowest
                // selected element then use the pointer
                return colView != null || colView.IsLowestSelected;*/
                return _element is CollectionFreeformView;
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
            element.ManipulationCompleted += ElementOnManipulationCompleted;
        }

        private void ElementOnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs manipulationCompletedRoutedEventArgs)
        {
            _isManipulating = false;
        }

        private void ElementOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (_isManipulating)
            {
                e.Complete();
                return;
            }
            if (e.PointerDeviceType == BlockedInputType && FilterInput)
            {
                e.Complete();
                _processManipulation = false;
                e.Handled = true;
                return;
            }
            //if (e.PointerDeviceType == PointerDeviceType.Mouse &&
            //    (Window.Current.CoreWindow.GetKeyState(VirtualKey.RightButton) & CoreVirtualKeyStates.Down) != CoreVirtualKeyStates.Down)
            //{
            //    e.Complete();
            //    return;
            //}
            _isManipulating = true;
            _processManipulation = true;

            _numberOfTimesDirChanged = 0;
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

            DetectShake(sender, e);

        }

        // keeps track of whether the node has been shaken hard enough
        private static int _numberOfTimesDirChanged = 0;
        private static double _direction;
        private static DispatcherTimer _dispatcherTimer;

        // these constants adjust the sensitivity of the shake
        private static int _millisecondsToShake = 600;
        private static int _sensitivity = 4;

        /// <summary>
        /// Determines whether a shake manipulation has occured based on the velocity and direction of the translation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void DetectShake(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // get the document view that's being manipulated
            var grid = sender as Grid;
            var docView = grid?.GetFirstAncestorOfType<DocumentView>();

            if (docView != null)
            {
                // calculate the speed of the translation from the velocities property of the eventargs
                var speed = Math.Sqrt(Math.Pow(e.Velocities.Linear.X, 2) + Math.Pow(e.Velocities.Linear.Y, 2));

                // calculate the direction of the velocity
                var dir = Math.Atan2(e.Velocities.Linear.Y, e.Velocities.Linear.X);
                
                // checks if a certain number of direction changes occur in a specified time span
                if (_numberOfTimesDirChanged == 0)
                {
                    StartTimer();
                    _numberOfTimesDirChanged++;
                    _direction = dir;
                }
                else if (_numberOfTimesDirChanged < _sensitivity)
                {
                    if (Math.Abs(Math.Abs(dir - _direction) - 3.14) < 1)
                    {
                        _numberOfTimesDirChanged++;
                        _direction = dir;
                    }
                }
                else
                {
                    if (Math.Abs(Math.Abs(dir - _direction) - 3.14) < 1)
                    {
                        // if we've reached enough direction changes, break the connection
                        docView.DisconnectFromLink();
                        _numberOfTimesDirChanged = 0;
                    }
                }
            }
        }

        private static void StartTimer()
        {
            if (_dispatcherTimer != null)
            {
                _dispatcherTimer.Stop();
            }
            else
            {
                _dispatcherTimer = new DispatcherTimer();
                _dispatcherTimer.Tick += dispatcherTimer_Tick;
            }
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, _millisecondsToShake);

            _dispatcherTimer.Start();

        }

        private static void dispatcherTimer_Tick(object sender, object e)
        {
            _numberOfTimesDirChanged = 0;
            _dispatcherTimer.Stop();
        }

        private void TranslateAndScale(PointerRoutedEventArgs e)
        {
            if (!_processManipulation) return;
            e.Handled = true;

            if ((e.KeyModifiers & VirtualKeyModifiers.Control) != 0)
            {

                //Get mousepoint in canvas space 
                var point = e.GetCurrentPoint(_element);

                // get the scale amount
                float scaleAmount = point.Properties.MouseWheelDelta > 0 ? 1.07f : 1 / 1.07f;
                //scaleAmount = Math.Max(Math.Min(scaleAmount, 1.7f), 0.4f);

                //Clamp the scale factor 
                ElementScale *= scaleAmount;

                if (!ClampScale(scaleAmount))
                    OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(new Point(),
                        point.Position, new Point(scaleAmount, scaleAmount)));
            }
            else
            {
                var scrollAmount = e.GetCurrentPoint(_element).Properties.MouseWheelDelta / 3.0f;
                OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(new Point(0, scrollAmount),
                    new Point(),  new Point(1, 1)));
            }
        }

        public void FitToParent()
        {
            var ff = _element as CollectionFreeformView;
            var par = ff?.Parent as FrameworkElement;
            if (par == null || ff == null)
                return;

            var rect = new Rect(new Point(), new Point(par.ActualWidth, par.ActualHeight)); //  par.GetBoundingRect();

            //if (ff.ViewModel.DocumentViewModels.Count == 1)
            //{
            //    ff.ViewModel.DocumentViewModels[0].GroupTransform = new TransformGroupData(new Point(), new Point(), new Point(1, 1));
            //    var aspect = rect.Width / rect.Height;
            //    var ffHeight = ff.ViewModel.DocumentViewModels[0].Height;
            //    var ffwidth = ff.ViewModel.DocumentViewModels[0].Width;
            //    var ffAspect = ffwidth / ffHeight;
            //    ff.ViewModel.DocumentViewModels[0].Width  = aspect > ffAspect ? rect.Height * ffAspect : rect.Width;
            //    ff.ViewModel.DocumentViewModels[0].Height = aspect < ffAspect ? rect.Width / ffAspect : rect.Height;
            //    return;
            //}

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

                OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(trans, new Point(r.Left + r.Width / 2, r.Top), scaleAmt));
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

            var scaleFactor = e.Delta.Scale;
            ElementScale *= scaleFactor;

            // set up translation transform
            var translate = Util.TranslateInCanvasSpace(e.Delta.Translation, handleControl);

            //Clamp the scale factor 
            if (!ClampScale(scaleFactor))
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
