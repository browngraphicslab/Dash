using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DashShared;
using Windows.UI.Input;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionFreeformView : UserControl
    {
        public bool CanLink;
        public PointerRoutedEventArgs PointerArgs;
        public Rect Bounds = new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity);
        public double CanvasScale { get; set; } = 1;
        public const float MaxScale = 10;
        public const float MinScale = 0.001f;

        /// <summary>
        /// HashSet of current pointers in use so that the OperatorView does not respond to multiple inputs 
        /// </summary>
        private HashSet<uint> _currentPointers = new HashSet<uint>();
        /// <summary>
        /// IOReference (containing reference to fields) being referred to when creating the visual connection between fields 
        /// </summary>
        private OperatorView.IOReference _currReference;
        private Windows.UI.Xaml.Shapes.Path _connectionLine;
        private BezierConverter _converter;
        private MultiBinding<PathFigureCollection> _lineBinding;
        private CollectionView _parentCollection;


        private Dictionary<FieldReference, Windows.UI.Xaml.Shapes.Path> _lineDict = new Dictionary<FieldReference, Windows.UI.Xaml.Shapes.Path>();
        //private CollectionView ParentCollection;
        private Canvas parentCanvas;
        public CollectionFreeformView()
        {
            this.InitializeComponent();
            this.Loaded += Freeform_Loaded;
            //ParentCollection = view;

        }
        private void Freeform_Loaded(object sender, RoutedEventArgs e)
        {
            var parentGrid = this.GetFirstAncestorOfType<Grid>();
            _parentCollection = this.GetFirstAncestorOfType<CollectionView>();
            parentGrid.PointerMoved += FreeformGrid_OnPointerMoved;
            parentGrid.PointerReleased += FreeformGrid_OnPointerReleased;
        }

        public void StartDrag(OperatorView.IOReference ioReference)
        {
            Debug.Write("1");
            if (!CanLink)
            {
                PointerArgs = ioReference.PointerArgs;
                return;
            }

            Debug.Write("2");

            if (ioReference.PointerArgs == null) return;

            if (_currentPointers.Contains(ioReference.PointerArgs.Pointer.PointerId)) return;

            parentCanvas = xItemsControl.ItemsPanelRoot as Canvas;


            Debug.Write("3");

            _currentPointers.Add(ioReference.PointerArgs.Pointer.PointerId);
            _currReference = ioReference;
            _connectionLine = new Windows.UI.Xaml.Shapes.Path
            {
                StrokeThickness = 5,
                Stroke = new SolidColorBrush(Colors.Orange),
                IsHitTestVisible = false,
                CompositeMode =
                    ElementCompositeMode.SourceOver //TODO Bug in xaml, shouldn't need this line when the bug is fixed 
                //                                    //(https://social.msdn.microsoft.com/Forums/sqlserver/en-US/d24e2dc7-78cf-4eed-abfc-ee4d789ba964/windows-10-creators-update-uielement-clipping-issue?forum=wpdevelop)
            };
            Canvas.SetZIndex(_connectionLine, -1);
            _converter = new BezierConverter(ioReference.FrameworkElement, null, parentCanvas);

            try
            {
                _converter.Pos2 = ioReference.PointerArgs.GetCurrentPoint(parentCanvas).Position;

            }
            catch (COMException ex)
            {
            }

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

            // TODO comment back in if/when editor mode is implemented  
            /* 
            Binding visibilityBinding = new Binding
            {
                Source = DataContext as CollectionViewModel,
                Path = new PropertyPath("IsEditorMode"),
                Converter = new VisibilityConverter()
            };
            _connectionLine.SetBinding(VisibilityProperty, visibilityBinding);
            */
            parentCanvas.Children.Add(_connectionLine);

            if (!ioReference.IsOutput)
            {
                CheckLinePresence(ioReference.FieldReference);
                _lineDict.Add(ioReference.FieldReference, _connectionLine);
            }
        }

        public void CancelDrag(Pointer p)
        {
            _currentPointers.Remove(p.PointerId);
            UndoLine();
        }
        private void UndoLine()
        {
            parentCanvas.Children.Remove(_connectionLine);
            _connectionLine = null;
            _currReference = null;
        }

        public void EndDrag(OperatorView.IOReference ioReference)
        {
            OperatorView.IOReference inputReference = ioReference.IsOutput ? _currReference : ioReference;
            OperatorView.IOReference outputReference = ioReference.IsOutput ? ioReference : _currReference;
            //if (!(DataContext as CollectionViewModel).IsEditorMode)
            //{
            //    return;
            //}
            _currentPointers.Remove(ioReference.PointerArgs.Pointer.PointerId);
            if (_connectionLine == null) return;

            if (_currReference.IsOutput == ioReference.IsOutput)
            {
                UndoLine();
                return;
            }
            if (_currReference.FieldReference == null) return; 

            string outId;
            string inId;
            if (_currReference.IsOutput)
            {
                //outId = _currReference.ReferenceFieldModelController.DereferenceToRoot(null).GetId();
                //inId = ioReference.ReferenceFieldModelController.DereferenceToRoot(null).GetId();
            }
            else
            {
                //outId = ioReference.ReferenceFieldModelController.DereferenceToRoot(null).GetId();
                //inId = _currReference.ReferenceFieldModelController.DereferenceToRoot(null).GetId();
            }
            //CollectionView.Graph.AddEdge(outId, inId);
            if (CollectionView.Graph.IsCyclic())
            {
                if (_currReference.IsOutput)
                {
              //      CollectionView.Graph.RemoveEdge(outId, inId);
                }
                else
                {
                //    CollectionView.Graph.RemoveEdge(outId, inId);
                }
                CancelDrag(ioReference.PointerArgs.Pointer);
                Debug.WriteLine("Cycle detected");
                return;
            }

            _converter.Element2 = ioReference.FrameworkElement;
            _lineBinding.AddBinding(ioReference.ContainerView, FrameworkElement.RenderTransformProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, FrameworkElement.WidthProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, FrameworkElement.HeightProperty);

            DocumentController inputController =
                inputReference.FieldReference.GetDocumentController(null);
            var thisRef = (outputReference.ContainerView.DataContext as DocumentViewModel).DocumentController.GetDereferencedField(DashConstants.KeyStore.ThisKey, null);
            if (inputController.DocumentType == OperatorDocumentModel.OperatorType &&
                (inputController.GetDereferencedField(OperatorDocumentModel.OperatorKey, null) as OperatorFieldModelController).Inputs[inputReference.FieldReference.FieldKey] == TypeInfo.Document &&
                inputReference.FieldReference is DocumentFieldReference && thisRef != null)
                inputController.SetField(inputReference.FieldReference.FieldKey, thisRef, true);
            else
                inputController.SetField(inputReference.FieldReference.FieldKey,
                    new ReferenceFieldModelController(outputReference.FieldReference), true);

            if (!ioReference.IsOutput && _connectionLine != null)
            {
                CheckLinePresence(ioReference.FieldReference);
                _lineDict.Add(ioReference.FieldReference, _connectionLine);
                _connectionLine = null;
            }
            CancelDrag(ioReference.PointerArgs.Pointer);
        }

        /// <summary>
        /// Method to add the dropped field to the documentview 
        /// </summary>
        public void EndDragOnDocumentView(ref DocumentController cont, OperatorView.IOReference ioReference)
        {
            if (_currReference != null)
            {
                cont.SetField(_currReference.FieldKey, _currReference.FMController, true);
                EndDrag(ioReference); 
            }
        }

        /// <summary>
        /// Pans and zooms upon touch manipulation 
        /// </summary>
        public void UserControl_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (!IsHitTestVisible) return;
            var canvas = xItemsControl.ItemsPanelRoot as Canvas;
            Debug.Assert(canvas != null);
            e.Handled = true;
            var delta = e.Delta;

            //Create initial translate and scale transforms
            //Translate is in screen space, scale is in canvas space
            var translate = new TranslateTransform
            {
                X = delta.Translation.X,
                Y = delta.Translation.Y
            };

            var p = Util.PointTransformFromVisual(e.Position, canvas);
            var scale = new ScaleTransform
            {
                CenterX = p.X,
                CenterY = p.Y,
                ScaleX = delta.Scale,
                ScaleY = delta.Scale
            };

            //Clamp the zoom
            CanvasScale *= delta.Scale;
            ClampScale(scale);


            //Create initial composite transform
            var composite = new TransformGroup();
            composite.Children.Add(scale);
            composite.Children.Add(canvas.RenderTransform);
            composite.Children.Add(translate);

            //Get top left and bottom right screen space points in canvas space
            var inverse = composite.Inverse;
            Debug.Assert(inverse != null);
            Debug.Assert(canvas.RenderTransform != null);
            var renderInverse = canvas.RenderTransform.Inverse;
            Debug.Assert(renderInverse != null);
            var topLeft = inverse.TransformPoint(new Point(0, 0));
            var bottomRight = inverse.TransformPoint(new Point(_parentCollection.Grid.ActualWidth, _parentCollection.Grid.ActualHeight));
            var preTopLeft = renderInverse.TransformPoint(new Point(0, 0));
            var preBottomRight = renderInverse.TransformPoint(new Point(_parentCollection.Grid.ActualWidth, _parentCollection.Grid.ActualHeight));

            //Check if the panning or zooming puts the view out of bounds of the canvas
            //Nullify scale or translate components accordingly
            var outOfBounds = false;
            //Create a canvas space translation to correct the translation if necessary
            var fixTranslate = new TranslateTransform();
            if (topLeft.X < Bounds.Left && bottomRight.X > Bounds.Right)
            {
                translate.X = 0;
                fixTranslate.X = 0;
                var scaleAmount = (bottomRight.X - topLeft.X) / Bounds.Width;
                scale.ScaleY = scaleAmount;
                scale.ScaleX = scaleAmount;
                outOfBounds = true;
            }
            else if (topLeft.X < Bounds.Left)
            {
                translate.X = 0;
                fixTranslate.X = preTopLeft.X;
                scale.CenterX = Bounds.Left;
                outOfBounds = true;
            }
            else if (bottomRight.X > Bounds.Right)
            {
                translate.X = 0;
                fixTranslate.X = -(Bounds.Right - preBottomRight.X - 1);
                scale.CenterX = Bounds.Right;
                outOfBounds = true;
            }
            if (topLeft.Y < Bounds.Top && bottomRight.Y > Bounds.Bottom)
            {
                translate.Y = 0;
                fixTranslate.Y = 0;
                var scaleAmount = (bottomRight.Y - topLeft.Y) / Bounds.Height;
                scale.ScaleX = scaleAmount;
                scale.ScaleY = scaleAmount;
                outOfBounds = true;
            }
            else if (topLeft.Y < Bounds.Top)
            {
                translate.Y = 0;
                fixTranslate.Y = preTopLeft.Y;
                scale.CenterY = Bounds.Top;
                outOfBounds = true;
            }
            else if (bottomRight.Y > Bounds.Bottom)
            {
                translate.Y = 0;
                fixTranslate.Y = -(Bounds.Bottom - preBottomRight.Y - 1);
                scale.CenterY = Bounds.Bottom;
                outOfBounds = true;
            }

            //If the view was out of bounds recalculate the composite matrix
            if (outOfBounds)
            {
                composite = new TransformGroup();
                composite.Children.Add(fixTranslate);
                composite.Children.Add(scale);
                composite.Children.Add(canvas.RenderTransform);
                composite.Children.Add(translate);
            }

            canvas.RenderTransform = new MatrixTransform { Matrix = composite.Value };
        }

        /// <summary>
        /// Zooms upon mousewheel interaction 
        /// </summary>
        public void UserControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (!IsHitTestVisible) return;
            var canvas = xItemsControl.ItemsPanelRoot as Canvas;
            Debug.Assert(canvas != null);
            e.Handled = true;
            //Get mousepoint in canvas space 
            var point = e.GetCurrentPoint(canvas);
            var scaleAmount = Math.Pow(1 + 0.15 * Math.Sign(point.Properties.MouseWheelDelta),
                Math.Abs(point.Properties.MouseWheelDelta) / 120.0f);
            scaleAmount = Math.Max(Math.Min(scaleAmount, 1.7f), 0.4f);
            CanvasScale *= (float)scaleAmount;
            Debug.Assert(canvas.RenderTransform != null);
            var p = point.Position;
            //Create initial ScaleTransform 
            var scale = new ScaleTransform
            {
                CenterX = p.X,
                CenterY = p.Y,
                ScaleX = scaleAmount,
                ScaleY = scaleAmount
            };

            //Clamp scale
            ClampScale(scale);

            //Create initial composite transform
            var composite = new TransformGroup();
            composite.Children.Add(scale);
            composite.Children.Add(canvas.RenderTransform);

            var inverse = composite.Inverse;
            Debug.Assert(inverse != null);
            var renderInverse = canvas.RenderTransform.Inverse;
            Debug.Assert(inverse != null);
            Debug.Assert(renderInverse != null);
             
            //var topLeft = inverse.TransformPoint(new Point(0, 0));
            //var bottomRight = inverse.TransformPoint(new Point(_parentCollection.Grid.ActualWidth, _parentCollection.Grid.ActualHeight));
            //var preTopLeft = renderInverse.TransformPoint(new Point(0, 0));
            //var preBottomRight = renderInverse.TransformPoint(new Point(_parentCollection.Grid.ActualWidth, _parentCollection.Grid.ActualHeight));
            ////Check if the zooming puts the view out of bounds of the canvas
            ////Nullify scale or translate components accordingly 
            //var outOfBounds = false;
            ////Create a canvas space translation to correct the translation if necessary
            //var fixTranslate = new TranslateTransform();
            //if (topLeft.X < Bounds.Left && bottomRight.X > Bounds.Right)
            //{
            //    fixTranslate.X = 0;
            //    scaleAmount = (bottomRight.X - topLeft.X) / Bounds.Width;
            //    scale.ScaleY = scaleAmount;
            //    scale.ScaleX = scaleAmount;
            //    outOfBounds = true;
            //}
            //else if (topLeft.X < Bounds.Left)
            //{
            //    fixTranslate.X = preTopLeft.X;
            //    scale.CenterX = Bounds.Left;
            //    outOfBounds = true;
            //}
            //else if (bottomRight.X > Bounds.Right)
            //{
            //    fixTranslate.X = -(Bounds.Right - preBottomRight.X - 1);
            //    scale.CenterX = Bounds.Right;
            //    outOfBounds = true;
            //}
            //if (topLeft.Y < Bounds.Top && bottomRight.Y > Bounds.Bottom)
            //{
            //    fixTranslate.Y = 0;
            //    scaleAmount = (bottomRight.Y - topLeft.Y) / Bounds.Height;
            //    scale.ScaleX = scaleAmount;
            //    scale.ScaleY = scaleAmount;
            //    outOfBounds = true;
            //}
            //else if (topLeft.Y < Bounds.Top)
            //{
            //    fixTranslate.Y = preTopLeft.Y;
            //    scale.CenterY = Bounds.Top;
            //    outOfBounds = true;
            //}
            //else if (bottomRight.Y > Bounds.Bottom)
            //{
            //    fixTranslate.Y = -(Bounds.Bottom - preBottomRight.Y - 1);
            //    scale.CenterY = Bounds.Bottom;
            //    outOfBounds = true;
            //}

            ////If the view was out of bounds recalculate the composite matrix
            //if (outOfBounds)
            //{
            //    composite = new TransformGroup();
            //    composite.Children.Add(fixTranslate);
            //    composite.Children.Add(scale);
            //    composite.Children.Add(canvas.RenderTransform);
            //}
            canvas.RenderTransform = new MatrixTransform { Matrix = composite.Value };
        }

        /// <summary>
        /// Make translation inertia slow down faster
        /// </summary>
        private void UserControl_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 0.01;
        }

        private void ClampScale(ScaleTransform scale)
        {
            if (CanvasScale > MaxScale)
            {
                CanvasScale = MaxScale;
                scale.ScaleX = 1;
                scale.ScaleY = 1;
            }
            if (CanvasScale < MinScale)
            {
                CanvasScale = MinScale;
                scale.ScaleX = 1;
                scale.ScaleY = 1;
            }
        }

        /// <summary>
        /// Helper function that checks if connection line is already present for input ellipse; if so, destroy that line and create a new one  
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void CheckLinePresence(FieldReference model)
        {
            if (!_lineDict.ContainsKey(model)) return;
            var line = _lineDict[model];
            parentCanvas.Children.Remove(line);
            _lineDict.Remove(model);
        }


        private class BezierConverter : IValueConverter
        {
            public BezierConverter(FrameworkElement element1, FrameworkElement element2, FrameworkElement toElement)
            {
                Element1 = element1;
                Element2 = element2;
                ToElement = toElement;
                _figure = new PathFigure();
                _bezier = new BezierSegment();
                _figure.Segments.Add(_bezier);
                _col.Add(_figure);
                
                Pos2 = Element1.TransformToVisual(ToElement)
                    .TransformPoint(new Point(Element1.ActualWidth / 2, Element1.ActualHeight / 2)); ;
            }
            public FrameworkElement Element1 { get; set; }
            public FrameworkElement Element2 { get; set; }
            public FrameworkElement ToElement { get; set; }
            public Point Pos2 { get; set; }
            private PathFigureCollection _col = new PathFigureCollection();
            private PathFigure _figure;
            private BezierSegment _bezier;
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                var pos1 = Element1.TransformToVisual(ToElement)
                    .TransformPoint(new Point(Element1.ActualWidth / 2, Element1.ActualHeight / 2));

                var pos2 = Element2?.TransformToVisual(ToElement)
                            .TransformPoint(new Point(Element2.ActualWidth / 2, Element2.ActualHeight / 2)) ?? Pos2;

                double offset = Math.Abs((pos1.X - pos2.X) / 3);
                if (pos1.X < pos2.X)
                {
                    _figure.StartPoint = new Point(pos1.X + Element1.ActualWidth / 2, pos1.Y);
                    _bezier.Point1 = new Point(pos1.X + offset, pos1.Y);
                    _bezier.Point2 = new Point(pos2.X - offset, pos2.Y);
                    _bezier.Point3 = new Point(pos2.X - (Element2?.ActualWidth / 2 ?? 0), pos2.Y);
                }
                else
                {
                    _figure.StartPoint = new Point(pos1.X - Element1.ActualWidth / 2, pos1.Y);
                    _bezier.Point1 = new Point(pos1.X - offset, pos1.Y);
                    _bezier.Point2 = new Point(pos2.X + offset, pos2.Y);
                    _bezier.Point3 = new Point(pos2.X + (Element2?.ActualWidth / 2 ?? 0), pos2.Y);
                }
                return _col;
            }
            public object ConvertBack(object value, Type targetType, object parameter, string language)
            {
                throw new NotImplementedException();
            }
        }

        private void FreeformGrid_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_connectionLine != null)
            {
                Point pos = e.GetCurrentPoint(parentCanvas).Position;
                _converter.Pos2 = pos;
                _lineBinding.ForceUpdate();
            }
        }

        private void FreeformGrid_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            DBTest.ResetCycleDetection();
            if (_currReference != null)
            {
                if (_currReference.IsOutput)
                {
                    var opDoc = (_currReference.ContainerView.DataContext as DocumentViewModel)?.DocumentController;
                    var searchOp = opDoc.GetField(OperatorDocumentModel.OperatorKey) as OperatorFieldModelController;
                    if (searchOp != null)
                    {
                        var outType = searchOp.Outputs[_currReference.FieldReference.FieldKey];
                        if (outType == TypeInfo.Collection)
                        {

                            var fields = new Dictionary<Key, FieldModelController> { {
                            DocumentCollectionFieldModelController.CollectionKey,  new ReferenceFieldModelController(
                                    opDoc.GetId(), _currReference.FieldReference.FieldKey) }  };

                            var col = new DocumentController(fields, DashConstants.DocumentTypeStore.CollectionDocument);
                            var layoutDoc =
                                new CollectionBox(new ReferenceFieldModelController(col.GetId(), DocumentCollectionFieldModelController.CollectionKey)).Document;
                            layoutDoc.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(e.GetCurrentPoint(MainPage.Instance).Position), true);
                            var layoutController = new DocumentFieldModelController(layoutDoc);
                            col.SetField(DashConstants.KeyStore.ActiveLayoutKey, layoutController, true);
                            col.SetField(DashConstants.KeyStore.LayoutListKey, new DocumentCollectionFieldModelController(new List<DocumentController> { layoutDoc }), true);
                            MainPage.Instance.DisplayDocument(col);
                            //col.SetField(DocumentCollectionFieldModelController.CollectionKey,
                            //    new ReferenceFieldModelController(
                            //        opDoc.GetId(), _currReference.FieldReference.FieldKey), true);
                        }
                    }
                }
                CancelDrag(_currReference.PointerArgs.Pointer);

                //DocumentView view = new DocumentView();
                //DocumentViewModel viewModel = new DocumentViewModel();
                //view.DataContext = viewModel;
                //FreeformView.MainFreeformView.Canvas.Children.Add(view);
            }
        }

        /// <summary>
        /// Dictionary that maps DocumentViews on maincanvas to its DocumentID 
        /// </summary>
        //private Dictionary<string, DocumentView> _documentViews = new Dictionary<string, DocumentView>();

        private class VisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                bool isEditorMode = (bool)value;
                return isEditorMode ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language)
            {
                throw new NotImplementedException();
            }

        }
    }
}
