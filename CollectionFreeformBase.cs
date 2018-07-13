using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Dash.Views.Collection;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Office.Interop.Word;
using NewControls.Geometry;
using Point = Windows.Foundation.Point;
using Rectangle = Windows.UI.Xaml.Shapes.Rectangle;
using Task = System.Threading.Tasks.Task;
using Window = Windows.UI.Xaml.Window;
using DashShared;
using System.Threading;
using Windows.Storage.Streams;
using Windows.Storage;
using Dash.Views;

namespace Dash
{
    public abstract class CollectionFreeformBase : UserControl, ICollectionView
    {
        MatrixTransform _transformBeingAnimated;// Transform being updated during animation
        Canvas _itemsPanelCanvas => GetCanvas();
        CollectionViewModel _lastViewModel = null;
        public abstract DocumentView ParentDocument { get; }
        //TODO: instantiate in derived class and define OnManipulatorTranslatedOrScaled
        public abstract ViewManipulationControls ViewManipulationControls { get; set; }
        public bool TagMode { get; set; }
        public KeyController TagKey { get; set; }
        public abstract CollectionViewModel ViewModel { get; }
        public abstract CollectionView.CollectionViewType Type { get; }
        private Mutex _mutex = new Mutex();

        //SET BACKGROUND IMAGE SOURCE
        public delegate void SetBackground(object backgroundImagePath);
        private static event SetBackground setBackground;

        //SET BACKGROUND IMAGE OPACITY
        public delegate void SetBackgroundOpacity(float opacity);
        private static event SetBackgroundOpacity setBackgroundOpacity;

        // TODO: get canvas in derived class
        public abstract Canvas GetCanvas();
        // TODO: get itemscontrol of derived class
        public abstract ItemsControl GetItemsControl();
        // TODO: get win2d canvascontrol of derived class
        public abstract CanvasControl GetBackgroundCanvas();
        // TODO: get outergrid of derived class
        public abstract Grid GetOuterGrid();
        // TODO: get tagbox of derived class
        public abstract AutoSuggestBox GetTagBox();
        // TODO: get selectioncanvas of derived class
        public abstract Canvas GetSelectionCanvas();
        // TODO: get dropindicationrect of derived class
        public abstract Rectangle GetDropIndicationRectangle();
        // TODO: get inkcanvas of derived class
        public abstract Canvas GetInkHostCanvas();

        protected virtual void OnLoad(object sender, RoutedEventArgs e)
        {
            MakePreviewTextbox();

            //make and add selectioncanvas 
            SelectionCanvas = new Canvas();
            Canvas.SetLeft(SelectionCanvas, -30000);
            Canvas.SetTop(SelectionCanvas, -30000);
            GetInkHostCanvas().Children.Add(SelectionCanvas);

            if (InkController != null)
            {
                MakeInkCanvas();
            }
            UpdateLayout(); // bcz: unfortunately, we need this because contained views may not be loaded yet which will mess up FitContents
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            setBackground += ChangeBackground;
            setBackgroundOpacity += ChangeOpacity;

            var settingsView = MainPage.Instance.GetSettingsView;
            if (settingsView.ImageState == SettingsView.BackgroundImageState.Custom)
            {
                var storedPath = settingsView.CustomImagePath;
                if (storedPath != null) _background = storedPath;
            }
            else
            {
                _background = settingsView.EnumToPathDict[settingsView.ImageState];
            }

            BackgroundOpacity = settingsView.BackgroundImageOpacity;
        }

        protected void OnDataContextChanged(object sender, DataContextChangedEventArgs e)
        {
            _lastViewModel = ViewModel;
        }

        protected void OnUnload(object sender, RoutedEventArgs e)
        {
            if (_lastViewModel != null)
            {
                _lastViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            _lastViewModel = null;
            setBackground -= ChangeBackground;
            setBackgroundOpacity -= ChangeOpacity;
        }

        protected void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.IBeam, 1);
        }

        protected void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
        }

        protected void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var grid = sender as Grid;
            if (grid?.Clip != null)
            {
                grid.Clip.Rect = new Rect(0, 0, grid.ActualWidth, grid.ActualHeight);
            }
        }


        public DocumentController Snapshot(bool copyData = false)
        {
            var controllers = new List<DocumentController>();
            foreach (var dvm in ViewModel.DocumentViewModels)
                controllers.Add(copyData ? dvm.DocumentController.GetDataCopy() : dvm.DocumentController.GetViewCopy());
            var snap = new CollectionNote(new Point(), CollectionView.CollectionViewType.Freeform, double.NaN, double.NaN, controllers).Document;
            snap.SetFitToParent(true);
            return snap;
        }

        #region Manipulation
        /// <summary>
        /// Animation storyboard for first half. Unfortunately, we can't use the super useful AutoReverse boolean of animations to do this with one storyboard
        /// </summary>
        Storyboard _storyboard1, _storyboard2;

        public void Move(TranslateTransform translate)
        {
            var composite = new TransformGroup();
            composite.Children.Add((GetItemsControl()?.ItemsPanelRoot as Canvas).RenderTransform);
            composite.Children.Add(translate);

            var matrix = composite.Value;
            ViewModel.TransformGroup = new TransformGroupData(new Point(matrix.OffsetX, matrix.OffsetY), new Point(matrix.M11, matrix.M22));
        }

        public void MoveAnimated(TranslateTransform translate)
        {
            var old = (_itemsPanelCanvas?.RenderTransform as MatrixTransform)?.Matrix;
            if (old == null)
            {
                return;
            }
            _transformBeingAnimated = new MatrixTransform() { Matrix = (Matrix)old };

            Debug.Assert(_transformBeingAnimated != null);
            var milliseconds = 1000;
            var duration = new Duration(TimeSpan.FromMilliseconds(milliseconds));
            var halfDuration = new Duration(TimeSpan.FromMilliseconds(milliseconds / 2.0));

            //Clear storyboard
            _storyboard1?.Stop();
            _storyboard1?.Children.Clear();
            _storyboard1 = new Storyboard { Duration = duration };

            _storyboard2?.Stop();
            _storyboard2?.Children.Clear();
            _storyboard2 = new Storyboard { Duration = duration };


            var startX = _transformBeingAnimated.Matrix.OffsetX;
            var startY = _transformBeingAnimated.Matrix.OffsetY;

            // Create a DoubleAnimation for translating
            var translateAnimationX = MakeAnimationElement(_transformBeingAnimated, startX, startX + translate.X, "MatrixTransform.Matrix.OffsetX", duration);
            var translateAnimationY = MakeAnimationElement(_transformBeingAnimated, startY, Math.Min(0, startY + translate.Y), "MatrixTransform.Matrix.OffsetY", duration);
            translateAnimationX.AutoReverse = false;
            translateAnimationY.AutoReverse = false;


            var scaleFactor = Math.Max(0.45, 3000 / Math.Sqrt(translate.X * translate.X + translate.Y * translate.Y));
            //Create a Double Animation for zooming in and out. Unfortunately, the AutoReverse bool does not work as expected.
            var zoomOutAnimationX = MakeAnimationElement(_transformBeingAnimated, _transformBeingAnimated.Matrix.M11, _transformBeingAnimated.Matrix.M11 * 0.5, "MatrixTransform.Matrix.M11", halfDuration);
            var zoomOutAnimationY = MakeAnimationElement(_transformBeingAnimated, _transformBeingAnimated.Matrix.M22, _transformBeingAnimated.Matrix.M22 * 0.5, "MatrixTransform.Matrix.M22", halfDuration);

            zoomOutAnimationX.AutoReverse = true;
            zoomOutAnimationY.AutoReverse = true;

            zoomOutAnimationX.RepeatBehavior = new RepeatBehavior(TimeSpan.FromMilliseconds(milliseconds));
            zoomOutAnimationY.RepeatBehavior = new RepeatBehavior(TimeSpan.FromMilliseconds(milliseconds));


            _storyboard1.Children.Add(translateAnimationX);
            _storyboard1.Children.Add(translateAnimationY);
            if (scaleFactor < 0.8)
            {
                _storyboard1.Children.Add(zoomOutAnimationX);
                _storyboard1.Children.Add(zoomOutAnimationY);
            }

            CompositionTarget.Rendering -= CompositionTargetOnRendering;
            CompositionTarget.Rendering += CompositionTargetOnRendering;

            // Begin the animation.
            _storyboard1.Begin();
            _storyboard1.Completed -= Storyboard1OnCompleted;
            _storyboard1.Completed += Storyboard1OnCompleted;
        }

        protected void Storyboard1OnCompleted(object sender, object e)
        {
            CompositionTarget.Rendering -= CompositionTargetOnRendering;
            _storyboard1.Completed -= Storyboard1OnCompleted;
        }

        protected void CompositionTargetOnRendering(object sender, object e)
        {
            var matrix = _transformBeingAnimated.Matrix;
            ViewModel.TransformGroup = new TransformGroupData(new Point(matrix.OffsetX, matrix.OffsetY), new Point(matrix.M11, matrix.M22));
        }
        protected DoubleAnimation MakeAnimationElement(MatrixTransform matrix, double from, double to, String name, Duration duration)
        {

            var toReturn = new DoubleAnimation();
            toReturn.EnableDependentAnimation = true;
            toReturn.Duration = duration;
            Storyboard.SetTarget(toReturn, matrix);
            Storyboard.SetTargetProperty(toReturn, name);

            toReturn.From = from;
            toReturn.To = to;

            toReturn.EasingFunction = new QuadraticEase();
            return toReturn;

        }

        /// <summary>
        /// Pans and zooms upon touch manipulation 
        /// </summary>   
        protected virtual void ManipulationControls_OnManipulatorTranslated(TransformGroupData transformation, bool abs)
        {
            // calculate the translate delta
            var translateDelta = new TranslateTransform
            {
                X = transformation.Translate.X,
                Y = transformation.Translate.Y
            };

            // calculate the scale delta
            var scaleDelta = new ScaleTransform
            {
                CenterX = transformation.ScaleCenter.X,
                CenterY = transformation.ScaleCenter.Y,
                ScaleX = transformation.ScaleAmount.X,
                ScaleY = transformation.ScaleAmount.Y
            };

            //Create initial composite transform
            var composite = new TransformGroup();
            if (!abs)
                composite.Children.Add(_itemsPanelCanvas.RenderTransform); // get the current transform
            composite.Children.Add(scaleDelta); // add the new scaling
            composite.Children.Add(translateDelta); // add the new translate
            var matrix = composite.Value;
            ViewModel.TransformGroup = new TransformGroupData(new Point(matrix.OffsetX, matrix.OffsetY), new Point(matrix.M11, matrix.M22));
        }

        #endregion

        #region BackgroundTiling
        bool _resourcesLoaded;
        CanvasImageBrush _bgBrush;
        const double NumberOfBackgroundRows = 2; // THIS IS A MAGIC NUMBER AND SHOULD CHANGE IF YOU CHANGE THE BACKGROUND IMAGE
        private float _bgOpacity = 1.0f;

        /// <summary>
        /// Collection background tiling image opacity
        /// </summary>
        public static float BackgroundOpacity { set => setBackgroundOpacity?.Invoke(value); }
        private static object _background = "ms-appx:///Assets/transparent_grid_tilable.png";
        private CanvasBitmap _bgImage;

        /// <summary>
        /// Collection background tiling image
        /// </summary>
        public static object BackgroundImage { set => setBackground?.Invoke(value); }

        /// <summary>
        /// Called when background opacity is set and the background tiling must be redrawn to reflect the change
        /// </summary>
        /// <param name="opacity"></param>
        private void ChangeOpacity(float opacity)
        {
            _bgOpacity = opacity;
            GetBackgroundCanvas().Invalidate();
        }
        #endregion

        /// <summary>
        /// All of the following background image updating logic was sourced from this article --> https://microsoft.github.io/Win2D/html/LoadingResourcesOutsideCreateResources.htm
        /// </summary>
        #region LOADING AND REDRAWING BACKUP ASYNC

        private Task _backgroundTask;

        // 1
        protected void CanvasControl_OnCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            _bgBrush = new CanvasImageBrush(sender);

            // Set the brush's edge behaviour to wrap, so the image repeats if the drawn region is too big
            _bgBrush.ExtendX = _bgBrush.ExtendY = CanvasEdgeBehavior.Wrap;
            _resourcesLoaded = true;

            args.TrackAsyncAction(CreateResourcesAsync(sender).AsAsyncAction());
        }

        // 2
        protected async Task CreateResourcesAsync(CanvasControl sender)
        {
            if (_backgroundTask != null)
            {
                _backgroundTask.AsAsyncAction().Cancel();
                try { await _backgroundTask; } catch (Exception e) { Debug.WriteLine(e); }
                _backgroundTask = null;
            }

            //Internally null checks _background
            //NOTE *** Invalid or null input will end the entire update chain and, to the user, nothing will visibily change. ***
            ChangeBackground(_background);
        }

        // 3
        protected async void ChangeBackground(object backgroundImagePath)
        {
            // Null-checking. WARNING - if null, Dash throws an Unhandled Exception
            if (backgroundImagePath == null) return;

            // Now, backgroundImagePath is either <string> or <IRandomAccessStream> - while local ms-appx (assets folder) paths <string> don't need conversion, 
            // external file system paths <string> need to be converted into <IRandomAccessStream>
            if (backgroundImagePath is string path && !path.Contains("ms-appx:"))
            {
                backgroundImagePath = await FileRandomAccessStream.OpenAsync(path, FileAccessMode.Read);
            }
            // Update the path/stream instance var to be used next in LoadBackgroundAsync
            _background = backgroundImagePath;
            // Now, register and perform the new loading
            _backgroundTask = LoadBackgroundAsync(GetBackgroundCanvas());
        }

        // 4
        protected async Task LoadBackgroundAsync(CanvasControl canvas)
        {
            // Convert the <IRandomAccessStream> and update the <CanvasBitmap> instance var to be used later by the <CanvasImageBrush> in CanvasControl_OnDraw
            if (_background is string s) // i.e. A rightfully unconverted ms-appx path
                _bgImage = await CanvasBitmap.LoadAsync(canvas, new Uri(s));
            else
                _bgImage = await CanvasBitmap.LoadAsync(canvas, (IRandomAccessStream)_background);
            // NOTE *** At this point, _backgroundTask will be marked completed. This has bearing on the IsLoadInProgress bool and how that dictates the rendered drawing (see immediately below).
            // Indicates that the contents of the CanvasControl need to be redrawn. Calling Invalidate results in the Draw event being raised shortly afterward (see immediately below).
            canvas.Invalidate();
        }

        // 5
        protected void CanvasControl_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (IsLoadInProgress())
            {
                // If the image failed to load in time, simply display a blank white background
                args.DrawingSession.FillRectangle(0, 0, (float)sender.Width, (float)sender.Height, Colors.White);
            }
            else
            {
                // If it successfully loaded, set the desired image and the opacity of the <CanvasImageBrush>
                _bgBrush.Image = _bgImage;
                _bgBrush.Opacity = _bgOpacity;

                // Lastly, fill a rectangle with the tiling image brush, covering the entire bounds of the canvas control
                var session = args.DrawingSession;
                session.FillRectangle(new Rect(new Point(), sender.Size), _bgBrush);
            }
        }

        protected bool IsLoadInProgress()
        {
            // Not gonna happen, see above sequence of events
            if (_backgroundTask == null) return false;

            // Unless the draw event from Invalidate() outpaces the actual async loading, this won't ever get hit as the LoadBackgroundAsync should have already returned a Task.Completed
            if (!_backgroundTask.IsCompleted) return true;

            try
            {
                // As _background task was set to LoadBackgroundAsync, should have already completed. Wait will be moot. 
                _backgroundTask.Wait();
            }
            catch (AggregateException ae)
            {
                // Catch any task-related errors along the way
                ae.Handle(ex => throw ex);
            }
            finally
            {
                // _backgroundTask will be set to null, so that CreateResourcesAsync won't be concerned with phantom existing tasks
                _backgroundTask = null;
            }
            // Permits the <CanvasControl> to render the safely loaded image 
            return false;
        }

        /// <summary>
        /// When the ViewModel's TransformGroup changes, this needs to update its background canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ViewModel == null) return;

            if (e.PropertyName == nameof(CollectionViewModel.TransformGroup))
            {

                if (_resourcesLoaded)
                {
                    double clampBackgroundScaleForAliasing(double currentScale, double numberOfBackgroundRows)
                    {
                        while (currentScale / numberOfBackgroundRows > numberOfBackgroundRows)
                        {
                            currentScale /= numberOfBackgroundRows;
                        }

                        while (currentScale > 0 && currentScale * numberOfBackgroundRows < numberOfBackgroundRows)
                        {
                            currentScale *= numberOfBackgroundRows;
                        }
                        return currentScale;
                    }

                    var transformation = ViewModel.TransformGroup;
                    // calculate the translate delta
                    var translateDelta = new TranslateTransform
                    {
                        X = transformation.Translate.X,
                        Y = transformation.Translate.Y
                    };

                    // calculate the scale delta
                    var scaleDelta = new ScaleTransform
                    {
                        CenterX = transformation.ScaleCenter.X,
                        CenterY = transformation.ScaleCenter.Y,
                        ScaleX = transformation.ScaleAmount.X,
                        ScaleY = transformation.ScaleAmount.Y
                    };

                    //Create initial composite transform
                    var composite = new TransformGroup();
                    composite.Children.Add(scaleDelta); // add the new scaling
                    composite.Children.Add(translateDelta); // add the new translate

                    var matrix = composite.Value;

                    var aliasSafeScale = clampBackgroundScaleForAliasing(matrix.M11, NumberOfBackgroundRows);
                    _bgBrush.Transform = new Matrix3x2((float)aliasSafeScale,
                        (float)matrix.M12,
                        (float)matrix.M21,
                        (float)aliasSafeScale,
                        (float)matrix.OffsetX,
                        (float)matrix.OffsetY);
                    GetBackgroundCanvas().Invalidate();
                }
            }
        }
        #endregion

        #region Tagging

        public bool TagNote(string tagValue, DocumentView docView)
        {
            return false;
            if (!TagMode)
            {
                return false;
            }
            //DocumentController image = null;
            //foreach (var documentController in group.TypedData)
            //{
            //    if (documentController.DocumentType.Equals(ImageBox.DocumentType))
            //    {
            //        image = documentController.GetDataDocument(null);
            //        break;
            //    }
            //}

            //if (image != null)
            //{
            //    image.SetField(TagKey, new TextController(tagValue), true);
            //    return true;
            //}
            return false;
        }

        public void ShowTagKeyBox()
        {
            GetTagBox().Visibility = Windows.UI.Xaml.Visibility.Visible;
            var mousePos = Util.PointTransformFromVisual(this.RootPointerPos(), Window.Current.Content, GetOuterGrid());
            GetTagBox().RenderTransform = new TranslateTransform { X = mousePos.X, Y = mousePos.Y };
        }

        public void HideTagKeyBox()
        {
            GetTagBox().Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        protected void TagKeyBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var keys = ContentController<FieldModel>.GetControllers<KeyController>();
                var names = keys.Where(k => !k.Name.StartsWith("_"));
                GetTagBox().ItemsSource = names;
            }
        }

        protected void TagKeyBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = ((KeyController)args.SelectedItem).Name;
        }

        protected void TagKeyBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                TagKey = (KeyController)args.ChosenSuggestion;
            }
            else
            {
                var keys = ContentController<FieldModel>.GetControllers<KeyController>();
                var key = keys.FirstOrDefault(k => k.Name == args.QueryText);

                if (key == null)
                {
                    TagKey = new KeyController(args.QueryText, Guid.NewGuid().ToString());
                }
                else
                {
                    TagKey = key;
                }
            }
            TagMode = true;

            HideTagKeyBox();
        }

        #endregion

        #region Marquee Select

        Rectangle _marquee;
        Point _marqueeAnchor;
        bool _isMarqueeActive;
        private MarqueeInfo mInfo;
        object _marqueeKeyHandler = null;

        protected virtual void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_marquee != null)
            {
                var pos = Util.PointTransformFromVisual(new Point(Canvas.GetLeft(_marquee), Canvas.GetTop(_marquee)),
                    GetSelectionCanvas(), GetItemsControl().ItemsPanelRoot);
                SelectionManager.SelectDocuments(DocsInMarquee(new Rect(pos, new Size(_marquee.Width, _marquee.Height))));
                GetSelectionCanvas().Children.Remove(_marquee);
                MainPage.Instance.RemoveHandler(KeyDownEvent, new KeyEventHandler(_marquee_KeyDown));
                _marquee = null;
                _isMarqueeActive = false;
                if (e != null) e.Handled = true;
            }

            SelectionCanvas?.Children.Clear();
            GetOuterGrid().PointerMoved -= OnPointerMoved;
            if (e != null) GetOuterGrid().ReleasePointerCapture(e.Pointer);
        }

        /// <summary>
        /// Handles mouse movement.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected virtual void OnPointerMoved(object sender, PointerRoutedEventArgs args)
        {
            if (_isMarqueeActive)
            {
                var pos = args.GetCurrentPoint(SelectionCanvas).Position;
                var dX = pos.X - _marqueeAnchor.X;
                var dY = pos.Y - _marqueeAnchor.Y;

                //Height and width depend on the difference in position of the current point and the anchor (initial point)
                double newWidth = (dX > 0) ? dX : -dX;
                double newHeight = (dY > 0) ? dY : -dY;

                //Anchor point should also be moved if dX or dY are moved
                var newAnchor = _marqueeAnchor;
                if (dX < 0) newAnchor.X += dX;
                if (dY < 0) newAnchor.Y += dY;

                if (newWidth > 5 && newHeight > 5 && _marquee == null)
                {
                    this.Focus(FocusState.Programmatic);
                    _marquee = new Rectangle()
                    {
                        Stroke = new SolidColorBrush(Colors.Gray),
                        StrokeThickness = 1.5 / Zoom,
                        StrokeDashArray = new DoubleCollection { 5, 2 },
                        CompositeMode = ElementCompositeMode.SourceOver
                    };
                    if (_marqueeKeyHandler != null)
                        MainPage.Instance.RemoveHandler(KeyDownEvent, _marqueeKeyHandler);
                    _marqueeKeyHandler = new KeyEventHandler(_marquee_KeyDown);
                    MainPage.Instance.AddHandler(KeyDownEvent, _marqueeKeyHandler, false);
                    _marquee.AllowFocusOnInteraction = true;
                    SelectionCanvas.Children.Add(_marquee);

                    mInfo = new MarqueeInfo();
                    SelectionCanvas.Children.Add(mInfo);
                }

                if (_marquee != null) //Adjust the marquee rectangle
                {
                    Canvas.SetLeft(_marquee, newAnchor.X);
                    Canvas.SetTop(_marquee, newAnchor.Y);
                    _marquee.Width = newWidth;
                    _marquee.Height = newHeight;
                    args.Handled = true;

                    Canvas.SetLeft(mInfo, newAnchor.X);
                    Canvas.SetTop(mInfo, newAnchor.Y + newHeight);
                }
            }
        }

        /// <summary>
        /// Handles mouse movement. Starts drawing Marquee selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected virtual void OnPointerPressed(object sender, PointerRoutedEventArgs args)
        {
            // marquee on left click by default
            if (MenuToolbar.Instance.GetMouseMode() == MenuToolbar.MouseMode.TakeNote)// bcz:  || args.IsRightPressed())
            {
                if (XInkCanvas.IsTopmost() &&
                    (args.KeyModifiers & VirtualKeyModifiers.Control) == 0 &&
                    ( // bcz: the next line makes right-drag pan within nested collections instead of moving them -- that doesn't seem right to me since MouseMode feels like it applies to left-button dragging only
                      // MenuToolbar.Instance.GetMouseMode() == MenuToolbar.MouseMode.PanFast || 
                        ((!args.GetCurrentPoint(GetOuterGrid()).Properties.IsRightButtonPressed)) && MenuToolbar.Instance.GetMouseMode() != MenuToolbar.MouseMode.PanFast))
                {
                    if ((args.KeyModifiers & VirtualKeyModifiers.Shift) == 0)
                        SelectionManager.DeselectAll();

                    GetOuterGrid().CapturePointer(args.Pointer);
                    _marqueeAnchor = args.GetCurrentPoint(GetSelectionCanvas()).Position;
                    _isMarqueeActive = true;
                    PreviewTextbox_LostFocus(null, null);
                    ParentDocument.ManipulationMode = ManipulationModes.None;
                    args.Handled = true;
                    GetOuterGrid().PointerMoved -= OnPointerMoved;
                    GetOuterGrid().PointerMoved += OnPointerMoved;
                }
            }
        }

        void _marquee_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_marquee != null && (e.Key == VirtualKey.C || e.Key == VirtualKey.T || e.Key == VirtualKey.Back || e.Key == VirtualKey.Delete || e.Key == VirtualKey.G || e.Key == VirtualKey.A))
            {
                TriggerActionFromSelection(e.Key, true);
                MainPage.Instance.RemoveHandler(KeyDownEvent, new KeyEventHandler(_marquee_KeyDown));
                e.Handled = true;
            }
        }

        public bool IsMarqueeActive => _isMarqueeActive;

        // called by SelectionManager to reset this collection's internal selection-based logic
        public void ResetMarquee()
        {
            GetSelectionCanvas()?.Children?.Clear();
            _marquee = null;
            _isMarqueeActive = false;
        }

        public List<DocumentView> DocsInMarquee(Rect marquee)
        {
            var selectedDocs = new List<DocumentView>();
            if (GetItemsControl().ItemsPanelRoot != null)
            {
                var docs = GetItemsControl().ItemsPanelRoot.Children;
                foreach (var documentView in docs.Select((d) => d.GetFirstDescendantOfType<DocumentView>()).Where(d => d != null && d.IsHitTestVisible))
                {
                    var rect = documentView.TransformToVisual(GetCanvas()).TransformBounds(
                        new Rect(new Point(), new Point(documentView.ActualWidth, documentView.ActualHeight)));
                    if (marquee.IntersectsWith(rect))
                    {
                        selectedDocs.Add(documentView);
                    }
                }
            }
            return selectedDocs;
        }

        public Rect GetBoundingRectFromSelection()
        {
            Point topLeftMostPoint = new Point(Double.PositiveInfinity, Double.PositiveInfinity);
            Point bottomRightMostPoint = new Point(Double.NegativeInfinity, Double.NegativeInfinity);

            bool isEmpty = true;

            foreach (DocumentView doc in SelectionManager.SelectedDocs)
            {
                isEmpty = false;
                topLeftMostPoint.X = doc.ViewModel.Position.X < topLeftMostPoint.X ? doc.ViewModel.Position.X : topLeftMostPoint.X;
                topLeftMostPoint.Y = doc.ViewModel.Position.Y < topLeftMostPoint.Y ? doc.ViewModel.Position.Y : topLeftMostPoint.Y;
                bottomRightMostPoint.X = doc.ViewModel.Position.X + doc.ViewModel.ActualSize.X > bottomRightMostPoint.X
                    ? doc.ViewModel.Position.X + doc.ViewModel.ActualSize.X
                    : bottomRightMostPoint.X;
                bottomRightMostPoint.Y = doc.ViewModel.Position.Y + doc.ViewModel.ActualSize.Y > bottomRightMostPoint.Y
                    ? doc.ViewModel.Position.Y + doc.ViewModel.ActualSize.Y
                    : bottomRightMostPoint.Y;
            }

            if (isEmpty) return Rect.Empty;

            return new Rect(topLeftMostPoint, bottomRightMostPoint);
        }

        /// <summary>
        /// Triggers one of the actions that you can do with selected documents, whether it's by dragging through a marquee or from currently selected ones.
        /// </summary>
        /// <param name="modifier"></param>
        /// <param name="fromMarquee">True if we select from the marquee, false if from currently selecte documents</param>
        public void TriggerActionFromSelection(VirtualKey modifier, bool fromMarquee)
        {
            void DoAction(Action<List<DocumentView>, Point, Size> action)
            {
                Point where;
                Rectangle marquee;
                IEnumerable<DocumentView> viewsToSelectFrom;

                if (fromMarquee)
                {
                    where = Util.PointTransformFromVisual(new Point(Canvas.GetLeft(_marquee), Canvas.GetTop(_marquee)),
                        SelectionCanvas, GetItemsControl().ItemsPanelRoot);
                    marquee = _marquee;
                    viewsToSelectFrom = DocsInMarquee(new Rect(where, new Size(_marquee.Width, _marquee.Height)));
                    OnPointerReleased(null, null);
                }
                else
                {
                    var bounds = GetBoundingRectFromSelection();

                    // hack to escape when CoreWindow fires the event a second time when it's actually from the marquee
                    if (bounds == Rect.Empty) return;

                    where = new Point(bounds.X, bounds.Y);
                    marquee = new Rectangle
                    {
                        Height = bounds.Height,
                        Width = bounds.Width
                    };
                    viewsToSelectFrom = SelectionManager.SelectedDocs;
                }

                var toSelectFrom = viewsToSelectFrom.ToList();
                action(toSelectFrom, where, new Size(marquee.Width, marquee.Height));
            }

            var type = CollectionView.CollectionViewType.Freeform;

            bool deselect = false;
            using (UndoManager.GetBatchHandle())
            {
                switch (modifier)
                {
                    //create a viewcopy of everything selected
                    case VirtualKey.A:
                        DoAction((dvs, where, size) =>
                        {
                            var docs = dvs.Select(dv => dv.ViewModel.DocumentController.GetViewCopy())
                                .ToList();
                            ViewModel.AddDocument(new CollectionNote(where, type, size.Width, size.Height, docs)
                                .Document);
                        });
                        deselect = true;
                        break;
                    case VirtualKey.T:
                        type = CollectionView.CollectionViewType.Schema;
                        goto case VirtualKey.C;
                    case VirtualKey.C:
                        DoAction((views, where, size) =>
                        {
                            var docss = views.Select(dvm => dvm.ViewModel.DocumentController).ToList();
                            ViewModel.AddDocument(
                                new CollectionNote(where, type, size.Width, size.Height, docss).Document);

                            foreach (var v in views)
                            {
                                v.DeleteDocument();
                            }
                        });
                        deselect = true;
                        break;
                    case VirtualKey.Back:
                    case VirtualKey.Delete:
                        DoAction((views, where, size) =>
                        {
                            foreach (var v in views)
                            {
                                v.DeleteDocument();
                            }
                        });

                        deselect = true;
                        break;
                    case VirtualKey.G:
                        DoAction((views, where, size) =>
                        {
                            ViewModel.AddDocument(Util.AdornmentWithPosition(BackgroundShape.AdornmentShape.Rectangular,
                                where, size.Width, size.Height));
                        });
                        deselect = true;
                        break;
                }
            }

            if (deselect)
                SelectionManager.DeselectAll();
        }
        #endregion

        #region DragAndDrop

        public void SetDropIndicationFill(Brush fill)
        {
            GetDropIndicationRectangle().Fill = fill;
        }

        #endregion

        #region Activation

        protected void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (ViewModel.ViewLevel.Equals(CollectionViewModel.StandardViewLevel.None) || ViewModel.ViewLevel.Equals(CollectionViewModel.StandardViewLevel.Detail))
            {
                if (XInkCanvas.IsTopmost())
                {
                    _isMarqueeActive = false;
                    if (!this.IsShiftPressed())
                        RenderPreviewTextbox(e.GetPosition(_itemsPanelCanvas));
                }
            }
        }

        public void RenderPreviewTextbox(Point where)
        {
            previewTextBuffer = "";
            if (previewTextbox != null)
            {
                Canvas.SetLeft(previewTextbox, where.X);
                Canvas.SetTop(previewTextbox, where.Y);
                previewTextbox.Visibility = Windows.UI.Xaml.Visibility.Visible;
                AddHandler(KeyDownEvent, previewTextHandler, false);
                previewTextbox.Text = string.Empty;
                previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
                previewTextbox.LostFocus += PreviewTextbox_LostFocus;
                previewTextbox.Focus(FocusState.Pointer);
            }
        }
        #endregion

        #region TextInputBox

        string previewTextBuffer = "";
        public InkController InkController;
        public FreeformInkControl InkControl;
        public InkCanvas XInkCanvas;
        public Canvas SelectionCanvas;
        public double Zoom => ViewManipulationControls.ElementScale;

        void MakeInkCanvas()
        {
            XInkCanvas = new InkCanvas() { Width = 60000, Height = 60000 };

            InkControl = new FreeformInkControl(this, XInkCanvas, SelectionCanvas);
            Canvas.SetLeft(XInkCanvas, -30000);
            Canvas.SetTop(XInkCanvas, -30000);
            GetInkHostCanvas().Children.Add(XInkCanvas);
        }

        bool loadingPermanentTextbox;

        TextBox previewTextbox { get; set; }

        object previewTextHandler = null;
        void MakePreviewTextbox()
        {
            if (previewTextHandler == null)
                previewTextHandler = new KeyEventHandler(PreviewTextbox_KeyDown);

            previewTextbox = new TextBox
            {
                Width = 200,
                Height = 50,
                Background = new SolidColorBrush(Colors.Transparent),
                Visibility = Visibility.Collapsed
            };
            previewTextbox.Paste += previewTextbox_Paste;
            previewTextbox.Unloaded += (s, e) => RemoveHandler(KeyDownEvent, previewTextHandler);
            GetInkHostCanvas().Children.Add(previewTextbox);
            previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
            previewTextbox.LostFocus += PreviewTextbox_LostFocus;
        }

        void PreviewTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            RemoveHandler(KeyDownEvent, previewTextHandler);
            previewTextbox.Visibility = Visibility.Collapsed;
            previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
        }

        protected void previewTextbox_Paste(object sender, TextControlPasteEventArgs e)
        {
            var text = previewTextbox.Text;
            if (previewTextbox.Visibility != Visibility.Collapsed)
            {
                var where = new Point(Canvas.GetLeft(previewTextbox), Canvas.GetTop(previewTextbox));
            }
        }

        void PreviewTextbox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key.Equals(VirtualKey.Escape))
            {
                PreviewTextbox_LostFocus(null, null);
                return;
            }
            previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
            var text = KeyCodeToUnicode(e.Key);
            if (string.IsNullOrEmpty(text))
                return;
            if (previewTextbox.Visibility != Visibility.Collapsed)
            {
                e.Handled = true;
                var where = new Point(Canvas.GetLeft(previewTextbox), Canvas.GetTop(previewTextbox));
                if (text == "v" && this.IsCtrlPressed())
                {
                    ViewModel.Paste(Clipboard.GetContent(), where);
                    previewTextbox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    previewTextBuffer += text;
                    if (text.Length > 0)
                        LoadNewActiveTextBox(text, where);
                }
            }
        }

        public void LoadNewActiveTextBox(string text, Point where, bool resetBuffer = false)
        {
            if (!loadingPermanentTextbox)
            {
                if (resetBuffer)
                    previewTextBuffer = "";
                loadingPermanentTextbox = true;        

                if (SettingsView.Instance.MarkdownEditOn)
                {
                    var postitNote = new MarkdownNote(text: text).Document;
                    Actions.DisplayDocument(ViewModel, postitNote, where);
                }
                else
                {
                    var postitNote = new RichTextNote(text: text).Document;
                    Actions.DisplayDocument(ViewModel, postitNote, where);
                }
            }
        }
        public void LoadNewDataBox(string keyname, Point where, bool resetBuffer = false)
        {
            if (!loadingPermanentTextbox)
            {
                if (resetBuffer)
                    previewTextBuffer = "";
                loadingPermanentTextbox = true;
                var containerData = ViewModel.ContainerDocument.GetDataDocument();
                var keycontroller = new KeyController(keyname);
                if (containerData.GetField(keycontroller, true) == null)
                    containerData.SetField(keycontroller, containerData.GetField(keycontroller) ?? new TextController("<default>"), true);
                var dbox = new DataBox(new DocumentReferenceController(containerData, keycontroller), where.X, where.Y).Document;
                dbox.Tag = "Auto DataBox " + DateTime.Now.Second + "." + DateTime.Now.Millisecond;
                dbox.SetField(KeyStore.DocumentContextKey, containerData, true);
                Actions.DisplayDocument(ViewModel, dbox, where);
            }
        }

        string KeyCodeToUnicode(VirtualKey key)
        {

            var shiftState = this.IsShiftPressed();
            var capState = this.IsCapsPressed();
            var virtualKeyCode = (uint)key;

            string character = null;

            // take care of symbols
            if (key == VirtualKey.Space)
            {
                character = " ";
            }
            if (key == VirtualKey.Multiply)
            {
                character = "*";
            }
            // TODO take care of more symbols

            //Take care of letters
            if (virtualKeyCode >= 65 && virtualKeyCode <= 90)
            {
                if ((!shiftState && !capState) || (shiftState && capState))
                {
                    character = key.ToString().ToLower();
                }
                else
                {
                    character = key.ToString();
                }
            }

            //Take care of numbers
            if (virtualKeyCode >= 48 && virtualKeyCode <= 57)
            {
                character = (virtualKeyCode - 48).ToString();
                if ((shiftState != false || capState != false) &&
                    (!shiftState || !capState))
                {
                    switch ((virtualKeyCode - 48))
                    {
                        case 1: character = "!"; break;
                        case 2: character = "@"; break;
                        case 3: character = "#"; break;
                        case 4: character = "$"; break;
                        case 5: character = "%"; break;
                        case 6: character = "^"; break;
                        case 7: character = "&"; break;
                        case 8: character = "*"; break;
                        case 9: character = "("; break;
                        case 0: character = ")"; break;
                        default: break;
                    }
                }
            }

            if (virtualKeyCode >= 186 && virtualKeyCode <= 222)
            {
                var shifted = ((shiftState != false || capState != false) &&
                    (!shiftState || !capState));
                switch (virtualKeyCode)
                {
                    case 186: character = shifted ? ":" : ";"; break;
                    case 187: character = shifted ? "=" : "+"; break;
                    case 188: character = shifted ? "<" : ","; break;
                    case 189: character = shifted ? "_" : "-"; break;
                    case 190: character = shifted ? ">" : "."; break;
                    case 191: character = shifted ? "?" : "/"; break;
                    case 192: character = shifted ? "~" : "`"; break;
                    case 219: character = shifted ? "{" : "["; break;
                    case 220: character = shifted ? "|" : "\\"; break;
                    case 221: character = shifted ? "}" : "]"; break;
                    case 222: character = shifted ? "\"" : "'"; break;
                }

            }
            //Take care of numpad numbers
            if (virtualKeyCode >= 96 && virtualKeyCode <= 105)
            {
                character = (virtualKeyCode - 96).ToString();
            }

            return character;
        }

        /// <summary>
        /// OnLoad handler. Interfaces with DocumentView to call corresponding functions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void DocumentViewOnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is DocumentView documentView)
            {
                if (loadingPermanentTextbox)
                {
                    var richEditBox = documentView.GetDescendantsOfType<RichEditBox>().FirstOrDefault();
                    if (richEditBox != null)
                    {
                        richEditBox.GotFocus -= RichEditBox_GotFocus;
                        richEditBox.GotFocus += RichEditBox_GotFocus;
                        richEditBox.Focus(FocusState.Programmatic);
                    }
                    var textBox = documentView.GetDescendantsOfType<EditableTextBlock>().FirstOrDefault();
                    if (textBox != null)
                    {
                        textBox.Loaded -= TextBox_Loaded;
                        textBox.Loaded += TextBox_Loaded;
                    }
                    var editableScriptBox = documentView.GetDescendantsOfType<EditableScriptView>().FirstOrDefault();
                    if (editableScriptBox != null)
                    {
                        editableScriptBox.Loaded -= EditableScriptView_Loaded;
                        editableScriptBox.Loaded += EditableScriptView_Loaded;
                    }
                    var a = documentView.GetDescendantsOfType<EditableMarkdownBlock>();
                    var editableMarkdownBox = documentView.GetDescendantsOfType<EditableMarkdownBlock>().FirstOrDefault();
                    if (editableMarkdownBox != null)
                    {
                        editableMarkdownBox.Loaded -= EditableMarkdownBlock_Loaded;
                        editableMarkdownBox.Loaded += EditableMarkdownBlock_Loaded;
                    }
                }
            }

        }

        protected void EditableScriptView_Loaded(object sender, RoutedEventArgs e)
        {
            var textBox = sender as EditableScriptView;
            textBox.Loaded -= EditableScriptView_Loaded;
            textBox.MakeEditable();
            textBox.XTextBox.GotFocus -= TextBox_GotFocus;
            textBox.XTextBox.GotFocus += TextBox_GotFocus;
            textBox.XTextBox.Focus(FocusState.Programmatic);
        }

        protected void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            var textBox = sender as EditableTextBlock;
            textBox.Loaded -= TextBox_Loaded;
            textBox.MakeEditable();
            textBox.XTextBox.GotFocus -= TextBox_GotFocus;
            textBox.XTextBox.GotFocus += TextBox_GotFocus;
            textBox.XTextBox.Focus(FocusState.Programmatic);
        }
        private void EditableMarkdownBlock_Loaded(object sender, RoutedEventArgs e)
        {
            var textBox = sender as EditableMarkdownBlock;
            textBox.Loaded -= EditableMarkdownBlock_Loaded;
            textBox.MakeEditable();
            textBox.XMarkdownBox.GotFocus -= TextBox_GotFocus;
            textBox.XMarkdownBox.GotFocus += TextBox_GotFocus;
            textBox.XMarkdownBox.Focus(FocusState.Programmatic);
        }

        void RichEditBox_GotFocus(object sender, RoutedEventArgs e)
        {
            RemoveHandler(KeyDownEvent, previewTextHandler);
            previewTextbox.Visibility = Visibility.Collapsed;
            loadingPermanentTextbox = false;
            var text = previewTextBuffer;
            var richEditBox = sender as RichEditBox;
            richEditBox.GotFocus -= RichEditBox_GotFocus;
            previewTextbox.Text = string.Empty;
            richEditBox.Document.Selection.SetRange(0, 0);
            richEditBox.Document.SetText(TextSetOptions.None, text);
            richEditBox.Document.Selection.SetRange(text.Length, text.Length);
        }

        void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;

            RemoveHandler(KeyDownEvent, previewTextHandler);
            previewTextbox.Visibility = Visibility.Collapsed;
            loadingPermanentTextbox = false;
            var text = previewTextBuffer;
            textBox.GotFocus -= TextBox_GotFocus;
            previewTextbox.Text = string.Empty;
        }

        #endregion

    }

}
