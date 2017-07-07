using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionView : UserControl
    {

        public double CanvasScale { get; set; } = 1;
        public const float MaxScale = 10;
        public const float MinScale = 0.5f;
        public Rect Bounds = new Rect(0, 0, 5000, 5000);

        public CollectionViewModel ViewModel;

        public CollectionView(CollectionViewModel vm)
        {
            this.InitializeComponent();
            DataContext = ViewModel = vm;
            var docFieldCtrler = ContentController.GetController<FieldModelController>(vm.CollectionFieldModelController.DocumentCollectionFieldModel.Id);
            docFieldCtrler.FieldModelUpdatedEvent += DocFieldCtrler_FieldModelUpdatedEvent;
            SetEventHandlers();
            xDocumentDisplayView.DataContextChanged += XDocumentDisplayView_DataContextChanged;
            InkSource.Presenters.Add(xInkCanvas.InkPresenter);
        }

        private void XDocumentDisplayView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            
        }

        private void DocFieldCtrler_FieldModelUpdatedEvent(FieldModelController sender)
        {
            DataContext = ViewModel;
        }

        private void SetEventHandlers()
        {
            Loaded += CollectionView_Loaded;
            ViewModel.DataBindingSource.CollectionChanged += DataBindingSource_CollectionChanged;
            FreeformOption.Tapped += ViewModel.FreeformButton_Tapped;
            GridViewOption.Tapped +=
                ViewModel.GridViewButton_Tapped;
            ListOption.Tapped += ViewModel.ListViewButton_Tapped;
            CloseButton.Tapped += CloseButton_Tapped;
            SelectButton.Tapped += ViewModel.SelectButton_Tapped;
            DeleteSelected.Tapped += ViewModel.DeleteSelected_Tapped;
            
        }

        private void CollectionView_Loaded(object sender, RoutedEventArgs e)
        {
            var parentDocument = this.GetFirstAncestorOfType<DocumentView>();

            if (parentDocument != MainPage.Instance.MainDocView)
            {
                DoubleTapped += ViewModel.Grid_DoubleTapped;
                parentDocument.SizeChanged += (ss, ee) =>
                {
                    var height = (parentDocument.DataContext as DocumentViewModel)?.Height;
                    if (height != null)
                        Height = (double)height;
                    var width = (parentDocument.DataContext as DocumentViewModel)?.Width;
                    if (width != null)
                        Width = (double)width;
                };
            }
        }

        private void DataBindingSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var eNewItem in e.NewItems)
                {
                    var docVM = eNewItem as DocumentViewModel;
                    Debug.Assert(docVM != null);
                    OperatorFieldModelController ofm =
                        docVM.DocumentController.GetField(OperatorDocumentModel.OperatorKey) as
                            OperatorFieldModelController;
                    if (ofm != null)
                    {
                        foreach (var inputKey in ofm.InputKeys)
                        {
                            foreach (var outputKey in ofm.OutputKeys)
                            {
                                ReferenceFieldModel irfm =
                                    new ReferenceFieldModel(docVM.DocumentController.GetId(), inputKey);
                                ReferenceFieldModel orfm =
                                    new ReferenceFieldModel(docVM.DocumentController.GetId(), outputKey);
                                _graph.AddEdge(ContentController.DereferenceToRootFieldModel(irfm).Id,
                                    ContentController.DereferenceToRootFieldModel(orfm).Id);
                            }
                        }
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var eOldItem in e.OldItems)
                {
                    var docVM = eOldItem as DocumentViewModel;
                    Debug.Assert(docVM != null);
                    OperatorFieldModelController ofm =
                        docVM.DocumentController.GetField(OperatorDocumentModel.OperatorKey) as
                            OperatorFieldModelController;
                    if (ofm != null)
                    {
                        foreach (var inputKey in ofm.InputKeys)
                        {
                            foreach (var outputKey in ofm.OutputKeys)
                            {
                                ReferenceFieldModel irfm =
                                    new ReferenceFieldModel(docVM.DocumentController.GetId(), inputKey);
                                ReferenceFieldModel orfm =
                                    new ReferenceFieldModel(docVM.DocumentController.GetId(), outputKey);
                                _graph.RemoveEdge(ContentController.DereferenceToRootFieldModel(irfm).Id,
                                    ContentController.DereferenceToRootFieldModel(orfm).Id);
                            }
                        }
                    }
                }
            }
        }
        

        private void CloseButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var contentPresentor = this.GetFirstAncestorOfType<ContentPresenter>();
            (VisualTreeHelper.GetParent(contentPresentor) as Canvas)?.Children.Remove(this
                .GetFirstAncestorOfType<ContentPresenter>());
        }

        /// <summary>
        /// Helper class to detect cycles 
        /// </summary>
        private Graph<string> _graph = new Graph<string>();
        /// <summary>
        /// Line to create and display connection lines between OperationView fields and Document fields 
        /// </summary>
        private Path _connectionLine;

        private MultiBinding<PathFigureCollection> _lineBinding;
        private BezierConverter _converter;

        /// <summary>
        /// IOReference (containing reference to fields) being referred to when creating the visual connection between fields 
        /// </summary>
        private OperatorView.IOReference _currReference;

        private Dictionary<ReferenceFieldModel, Path> _lineDict = new Dictionary<ReferenceFieldModel, Path>();

        /// <summary>
        /// HashSet of current pointers in use so that the OperatorView does not respond to multiple inputs 
        /// </summary>
        private HashSet<uint> _currentPointers = new HashSet<uint>();

        /// <summary>
        /// Dictionary that maps DocumentViews on maincanvas to its DocumentID 
        /// </summary>
        //private Dictionary<string, DocumentView> _documentViews = new Dictionary<string, DocumentView>();

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

        private class VisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                bool isEditorMode = (bool)value;
                return isEditorMode ? Visibility.Visible : Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language)
            {
                throw new NotImplementedException();
            }
        }
    }
}
