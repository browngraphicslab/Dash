using DashShared;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Dash;
using static Dash.NoteDocuments;
using Dash.Controllers.Operators;

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
        //public Dictionary<FieldReference, LinePackage> LineDict = new Dictionary<FieldReference, LinePackage>();

        public Dictionary<FieldReference, Path> RefToLine = new Dictionary<FieldReference, Path>();
        public Dictionary<Path, BezierConverter> LineToConverter = new Dictionary<Path, BezierConverter>();
        private Dictionary<FieldReference, Path> _linesToBeDeleted = new Dictionary<FieldReference, Path>();
        private Canvas itemsPanelCanvas;

        #endregion


        public ManipulationControls ManipulationControls;

        private float _backgroundOpacity = .7f;

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
            Loaded += Freeform_Loaded;
            Unloaded += Freeform_Unloaded;
            DataContextChanged += OnDataContextChanged;

            DragLeave += Collection_DragLeave;
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
                ViewModel = vm;
                ViewModel.SetSelected(this, IsSelected);
            }
        }


        private void Freeform_Unloaded(object sender, RoutedEventArgs e)
        {
            ManipulationControls?.Dispose();
        }

        private void Freeform_Loaded(object sender, RoutedEventArgs e)
        {
            itemsPanelCanvas = xItemsControl.ItemsPanelRoot as Canvas;

            ManipulationControls = new ManipulationControls(this, doesRespondToManipulationDelta: true, doesRespondToPointerWheel: true);
            ManipulationControls.OnManipulatorTranslatedOrScaled += ManipulationControls_OnManipulatorTranslated;

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

        public void DeleteLine(FieldReference reff, Path line)
        {
            itemsPanelCanvas.Children.Remove(line);
            RefToLine.Remove(reff);
            LineToConverter.Remove(line);
        }

        /// <summary>
        /// Called when documentview is deleted; delete all connections coming from it as well  
        /// </summary>
        public void DeleteConnections(DocumentView docView)
        {
            var ToBeDeleted = new List<Path>();
            foreach (var pair in LineToConverter)
            {
                var converter = pair.Value;
                var view1 = converter.Element1.GetFirstAncestorOfType<DocumentView>();
                var view2 = converter.Element2.GetFirstAncestorOfType<DocumentView>();

                if (view1 == docView || view2 == docView)
                {
                    ToBeDeleted.Add(pair.Key);
                }
            }
            foreach (var line in ToBeDeleted)
            {
                var fieldRef = RefToLine.FirstOrDefault(x => x.Value == line).Key;
                DeleteLine(fieldRef, line);
            }
            //var refs = linesToBeDeleted.Keys.ToList();
            //for (int i = linesToBeDeleted.Count - 1; i >= 0; i--)
            //{
            //    var package = linesToBeDeleted[refs[i]];
            //    itemsPanelCanvas.Children.Remove(package.Line);
            //    LineDict.Remove(refs[i]);
            //}
            //linesToBeDeleted = new Dictionary<FieldReference, LinePackage>();
        }

        public void DeleteConnection(KeyValuePair<FieldReference, Path> pair)
        {
            if (RefToLine.ContainsKey(pair.Key))
            {
                var line = pair.Value;
                var converter = LineToConverter[line];
                var view1 = converter.Element1.GetFirstAncestorOfType<DocumentView>();
                var view2 = converter.Element2.GetFirstAncestorOfType<DocumentView>();
                var fieldRef = RefToLine.FirstOrDefault(x => x.Value == line).Key;
                DeleteLine(fieldRef, line);
            }
            _linesToBeDeleted = new Dictionary<FieldReference, Path>();
        }
        /// <summary>
        /// Adds the lines to be deleted as part of fading storyboard 
        /// </summary>
        /// <param name="fadeout"></param>
        public void AddToStoryboard(Windows.UI.Xaml.Media.Animation.Storyboard fadeout, DocumentView docView)
        {
            foreach (var pair in RefToLine)
            {
                var converter = LineToConverter[pair.Value];
                var view1 = converter.Element1.GetFirstAncestorOfType<DocumentView>();
                var view2 = converter.Element2.GetFirstAncestorOfType<DocumentView>();

                if (view1 == docView || view2 == docView)
                {
                    var animation = new Windows.UI.Xaml.Media.Animation.FadeOutThemeAnimation();
                    Windows.UI.Xaml.Media.Animation.Storyboard.SetTarget(animation, pair.Value);
                    fadeout.Children.Add(animation);
                    _linesToBeDeleted.Add(pair.Key, pair.Value);
                }
            }
        }

        private List<KeyValuePair<FieldReference, Path>> GetLinesToDelete()
        {
            var result = new List<KeyValuePair<FieldReference, Path>>();
            //var views = new HashSet<DocumentView>(_payload.Keys);
            foreach (var pair in RefToLine)
            {
                var converter = LineToConverter[pair.Value];
                var view1 = converter.Element1.GetFirstAncestorOfType<DocumentView>();
                var view2 = converter.Element2.GetFirstAncestorOfType<DocumentView>();
                //if (views.Contains(view1) || views.Contains(view2))
                if (view1 == null || view2 == null) // because at time of drop document disappears from VisualTree
                    result.Add(pair);
            }
            return result;
        }

        /// <summary>
        /// Update the bindings on lines when documentview is minimized to icon view 
        /// </summary>
        /// <param name="becomeSmall">whether the document has minimized or regained normal view</param>
        /// <param name="docView">the documentview that calls the method</param>
        public void UpdateBinding(bool becomeSmall, DocumentView docView)
        {
            foreach (var converter in LineToConverter.Values)
            {
                DocumentView view1, view2;
                try
                {
                    view1 = converter.Element1.GetFirstAncestorOfType<DocumentView>();
                    view2 = converter.Element2.GetFirstAncestorOfType<DocumentView>();
                }
                catch (ArgumentException) { return; }
                if (docView == view1)
                {
                    if (becomeSmall)
                    {
                        if (!(converter.Element1 is Grid)) converter.Temp1 = converter.Element1;
                        converter.Element1 = docView.xIcon;
                    }
                    else
                    {
                        converter.Element1 = converter.Temp1;
                    }
                }
                else if (docView == view2)
                {
                    if (becomeSmall)
                    {
                        if (!(converter.Element2 is Grid)) converter.Temp2 = converter.Element2;
                        converter.Element2 = docView.xIcon;
                    }
                    else
                    {
                        converter.Element2 = converter.Temp2;
                    }
                }
            }
        }

        /// <summary>
        /// Helper method to start changing the connectionLines upon drag 
        /// </summary>
        /// <param name="dropPoint"> origin of manipulation </param>
        /// <param name="line"> the connection line to change </param>
        /// <param name="ioReference"> the reference for starting field </param>
        private void ChangeLineConnection(Point dropPoint, Path line, IOReference ioReference)
        {
            if (line.Stroke != (SolidColorBrush)App.Instance.Resources["AccentGreen"])
            {
                _converter = LineToConverter[line];
                //set up to manipulate connection line again 
                ViewModel.SetGlobalHitTestVisiblityOnSelectedItems(true);
                _connectionLine = line;
                _converter.Element2 = null;
                _converter.Pos2 = dropPoint;
                _currReference = ioReference;
                ManipulationControls.OnManipulatorTranslatedOrScaled -= ManipulationControls_OnManipulatorTranslated;

                //replace referencefieldmodelcontrollers with the raw fieldmodelcontrollers  
                var refField = RefToLine.FirstOrDefault(x => x.Value == line).Key;
                DocumentController inputController = refField.GetDocumentController(null);
                var rawField = inputController.GetField(refField.FieldKey);
                if (rawField as ReferenceFieldModelController != null)
                    rawField = (rawField as ReferenceFieldModelController).DereferenceToRoot(null);
                inputController.SetField(refField.FieldKey, rawField, false);
                RefToLine.Remove(refField);
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
            //itemsPanelCanvas = xItemsControl.ItemsPanelRoot as Canvas;

            _currentPointers.Add(ioReference.PointerArgs.Pointer.PointerId);
            _currReference = ioReference;
            _connectionLine = new Path
            {
                //TODO: made this hit test invisible because it was getting in the way of ink (which can do [almost] all the same stuff). sry :/
                IsHitTestVisible = false,
                StrokeThickness = 5,
                Stroke = (SolidColorBrush)App.Instance.Resources["AccentGreen"],
                IsHoldingEnabled = false,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                CompositeMode = ElementCompositeMode.SourceOver //TODO Bug in xaml, shouldn't need this line when the bug is fixed 
                                                                //(https://social.msdn.microsoft.com/Forums/sqlserver/en-US/d24e2dc7-78cf-4eed-abfc-ee4d789ba964/windows-10-creators-update-uielement-clipping-issue?forum=wpdevelop)
            };

            // set up for manipulation on lines 
            _connectionLine.Tapped += (s, e) =>
            {
                e.Handled = true;
                var line = s as Path;
                var green = (SolidColorBrush)App.Instance.Resources["AccentGreen"];
                line.Stroke = line.Stroke == green ? new SolidColorBrush(Colors.Goldenrod) : green;
                line.IsHoldingEnabled = !line.IsHoldingEnabled;
            };

            _connectionLine.Holding += (s, e) =>
            {
                if (_connectionLine != null) return;
                ChangeLineConnection(e.GetPosition(itemsPanelCanvas), s as Path, ioReference);
            };

            _connectionLine.PointerPressed += (s, e) =>
            {
                if (!e.GetCurrentPoint(itemsPanelCanvas).Properties.IsRightButtonPressed) return;
                ChangeLineConnection(e.GetCurrentPoint(itemsPanelCanvas).Position, s as Path, ioReference);
            };

            Canvas.SetZIndex(_connectionLine, -1);
            _converter = new BezierConverter(ioReference.FrameworkElement, null, itemsPanelCanvas);
            _converter.Pos2 = ioReference.PointerArgs.GetCurrentPoint(itemsPanelCanvas).Position;

            _lineBinding = new MultiBinding<PathFigureCollection>(_converter, null);
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
        }

        

        public void CancelDrag(Pointer p)
        {
            ViewModel.SetGlobalHitTestVisiblityOnSelectedItems(false);
            ManipulationControls.OnManipulatorTranslatedOrScaled -= ManipulationControls_OnManipulatorTranslated;
            ManipulationControls.OnManipulatorTranslatedOrScaled += ManipulationControls_OnManipulatorTranslated;
            if (p != null) _currentPointers.Remove(p.PointerId);
            UndoLine();
        }

        /// <summary>
        /// Frees references and removes the line graphically 
        /// </summary>
        private void UndoLine()
        {
            if (_connectionLine != null) itemsPanelCanvas.Children.Remove(_connectionLine);
            _connectionLine = null;
            _currReference = null;
        }

        public void EndDrag(IOReference ioReference, bool isCompoundOperator)
        {
            IOReference inputReference = ioReference.IsOutput ? _currReference : ioReference;
            IOReference outputReference = ioReference.IsOutput ? ioReference : _currReference;

            // condition checking 
            if (ioReference.PointerArgs != null) _currentPointers.Remove(ioReference.PointerArgs.Pointer.PointerId);
            if (_connectionLine == null) return;

            // only allow input-output pairs to be connected 
            if (_currReference == null || _currReference.IsOutput == ioReference.IsOutput)
            {
                UndoLine();
                return;
            }

            if ((inputReference.Type & outputReference.Type) == 0)
            {
                UndoLine();
                return;
            }

            // undo line if connecting the same fields 
            if (inputReference.FieldReference.Equals(outputReference.FieldReference) || _currReference.FieldReference == null)
            {
                UndoLine();
                return;
            }

            if (!isCompoundOperator)
            {
                DocumentController inputController = inputReference.FieldReference.GetDocumentController(null);
                var thisRef = (outputReference.ContainerView.DataContext as DocumentViewModel).DocumentController.GetDereferencedField(KeyStore.ThisKey, null);
                if (inputController.DocumentType == OperatorDocumentModel.OperatorType && inputReference.FieldReference is DocumentFieldReference && thisRef != null)
                    inputController.SetField(inputReference.FieldReference.FieldKey, thisRef, true);
                else
                    inputController.SetField(inputReference.FieldReference.FieldKey, new ReferenceFieldModelController(outputReference.FieldReference), true);
            }

            //binding line position 
            _converter.Element2 = ioReference.FrameworkElement;
            _lineBinding.AddBinding(ioReference.ContainerView, RenderTransformProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, WidthProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, HeightProperty);

            if (_connectionLine != null)
            {
                _connectionLine.Stroke = (SolidColorBrush)App.Instance.Resources["AccentGreen"];
                CheckLinePresence(ioReference.FieldReference);
                RefToLine.Add(ioReference.FieldReference, _connectionLine);
                if (!LineToConverter.ContainsKey(_connectionLine)) LineToConverter.Add(_connectionLine, _converter);
                _connectionLine = null;
            }
            if (ioReference.PointerArgs != null) CancelDrag(ioReference.PointerArgs.Pointer);
        }

        /// <summary>
        /// Method to add the dropped off field to the documentview; shows up in keyvalue pane but not in the immediate displauy  
        /// </summary>
        //public void EndDragOnDocumentView(ref DocumentController cont, IOReference ioReference)
        //{
        //    if (_currReference != null)
        //    {
        //        cont.SetField(_currReference.FieldKey, _currReference.FMController, true);
        //        EndDrag(ioReference);
        //    }
        //}

        /// <summary>
        /// Helper function that checks if connection line is already present for input ellipse; if so, destroy that line and create a new one  
        /// </summary>
        private void CheckLinePresence(FieldReference reference)
        {
            if (!RefToLine.ContainsKey(reference)) return;
            var line = RefToLine[reference];
            itemsPanelCanvas.Children.Remove(line);
            RefToLine.Remove(reference);
            LineToConverter.Remove(line);
        }

        private void FreeformGrid_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_connectionLine != null)
            {
                Point pos = e.GetCurrentPoint(itemsPanelCanvas).Position;
                _converter.Pos2 = pos;
                _converter.UpdateLine();
                //_lineBinding.ForceUpdate();
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

            //Create initial translate and scale transforms
            var translate = new TranslateTransform
            {
                X = transformationDelta.Translate.X,
                Y = transformationDelta.Translate.Y
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
            composite.Children.Add(itemsPanelCanvas.RenderTransform);
            composite.Children.Add(scale);
            composite.Children.Add(translate);

            var matrix = new MatrixTransform { Matrix = composite.Value };

            itemsPanelCanvas.RenderTransform = matrix;
            InkHostCanvas.RenderTransform = matrix;
            SetTransformOnBackground(composite);

            // Updates line position if the collectionfreeformview canvas is manipulated within a compoundoperator view                                                                              
            if (this.GetFirstAncestorOfType<CompoundOperatorEditor>() != null)
            {
                foreach (var converter in LineToConverter.Values)
                    converter.UpdateLine();
            }
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
            (sender as DocumentView).OuterGrid.Tapped += DocumentView_Tapped;
            _documentViews.Add((sender as DocumentView));
        }

        private void FreeformGrid_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_currReference?.IsOutput == true && _currReference?.Type == TypeInfo.Document)
            {
                //var doc = _currReference.FieldReference.DereferenceToRoot<DocumentFieldModelController>(null).Data;
                var pos = e.GetCurrentPoint(this).Position;
                var doc = new DocumentController(new Dictionary<KeyController, FieldModelController>
                {
                    [KeyStore.DataKey] = new ReferenceFieldModelController(_currReference.FieldReference)
                }, DocumentType.DefaultType);
                var layout = new DocumentBox(new ReferenceFieldModelController(doc.GetId(), KeyStore.DataKey), pos.X, pos.Y).Document;
                doc.SetActiveLayout(layout, true, false);
                ViewModel.AddDocument(doc, null);
            }
            else if (_currReference?.IsOutput == true && _currReference?.Type == TypeInfo.Collection)
            {
                var doc = _currReference.FieldReference.DereferenceToRoot<DocumentCollectionFieldModelController>(null)?.Data;
                var pos = e.GetCurrentPoint(this).Position;
                var cnote = new CollectionNote(pos, _currReference.FieldReference.FieldKey == DBFilterOperatorFieldModelController.ResultsKey ? CollectionView.CollectionViewType.DB : CollectionView.CollectionViewType.Freeform);
                if (_currReference.FieldReference.FieldKey == DBFilterOperatorFieldModelController.ResultsKey)
                {
                    var dropSourceDoc = _currReference.FieldReference.GetDocumentController(null);
                    var droppedRef = doc == null ? new ReferenceFieldModelController(DBTest.DBDoc.GetId(), KeyStore.DataKey) :
                        new ReferenceFieldModelController(dropSourceDoc.GetId(), _currReference.FieldReference.FieldKey);
                    cnote.Document.SetField(CollectionNote.CollectedDocsKey, droppedRef, true);
                    var field = dropSourceDoc.GetDereferencedField<TextFieldModelController>(DBFilterOperatorFieldModelController.FilterFieldKey, null);
                    cnote.Document.SetField(DBFilterOperatorFieldModelController.FilterFieldKey, new TextFieldModelController(field.Data), true);
                    cnote.Document.GetPositionField().Data = pos;
                }
                else
                {
                    cnote.Document.SetField(CollectionNote.CollectedDocsKey, new DocumentCollectionFieldModelController(), true);
                }
                ViewModel.AddDocument(cnote.Document, null);
                DBTest.DBDoc.AddChild(cnote.Document);
            }
            CancelDrag(e.Pointer);
        }

        #region Flyout

        //private void InitializeFlyout()
        //{
        //    _flyout = new MenuFlyout();
        //    var menuItem = new MenuFlyoutItem { Text = "Add Operators" };
        //    menuItem.Click += MenuItem_Click;
        //    _flyout.Items?.Add(menuItem);

        //    var menuItem2 = new MenuFlyoutItem { Text = "Add Document" };
        //    menuItem2.Click += MenuItem_Click2;
        //    _flyout.Items?.Add(menuItem2);

        //    var menuItem3 = new MenuFlyoutItem { Text = "Add Collection" };
        //    menuItem3.Click += MenuItem_Click3;
        //    _flyout.Items?.Add(menuItem3);
        //}

        //private void DisposeFlyout()
        //{
        //    if (_flyout.Items != null)
        //        foreach (var item in _flyout.Items)
        //        {
        //            var menuFlyoutItem = item as MenuFlyoutItem;
        //            if (menuFlyoutItem != null) menuFlyoutItem.Click -= MenuItem_Click;
        //        }
        //    _flyout = null;
        //}

        //private void MenuItem_Click(object sender, RoutedEventArgs e)
        //{
        //    var xCanvas = MainPage.Instance.xCanvas;
        //    if (!xCanvas.Children.Contains(TabMenu.Instance))
        //        xCanvas.Children.Add(TabMenu.Instance);
        //    // set the operator menu to the current location of the flyout
        //    var menu = sender as MenuFlyoutItem;
        //    var transform = menu.TransformToVisual(MainPage.Instance.xCanvas);
        //    var pointOnCanvas = transform.TransformPoint(new Point());
        //    // reset the render transform on the operator search view
        //    TabMenu.Instance.RenderTransform = new TranslateTransform();
        //    var floatBorder = TabMenu.Instance.SearchView.GetFirstDescendantOfType<Border>();
        //    if (floatBorder != null)
        //    {
        //        Canvas.SetLeft(floatBorder, 0);
        //        Canvas.SetTop(floatBorder, 0);
        //    }
        //    Canvas.SetLeft(TabMenu.Instance, pointOnCanvas.X);
        //    Canvas.SetTop(TabMenu.Instance, pointOnCanvas.Y);
        //    TabMenu.AddsToThisCollection = this;

        //    TabMenu.Instance.LostFocus += (ss, ee) => xCanvas.Children.Remove(TabMenu.Instance);

        //    DisposeFlyout();
        //}


        //private void MenuItem_Click2(object sender, RoutedEventArgs e)
        //{
        //    var menu = sender as MenuFlyoutItem;
        //    var transform = menu.TransformToVisual(MainPage.Instance.xCanvas);
        //    var pointOnCanvas = transform.TransformPoint(new Point());

        //    var fields = new Dictionary<KeyController, FieldModelController>()
        //    {
        //        [KeyStore.ActiveLayoutKey] = new DocumentFieldModelController(new FreeFormDocument(new List<DocumentController>(), pointOnCanvas, new Size(100, 100)).Document)
        //    };

        //    ViewModel.AddDocument(new DocumentController(fields, DocumentType.DefaultType), null);


        //    DisposeFlyout();
        //}

        //private void MenuItem_Click3(object sender, RoutedEventArgs e)
        //{
        //    var menu = sender as MenuFlyoutItem;
        //    var transform = menu.TransformToVisual(MainPage.Instance.xCanvas);
        //    var pointOnCanvas = transform.TransformPoint(new Point());

        //    var fields = new Dictionary<KeyController, FieldModelController>()
        //    {
        //        [DocumentCollectionFieldModelController.CollectionKey] = new DocumentCollectionFieldModelController(),
        //    };

        //    var documentController = new DocumentController(fields, DocumentType.DefaultType);
        //    documentController.SetActiveLayout(new CollectionBox(new ReferenceFieldModelController(documentController.GetId(), DocumentCollectionFieldModelController.CollectionKey), pointOnCanvas.X, pointOnCanvas.Y).Document, true, true);
        //    ViewModel.AddDocument(documentController, null);


        //    DisposeFlyout();
        //}

        //private void CollectionView_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        //{
        //    if (InkControl == null || InkControl != null && !InkControl.IsDrawing)
        //    {
        //        if (_flyout == null)
        //            InitializeFlyout();
        //        e.Handled = true;
        //        var thisUi = this as UIElement;
        //        var position = e.GetPosition(thisUi);
        //        _flyout.ShowAt(thisUi, new Point(position.X, position.Y));
        //    }
        //}

        #endregion

        #region DragAndDrop

        private void CollectionViewOnDrop(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDrop(sender, e);
        }

        public void SetDropIndicationFill(Brush fill)
        {
            XDropIndicationRectangle.Fill = fill;
        }

        #endregion

        #region Activation

        protected override void OnActivated(bool isSelected)
        {
            ViewModel.SetSelected(this, isSelected);
            ViewModel.UpdateDocumentsOnSelection(isSelected);
            if (InkFieldModelController != null)
            {
                InkHostCanvas.IsHitTestVisible = isSelected;
                XInkCanvas.InkPresenter.IsInputEnabled = isSelected;
            }
        }

        protected override void OnLowestActivated(bool isLowestSelected)
        {
            ViewModel.SetLowestSelected(this, isLowestSelected);
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

        #region SELECTION

        private bool _isSelectionEnabled;
        public bool IsSelectionEnabled
        {
            get { return _isSelectionEnabled; }
            set
            {
                _isSelectionEnabled = value;
                if (!value) // turn colors back ... 
                {
                    foreach (var pair in _payload)
                    {
                        Deselect(pair.Key);
                    }
                    _payload = new Dictionary<DocumentView, DocumentController>();
                }
            }
        }


        private Dictionary<DocumentView, DocumentController> _payload = new Dictionary<DocumentView, DocumentController>();
        private List<DocumentView> _documentViews = new List<DocumentView>();

        private bool _isToggleOn;
        public void ToggleSelectAllItems()
        {
            _isToggleOn = !_isToggleOn;
            _payload = new Dictionary<DocumentView, DocumentController>();
            foreach (var docView in _documentViews)
            {
                if (_isToggleOn)
                {
                    Select(docView);
                    _payload.Add(docView, (docView.DataContext as DocumentViewModel).DocumentController);
                }
                else
                {
                    Deselect(docView);
                    _payload.Remove(docView);
                }
            }
        }

        public void DeselectAll()
        {
            foreach (var docView in _documentViews)
            {
                Deselect(docView);
                _payload.Remove(docView);
            }
        }

        private void Deselect(DocumentView docView)
        {
            docView.OuterGrid.Background = new SolidColorBrush(Colors.Transparent);
            docView.CanDrag = false;
            docView.ManipulationMode = ManipulationModes.All;
            docView.DragStarting -= DocView_OnDragStarting;
        }

        public void Select(DocumentView docView)
        {
            docView.OuterGrid.Background = new SolidColorBrush(Colors.LimeGreen);
            docView.CanDrag = true;
            docView.ManipulationMode = ManipulationModes.None;
            docView.DragStarting += DocView_OnDragStarting;
        }

        public void AddToPayload(DocumentView docView)
        {
            _payload.Add(docView, (docView.DataContext as DocumentViewModel).DocumentController);
        }

        private void DocumentView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!IsSelectionEnabled) return;

            var docView = (sender as Grid).GetFirstAncestorOfType<DocumentView>();
            if (docView.CanDrag)
            {
                Deselect(docView);
                _payload.Remove(docView);
            }
            else
            {
                Select(docView);
                _payload.Add(docView, (docView.DataContext as DocumentViewModel).DocumentController);
            }
            e.Handled = true;
        }

        private void Collection_DragLeave(object sender, DragEventArgs e)
        {
            Debug.WriteLine("CollectionViewOnDragLeave FreeForm");
            ViewModel.CollectionViewOnDragLeave(sender, e);

            //if (ItemsCarrier.Instance.StartingCollection == null)
            //    return;
            //ViewModel.RemoveDocuments(ItemsCarrier.Instance.Payload);
            //foreach (var view in _payload.Keys.ToList())
            //    _documentViews.Remove(view);

            //_payload = new Dictionary<DocumentView, DocumentController>();
            //XDropIndicationRectangle.Fill = new SolidColorBrush(Colors.Transparent);
        }

        private void CollectionViewOnDragEnter(object sender, DragEventArgs e)
        {
            Debug.WriteLine("CollectionViewOnDragEnter FreeForm");
            ViewModel.CollectionViewOnDragEnter(sender, e);

        }

        public void DocView_OnDragStarting(object sender, DragStartingEventArgs e)
        {
            ViewModel.SetGlobalHitTestVisiblityOnSelectedItems(true);
            
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }
        #endregion

        #region Ink

        public InkFieldModelController InkFieldModelController;
        public FreeformInkControl InkControl;
        public double Zoom { get { return ManipulationControls.ElementScale; } }

        private void MakeInkCanvas()
        {
            XInkCanvas = new InkCanvas() { Width = 60000, Height = 60000 };
            SelectionCanvas = new Canvas();
            InkControl = new FreeformInkControl(this, XInkCanvas, SelectionCanvas);
            Canvas.SetLeft(XInkCanvas, -30000);
            Canvas.SetTop(XInkCanvas, -30000);
            Canvas.SetLeft(SelectionCanvas, -30000);
            Canvas.SetTop(SelectionCanvas, -30000);
            InkHostCanvas.Children.Add(XInkCanvas);
            InkHostCanvas.Children.Add(SelectionCanvas);
        }
        #endregion

        private void ElementOnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if(e.Pointer.PointerDeviceType == ManipulationControls.BlockedInputType && ManipulationControls.FilterInput)
            Debug.WriteLine("Pointer entered: " + sender.GetType());
        }

    }
}