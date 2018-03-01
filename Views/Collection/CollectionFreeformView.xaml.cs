﻿using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using static Dash.NoteDocuments;
using Visibility = Windows.UI.Xaml.Visibility;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Animation;
using Dash.Annotations;
using DashShared.Models;
using NewControls.Geometry;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionFreeformView : ICollectionView, INotifyPropertyChanged
    {
        MatrixTransform     _transformBeingAnimated;// Transform being updated during animation
        TransformGroupData  _transformGroup = new TransformGroupData(new Point(), new Point());
        Canvas              _itemsPanelCanvas => xItemsControl.ItemsPanelRoot as Canvas;
        CollectionViewModel _lastViewModel = null;

        public ViewManipulationControls  ViewManipulationControls { get; set; }
        public bool                      TagMode { get; set; }
        public KeyController             TagKey { get; set; }
        public CollectionViewModel       ViewModel { get => DataContext as CollectionViewModel; }
        public List<DocumentView>        SelectedDocs { get; set; } = new List<DocumentView>();
        public DocumentView              ParentDocument => this.GetFirstAncestorOfType<DocumentView>();
        public TransformGroupData        TransformGroup {
            get => _transformGroup;
            set
            {   //TODO possibly handle a scale center not being 0,0 here
                if (!_transformGroup.Equals(value))
                {
                    _transformGroup = value;
                    var viewdoc = ViewModel.ContainerDocument;
                    if (viewdoc != null)
                    {
                        viewdoc.GetFieldOrCreateDefault<PointController>(KeyStore.PanPositionKey).Data = value.Translate;
                        viewdoc.GetFieldOrCreateDefault<PointController>(KeyStore.PanZoomKey).Data = value.ScaleAmount;
                    }
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public CollectionFreeformView()
        {
            InitializeComponent();
            Loaded += (sender, e) =>
            {
                DataContextChanged -= OnDataContextChanged;
                DataContextChanged += OnDataContextChanged;
                if (ViewModel != null)
                    OnDataContextChanged(null, null);
                setupCanvases();
            };
            Unloaded  += (sender, e) => _lastViewModel = null;
            DragLeave += (sender, e) => ViewModel.CollectionViewOnDragLeave(sender, e);
            DragEnter += (sender, e) => ViewModel.CollectionViewOnDragEnter(sender, e);
            Drop      += (sender, e) => ViewModel.CollectionViewOnDrop(sender, e);
            xOuterGrid.PointerEntered += (sender, e) => Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.IBeam, 1);
            xOuterGrid.PointerExited  += (sender, e) => Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
            xOuterGrid.SizeChanged    += (sender, e) => xClippingRect.Rect = new Rect(0, 0, xOuterGrid.ActualWidth, xOuterGrid.ActualHeight);
            xOuterGrid.PointerPressed  += OnPointerPressed;
            xOuterGrid.PointerReleased += OnPointerReleased;
            ViewManipulationControls = new ViewManipulationControls(this);
            ViewManipulationControls.OnManipulatorTranslatedOrScaled += ManipulationControls_OnManipulatorTranslated;
        }

        #region data configuration

        void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (ViewModel != null && ViewModel != _lastViewModel)
            {
                var viewdoc = ViewModel.ContainerDocument;
                var pos     = viewdoc.GetField<PointController>(KeyStore.PanPositionKey)?.Data ?? new Point();
                var zoom    = viewdoc.GetField<PointController>(KeyStore.PanZoomKey)?.Data ?? new Point(1, 1);
                if (ViewManipulationControls != null)
                {
                    ViewManipulationControls.ElementScale = zoom.X;
                }

                SetFreeformTransform(new MatrixTransform() { Matrix = new Matrix(zoom.X, 0, 0, zoom.Y, pos.X, pos.Y) });
                _lastViewModel = ViewModel;
            }
        }

        [NotifyPropertyChangedInvocator]
        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        void setupCanvases() {

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
            
            if (ParentDocument.ViewModel.LayoutDocument?.GetField(KeyStore.CollectionFitToParentKey) != null)
            {
                ViewManipulationControls.FitToParent();
            }
        }
        #endregion

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

            SetFreeformTransform(new MatrixTransform { Matrix = composite.Value });
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

        private void Storyboard1OnCompleted(object sender, object e)
        {
            CompositionTarget.Rendering -= CompositionTargetOnRendering;
            _storyboard1.Completed -= Storyboard1OnCompleted;
        }

        private void CompositionTargetOnRendering(object sender, object e)
        {
            SetFreeformTransform(_transformBeingAnimated); //Update the transform
        }

        private DoubleAnimation MakeAnimationElement(double from, double to, String name, Duration duration)
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
        private void ManipulationControls_OnManipulatorTranslated(TransformGroupData transformation, bool abs)
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

            var matrix = new MatrixTransform { Matrix = composite.Value };
            SetFreeformTransform(matrix);
        }

        #endregion

        #region BackgroundTiling

        bool _resourcesLoaded;
        CanvasImageBrush _bgBrush;
        const double NumberOfBackgroundRows = 2; // THIS IS A MAGIC NUMBER AND SHOULD CHANGE IF YOU CHANGE THE BACKGROUND IMAGE
        const float BackgroundOpacity = 1.0f;

        void SetFreeformTransform(MatrixTransform matrixTransform)
        {
            // clamp the y offset so that we can only scrollw down
            var matrix = matrixTransform.Matrix;
            if (matrix.OffsetY > 0)
            {
                matrix.OffsetY = 0;
            }

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

                var aliasSafeScale = clampBackgroundScaleForAliasing(matrix.M11, NumberOfBackgroundRows);
                _bgBrush.Transform = new Matrix3x2((float)aliasSafeScale,
                    (float)matrix.M12,
                    (float)matrix.M21,
                    (float)aliasSafeScale,
                    (float)matrix.OffsetX,
                    (float)matrix.OffsetY);
                xBackgroundCanvas.Invalidate();
            }

            TransformGroup = new TransformGroupData(new Point(matrix.OffsetX, matrix.OffsetY), new Point(matrix.M11, matrix.M22));
        }

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
            var mousePos = CoreWindow.GetForCurrentThread().PointerPosition;
            mousePos = new Point(mousePos.X - Window.Current.Bounds.X, mousePos.Y - Window.Current.Bounds.Y);
            Debug.WriteLine(mousePos);
            mousePos = Util.PointTransformFromVisual(mousePos, Window.Current.Content, xOuterGrid);
            TagKeyBox.RenderTransform = new TranslateTransform { X = mousePos.X, Y = mousePos.Y };
        }

        public void HideTagKeyBox()
        {
            TagKeyBox.Visibility = Visibility.Collapsed;
        }

        private void TagKeyBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var keys = ContentController<FieldModel>.GetControllers<KeyController>();
                var names = keys.Where(k => !k.Name.StartsWith("_"));
                TagKeyBox.ItemsSource = names;
            }
        }

        private void TagKeyBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = ((KeyController)args.SelectedItem).Name;
        }

        private void TagKeyBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
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
                    MainPage.Instance.RemoveHandler(KeyDownEvent, new KeyEventHandler(_marquee_KeyDown));
                    MainPage.Instance.AddHandler(KeyDownEvent, new KeyEventHandler(_marquee_KeyDown), true);
                    _marquee.AllowFocusOnInteraction = true;
                    SelectionCanvas.Children.Add(_marquee);
                }

                if (_marquee != null) //Adjust the marquee rectangle
                {
                    Canvas.SetLeft(_marquee, newAnchor.X);
                    Canvas.SetTop(_marquee, newAnchor.Y);
                    _marquee.Width = newWidth;
                    _marquee.Height = newHeight;
                    args.Handled = true;
                }
            }
        }

        void OnPointerPressed(object sender, PointerRoutedEventArgs args)
        {
            if (XInkCanvas.IsTopmost() &&
                (args.KeyModifiers & VirtualKeyModifiers.Control) == 0 &&
                 !args.GetCurrentPoint(xOuterGrid).Properties.IsRightButtonPressed)
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
        
        void _marquee_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var where = Util.PointTransformFromVisual(_marqueeAnchor, SelectionCanvas, this.xItemsControl.ItemsPanelRoot);
            if (_marquee != null && (e.Key == VirtualKey.Back || e.Key == VirtualKey.C))
            {
                var viewsinMarquee = DocsInMarquee(new Rect(where, new Size(_marquee.Width, _marquee.Height)));
                var docsinMarquee = viewsinMarquee.Select((dvm) => dvm.ViewModel.DocumentController).ToList();

                if (e.Key == VirtualKey.C)
                {
                    ViewModel.AddDocument(
                        new CollectionNote(where, CollectionView.CollectionViewType.Freeform, _marquee.Width, _marquee.Height, docsinMarquee).Document, null);
                }

                foreach (var v in viewsinMarquee)
                    v.DeleteDocument();

                DeselectAll();
                MainPage.Instance.RemoveHandler(KeyDownEvent, new KeyEventHandler(_marquee_KeyDown));
                e.Handled = true;
            }
            if (_marquee != null && e.Key == VirtualKey.G)
            {
                ViewModel.AddDocument(Util.BlankDocWithPosition(where, _marquee.Width, _marquee.Height), null);

                DeselectAll();
                MainPage.Instance.RemoveHandler(KeyDownEvent, new KeyEventHandler(_marquee_KeyDown));
                e.Handled = true;
            }
        }

        List<DocumentView> DocsInMarquee(Rect marquee)
        {
            var selectedDocs = new List<DocumentView>();
            if (xItemsControl.ItemsPanelRoot != null)
            {
                var docs = xItemsControl.ItemsPanelRoot.Children;
                foreach (var documentView in docs.Select((d)=>d.GetFirstDescendantOfType<DocumentView>()).Where((d) => d != null))
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

        #endregion

        #region DragAndDrop

        public void SetDropIndicationFill(Brush fill)
        {
            XDropIndicationRectangle.Fill = fill;
        }

        #endregion

        #region Activation

        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (XInkCanvas.IsTopmost())
            {
                DeselectAll();
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
                previewTextbox.Visibility = Visibility.Collapsed;
                previewTextbox.Visibility = Visibility.Visible;
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
            SelectedDocs.Clear();
            _marquee = null;
            _isMarqueeActive = false;
        }
        
        public void SelectDocs(IEnumerable<DocumentView> selected)
        {
            SelectionCanvas.Children.Clear();

            SelectedDocs.AddRange(selected);
            
            foreach (var doc in SelectedDocs)
            {
                doc.SetSelectionBorder(true);
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
            InkHostCanvas.Children.Add(XInkCanvas);
        }

        bool loadingPermanentTextbox;

        TextBox previewTextbox { get; set; }

        void MakePreviewTextbox()
        {
            previewTextbox = new TextBox
            {
                Width = 200,
                Height = 50,
                Background = new SolidColorBrush(Colors.Transparent),
                Visibility = Visibility.Collapsed
            };
            AddHandler(KeyDownEvent, new KeyEventHandler(PreviewTextbox_KeyDown), true);
            InkHostCanvas.Children.Add(previewTextbox);
            previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
            previewTextbox.LostFocus += PreviewTextbox_LostFocus;
        }

        void PreviewTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            previewTextbox.Visibility = Visibility.Collapsed;
            previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
        }

        void PreviewTextbox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control)
                .HasFlag(CoreVirtualKeyStates.Down);
            previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
            var text = KeyCodeToUnicode(e.Key);
            if (text is null) return;
            if (previewTextbox.Visibility == Visibility.Collapsed)
                return;
            e.Handled = true;
            var where = new Point(Canvas.GetLeft(previewTextbox), Canvas.GetTop(previewTextbox));
            if (text == "v" && ctrlState)
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

        public void LoadNewActiveTextBox(string text, Point where, bool resetBuffer = false)
        {
            if (!loadingPermanentTextbox)
            {
                if (resetBuffer)
                    previewTextBuffer = "";
                loadingPermanentTextbox = true;
                var postitNote = new RichTextNote(PostitNote.DocumentType, text: text, size: new Size(400, 40)).Document;
                Actions.DisplayDocument(ViewModel, postitNote, where);
            }
        }

        string KeyCodeToUnicode(VirtualKey key)
        {

            var shiftState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift)
                .HasFlag(CoreVirtualKeyStates.Down);
            var capState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.CapitalLock)
                .HasFlag(CoreVirtualKeyStates.Down) || CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.CapitalLock)
                               .HasFlag(CoreVirtualKeyStates.Locked);
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
                if (shiftState == false && capState == false ||
                    shiftState && capState)
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
                }
            }

        }
        void RichEditBox_GotFocus(object sender, RoutedEventArgs e)
        {
            previewTextbox.Visibility = Visibility.Collapsed;
            loadingPermanentTextbox = false;
            var text = previewTextBuffer;
            var richEditBox = sender as RichEditBox;
            richEditBox.GotFocus -= RichEditBox_GotFocus;
            previewTextbox.Text = string.Empty;
            richEditBox.Document.SetText(TextSetOptions.None, text);
            richEditBox.Document.Selection.SetRange(text.Length, text.Length);
        }

        #endregion
    }
}