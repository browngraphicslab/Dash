using DashShared;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Dash.Views;
using Visibility = Windows.UI.Xaml.Visibility;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionFreeformView : SelectionElement, ICollectionView
    {

        #region ScalingVariables

        public Rect Bounds = new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity);
        public double CanvasScale { get; set; } = 1;
        public BaseCollectionViewModel ViewModel { get; private set; }

        public const float MaxScale = 4;
        public const float MinScale = 0.25f;

        #endregion


        #region LinkingVariables

        public bool CanLink = true;
        public PointerRoutedEventArgs PointerArgs;
        private HashSet<uint> _currentPointers = new HashSet<uint>();
        private IOReference _currReference;
        private Path _connectionLine;
        private BezierConverter _converter;
        private MultiBinding<PathFigureCollection> _lineBinding;
        private Dictionary<BezierConverter, Path> _lineDict = new Dictionary<BezierConverter, Path>();
        private Canvas itemsPanelCanvas;

        #endregion

        public ManipulationControls ManipulationControls;
        private MenuFlyout _flyout;
        private float _backgroundOpacity = .7f;

        #region Ink

        private Canvas SelectionCanvas = new Canvas();
        private InkCanvas XInkCanvas = new InkCanvas
        {
            Width = 60000, Height = 60000,
        };
        public InkFieldModelController InkFieldModelController;
        Symbol SelectIcon = (Symbol)0xEF20;
        private enum InkSelectionMode
        {
            Document, Ink
        }
        private InkSelectionMode _inkSelectionMode;
        private bool _isSelectionEnabled;
        private InkPresenterRuler _ruler;
        private InkAnalyzer _inkAnalyzer;
        private Polyline _lasso;
        private Rect _boundingRect;
        private InkSelectionRect _rectangle;
        private LassoSelectHelper _lassoHelper;
        public double Zoom => ManipulationControls.ElementScale;
        #endregion

        #region Background Translation Variables
        private CanvasBitmap _bgImage;
        private bool _resourcesLoaded;
        private CanvasImageBrush _bgBrush;
        private Uri _backgroundPath = new Uri("ms-appx:///Assets/gridbg.png");
        

        private const double _numberOfBackgroundRows = 2; // THIS IS A MAGIC NUMBER AND SHOULD CHANGE IF YOU CHANGE THE BACKGROUND IMAGE
        #endregion

        public delegate void OnDocumentViewLoadedHandler(CollectionFreeformView sender, DocumentView documentView);
        public event OnDocumentViewLoadedHandler OnDocumentViewLoaded;

        public CollectionFreeformView()
        {
            InitializeComponent();
            _lassoHelper = new LassoSelectHelper(this);
            Loaded += Freeform_Loaded;
            Unloaded += Freeform_Unloaded;
            DataContextChanged += OnDataContextChanged;
            ManipulationControls = new ManipulationControls(this, doesRespondToManipulationDelta:true, doesRespondToPointerWheel: true);
            ManipulationControls.OnManipulatorTranslatedOrScaled += ManipulationControls_OnManipulatorTranslated;
        }

        public IOReference GetCurrentReference()
        {
            return _currReference; 
        }

        #region DataContext and Events

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var vm = DataContext as BaseCollectionViewModel;

            if (vm != null)
            {
                //var itemsBinding = new Binding
                //{
                //    Source = vm,
                //    Path = new PropertyPath(nameof(vm.DocumentViewModels)),
                //    Mode = BindingMode.OneWay
                //};
                //xItemsControl.SetBinding(ItemsControl.ItemsSourceProperty, itemsBinding);

                ViewModel = vm;
                ViewModel.SetSelected(this, IsSelected);
            }
        }


        private void Freeform_Unloaded(object sender, RoutedEventArgs e)
        {
            ManipulationControls.Dispose();
        }

        private void Freeform_Loaded(object sender, RoutedEventArgs e)
        {
            var parentGrid = this.GetFirstAncestorOfType<Grid>();
            parentGrid.PointerMoved += FreeformGrid_OnPointerMoved;
            parentGrid.PointerReleased += FreeformGrid_OnPointerReleased;
            if (InkFieldModelController != null)
            {
                MakeInkCanvas();
            }
        }

        #endregion

        #region DraggingLinesAround

        /// <summary>
        /// Update the bindings on lines when documentview is minimized to icon view 
        /// </summary>
        /// <param name="becomeSmall">whether the document has minimized or regained normal view</param>
        /// <param name="docView">the documentview that calls the method</param>
        public void UpdateBinding(bool becomeSmall, DocumentView docView)
        {
            foreach (var line in _lineDict)
            {
                var converter = line.Key;
                var view1 = converter.Element1.GetFirstAncestorOfType<DocumentView>();
                var view2 = converter.Element2.GetFirstAncestorOfType<DocumentView>();
                Debug.Assert(view1 != null);
                Debug.Assert(view2 != null);
                if (view1 == docView)
                {
                    if (becomeSmall)
                    {
                        if (!(converter.Element1 is Grid)) converter.Temp1 = converter.Element1;
                        converter.Element1 = docView.xIcon;
                    }
                    else
                    {
                        converter.Element1 = converter.Temp1;
                        //converter.Temp1 = converter.Element1;
                    }
                }
                else if (view2 == docView)
                {
                    if (becomeSmall)
                    {
                        if (!(converter.Element2 is Grid)) converter.Temp2 = converter.Element2;
                        converter.Element2 = docView.xIcon;
                    }
                    else
                    {
                        converter.Element2 = converter.Temp2;
                        //converter.Temp2 = converter.Element2;
                    }
                }
            }
        }

        public void StartDrag(IOReference ioReference)
        {
            if (_currReference != null) return;
            if (!CanLink)
            {
                PointerArgs = ioReference.PointerArgs;
                return;
            }

            if (ioReference.PointerArgs == null) return;

            if (_currentPointers.Contains(ioReference.PointerArgs.Pointer.PointerId)) return;

            ViewModel.SetGlobalHitTestVisiblityOnSelectedItems(true); 

            itemsPanelCanvas = xItemsControl.ItemsPanelRoot as Canvas;

            _currentPointers.Add(ioReference.PointerArgs.Pointer.PointerId);
            _currReference = ioReference;
            _connectionLine = new Path
            {
                StrokeThickness = 5,
                Stroke = new SolidColorBrush(Colors.Orange),
                IsHitTestVisible = false,
                CompositeMode =
                    ElementCompositeMode.SourceOver //TODO Bug in xaml, shouldn't need this line when the bug is fixed 
                                                    //(https://social.msdn.microsoft.com/Forums/sqlserver/en-US/d24e2dc7-78cf-4eed-abfc-ee4d789ba964/windows-10-creators-update-uielement-clipping-issue?forum=wpdevelop)
            };
            Canvas.SetZIndex(_connectionLine, -1);
            _converter = new BezierConverter(ioReference.FrameworkElement, null, itemsPanelCanvas);
            _converter.Pos2 = ioReference.PointerArgs.GetCurrentPoint(itemsPanelCanvas).Position;

            _lineBinding =
                new MultiBinding<PathFigureCollection>(_converter, null);
            _lineBinding.AddBinding(ioReference.ContainerView, RenderTransformProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, WidthProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, HeightProperty);
            Binding lineBinding = new Binding
            {
                Source = _lineBinding,
                Path = new PropertyPath("Property")
            };
            PathGeometry pathGeo = new PathGeometry();
            BindingOperations.SetBinding(pathGeo, PathGeometry.FiguresProperty, lineBinding);
            _connectionLine.Data = pathGeo;

            itemsPanelCanvas.Children.Add(_connectionLine);

            if (!ioReference.IsOutput)
            {
                CheckLinePresence(_converter);
                _lineDict.Add(_converter, _connectionLine);
            }
        }

        public void CancelDrag(Pointer p)
        {
            ViewModel.SetGlobalHitTestVisiblityOnSelectedItems(false);
            if (p != null) _currentPointers.Remove(p.PointerId);
            UndoLine();
        }

        private void UndoLine()
        {
            itemsPanelCanvas.Children.Remove(_connectionLine);
            _connectionLine = null;
            _currReference = null;
        }

        public void EndDrag(IOReference ioReference)
        {
            IOReference inputReference = ioReference.IsOutput ? _currReference : ioReference;
            IOReference outputReference = ioReference.IsOutput ? ioReference : _currReference;

            if (ioReference.PointerArgs != null) _currentPointers.Remove(ioReference.PointerArgs.Pointer.PointerId);
            if (_connectionLine == null)
            {
                return;
            }
            if (_currReference == null || _currReference.IsOutput == ioReference.IsOutput)
            {
                UndoLine();
                return;
            }
            if (_currReference.FieldReference == null) return;

            _converter.Element2 = ioReference.FrameworkElement;
            _lineBinding.AddBinding(ioReference.ContainerView, RenderTransformProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, WidthProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, HeightProperty);

            DocumentController inputController =
                inputReference.FieldReference.GetDocumentController(null);
            var thisRef = (outputReference.ContainerView.DataContext as DocumentViewModel).DocumentController.GetDereferencedField(KeyStore.ThisKey, null);
            if (inputController.DocumentType == OperatorDocumentModel.OperatorType &&
                // (inputController.GetDereferencedField(OperatorDocumentModel.OperatorKey, null) as OperatorFieldModelController).Inputs[inputReference.FieldReference.FieldKey] == TypeInfo.Document && 
                inputReference.FieldReference is DocumentFieldReference && thisRef != null)
                inputController.SetField(inputReference.FieldReference.FieldKey, thisRef, true);
            else
                inputController.SetField(inputReference.FieldReference.FieldKey,
                    new ReferenceFieldModelController(outputReference.FieldReference), true);

            if (/*!ioReference.IsOutput &&*/ _connectionLine != null)
            {
                CheckLinePresence(_converter);
                _lineDict.Add(_converter, _connectionLine);
                _connectionLine = null;
            }
            if (ioReference.PointerArgs != null) CancelDrag(ioReference.PointerArgs.Pointer);
        }

        /// <summary>
        /// Method to add the dropped off field to the documentview; shows up in keyvalue pane but not in the immediate displauy  
        /// </summary>
        public void EndDragOnDocumentView(ref DocumentController cont, IOReference ioReference)
        {
            if (_currReference != null)
            {
                cont.SetField(_currReference.FieldKey, _currReference.FMController, true);
                EndDrag(ioReference);
            }
        }

        /// <summary>
        /// Helper function that checks if connection line is already present for input ellipse; if so, destroy that line and create a new one  
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void CheckLinePresence(BezierConverter converter)
        {
            if (!_lineDict.ContainsKey(converter)) return;
            var line = _lineDict[converter];
            itemsPanelCanvas.Children.Remove(line);
            _lineDict.Remove(converter);
        }

        private void FreeformGrid_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_connectionLine != null)
            {
                Point pos = e.GetCurrentPoint(itemsPanelCanvas).Position;
                _converter.Pos2 = pos;
                _lineBinding.ForceUpdate();
            }
        }


        #endregion

        #region Manipulation

        /// <summary>
        /// Pans and zooms upon touch manipulation 
        /// </summary>
        private void ManipulationControls_OnManipulatorTranslated(TransformGroupData transformationDelta)
        {
            if (!IsHitTestVisible) return;
            var canvas = xItemsControl.ItemsPanelRoot as Canvas;
            Debug.Assert(canvas != null);
            var delta = transformationDelta.Translate;

            //Create initial translate and scale transforms
            //Translate is in screen space, scale is in canvas space
            var translate = new TranslateTransform
            {
                X = delta.X,
                Y = delta.Y
            };

            var scale = new ScaleTransform
            {
                CenterX = transformationDelta.ScaleCenter.X,
                CenterY = transformationDelta.ScaleCenter.Y,
                ScaleX = transformationDelta.ScaleAmount.X,
                ScaleY = transformationDelta.ScaleAmount.Y
            };

            //Create initial composite transform
            var composite = new TransformGroup();
            composite.Children.Add(scale);
            composite.Children.Add(canvas.RenderTransform);
            composite.Children.Add(translate);

            canvas.RenderTransform = new MatrixTransform { Matrix = composite.Value };
            SetTransformOnBackground(composite);
        }

        #endregion

        #region BackgroundTiling


        private void SetTransformOnBackground(TransformGroup composite)
        {
            var aliasSafeScale = ClampBackgroundScaleForAliasing(composite.Value.M11, _numberOfBackgroundRows);

            if (_resourcesLoaded)
            {
                _bgBrush.Transform = new Matrix3x2((float)aliasSafeScale,
                    (float)composite.Value.M12,
                    (float)composite.Value.M21,
                    (float)aliasSafeScale,
                    (float)composite.Value.OffsetX,
                    (float)composite.Value.OffsetY);
                xBackgroundCanvas.Invalidate();
            }
        }

        private void SetInitialTransformOnBackground()
        {
            var composite = new TransformGroup();
            var scale = new ScaleTransform
            {
                CenterX = 0,
                CenterY = 0,
                ScaleX = CanvasScale,
                ScaleY = CanvasScale
            };

            composite.Children.Add(scale);
            SetTransformOnBackground(composite);
        }

        private void CanvasControl_OnCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            var task = Task.Run(async () =>
            {
                // Load the background image and create an image brush from it
                _bgImage = await CanvasBitmap.LoadAsync(sender, _backgroundPath);
                _bgBrush = new CanvasImageBrush(sender, _bgImage)
                {
                    Opacity = _backgroundOpacity
                };

                // Set the brush's edge behaviour to wrap, so the image repeats if the drawn region is too big
                _bgBrush.ExtendX = _bgBrush.ExtendY = CanvasEdgeBehavior.Wrap;

                _resourcesLoaded = true;
            });
            args.TrackAsyncAction(task.AsAsyncAction());

            task.ContinueWith(continuationTask =>
            {
                SetInitialTransformOnBackground();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void CanvasControl_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (!_resourcesLoaded) return;

            // Just fill a rectangle with our tiling image brush, covering the entire bounds of the canvas control
            var session = args.DrawingSession;
            session.FillRectangle(new Rect(new Point(), sender.Size), _bgBrush);
        }

        private double ClampBackgroundScaleForAliasing(double currentScale, double numberOfBackgroundRows)
        {
            while (currentScale / numberOfBackgroundRows > numberOfBackgroundRows)
            {
                currentScale /= numberOfBackgroundRows;
            }

            while (currentScale * numberOfBackgroundRows < numberOfBackgroundRows)
            {
                currentScale *= numberOfBackgroundRows;
            }
            return currentScale;
        }

        #endregion

        #region Clipping

        private void XOuterGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            xClippingRect.Rect = new Rect(0, 0, xOuterGrid.ActualWidth, xOuterGrid.ActualHeight);
        }

        #endregion

        private void DocumentViewOnLoaded(object sender, RoutedEventArgs e)
        {
            OnDocumentViewLoaded?.Invoke(this, sender as DocumentView);
        }

        private void FreeformGrid_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            DBTest.ResetCycleDetection();
        }

        #region Flyout

        private void InitializeFlyout()
        {
            _flyout = new MenuFlyout();
            var menuItem = new MenuFlyoutItem { Text = "Add Operators" };
            menuItem.Click += MenuItem_Click;
            _flyout.Items?.Add(menuItem);
        }

        private void DisposeFlyout()
        {
            if (_flyout.Items != null)
                foreach (var item in _flyout.Items)
                {
                    var menuFlyoutItem = item as MenuFlyoutItem;
                    if (menuFlyoutItem != null) menuFlyoutItem.Click -= MenuItem_Click;
                }
            _flyout = null;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var xCanvas = MainPage.Instance.xCanvas;
            if (!xCanvas.Children.Contains(OperatorSearchView.Instance))
                xCanvas.Children.Add(OperatorSearchView.Instance);
            // set the operator menu to the current location of the flyout
            var menu = sender as MenuFlyoutItem;
            var transform = menu.TransformToVisual(MainPage.Instance.xCanvas);
            var pointOnCanvas = transform.TransformPoint(new Point());
            // reset the render transform on the operator search view
            OperatorSearchView.Instance.RenderTransform = new TranslateTransform();
            var floatBorder = OperatorSearchView.Instance.SearchView.GetFirstDescendantOfType<Border>();
            if (floatBorder != null)
            {
                Canvas.SetLeft(floatBorder, 0);
                Canvas.SetTop(floatBorder, 0);
            }
            Canvas.SetLeft(OperatorSearchView.Instance, pointOnCanvas.X);
            Canvas.SetTop(OperatorSearchView.Instance, pointOnCanvas.Y);
            OperatorSearchView.AddsToThisCollection = this;
            DisposeFlyout();
        }

        private void CollectionView_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (_flyout == null)
                InitializeFlyout();
            e.Handled = true;
            var thisUi = this as UIElement;
            var position = e.GetPosition(thisUi);
            _flyout.ShowAt(thisUi, new Point(position.X, position.Y));
        }

        #endregion

        #region DragAndDrop

        private void CollectionViewOnDrop(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDrop(sender, e);
        }

        private void CollectionViewOnDragEnter(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDragEnter(sender, e);
        }

        #endregion

        #region Activation

        protected override void OnActivated(bool isSelected)
        {
            ViewModel.SetSelected(this, isSelected);
        }

        protected override void OnLowestActivated(bool isLowestSelected)
        {
            ViewModel.SetLowestSelected(this, isLowestSelected);
            if (IsDrawing)
            {
                if (!isLowestSelected) XInkCanvas.InkPresenter.IsInputEnabled = false;
                else XInkCanvas.InkPresenter.IsInputEnabled = true;
            }
        }

        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_connectionLine != null) CancelDrag(_currReference.PointerArgs.Pointer);

            e.Handled = true;
            if (ViewModel.IsInterfaceBuilder)
                return;

            OnSelected();

        }

        #endregion


        public void ToggleSelectAllItems()
        {
            throw new NotImplementedException();
        }

        #region Ink

        private void MakeInkCanvas()
        {
            _inkAnalyzer = new InkAnalyzer();
            GlobalInkSettings.Presenters.Add(XInkCanvas.InkPresenter);
            GlobalInkSettings.SetAttributes();
            Canvas.SetLeft(XInkCanvas, -30000);
            Canvas.SetTop(XInkCanvas, -30000);
            Canvas.SetLeft(SelectionCanvas, -30000);
            Canvas.SetTop(SelectionCanvas, -30000);
            XInkCanvas.InkPresenter.InputDeviceTypes = GlobalInkSettings.InkInputType;
            xItemsControl.ItemsPanelRoot.Children.Insert(0, XInkCanvas);
            xItemsControl.ItemsPanelRoot.Children.Insert(1, SelectionCanvas);
            GlobalInkSettings.InkInputChanged += GlobalInkSettingsOnInkInputChanged;
            XInkCanvas.InkPresenter.StrokesCollected += InkPresenterOnStrokesCollected;
            XInkCanvas.InkPresenter.StrokesErased += InkPresenterOnStrokesErased;
            XInkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInputOnStrokeStarted;
            InkFieldModelController.InkUpdated += InkFieldModelControllerOnInkUpdated;
            InkToolbar.EraseAllClicked += InkToolbarOnEraseAllClicked;
            InkToolbar.ActiveToolChanged += InkToolbarOnActiveToolChanged;
            xItemsControl.Items.VectorChanged += ItemsOnVectorChanged;
            UpdateStrokes();
            IsDrawing = true;
            ToggleDraw();
        }

        private void InkToolbarOnActiveToolChanged(InkToolbar sender, object args)
        {
            UpdateSelectionMode();
            if (XInkCanvas.InkPresenter.InputProcessingConfiguration.Mode == InkInputProcessingMode.Erasing)
            {
                ClearSelection();
            }
        }

        private void StrokeInputOnStrokeStarted(InkStrokeInput sender, PointerEventArgs args)
        {
            ClearSelection();
        }

        private void ClearSelection()
        {
            var strokes = XInkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            foreach (var stroke in strokes)
            {
                stroke.Selected = false;
            }
            ClearBoundingRect();
        }

        private void ClearBoundingRect()
        {
            if (SelectionCanvas.Children.Any())
            {
                SelectionCanvas.Children.Clear();
                _boundingRect = Rect.Empty;
            }
        }

        private void InkFieldModelControllerOnInkUpdated(InkCanvas sender, FieldUpdatedEventArgs args)
        {
            if (!sender.Equals(XInkCanvas) || args?.Action == DocumentController.FieldUpdatedAction.Replace)
            {
                UpdateStrokes();
            }
        }

        private void ItemsOnVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
        {
            Canvas.SetZIndex(XInkCanvas, 0);
            if (xItemsControl.ItemsPanelRoot.Children.Contains(XInkCanvas))
            {
                xItemsControl.ItemsPanelRoot.Children.Remove(XInkCanvas);
                xItemsControl.ItemsPanelRoot.Children.Remove(SelectionCanvas);
                xItemsControl.ItemsPanelRoot.Children.Insert(0, XInkCanvas);
                xItemsControl.ItemsPanelRoot.Children.Insert(1, SelectionCanvas);
            }
        }

        public void ToggleDraw()
        {
            if (IsDrawing)
            {
                InkSettingsPanel.Visibility = Visibility.Collapsed;
                SetInkInputType(CoreInputDeviceTypes.None);
                XInkCanvas.InkPresenter.IsInputEnabled = false;
            }
            else
            {
                InkSettingsPanel.Visibility = Visibility.Visible;
                SetInkInputType(GlobalInkSettings.InkInputType);
                XInkCanvas.InkPresenter.IsInputEnabled = true;
            }
            IsDrawing = !IsDrawing;

        }

        public bool IsDrawing { get; set; }

        private void GlobalInkSettingsOnInkInputChanged(CoreInputDeviceTypes newInputType)
        {
            SetInkInputType(newInputType);
        }

        private void SetInkInputType(CoreInputDeviceTypes type)
        {
            switch (type)
            {
                case CoreInputDeviceTypes.Mouse:
                    ManipulationControls.BlockedInputType = PointerDeviceType.Mouse;
                    ManipulationControls.FilterInput = IsDrawing;
                    break;
                case CoreInputDeviceTypes.Pen:
                    ManipulationControls.BlockedInputType = PointerDeviceType.Pen;
                    ManipulationControls.FilterInput = IsDrawing;
                    break;
                case CoreInputDeviceTypes.Touch:
                    ManipulationControls.BlockedInputType = PointerDeviceType.Touch;
                    ManipulationControls.FilterInput = IsDrawing;
                    break;
                default:
                    ManipulationControls.FilterInput = false;
                    break;
            }
        }

        public void UpdateInkFieldModelController()
        {
            if (InkFieldModelController != null)
                InkFieldModelController.UpdateStrokesFromList(XInkCanvas.InkPresenter.StrokeContainer.GetStrokes(), XInkCanvas);
        }

        private void InkToolbarOnEraseAllClicked(InkToolbar sender, object args)
        {
            UpdateInkFieldModelController();
        }

        private void UndoButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            InkFieldModelController?.Undo(XInkCanvas);
            ClearSelection();
        }

        private void RedoButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            InkFieldModelController?.Redo(XInkCanvas);
            ClearSelection();
        }

        private void UpdateStrokes()
        {
            XInkCanvas.InkPresenter.StrokeContainer.Clear();
            if (InkFieldModelController != null && InkFieldModelController.GetStrokes() != null)
                XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(InkFieldModelController.GetStrokes().Select(stroke => stroke.Clone()));
        }

        private void InkPresenterOnStrokesErased(InkPresenter sender, InkStrokesErasedEventArgs e)
        {
            UpdateInkFieldModelController();
        }

        private void InkPresenterOnStrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            if (_isSelectionEnabled)
            {
                switch (_inkSelectionMode)
                {
                    case InkSelectionMode.Ink:
                        SelectInk(args.Strokes.Last());
                        break;
                    case InkSelectionMode.Document:
                        SelectDocs(args.Strokes.Last());
                        break;
                }

            }
            UpdateInkFieldModelController();
        }

        private void SelectDocs(InkStroke selectionStroke)
        {
            selectionStroke.Selected = true;
            XInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
            var points = selectionStroke.GetInkPoints().Select(i => new Point(i.Position.X - 30000, i.Position.Y - 30000));
            ViewModel.SelectionGroup = _lassoHelper.GetSelectedDocuments(new List<Point>(points));
        }

        private void SelectInk(InkStroke selectionStroke)
        {
            selectionStroke.Selected = true;
            XInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
            _boundingRect =
                XInkCanvas.InkPresenter.StrokeContainer.SelectWithPolyLine(
                    selectionStroke.GetInkPoints().Select(i => i.Position));
            DrawBoundingRect();
        }

        //TODO: position ruler
        private void InkToolbar_OnIsRulerButtonCheckedChanged(InkToolbar sender, object args)
        {
            InkPresenterRuler ruler = new InkPresenterRuler(XInkCanvas.InkPresenter);
            ruler.Transform = Matrix3x2.CreateTranslation(new Vector2(30000, 30000));
        }

        private void SelectButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            UpdateSelectionMode();
        }

        private void DrawBoundingRect()
        {
            SelectionCanvas.Children.Clear();

            // Draw a bounding rectangle only if there are ink strokes 
            // within the lasso area.
            if (!(_boundingRect.Width == 0 ||
                  _boundingRect.Height == 0 ||
                  _boundingRect.IsEmpty))
            {
                _rectangle = new InkSelectionRect(this, XInkCanvas.InkPresenter.StrokeContainer)
                {
                    Width = _boundingRect.Width + 30,
                    Height = _boundingRect.Height + 30,
                };

                Canvas.SetLeft(_rectangle, _boundingRect.X - 15);
                Canvas.SetTop(_rectangle, _boundingRect.Y - 15);

                SelectionCanvas.Children.Add(_rectangle);
            }
        }

        private void InkSelect_OnChecked(object sender, RoutedEventArgs e)
        {
            UpdateSelectionMode();
        }
        

        private void DocumentSelect_OnChecked(object sender, RoutedEventArgs e)
        {
            UpdateSelectionMode();
        }
        #endregion

        private void SelectButton_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateSelectionMode();
        }

        private void UpdateSelectionMode()
        {
            if (SelectButton.IsChecked != null && (bool) SelectButton.IsChecked)
            {
                _isSelectionEnabled = true;
                if (InkSelect.IsChecked != null && (bool) InkSelect.IsChecked)
                {
                    _inkSelectionMode = InkSelectionMode.Ink;
                    var newAttributes = new InkDrawingAttributes();
                    newAttributes.Size = new Size(2, 2);
                    newAttributes.Color = ((SolidColorBrush) Application.Current.Resources["WindowsBlue"]).Color;
                    XInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(newAttributes);
                    XInkCanvas.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
                }
                if (DocumentSelect.IsChecked != null && (bool) DocumentSelect.IsChecked)
                {
                    _inkSelectionMode = InkSelectionMode.Document;
                    var newAttributes = new InkDrawingAttributes();
                    newAttributes.Size = new Size(2, 2);
                    newAttributes.Color = ((SolidColorBrush) Application.Current.Resources["WindowsBlue"]).Color;
                    XInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(newAttributes);
                    XInkCanvas.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
                }
            }
            else
            {
                _isSelectionEnabled = false;
            }
            
            
        }
    }
}
