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


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionFreeformView : UserControl
    {
        public bool CanLink;
        public PointerRoutedEventArgs PointerArgs;

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
            parentGrid.PointerMoved += FreeformGrid_OnPointerMoved;
            parentGrid.PointerReleased += FreeformGrid_OnPointerReleased;
        }

        //private void DocumentView_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        //{
        //    var cvm = DataContext as CollectionViewModel;
        //    //(sender as DocumentView).Manipulator.TurnOff();

        //}
        //private void DocumentView_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        //{
        //    //var cvm = DataContext as CollectionViewModel;
        //    //var dv = (sender as DocumentView);
        //    //var dvm = dv.DataContext as DocumentViewModel;
        //    //var where = dv.RenderTransform.TransformPoint(new Point(e.Delta.Translation.X, e.Delta.Translation.Y));
        //    //dvm.Position = where;
        //    //e.Handled = true;
        //}

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
                inputController.SetField(inputReference.FieldReference.FieldKey,
                    new ReferenceFieldModelController(outputReference.FieldReference), true);

            if (!ioReference.IsOutput && _connectionLine != null)
            {
                CheckLinePresence(ioReference.FieldReference);
                _lineDict.Add(ioReference.FieldReference, _connectionLine);
                _connectionLine = null;
            }
        }

        /// <summary>
        /// Helper function that checks if connection line is already present for input ellipse; if so, destroy that line and create a new one  
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void CheckLinePresence(FieldReference model)
        {
            if (_lineDict.ContainsKey(model))
            {
                Windows.UI.Xaml.Shapes.Path line = _lineDict[model];
                parentCanvas.Children.Remove(line);
                _lineDict.Remove(model);
            }
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
            if (_currReference != null)
            {
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
