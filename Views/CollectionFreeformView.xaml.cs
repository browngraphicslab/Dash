﻿using DashShared;
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
using Windows.ApplicationModel.DataTransfer;
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

        public class LinePackage
        {
            public Path Line;
            public BezierConverter Converter; 
            public LinePackage(BezierConverter converter, Path line)
            {
                Converter = converter;
                Line = line; 
            }
        }

        public bool CanLink = true;
        public PointerRoutedEventArgs PointerArgs;
        private HashSet<uint> _currentPointers = new HashSet<uint>();
        private IOReference _currReference;
        private Path _connectionLine;
        private BezierConverter _converter;
        private MultiBinding<PathFigureCollection> _lineBinding;
        private Dictionary<FieldReference, LinePackage> _lineDict = new Dictionary<FieldReference, LinePackage>();
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
        public FreeformInkControls InkControls;
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
            Loaded += Freeform_Loaded;
            Unloaded += Freeform_Unloaded;
            DataContextChanged += OnDataContextChanged;
            
            ManipulationControls = new ManipulationControls(this, doesRespondToManipulationDelta:true, doesRespondToPointerWheel: true);
            ManipulationControls.OnManipulatorTranslatedOrScaled += ManipulationControls_OnManipulatorTranslated;

            DragLeave += DocView_DragOver;
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
        /// Called when documentview is deleted; delete all connections coming from it as well  
        /// </summary>
        public void DeleteConnections(DocumentView docView)
        {
            var refs = _lineDict.Keys.ToList(); 
            for (int i = _lineDict.Count -1; i >= 0; i--)
            {
                var package = _lineDict[refs[i]];
                var converter = package.Converter;
                var view1 = converter.Element1.GetFirstAncestorOfType<DocumentView>();
                var view2 = converter.Element2.GetFirstAncestorOfType<DocumentView>();

                if (view1 == docView || view2 == docView)
                {
                    itemsPanelCanvas.Children.Remove(package.Line);
                    _lineDict.Remove(refs[i]); 
                }
            }
        }

        /// <summary>
        /// Update the bindings on lines when documentview is minimized to icon view 
        /// </summary>
        /// <param name="becomeSmall">whether the document has minimized or regained normal view</param>
        /// <param name="docView">the documentview that calls the method</param>
        public void UpdateBinding(bool becomeSmall, DocumentView docView)
        {
            foreach (var package in _lineDict.Values)
            {
                var converter = package.Converter;
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
                Stroke = (SolidColorBrush)App.Instance.Resources["AccentGreen"],
                IsHitTestVisible = false,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
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

            //if (!ioReference.IsOutput)
            //{
                //CheckLinePresence(_converter);
                //_lineDict.Add(ioReference.FieldReference, new LinePackage(_converter,_connectionLine));
            //}
        }

        public void CancelDrag(Pointer p)
        {
            ViewModel.SetGlobalHitTestVisiblityOnSelectedItems(false);
            if (p != null) _currentPointers.Remove(p.PointerId);
            UndoLine();
        }

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

            if (!isCompoundOperator)
            {
                DocumentController inputController =
                    inputReference.FieldReference.GetDocumentController(null);
                var thisRef = (outputReference.ContainerView.DataContext as DocumentViewModel).DocumentController.GetDereferencedField(KeyStore.ThisKey, null);
                if (inputController.DocumentType == OperatorDocumentModel.OperatorType && inputReference.FieldReference is DocumentFieldReference && thisRef != null)
                    inputController.SetField(inputReference.FieldReference.FieldKey, thisRef, true);
                else
                    inputController.SetField(inputReference.FieldReference.FieldKey,
                        new ReferenceFieldModelController(outputReference.FieldReference), true);
            }

            if (_connectionLine != null)
            {
                CheckLinePresence(ioReference.FieldReference);
                _lineDict.Add(ioReference.FieldReference, new LinePackage(_converter, _connectionLine));
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
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void CheckLinePresence(FieldReference reference)
        {
            if (!_lineDict.ContainsKey(reference)) return;
            var line = _lineDict[reference];
            itemsPanelCanvas.Children.Remove(line.Line);
            _lineDict.Remove(reference);
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
            (sender as DocumentView).OuterGrid.Tapped += DocumentView_Tapped;
            _documentViews.Add((sender as DocumentView)); 
        }

        private void FreeformGrid_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            DBTest.ResetCycleDetection();
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
            CancelDrag(e.Pointer);
        }

        #region Flyout

        private void InitializeFlyout()
        {
            _flyout = new MenuFlyout();
            var menuItem = new MenuFlyoutItem { Text = "Add Operators" };
            menuItem.Click += MenuItem_Click;
            _flyout.Items?.Add(menuItem);

            var menuItem2 = new MenuFlyoutItem { Text = "Add Document" };
            menuItem2.Click += MenuItem_Click2;
            _flyout.Items?.Add(menuItem2);

            var menuItem3 = new MenuFlyoutItem { Text = "Add Collection" };
            menuItem3.Click += MenuItem_Click3;
            _flyout.Items?.Add(menuItem3);
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

            OperatorSearchView.Instance.LostFocus += (ss, ee) => xCanvas.Children.Remove(OperatorSearchView.Instance);

            DisposeFlyout();
        }

        private void MenuItem_Click2(object sender, RoutedEventArgs e)
        {
            var menu = sender as MenuFlyoutItem;
            var transform = menu.TransformToVisual(MainPage.Instance.xCanvas);
            var pointOnCanvas = transform.TransformPoint(new Point());

            var fields = new Dictionary<KeyController, FieldModelController>()
            {
                [KeyStore.ActiveLayoutKey] = new DocumentFieldModelController(new FreeFormDocument(new List<DocumentController>(), pointOnCanvas, new Size(100, 100)).Document)
            };

            ViewModel.AddDocument(new DocumentController(fields, DocumentType.DefaultType), null);


            DisposeFlyout();
        }

        private void MenuItem_Click3(object sender, RoutedEventArgs e)
        {
            var menu = sender as MenuFlyoutItem;
            var transform = menu.TransformToVisual(MainPage.Instance.xCanvas);
            var pointOnCanvas = transform.TransformPoint(new Point());

            var fields = new Dictionary<KeyController, FieldModelController>()
            {
                [DocumentCollectionFieldModelController.CollectionKey] = new DocumentCollectionFieldModelController(),
            };

            var documentController = new DocumentController(fields, DocumentType.DefaultType);
            documentController.SetActiveLayout(new CollectionBox(new ReferenceFieldModelController(documentController.GetId(), DocumentCollectionFieldModelController.CollectionKey), pointOnCanvas.X, pointOnCanvas.Y).Document, true, true);
            ViewModel.AddDocument(documentController, null);


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
            if (InkControls != null && InkControls.IsDrawing)
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

        private bool _isToggleOn; 

        private Dictionary<DocumentView, DocumentController> _payload = new Dictionary<DocumentView, DocumentController>();
        private List<DocumentView> _documentViews = new List<DocumentView>(); 

        public void ToggleSelectAllItems()
        {
            _isToggleOn = !_isToggleOn; 
            foreach (var docView in _documentViews)
            {
                if (_isToggleOn)
                {
                    Select(docView);
                    _payload.Add(docView, (docView.DataContext as DocumentViewModel).DocumentController);
                } else
                {
                    Deselect(docView);
                    _payload.Remove(docView); 
                }
            }
        }
        

        private void Deselect(DocumentView docView)
        {
            docView.OuterGrid.Background = new SolidColorBrush(Colors.Transparent);
            docView.CanDrag = false;
            docView.ManipulationMode = ManipulationModes.All;
            docView.DragStarting -= DocView_OnDragStarting;
        }

        private void Select(DocumentView docView)
        {
            docView.OuterGrid.Background = new SolidColorBrush(Colors.LimeGreen);
            docView.CanDrag = true;
            docView.ManipulationMode = ManipulationModes.None;
            docView.DragStarting += DocView_OnDragStarting;
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

        private void DocView_DragOver(object sender, DragEventArgs args)
        {
            _payload = new Dictionary<DocumentView, DocumentController>();

            ViewModel.SetGlobalHitTestVisiblityOnSelectedItems(false);

            var carrier = ItemsCarrier.Instance;
            if (carrier.Source == carrier.Destination)
                return; // we don't want to drop items on ourself

            ViewModel.RemoveDocuments(carrier.Payload);
            foreach (var view in _payload.Keys.ToList())
                _documentViews.Remove(view); 
        }

        public void DocView_OnDragStarting(object sender, DragStartingEventArgs e)
        {
            ViewModel.SetGlobalHitTestVisiblityOnSelectedItems(true);

            var carrier = ItemsCarrier.Instance;

            carrier.Destination = null;
            carrier.Source = ViewModel;
            carrier.Payload = _payload.Values.ToList();
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }

        private void MakeInkCanvas()
        {
            InkControls = new FreeformInkControls(this, XInkCanvas, SelectionCanvas)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top
            };
            xOuterGrid.Children.Add(InkControls);
            Canvas.SetLeft(XInkCanvas, -30000);
            Canvas.SetTop(XInkCanvas, -30000);
            Canvas.SetLeft(SelectionCanvas, -30000);
            Canvas.SetTop(SelectionCanvas, -30000);
            xItemsControl.ItemsPanelRoot.Children.Insert(0, XInkCanvas);
            xItemsControl.ItemsPanelRoot.Children.Insert(1, SelectionCanvas);
            xItemsControl.Items.VectorChanged += ItemsOnVectorChanged;
            
        }
        

        private void ItemsOnVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
        {
            Canvas.SetZIndex(XInkCanvas, 0);
            if (xItemsControl.ItemsPanelRoot != null && xItemsControl.ItemsPanelRoot.Children.Contains(XInkCanvas))
            {
                xItemsControl.ItemsPanelRoot.Children.Remove(XInkCanvas);
                xItemsControl.ItemsPanelRoot.Children.Remove(SelectionCanvas);
                xItemsControl.ItemsPanelRoot.Children.Insert(0, XInkCanvas);
                xItemsControl.ItemsPanelRoot.Children.Insert(1, SelectionCanvas);
            }
        }
    #endregion
    }
}
