using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Visibility = Windows.UI.Xaml.Visibility;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Animation;
using Dash.Annotations;
using Dash.Views.Collection;
using DashShared;
using NewControls.Geometry;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionFreeformView : ICollectionView
    {
        MatrixTransform     _transformBeingAnimated;// Transform being updated during animation
        Canvas              _itemsPanelCanvas => xItemsControl.ItemsPanelRoot as Canvas;
        CollectionViewModel _lastViewModel = null;
        List<DocumentView>  _selectedDocs = new List<DocumentView>();

        public ViewManipulationControls  ViewManipulationControls { get; set; }
        public bool                      TagMode { get; set; }
        public KeyController             TagKey { get; set; }
        public CollectionViewModel       ViewModel { get => DataContext as CollectionViewModel; }
        public IEnumerable<DocumentView> SelectedDocs { get => _selectedDocs.Where((dv) => dv?.ViewModel?.DocumentController != null).ToList(); }
        public DocumentView              ParentDocument => this.GetFirstAncestorOfType<DocumentView>();

        public CollectionFreeformView()
        {
            InitializeComponent();
            DataContextChanged += (s, args) => _lastViewModel = ViewModel;
            Loaded += (sender, e) =>
            {
                MakePreviewTextbox();

                //make and add selectioncanvas 
                SelectionCanvas = new Canvas();
                Canvas.SetLeft(SelectionCanvas, -30000);
                Canvas.SetTop(SelectionCanvas, -30000);
                InkHostCanvas.Children.Add(SelectionCanvas);

                if (InkController != null)
                {
                    MakeInkCanvas();
                }
                UpdateLayout(); // bcz: unfortunately, we need this because contained views may not be loaded yet which will mess up FitContents
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            };
            Unloaded += (sender, e) =>
            {
                if (_lastViewModel != null)
                {
                    _lastViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                }
                
                _lastViewModel = null;
            };
            xOuterGrid.PointerEntered  += (sender, e) => Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.IBeam, 1);
            xOuterGrid.PointerExited   += (sender, e) => Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
            xOuterGrid.SizeChanged     += (sender, e) => xClippingRect.Rect = new Rect(0, 0, xOuterGrid.ActualWidth, xOuterGrid.ActualHeight);
            xOuterGrid.PointerPressed  += OnPointerPressed;
            xOuterGrid.PointerReleased += OnPointerReleased;
            ViewManipulationControls = new ViewManipulationControls(this);
            ViewManipulationControls.OnManipulatorTranslatedOrScaled += ManipulationControls_OnManipulatorTranslated;
        }

        public DocumentController Snapshot(bool copyData=false)
        {
            var controllers = new List<DocumentController>();
            foreach (var dvm in ViewModel.DocumentViewModels)
                controllers.Add(copyData  ? dvm.DocumentController.GetDataCopy():dvm.DocumentController.GetViewCopy());
            var snap = new CollectionNote(new Point(), CollectionView.CollectionViewType.Freeform, double.NaN, double.NaN, controllers).Document;
            snap.SetField(KeyStore.CollectionFitToParentKey, new TextController("false"), true);
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
            composite.Children.Add((xItemsControl.ItemsPanelRoot as Canvas).RenderTransform);
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
            var translateAnimationX = MakeAnimationElement(startX, startX + translate.X, "MatrixTransform.Matrix.OffsetX", duration);
            var translateAnimationY = MakeAnimationElement(startY, Math.Min(0, startY + translate.Y), "MatrixTransform.Matrix.OffsetY", duration);
            translateAnimationX.AutoReverse = false;
            translateAnimationY.AutoReverse = false;


            var scaleFactor = Math.Max(0.45, 3000 / Math.Sqrt(translate.X * translate.X + translate.Y * translate.Y));
            //Create a Double Animation for zooming in and out. Unfortunately, the AutoReverse bool does not work as expected.
            var zoomOutAnimationX = MakeAnimationElement(_transformBeingAnimated.Matrix.M11, _transformBeingAnimated.Matrix.M11 * 0.5, "MatrixTransform.Matrix.M11", halfDuration);
            var zoomOutAnimationY = MakeAnimationElement(_transformBeingAnimated.Matrix.M22, _transformBeingAnimated.Matrix.M22 * 0.5, "MatrixTransform.Matrix.M22", halfDuration);

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

        void Storyboard1OnCompleted(object sender, object e)
        {
            CompositionTarget.Rendering -= CompositionTargetOnRendering;
            _storyboard1.Completed -= Storyboard1OnCompleted;
        }

        void CompositionTargetOnRendering(object sender, object e)
        {
            var matrix = _transformBeingAnimated.Matrix;
            ViewModel.TransformGroup = new TransformGroupData(new Point(matrix.OffsetX, matrix.OffsetY), new Point(matrix.M11, matrix.M22));
        }

        DoubleAnimation MakeAnimationElement(double from, double to, String name, Duration duration)
        {

            var toReturn = new DoubleAnimation();
            toReturn.EnableDependentAnimation = true;
            toReturn.Duration = duration;
            Storyboard.SetTarget(toReturn, _transformBeingAnimated);
            Storyboard.SetTargetProperty(toReturn, name);

            toReturn.From = from;
            toReturn.To = to;

            toReturn.EasingFunction = new QuadraticEase();
            return toReturn;

        }

        /// <summary>
        /// Pans and zooms upon touch manipulation 
        /// </summary>   
        void ManipulationControls_OnManipulatorTranslated(TransformGroupData transformation, bool abs)
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

        bool             _resourcesLoaded;
        CanvasImageBrush _bgBrush;
        const double     NumberOfBackgroundRows = 2; // THIS IS A MAGIC NUMBER AND SHOULD CHANGE IF YOU CHANGE THE BACKGROUND IMAGE
        const float      BackgroundOpacity = 1.0f;

        void CanvasControl_OnCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            var task = Task.Run(async () =>
            {
                // Load the background image and create an image brush from it
                var bgImage = await CanvasBitmap.LoadAsync(sender, new Uri("ms-appx:///Assets/transparent_grid_tilable.png"));
                _bgBrush = new CanvasImageBrush(sender, bgImage)
                {
                    Opacity = BackgroundOpacity
                };

                // Set the brush's edge behaviour to wrap, so the image repeats if the drawn region is too big
                _bgBrush.ExtendX = _bgBrush.ExtendY = CanvasEdgeBehavior.Wrap;

                _resourcesLoaded = true;
            });
            args.TrackAsyncAction(task.AsAsyncAction());
        }

        void CanvasControl_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (_resourcesLoaded)
            {
                // Just fill a rectangle with our tiling image brush, covering the entire bounds of the canvas control
                var session = args.DrawingSession;
                session.FillRectangle(new Rect(new Point(), sender.Size), _bgBrush);
            }
        }

        /// <summary>
        /// When the ViewModel's TransformGroup changes, this needs to update its background canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
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
                    xBackgroundCanvas.Invalidate();
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
            TagKeyBox.Visibility = Visibility.Visible;
            var mousePos = Util.PointTransformFromVisual(this.RootPointerPos(), Window.Current.Content, xOuterGrid);
            TagKeyBox.RenderTransform = new TranslateTransform { X = mousePos.X, Y = mousePos.Y };
        }

        public void HideTagKeyBox()
        {
            TagKeyBox.Visibility = Visibility.Collapsed;
        }

        void TagKeyBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var keys = ContentController<FieldModel>.GetControllers<KeyController>();
                var names = keys.Where(k => !k.Name.StartsWith("_"));
                TagKeyBox.ItemsSource = names;
            }
        }

        void TagKeyBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = ((KeyController)args.SelectedItem).Name;
        }

        void TagKeyBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
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
                    TagKey = new KeyController(Guid.NewGuid().ToString(), args.QueryText);
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
        Point     _marqueeAnchor;
        bool      _isMarqueeActive;
        private MarqueeInfo mInfo;
        object    _marqueeKeyHandler = null;

        void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_marquee != null)
            {
                var pos = Util.PointTransformFromVisual(new Point(Canvas.GetLeft(_marquee), Canvas.GetTop(_marquee)),
                    SelectionCanvas, xItemsControl.ItemsPanelRoot);
                SelectDocs(DocsInMarquee(new Rect(pos, new Size(_marquee.Width, _marquee.Height))));
                SelectionCanvas.Children.Remove(_marquee);
                MainPage.Instance.RemoveHandler(KeyDownEvent, new KeyEventHandler(_marquee_KeyDown));
                _marquee = null;
                _isMarqueeActive = false;
                e.Handled = true;
            }

            xOuterGrid.PointerMoved -= OnPointerMoved;
            xOuterGrid.ReleasePointerCapture(e.Pointer);
        }

        /// <summary>
        /// Handles mouse movement.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void OnPointerMoved(object sender, PointerRoutedEventArgs args)
        {
            if (_isMarqueeActive)
            {
                var pos = args.GetCurrentPoint(SelectionCanvas).Position;
                var dX  = pos.X - _marqueeAnchor.X;
                var dY =  pos.Y - _marqueeAnchor.Y;

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
        void OnPointerPressed(object sender, PointerRoutedEventArgs args)
        {
            // marquee on left click by default
            if (MenuToolbar.Instance.GetMouseMode() == MenuToolbar.MouseMode.TakeNote)// bcz:  || args.IsRightPressed())
            {
                if (XInkCanvas.IsTopmost() &&
                    (args.KeyModifiers & VirtualKeyModifiers.Control) == 0 &&
                     ( // bcz: the next line makes right-drag pan within nested collections instead of moving them -- that doesn't seem right to me since MouseMode feels like it applies to left-button dragging only
                       // MenuToolbar.Instance.GetMouseMode() == MenuToolbar.MouseMode.PanFast || 
                     ((!args.GetCurrentPoint(xOuterGrid).Properties.IsRightButtonPressed)) && MenuToolbar.Instance.GetMouseMode() != MenuToolbar.MouseMode.PanFast))
                {
                    if ((args.KeyModifiers & VirtualKeyModifiers.Shift) == 0)
                        DeselectAll();

                    xOuterGrid.CapturePointer(args.Pointer);
                    _marqueeAnchor = args.GetCurrentPoint(SelectionCanvas).Position;
                    _isMarqueeActive = true;
                    PreviewTextbox_LostFocus(null, null);
                    ParentDocument.ManipulationMode = ManipulationModes.None;
                    args.Handled = true;
                    xOuterGrid.PointerMoved -= OnPointerMoved;
                    xOuterGrid.PointerMoved += OnPointerMoved;
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

        public bool IsMarqueeActive()
        {
            return _isMarqueeActive;
        }
        
        public List<DocumentView> DocsInMarquee(Rect marquee)
        {
            var selectedDocs = new List<DocumentView>();
            if (xItemsControl.ItemsPanelRoot != null)
            {
                var docs = xItemsControl.ItemsPanelRoot.Children;
                foreach (var documentView in docs.Select((d)=>d.GetFirstDescendantOfType<DocumentView>()).Where(d => d != null && d.IsHitTestVisible))
                {
                    var rect = documentView.TransformToVisual(_itemsPanelCanvas).TransformBounds(
                        new Rect(new Point(), new Point(documentView.ActualWidth, documentView.ActualHeight)));
                    if (marquee.IntersectsWith(rect))
                    {
                        selectedDocs.Add( documentView);
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

            foreach (DocumentView doc in SelectedDocs)
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
            Point where;
            Rectangle marquee;
            CollectionView.CollectionViewType type = CollectionView.CollectionViewType.Freeform;
            IEnumerable<DocumentView> viewsToSelectFrom;

            if (fromMarquee)
            {
                where = Util.PointTransformFromVisual(new Point(Canvas.GetLeft(_marquee), Canvas.GetTop(_marquee)),
                    SelectionCanvas, xItemsControl.ItemsPanelRoot);
                marquee = _marquee;
               viewsToSelectFrom = DocsInMarquee(new Rect(where, new Size(_marquee.Width, _marquee.Height)));
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
                viewsToSelectFrom = SelectedDocs;
            }

            var toSelectFrom = viewsToSelectFrom.ToList();

            bool deselect = false;
            switch (modifier)
            {
                //create a viewcopy of everything selected
                case VirtualKey.A:
                    var docs = toSelectFrom.Select(dv => dv.ViewModel.DocumentController.GetViewCopy()).ToList();
                    ViewModel.AddDocument(new CollectionNote(where, type, marquee.Width, marquee.Height, docs).Document);
                    deselect = true;
                    break;
                case VirtualKey.T:
                    type = CollectionView.CollectionViewType.Schema;
                    goto case VirtualKey.C;
                case VirtualKey.C:
                    var docss = toSelectFrom.Select(dvm => dvm.ViewModel.DocumentController).ToList();
                    ViewModel.AddDocument(
                        new CollectionNote(where, type, marquee.Width, marquee.Height, docss).Document);
                    goto case VirtualKey.Delete;
                case VirtualKey.Back:
                case VirtualKey.Delete:
                    foreach (var v in toSelectFrom)
                    {
                        v.DeleteDocument();
                    }
                    deselect = true;
                    break;
                case VirtualKey.G:
                    ViewModel.AddDocument(Util.AdornmentWithPosition(BackgroundShape.AdornmentShape.Rectangular, where, marquee.Width, marquee.Height));
                    deselect = true;
                    break;
            }

            if(deselect) DeselectAll();
        }

        #endregion

        #region DragAndDrop

        public void SetDropIndicationFill(Brush fill)
        {
            XDropIndicationRectangle.Fill = fill;
        }

        #endregion

        #region Activation

        void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (XInkCanvas.IsTopmost())
            {
                _isMarqueeActive = false;
                RenderPreviewTextbox(e.GetPosition(_itemsPanelCanvas));
            }
        }

        public void RenderPreviewTextbox(Point where)
        {
            previewTextBuffer = "";
            if (previewTextbox != null)
            {
                Canvas.SetLeft(previewTextbox, where.X);
                Canvas.SetTop(previewTextbox, where.Y);
                previewTextbox.Visibility = Visibility.Visible;
                AddHandler(KeyDownEvent, previewTextHandler, false);
                previewTextbox.Text = string.Empty;
                previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
                previewTextbox.LostFocus += PreviewTextbox_LostFocus;
                previewTextbox.Focus(FocusState.Pointer);
            }
        }
        #endregion

        #region SELECTION
        
        public void DeselectAll()
        {
            SelectionCanvas?.Children?.Clear();
            foreach (var doc in SelectedDocs)
            {
                doc.SetSelectionBorder(false);
            }
            _selectedDocs.Clear();
            _marquee = null;
            _isMarqueeActive = false;
            MainPage.Instance.DeselectAllDocuments();
        }
        
        /// <summary>
        /// Selects all of the documents in selected. Works on a view-specific level.
        /// </summary>
        /// <param name="selected"></param>
        public void SelectDocs(IEnumerable<DocumentView> selected)
        {
            SelectionCanvas.Children.Clear();

            foreach (var doc in selected)
            {
                if (!_selectedDocs.Contains(doc))
                {
                    _selectedDocs.Add(doc);
                    doc.SetSelectionBorder(true);
                }
            }

            MainPage.Instance.SelectDocuments(_selectedDocs);
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
            InkHostCanvas.Children.Add(XInkCanvas);
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
            previewTextbox.Unloaded += (s, e) => RemoveHandler(KeyDownEvent, previewTextHandler);
            InkHostCanvas.Children.Add(previewTextbox);
            previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
            previewTextbox.LostFocus += PreviewTextbox_LostFocus;
        }

        void PreviewTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            RemoveHandler(KeyDownEvent, previewTextHandler);
            previewTextbox.Visibility = Visibility.Collapsed;
            previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
        }

        void PreviewTextbox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var text = KeyCodeToUnicode(e.Key);
            if (string.IsNullOrEmpty(text))
                return;
            RemoveHandler(KeyDownEvent, previewTextHandler);
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
                //TODO: make a markdown box here instead
                if (SettingsView.Instance.MarkdownEditOn)
                {
                    var postitNote = new MarkdownNote(text: text).Document;
                    Actions.DisplayDocument(ViewModel, postitNote, where);

                    var postitNote2 = new PostitNote(text: text).Document;
                    Actions.DisplayDocument(ViewModel, postitNote2, where);
                    //  EditableMarkdownBlock.Instance.XTextBox.Visibility = Visibility.Visible;
                    //var postitNote = new EditableMarkdownBlock();
                    //postitNote.Text = text;
                    //DocumentController noteDoc = await TextToDashUtil.ParseFileAsync();
                    // Actions.DisplayDocument(ViewModel, postitNote.DataContext as EditableScriptViewModel, where);
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
                var keycontroller = KeyController.LookupKeyByName(keyname, true);
                if (containerData.GetField(keycontroller, true) == null)
                    containerData.SetField(keycontroller, containerData.GetField(keycontroller) ?? new TextController("<default>"), true);
                var dbox = new DataBox(new DocumentReferenceController(containerData.Id, keycontroller), where.X, where.Y).Document;
                dbox.Tag = "Auto DataBox " + DateTime.Now.Second + "." + DateTime.Now.Millisecond;
                dbox.SetField(KeyStore.DocumentContextKey, containerData, true);
                Actions.DisplayDocument(ViewModel, dbox, where);
            }
        }

        string KeyCodeToUnicode(VirtualKey key)
        {

            var shiftState = this.IsShiftPressed();
            var capState   = this.IsCapsPressed();
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
        void DocumentViewOnLoaded(object sender, RoutedEventArgs e)
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

        private void EditableScriptView_Loaded(object sender, RoutedEventArgs e)
        {
            var textBox = sender as EditableScriptView;
            textBox.Loaded -= EditableScriptView_Loaded;
            textBox.MakeEditable();
            textBox.XTextBox.GotFocus -= TextBox_GotFocus;
            textBox.XTextBox.GotFocus += TextBox_GotFocus;
            textBox.XTextBox.Focus(FocusState.Programmatic);
        }

        private void TextBox_Loaded(object sender, RoutedEventArgs e)
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