﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using Dash.Views.Collection;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using NewControls.Geometry;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Point = Windows.Foundation.Point;
using Rectangle = Windows.UI.Xaml.Shapes.Rectangle;
using Task = System.Threading.Tasks.Task;

namespace Dash
{
    public abstract class CollectionFreeformBase : UserControl, ICollectionView
    {
        public CollectionViewType ViewType => CollectionViewType.Freeform;

        private MatrixTransform _transformBeingAnimated;// Transform being updated during animation

        private Panel xTransformedCanvas => GetTransformedCanvas();

        private CollectionViewModel _lastViewModel = null;
        public UserControl UserControl => this;
        public abstract DocumentView ParentDocument { get; }
        //TODO: instantiate in derived class and define OnManipulatorTranslatedOrScaled
        public abstract ViewManipulationControls ViewManipulationControls { get; set; }
        public bool TagMode { get; set; }
        public KeyController TagKey { get; set; }
        public abstract CollectionViewModel ViewModel { get; }
        private Mutex _mutex = new Mutex();
        public void SetupContextMenu(MenuFlyout contextMenu)
        {

        }

        //SET BACKGROUND IMAGE SOURCE
        public delegate void SetBackground(object backgroundImagePath);
        private static event SetBackground setBackground;

        //SET BACKGROUND IMAGE OPACITY
        public delegate void SetBackgroundOpacity(float opacity);
        private static event SetBackgroundOpacity setBackgroundOpacity;

        public abstract Panel GetTransformedCanvas();

        public abstract ItemsControl GetItemsControl();

        // This uses a content presenter mainly because of this http://microsoft.github.io/Win2D/html/RefCycles.htm
        // If that link dies, google win2d canvascontrol refcycle
        // If nothing comes up, maybe the issue is fixed.
        // Currently, the issue is that CanvasControls make it extremely easy to create memory leaks and need to be dealt with carefully
        // As the link states, adding handlers to events on a CanvasControl creates reference cycles that the GC can't detect, so the events need to be handled manually
        // What the link doesn't say is that apparently having events on any siblings of the CanvasControl without having events on the CanvasControl still somehow prevents it from
        // being GC-ed, so it is easier to just create and destroy it on load and unload rather than try to manage all of the events and references... 
        // Because we create it in this class, we need a content presenter to put it in, otherwise the subclass can't decide where to put it
        // This ContentPresenter also shouldn't have any events attached to it, as for some reason that also messes with the Garbage Collection
        public abstract ContentPresenter GetBackgroundContentPresenter();
        private CanvasControl _backgroundCanvas;

        public abstract Grid GetOuterGrid();

        public abstract Canvas GetSelectionCanvas();

        public abstract Rectangle GetDropIndicationRectangle();

        public abstract Canvas GetInkHostCanvas();

        //records number of fingers on screen for touch interactions
        public static int NumFingers;
        private List<PointerRoutedEventArgs> handledTouch = new List<PointerRoutedEventArgs>();

        protected CollectionFreeformBase()
        {
            Loaded += OnBaseLoaded;
            Unloaded += OnBaseUnload;
            KeyDown += _marquee_KeyDown;

            previewTextbox = new TextBox
            {
                Width = 200,
                Height = 50,
                Background = new SolidColorBrush(Colors.Transparent),
                Visibility = Visibility.Collapsed,
                ManipulationMode = ManipulationModes.All
            };
            previewTextbox.LostFocus += (s, e) => previewTextbox.Visibility = Visibility.Collapsed;
            previewTextbox.KeyDown += PreviewTextbox_KeyDown;
        }

        private void OnBaseLoaded(object sender, RoutedEventArgs e)
        {
            if (_backgroundCanvas == null)
            {
                _backgroundCanvas = new CanvasControl();
                _backgroundCanvas.Height = 2000;
                GetBackgroundContentPresenter().VerticalContentAlignment = VerticalAlignment.Top;
                GetBackgroundContentPresenter().VerticalAlignment = VerticalAlignment.Top;
                GetBackgroundContentPresenter().Content = _backgroundCanvas;
                _backgroundCanvas.CreateResources += CanvasControl_OnCreateResources;
                _backgroundCanvas.LayoutUpdated += _backgroundCanvas_LayoutUpdated;
            }
            _backgroundCanvas.Draw += CanvasControl_OnDraw;
            _backgroundCanvas.VerticalAlignment = VerticalAlignment.Stretch;

            GetInkHostCanvas().Children.Clear();
            GetInkHostCanvas().Children.Add(previewTextbox);

            //make and add selectioncanvas 
            SelectionCanvas = new Canvas();
            Canvas.SetLeft(SelectionCanvas, -30000);
            Canvas.SetTop(SelectionCanvas, -30000);
            //Canvas.SetZIndex(GetInkHostCanvas(), 2);//Uncomment this to get the Marquee on top, but it causes issues with regions
            GetInkHostCanvas().Children.Add(SelectionCanvas);

            if (ViewModel.InkController == null)
                ViewModel.ContainerDocument.SetField<InkController>(KeyStore.InkDataKey, new List<InkStroke>(), true);
            MakeInkCanvas();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            setBackground -= ChangeBackground;
            setBackground += ChangeBackground;
            setBackgroundOpacity -= ChangeOpacity;
            setBackgroundOpacity += ChangeOpacity;

            var settingsView = MainPage.Instance.SettingsView;
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

        private void _backgroundCanvas_LayoutUpdated(object sender, object e)
        {
            var realParent = _backgroundCanvas.GetFirstAncestorOfType<ContentPresenter>()?.Parent as FrameworkElement;
            if (realParent != null)
            {
                var realRect = realParent.TransformToVisual(_backgroundCanvas).TransformBounds(new Rect(new Point(), new Size(realParent.ActualWidth, realParent.ActualHeight)));
                var mainBounds = _backgroundCanvas.TransformToVisual(MainPage.Instance.xOuterGrid).TransformBounds(realRect);
                var mainClipRect = new Rect(new Point(Math.Max(0, mainBounds.Left), Math.Max(0, mainBounds.Top)),
                                            new Point(Math.Min(mainBounds.Right, MainPage.Instance.xOuterGrid.ActualWidth), Math.Min(mainBounds.Bottom, MainPage.Instance.xOuterGrid.ActualHeight)));
                var clipBounds = MainPage.Instance.xOuterGrid.TransformToVisual(_backgroundCanvas).TransformBounds(mainClipRect);
                var newHeight = Math.Min(8000, Math.Max(0, clipBounds.Bottom));
                if (newHeight != _backgroundCanvas.Height)
                {
                    _backgroundCanvas.Height = newHeight;
                }
            }
        }

        private void OnBaseUnload(object sender, RoutedEventArgs e)
        {
            if (_backgroundCanvas != null)
            {
                _backgroundCanvas.CreateResources -= CanvasControl_OnCreateResources;
                _backgroundCanvas.Draw -= CanvasControl_OnDraw;
                _backgroundCanvas.LayoutUpdated -= _backgroundCanvas_LayoutUpdated;
            }
            if (_lastViewModel != null)
            {
                _lastViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            _lastViewModel = null;
            setBackground -= ChangeBackground;
            setBackgroundOpacity -= ChangeOpacity;
        }

        protected void OnDataContextChanged(object sender, DataContextChangedEventArgs e)
        {
            _lastViewModel = ViewModel;
        }

        protected void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            //Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.IBeam, 1);
        }

        protected void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            //Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
        }

        protected void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var grid = sender as Grid;
            if (grid?.Clip != null)
            {
                grid.Clip.Rect = new Rect(0, 0, grid.ActualWidth, grid.ActualHeight);
            }
        }
        public void OnDocumentSelected(bool selected)
        {
        }


        #region Manipulation
        /// <summary>
        /// Animation storyboard for first half. Unfortunately, we can't use the super useful AutoReverse boolean of animations to do this with one storyboard
        /// </summary>
        private Storyboard _storyboard1, _storyboard2;

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
            var old = (xTransformedCanvas?.RenderTransform as MatrixTransform)?.Matrix;
            if (old == null)
            {
                return;
            }
            _transformBeingAnimated = new MatrixTransform() { Matrix = (Matrix)old };

            Debug.Assert(_transformBeingAnimated != null);
            var milliseconds = 1000;
            var duration = new Duration(TimeSpan.FromMilliseconds(milliseconds));

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
            var translateAnimationY = MakeAnimationElement(_transformBeingAnimated, startY, startY + translate.Y, "MatrixTransform.Matrix.OffsetY", duration);
            translateAnimationX.AutoReverse = false;
            translateAnimationY.AutoReverse = false;

            _storyboard1.Children.Add(translateAnimationX);
            _storyboard1.Children.Add(translateAnimationY);

            CompositionTarget.Rendering -= CompositionTargetOnRendering;
            CompositionTarget.Rendering += CompositionTargetOnRendering;

            // Begin the animation.
            _storyboard1.Begin();
            _storyboard1.Completed -= Storyboard1OnCompleted;
            _storyboard1.Completed += Storyboard1OnCompleted;
        }

        public void SetTransform(TranslateTransform translate, ScaleTransform scale)
        {
            var composite = new TransformGroup();
            //composite.Children.Add((GetItemsControl()?.ItemsPanelRoot as Canvas).RenderTransform);
            if (scale != null)
            {
                composite.Children.Add(scale);
            }
            composite.Children.Add(translate);

            var matrix = composite.Value;
            ViewModel.TransformGroup = new TransformGroupData(new Point(matrix.OffsetX, matrix.OffsetY), new Point(matrix.M11, matrix.M22));
            ViewManipulationControls.ElementScale = matrix.M11;
        }

        public void SetTransformAnimated(TranslateTransform translate, ScaleTransform scale)
        {
            UndoManager.StartBatch();
            //get rendering postion of _itemsPanelCanvas, 2x3 matrix
            var old = (xTransformedCanvas?.RenderTransform as MatrixTransform)?.Matrix;
            if (old == null)
            {
                return;
            }
            //set transformBeingAnimated to matrix of old
            _transformBeingAnimated = new MatrixTransform() { Matrix = (Matrix)old };

            Debug.Assert(_transformBeingAnimated != null);
            var milliseconds = 1000;
            var duration = new Duration(TimeSpan.FromMilliseconds(milliseconds));

            //Clear storyboard
            _storyboard1?.Stop();
            _storyboard1?.Children.Clear();
            _storyboard1 = new Storyboard { Duration = duration };

            _storyboard2?.Stop();
            _storyboard2?.Children.Clear();
            _storyboard2 = new Storyboard { Duration = duration };

            var startMatrix = _transformBeingAnimated.Matrix;

            var scaleMatrix = scale.GetMatrix();

            //Create a Double Animation for zooming in and out. Unfortunately, the AutoReverse bool does not work as expected.
            //the higher number, the more it xooms, but doesn't actually change final view 
            var zoomAnimationX = MakeAnimationElement(_transformBeingAnimated, startMatrix.M11, scaleMatrix.M11, "MatrixTransform.Matrix.M11", duration);
            var zoomAnimationY = MakeAnimationElement(_transformBeingAnimated, startMatrix.M22, scaleMatrix.M22, "MatrixTransform.Matrix.M22", duration);

            _storyboard1.Children.Add(zoomAnimationX);
            _storyboard1.Children.Add(zoomAnimationY);

            // Create a DoubleAnimation for translating
            var translateAnimationX = MakeAnimationElement(_transformBeingAnimated, startMatrix.OffsetX, translate.X + scaleMatrix.OffsetX, "MatrixTransform.Matrix.OffsetX", duration);
            var translateAnimationY = MakeAnimationElement(_transformBeingAnimated, startMatrix.OffsetY, translate.Y + scaleMatrix.OffsetY, "MatrixTransform.Matrix.OffsetY", duration);

            _storyboard1.Children.Add(translateAnimationX);
            _storyboard1.Children.Add(translateAnimationY);


            CompositionTarget.Rendering -= CompositionTargetOnRendering;
            CompositionTarget.Rendering += CompositionTargetOnRendering;

            // Begin the animation.
            _storyboard1.Completed -= Storyboard1OnCompleted;
            _storyboard1.Completed += Storyboard1OnCompleted;
            _storyboard1.Begin();
        }

        protected void Storyboard1OnCompleted(object sender, object e)
        {
            CompositionTarget.Rendering -= CompositionTargetOnRendering;
            _storyboard1.Completed -= Storyboard1OnCompleted;
            UndoManager.EndBatch();
        }

        protected void CompositionTargetOnRendering(object sender, object e)
        {
            var matrix = _transformBeingAnimated.Matrix;
            ViewModel.TransformGroup = new TransformGroupData(new Point(matrix.OffsetX, matrix.OffsetY), new Point(matrix.M11, matrix.M22));
            ViewManipulationControls.ElementScale = matrix.M11; // bcz: don't update elementscale to have no zoom bounds on jumping between things (not scroll zooming)
        }

        protected DoubleAnimation MakeAnimationElement(MatrixTransform matrix, double from, double to, String name, Duration duration)
        {

            var toReturn = new DoubleAnimation();
            toReturn.EnableDependentAnimation = true;
            toReturn.Duration = duration;
            //Storyboard.TargetProperty targets a particular property of the element as named by Storyboard.TargetName
            Storyboard.SetTarget(toReturn, matrix);
            Storyboard.SetTargetProperty(toReturn, name);

            //The animation progresses from the value specified by the From property to the value specified by the To property
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
                composite.Children.Add(xTransformedCanvas.RenderTransform); // get the current transform            
            composite.Children.Add(translateDelta); // add the new translate
            composite.Children.Add(scaleDelta); // add the new scaling
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
        private static object _backgroundDot = "ms-appx:///Assets/transparent_dot_tilable.png";
        private CanvasBitmap _bgImage;
        private CanvasBitmap _bgImageDot;

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
            _backgroundCanvas?.Invalidate();
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
            _backgroundTask = LoadBackgroundAsync(_backgroundCanvas);
        }

        // 4
        protected async Task LoadBackgroundAsync(CanvasControl canvas)
        {
            // Convert the <IRandomAccessStream> and update the <CanvasBitmap> instance var to be used later by the <CanvasImageBrush> in CanvasControl_OnDraw
            if (_background is string s) // i.e. A rightfully unconverted ms-appx path
                _bgImage = await CanvasBitmap.LoadAsync(canvas, new Uri(s));
            else
                _bgImage = await CanvasBitmap.LoadAsync(canvas, (IRandomAccessStream)_background);
            _bgImageDot = await CanvasBitmap.LoadAsync(canvas, new Uri((string)_backgroundDot));
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
                //var ff    = this as CollectionFreeformView;
                //var mat   = ff?.xTransformedCanvas?.RenderTransform as MatrixTransform;
                //var scale = mat?.Matrix.M11 ?? 1;
                //// If it successfully loaded, set the desired image and the opacity of the <CanvasImageBrush>
                //_bgBrush.Image = scale < 1 ? _bgImageDot : _bgImage;
                //_bgBrush.Opacity = _bgOpacity;

                //// Lastly, fill a rectangle with the tiling image brush, covering the entire bounds of the canvas control
                //var drawRect = new Rect(new Point(), new Size(sender.Size.Width, sender.Size.Height));
                //args.DrawingSession.FillRectangle(drawRect, _bgBrush);
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

                    var aliasSafeScale = matrix.M11;// clampBackgroundScaleForAliasing(matrix.M11, NumberOfBackgroundRows);
                    _bgBrush.Transform = new Matrix3x2((float)aliasSafeScale,
                        (float)matrix.M12,
                        (float)matrix.M21,
                        (float)aliasSafeScale,
                        (float)matrix.OffsetX,
                        (float)matrix.OffsetY);
                    _backgroundCanvas.Invalidate();
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

        #endregion

        #region Marquee Select

        private Rectangle   _marquee;
        private Point       _marqueeAnchor;
        private bool        _isMarqueeActive;
        private MarqueeInfo mInfo;

        protected virtual void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (e != null && e.Pointer.PointerDeviceType == PointerDeviceType.Touch && sender != null && !handledTouch.Contains(e))
            {
                handledTouch.Add(e);
                if (NumFingers > 0) NumFingers--;
            }
            if (_marquee != null)
            {
                var pos = Util.PointTransformFromVisual(new Point(Canvas.GetLeft(_marquee), Canvas.GetTop(_marquee)),
                    GetSelectionCanvas(), GetItemsControl().ItemsPanelRoot);
                var marqueeDocs = DocsInMarquee(new Rect(pos, new Size(_marquee.Width, _marquee.Height)));
                if (marqueeDocs.Any())
                {
                    SelectionManager.SelectDocuments(marqueeDocs, this.IsShiftPressed());
                    Focus(FocusState.Programmatic);
                }
                ResetMarquee(true);
                if (e != null) e.Handled = true;
            }

            if (NumFingers == 0) ViewManipulationControls.IsPanning = false;

            GetOuterGrid().PointerMoved -= OnPointerMoved;
            //if (e != null) GetOuterGrid().ReleasePointerCapture(e.Pointer);
        }

        protected virtual void OnPointerCancelled(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch && sender != null && !handledTouch.Contains(e))
            {
                handledTouch.Add(e);
                if (NumFingers > 0) NumFingers--;
            }
            if (_marquee != null)
            {
                var pos = Util.PointTransformFromVisual(new Point(Canvas.GetLeft(_marquee), Canvas.GetTop(_marquee)),
                    GetSelectionCanvas(), GetItemsControl().ItemsPanelRoot);
                SelectionManager.SelectDocuments(DocsInMarquee(new Rect(pos, new Size(_marquee.Width, _marquee.Height))), this.IsShiftPressed());
                GetSelectionCanvas().Children.Remove(_marquee);
                _marquee = null;
                _isMarqueeActive = false;
                if (e != null) e.Handled = true;
            }
            if (NumFingers == 0) ViewManipulationControls.IsPanning = false;
        }

        public bool StartMarquee(Point pos)
        {
            if (_isMarqueeActive)
            {
                var dX = pos.X - _marqueeAnchor.X;
                var dY = pos.Y - _marqueeAnchor.Y;

                //Debug.WriteLine(dX + " and " + dY);

                //if (_marquee == null)
                //{
                //    dX = dX + MainPage.Instance.xMainTreeView.ActualWidth;
                //}

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
                        Stroke = new SolidColorBrush(Color.FromArgb(200, 66, 66, 66)),
                        StrokeThickness = 1.5 / Zoom,
                        StrokeDashArray = new DoubleCollection { 4, 1 },
                        CompositeMode = ElementCompositeMode.SourceOver
                    };
                    this.IsTabStop = true;
                    this.Focus(FocusState.Pointer);
                    _marquee.AllowFocusOnInteraction = true;
                    SelectionCanvas?.Children.Add(_marquee);

                    mInfo = new MarqueeInfo();
                    SelectionCanvas?.Children.Add(mInfo);
                }

                if (_marquee != null) //Adjust the marquee rectangle
                {
                    Canvas.SetLeft(_marquee, newAnchor.X);
                    Canvas.SetTop(_marquee, newAnchor.Y);
                    _marquee.Width = newWidth;
                    _marquee.Height = newHeight;

                    Canvas.SetLeft(mInfo, newAnchor.X);
                    Canvas.SetTop(mInfo, newAnchor.Y - 32);

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Handles mouse movement.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected virtual void OnPointerMoved(object sender, PointerRoutedEventArgs args)
        {
            if (args.GetCurrentPoint(null).Properties.PointerUpdateKind == PointerUpdateKind.Other)
            {
                var pos = args.GetCurrentPoint(SelectionCanvas).Position;
                if (StartMarquee(pos))
                {
                    args.Handled = true;
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
            if (args.Pointer.PointerDeviceType == PointerDeviceType.Touch && !handledTouch.Contains(args))
            {
                handledTouch.Add(args);
                NumFingers++;
                ViewManipulationControls.IsPanning = false;
            }
            if (this.GetDocumentView().AreContentsActive && args.IsLeftPressed())
            {
                GetOuterGrid().CapturePointer(args.Pointer);
                _marqueeAnchor = args.GetCurrentPoint(SelectionCanvas).Position;
                _isMarqueeActive = true;
                previewTextbox.Visibility = Visibility.Collapsed;
                args.Handled = true;
                GetOuterGrid().PointerMoved -= OnPointerMoved;
                GetOuterGrid().PointerMoved += OnPointerMoved;
            }
        }

        private static readonly List<VirtualKey> MarqueeKeys = new List<VirtualKey>
        {
            VirtualKey.A,
            VirtualKey.Back,
            VirtualKey.C,
            VirtualKey.Delete,
            VirtualKey.G,
            VirtualKey.R,
            VirtualKey.T,
            VirtualKey.Left,
            VirtualKey.Right,
            VirtualKey.Up,
            VirtualKey.Down,
        };

        private void _marquee_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!(FocusManager.GetFocusedElement() is RichEditBox) && !(FocusManager.GetFocusedElement() is TextBox))
            {
                var useMarquee = _marquee != null && MarqueeKeys.Contains(e.Key) && _isMarqueeActive;
                TriggerActionFromSelection(e.Key, useMarquee);
            }
            PreviewTextBuffer += Util.KeyCodeToUnicode(e.Key, this.IsShiftPressed(), this.IsCapsPressed());
        }

        public bool IsMarqueeActive => _isMarqueeActive;

        // called by SelectionManager to reset this collection's internal selection-based logic
        public void ResetMarquee(bool hardClear)
        {
            if (hardClear)
            {
                SelectionCanvas?.Children?.Clear();
                _marquee = null;
                _isMarqueeActive = false;
            }
            else
            {
                foreach (var selectionCanvasChild in SelectionCanvas.Children)
                {
                    //This is a hack because modifying the visual tree during a manipulation seems to screw up UWP
                    selectionCanvasChild.Visibility = Visibility.Collapsed;
                }
            }
        }

        public IEnumerable<DocumentView> DocsInMarquee(Rect marquee)
        {
            if (GetItemsControl().ItemsPanelRoot != null)
            {
                var items = GetItemsControl().ItemsPanelRoot.Children.Select(i => i.GetFirstDescendantOfType<DocumentView>()).
                                Where(dv => dv != null && marquee.IntersectsWith(dv.ViewModel.LayoutDocument.GetBounds()));
                var docViewsSelected = items.Where(dv => !dv.ViewModel.LayoutDocument.DocumentType.Equals(BackgroundShape.DocumentType) && 
                                                          dv.ViewModel.LayoutDocument.GetAreContentsHitTestVisible());

                foreach (var dv in docViewsSelected.Any() ?  docViewsSelected : items)
                {
                    yield return dv;
                }
            }
        }

        public Rect GetBoundingRectFromSelection()
        {
            var topLeftMostPoint = new Point(double.PositiveInfinity, double.PositiveInfinity);
            var bottomRightMostPoint = new Point(double.NegativeInfinity, double.NegativeInfinity);

            bool isEmpty = true;

            foreach (var d in SelectionManager.SelectedDocViewModels.Select(dv => dv.LayoutDocument))
            {
                isEmpty = false;
                topLeftMostPoint.X = d.GetPosition().X < topLeftMostPoint.X ? d.GetPosition().X : topLeftMostPoint.X;
                topLeftMostPoint.Y = d.GetPosition().Y < topLeftMostPoint.Y ? d.GetPosition().Y : topLeftMostPoint.Y;
                var actualX = (double.IsNaN(d.GetActualSize().X) ? 0 : d.GetActualSize().X);
                var actualY =(double.IsNaN(d.GetActualSize().Y) ? 0 : d.GetActualSize().Y);
                bottomRightMostPoint.X = d.GetPosition().X + actualX > bottomRightMostPoint.X
                    ? d.GetPosition().X + actualX : bottomRightMostPoint.X;
                bottomRightMostPoint.Y = d.GetPosition().Y + actualY > bottomRightMostPoint.Y
                    ? d.GetPosition().Y + actualY : bottomRightMostPoint.Y;
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
            void DoAction(Action<IEnumerable<DocumentViewModel>, Point, Size> action)
            {
                if (fromMarquee)
                {
                    var where = Util.PointTransformFromVisual(new Point(Canvas.GetLeft(_marquee), Canvas.GetTop(_marquee)),
                                                          SelectionCanvas, GetItemsControl().ItemsPanelRoot);
                    var size = new Size(_marquee.Width, _marquee.Height);
                    using (UndoManager.GetBatchHandle())
                    {
                        action(DocsInMarquee(new Rect(where, size)).Select(dv => dv.ViewModel), where, size);
                    }
                }
                else
                {
                    if (GetBoundingRectFromSelection() is Rect bounds && bounds != Rect.Empty)
                    {
                        using (UndoManager.GetBatchHandle())
                        {
                            action(SelectionManager.SelectedDocViewModels, new Point(bounds.X, bounds.Y), new Size(bounds.Width, bounds.Height));
                        }
                    }
                }
                
                ResetMarquee(false);
            }

            var type = CollectionViewType.Freeform;

            var deselect = false;
            if (!this.IsAltPressed() && (SelectionManager.SelectedDocViewModels.Count() > 1 || fromMarquee|| modifier == VirtualKey.Back || modifier == VirtualKey.Delete)) { 
                switch (modifier)
                {
                case VirtualKey.A:  //create a viewcopy of everything selected
                    DoAction((viewModels, where, size) =>
                    {
                        var docs = viewModels.Select(dvm => dvm.DocumentController.GetViewCopy()).ToList();
                        ViewModel.AddDocument(new CollectionNote(where, type, size.Width, size.Height, docs).Document);
                    });
                    deselect = true;
                    break;
                case VirtualKey.T:
                    type = CollectionViewType.Schema;
                    goto case VirtualKey.C;
                case VirtualKey.C:
                    DoAction((viewModels, where, size) =>
                        {
                            var docss = viewModels.Select(dvm => dvm.DocumentController).ToList();
                            ViewModel.AddDocument(new CollectionNote(where, type, size.Width, size.Height, docss).Document);

                            foreach (var viewModel in viewModels.ToArray())
                            {
                                viewModel.RequestDelete();
                            }
                        });
                    deselect = true;
                    break;
                case VirtualKey.Down:
                case VirtualKey.Left:
                case VirtualKey.Up:
                case VirtualKey.Right:
                    if (!MainPage.Instance.IsShiftPressed()) // arrow aligns to left or right (ctrl + arrow aligns to horizontal or vertical center)
                    {
                        DoAction((viewModels, where, size) =>
                        {
                            var docDec = MainPage.Instance.XDocumentDecorations;
                            var rect = docDec.TransformToVisual(GetTransformedCanvas()).TransformBounds(new Rect(new Point(),new Size(docDec.ContentColumn.Width.Value,docDec.ContentRow.Height.Value)));
                            var centered = MainPage.Instance.IsCtrlPressed();
                            foreach (var d in viewModels.Select(v => v.LayoutDocument))
                            {
                                double alignedX = d.GetPosition().X;
                                double alignedY = d.GetPosition().Y;
                                if (centered)
                                {
                                    alignedX = (modifier == VirtualKey.Down || modifier == VirtualKey.Up) ? (rect.Left + rect.Right) / 2 - d.GetActualSize().X / 2 : alignedX;
                                    alignedY = (modifier == VirtualKey.Left || modifier == VirtualKey.Right) ? (rect.Top + rect.Bottom) / 2 - d.GetActualSize().Y / 2 : alignedY;

                                }
                                else
                                {
                                    alignedX = modifier == VirtualKey.Left ? rect.Left : modifier == VirtualKey.Right ? rect.Right - d.GetActualSize().X : alignedX;
                                    alignedY = modifier == VirtualKey.Up ? rect.Top : modifier == VirtualKey.Down ? rect.Bottom - d.GetActualSize().Y : alignedY;
                                }
                                d.SetPosition(new Point(alignedX, alignedY));
                            }
                        });
                    }
                    else // shift + arrow distributes objects horizontally or vertically
                    {
                        DoAction((views, where, size) =>
                        {
                            var sortY = modifier == VirtualKey.Down || modifier == VirtualKey.Up;
                            var sortedViewModels = views.ToList();
                            sortedViewModels.Sort((dv1, dv2) =>
                            {
                                var v1p = dv1.LayoutDocument.GetPosition();
                                var v2p = dv2.LayoutDocument.GetPosition();
                                var v1 = sortY ? v1p.Y : v1p.X;
                                var v2 = sortY ? v2p.Y : v2p.X;
                                var v1o = sortY ? v1p.X : v1p.Y;
                                var v2o = sortY ? v2p.X : v2p.Y;
                                if (v1 < v2)        return -1;
                                else if (v1 > v2)   return  1;
                                else if (v1o < v2o) return -1;
                                else if (v1o > v2o) return  1;
                                return 0;
                            });

                            var docDec       = MainPage.Instance.XDocumentDecorations;
                            var usedDim      = sortedViewModels.Aggregate(0.0, (val,view) => val + (sortY ? view.LayoutDocument.GetActualSize().Y : view.LayoutDocument.GetActualSize().X));
                            var bounds       = docDec.TransformToVisual(GetTransformedCanvas()).TransformBounds(new Rect(new Point(),new Size(docDec.ContentColumn.Width.Value, docDec.ContentRow.Height.Value)));
                            var spacing      = ((sortY ? bounds.Height: bounds.Width) -usedDim) / (sortedViewModels.Count() -1);
                            double placement = sortY ? bounds.Top : bounds.Left;
                            if (modifier == VirtualKey.Down || modifier == VirtualKey.Left)
                            {
                                sortedViewModels.Reverse();
                            }

                            foreach (var v in sortedViewModels)
                            {
                                if (modifier == VirtualKey.Down || modifier == VirtualKey.Up)
                                {
                                    v.LayoutDocument.SetPosition(new Point(v.LayoutDocument.GetPosition().X, placement));
                                    placement += v.LayoutDocument.GetActualSize().Y + spacing;
                                }
                                if (modifier == VirtualKey.Left || modifier == VirtualKey.Right)
                                {
                                    v.LayoutDocument.SetPosition(new Point(placement, v.LayoutDocument.GetPosition().Y));
                                    placement += v.LayoutDocument.GetActualSize().X + spacing;
                                }
                            }
                        });
                    }
                    break;
                case VirtualKey.Back:
                case VirtualKey.Delete:
                    DoAction((viewModels, where, size) => viewModels.ToList().ForEach(dvm => dvm.RequestDelete()));
                    deselect = true;
                    break;
                case VirtualKey.G:
                    DoAction((views, where, size) => ViewModel.AddDocument(Util.AdornmentWithPosition(BackgroundShape.AdornmentShape.Rectangular, where, size.Width, size.Height)));
                    deselect = true;
                    break;
                case VirtualKey.R:
                    DoAction((views, where, size) => ((size.Width >= 215 && size.Height >= 200) ? ViewModel : null)?.AddDocument(new DishReplBox(where.X, where.Y, size.Width, size.Height).Document));
                    deselect = true;
                    break;
                }
            }

            if (deselect)
            {
                SelectionManager.DeselectAll();
            }
        }

        #endregion

        #region DragAndDrop

        public void SetDropIndicationFill(Brush fill)
        {
            GetDropIndicationRectangle().Fill = fill;
        }

        #endregion

        public FreeformInkControl InkControl;
        public InkCanvas XInkCanvas;
        public Canvas SelectionCanvas;
        public bool _doubleTapped = false;
        public double Zoom => ViewManipulationControls.ElementScale;

        void MakeInkCanvas()
        {

            if (MainPage.Instance.xSettingsView.UseInkCanvas)
            {
                XInkCanvas = new InkCanvas()
                {
                    Width = 60000,
                    Height = 60000
                };

                //InkControl = new FreeformInkControl(this, XInkCanvas, SelectionCanvas);
                Canvas.SetLeft(XInkCanvas, -30000);
                Canvas.SetTop(XInkCanvas, -30000);
                GetInkHostCanvas().Children.Add(XInkCanvas);
            }
        }

        #region TextInputBox

        static public string PreviewFormatString = "#";
        public string PreviewTextBuffer { get; set; } = "";

        private TextBox previewTextbox  = null;
        public void ClearPreview()
        {
            previewTextbox.Visibility = Visibility.Collapsed;
            previewTextbox.Text = string.Empty;
        }

        protected  void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            _doubleTapped = true;
        }
        protected async void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            _doubleTapped = false;
            await Task.Delay(100);
            if (!_doubleTapped)
            {
                _isMarqueeActive = false;
                if (!this.IsShiftPressed())
                {
                    var pt = e.GetPosition(xTransformedCanvas);
                    ShowPreviewTextbox(pt);
                }
                foreach (var rtv in Content.GetDescendantsOfType<RichTextView>())
                {
                    rtv.Document.Selection.EndPosition = rtv.Document.Selection.StartPosition;
                }
            }
        }
        private void ShowPreviewTextbox(Point where)
        {
            PreviewTextBuffer = PreviewFormatString;
            if (previewTextbox != null)
            {
                //MainPage.Instance.ClearForceFocus();
                Canvas.SetLeft(previewTextbox, where.X);
                Canvas.SetTop(previewTextbox, where.Y);
                previewTextbox.Visibility = Visibility.Visible;
                previewTextbox.Text = PreviewFormatString;
                previewTextbox.SelectionStart = PreviewFormatString.Length;
                previewTextbox.Focus(FocusState.Pointer);
            }
        }

        private void PreviewTextbox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key.Equals(VirtualKey.Escape))
            {
                e.Handled = true;
                previewTextbox.Visibility = Visibility.Collapsed;
            }
            else if (e.Key == VirtualKey.Back)
            {
                PreviewTextBuffer = PreviewTextBuffer == PreviewFormatString ? "" : PreviewTextBuffer;
                previewTextbox.Text = PreviewTextBuffer;
            }
            else
            {
                var text = Util.KeyCodeToUnicode(e.Key, this.IsShiftPressed(), this.IsCapsPressed());
                if (!string.IsNullOrEmpty(text) && previewTextbox.Visibility == Visibility.Visible)
                {
                    e.Handled = true;
                    convertPreviewToRealText(this.IsCtrlPressed() && e.Key == VirtualKey.V ? null : text);
                }
            }
        }

        private void convertPreviewToRealText(string text)
        {
            var where = new Point(Canvas.GetLeft(previewTextbox), Canvas.GetTop(previewTextbox));
            using (UndoManager.GetBatchHandle())
            {
                if (text == null && Clipboard.GetContent()?.HasClipboardData() == true) // clipboard will have data if from outside of Dash, otherwise fall through to paste from within dash
                {
                    foreach (var doc in Clipboard.GetContent().GetClipboardData().GetDocuments(where))
                    {
                        ViewModel.AddDocument(doc);
                    }
                }
                else
                {
                    PreviewTextBuffer += text;
                    //if (MainPage.Instance.ForceFocusPoint == null)
                    //{
                    //    LoadNewActiveTextBox(text, where);
                    //}
                }
                if (text == null)
                {
                    previewTextbox.Visibility = Visibility.Collapsed;
                }
            }
        }

        public async void LoadNewActiveTextBox(string text, Point where)
        {
            var postitNote  = text == null ? await ViewModel.Paste(Clipboard.GetContent(), where) : SettingsView.Instance.MarkdownEditOn ? new MarkdownNote(text: text).Document : new RichTextNote(text: text).Document;
            var defaultXaml = ViewModel.ContainerDocument.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DefaultTextboxXamlKey, null)?.Data;
            if (!string.IsNullOrEmpty(defaultXaml))
            {
                postitNote.SetXaml(defaultXaml);
            }
            if (text != null)
            {
                //MainPage.Instance.SetForceFocusPoint(this, xTransformedCanvas.TransformToVisual(MainPage.Instance).TransformPoint(new Point(where.X + 1, where.Y + 1)));

                Actions.DisplayDocument(ViewModel, postitNote, where);
            } else
            {
                //MainPage.Instance.ClearForceFocus();
            }

            ViewModel.GenerateDocumentAddedEvent(postitNote, Util.PointTransformFromVisual(postitNote.GetPosition(), xTransformedCanvas, MainPage.Instance));
        }

        #endregion
    }
}
