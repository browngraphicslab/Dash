﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
   
    
    public class GraphNodeViewModel : ViewModelBase
    {
        #region variables

        private DocumentViewModel _documentViewModel;
        private DocumentController _documentController;
        private double _xPosition;
        private double _yPosition;

        public DocumentViewModel DocumentViewModel
        {
            get => _documentViewModel;
            set => SetProperty(ref _documentViewModel, value);
        }

        public DocumentController DocumentController
        {
            get => _documentController;
            set => SetProperty(ref _documentController, value);
        }

        public double XPosition
        {
            get => _xPosition;
            set => SetProperty(ref _xPosition, value);
        }

        public double YPosition
        {
            get => _yPosition;
            set => SetProperty(ref _yPosition, value);
        }

        public ObservableCollection<Polyline> Links;

        #endregion

        #region constructors
        public GraphNodeViewModel()
        {
        }

        public GraphNodeViewModel(DocumentViewModel dvm, double x, double y)
        {
            Links = new ObservableCollection<Polyline>();
            DocumentViewModel = dvm;
            DocumentController = dvm.DocumentController;
            XPosition = x;
            YPosition = y;
        }

        #endregion
    }



    public sealed partial class CollectionGraphView : UserControl, ICollectionView
    {
        private DocumentController _parentDocument;
        private ObservableCollection<GraphNodeViewModel> _nodes;
        private Random _randInt;
        private ObservableCollection<DocumentController> CollectionDocuments { get; set; }

        public CollectionViewModel ViewModel { get; set; }

        public DocumentController ParentDocument
        {
            get => _parentDocument;
            set
            {
                _parentDocument = value;
                if (value != null)
                {
                    if (ParentDocument.GetField(CollectionDBView.FilterFieldKey) == null)
                        ParentDocument.SetField(CollectionDBView.FilterFieldKey, new KeyController(), true);
                }
            }
        }

        private ObservableDictionary<DocumentViewModel, ObservableCollection<DocumentViewModel>> AdjacencyLists { get; set; }

        public ObservableDictionary<DocumentViewModel, DocumentViewModel> Connections { get; private set; }

        private double _minGap = 50;
        private double _maxGap = 100;

        public CollectionGraphView()
        {
            InitializeComponent();
            AdjacencyLists = new ObservableDictionary<DocumentViewModel, ObservableCollection<DocumentViewModel>>();
            Connections = new ObservableDictionary<DocumentViewModel, DocumentViewModel>();
            CollectionDocuments = new ObservableCollection<DocumentController>();
            _nodes = new ObservableCollection<GraphNodeViewModel>();
            _randInt = new Random();

            Loaded += CollectionGraphView_Loaded;
            Unloaded += CollectionGraphView_Unloaded;
        }

        private void CollectionGraphView_Loaded(object sender, RoutedEventArgs e)
        {
            xScrollViewCanvas.Width = xScrollViewer.ActualWidth;
            xScrollViewCanvas.Height = xScrollViewer.ActualHeight;

            DataContextChanged += CollectionGraphView_DataContextChanged;
            CollectionGraphView_DataContextChanged(null, null);
            xInfoPanel.Children.Add(new GraphInfoView(ViewModel, this));
        }

        private void CollectionGraphView_Unloaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged -= CollectionGraphView_DataContextChanged;
        }

        private void CollectionGraphView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is CollectionViewModel cvm)
            {
                // if datacontext hasn't actually changed just return
                if (ViewModel != null && ViewModel.CollectionController.Equals(cvm.CollectionController)) return;

                // remove events from previous datacontext
                if (ViewModel != null)
                    ViewModel.CollectionController.FieldModelUpdated -= CollectionController_FieldModelUpdated;

                // add events to new datacontext and set it
                // cvm.CollectionController.FieldModelUpdated += CollectionController_FieldModelUpdated;
                ViewModel = cvm;
                ViewModel.DocumentViewModels.CollectionChanged += DocumentViewModels_CollectionChanged;

                // set the parentDocument which is the document holding this collection
                ParentDocument = this.GetFirstAncestorOfType<DocumentView>()?.ViewModel?.DocumentController;
                if (ParentDocument != null)
                {
                    CreateCollection();
                    GenerateGraph();
                }
            }
        }

        private void DocumentViewModels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddNodes(new ObservableCollection<DocumentViewModel>(e.NewItems.Cast<DocumentViewModel>()));
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveNodes(new ObservableCollection<DocumentViewModel>(e.OldItems.Cast<DocumentViewModel>()));
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    CollectionDocuments.Clear();
                    _nodes.Clear();
                    break;
            }
        }

        private void RemoveNodes(ObservableCollection<DocumentViewModel> oldDocs)
        {
            foreach (var doc in oldDocs)
            {
                CollectionDocuments.Remove(doc.DocumentController);
                var nodeToRemove = _nodes.First(gvm => gvm.DocumentViewModel.Equals(doc));
                if (nodeToRemove != null)
                    _nodes.Remove(nodeToRemove);
            }
        }

        private void CreateCollection()
        {
            foreach (var dvm in ViewModel.DocumentViewModels)
            {
                if (dvm != null)
                {
                    CollectionDocuments.Add(dvm.DocumentController);
                    
                }
            }
        }

        private void GenerateGraph()
        {
            AdjacencyLists.Clear();
            foreach (var dvm in ViewModel.DocumentViewModels)
            {
                if (dvm != null)
                {
                    _nodes.Add(new GraphNodeViewModel(dvm, _randInt.Next(0, (int)xScrollViewCanvas.Width), _randInt.Next(0, (int)xScrollViewCanvas.Height)));
                   
                }
            }

            foreach (var dvm in ViewModel.DocumentViewModels)
            {
                AdjacencyLists[dvm] = new ObservableCollection<DocumentViewModel>();

                if (dvm.DataDocument != null)
                {
                    var fromConnections = dvm.DataDocument
                                              .GetDereferencedField<ListController<DocumentController>>(
                                                  KeyStore.LinkFromKey, null)?.TypedData ??
                                          new List<DocumentController>();
                    var toConnections = dvm.DataDocument
                                            .GetDereferencedField<ListController<DocumentController>>(
                                                KeyStore.LinkToKey, null)?.TypedData ??
                                        new List<DocumentController>();


                    //if a to connection has already connection, a from connection cannot be added
                    //foreach (var link in fromConnections)
                    //{
                    //    var fromDocs = link.GetDataDocument()
                    //        .GetField<ListController<DocumentController>>(KeyStore.LinkToKey).TypedData;

                    //    foreach (var fromDoc in fromDocs)
                    //    {
                    //        var fromViewModel = ViewModel.DocumentViewModels.First(vm =>
                    //            vm.DocumentController.GetDataDocument().Equals(fromDoc));
                    //        if (fromViewModel != null)
                    //        {
                    //            AdjacencyLists[dvm].Add(fromViewModel);
                    //            Connections.Add(fromViewModel, dvm);
                    //            AddLink(fromViewModel, dvm);
                    //        }
                    //    }

                    //}

                    foreach (var link in toConnections)
                    {
                        var toDocs = link.GetDataDocument()
                            .GetField<ListController<DocumentController>>(KeyStore.LinkToKey).TypedData;
                        foreach (var toDoc in toDocs)
                        {
                            var toViewModel = ViewModel.DocumentViewModels.First(vm =>
                                vm.DocumentController.GetDataDocument().Equals(toDoc));

                            if (toViewModel != null)
                            {
                                AdjacencyLists[dvm].Add(toViewModel);
                                Connections.Add(toViewModel, dvm);
                                AddLink(toViewModel, dvm);
                            }
                        }
                    }
                }
            }

        }

        private void AddLink(DocumentViewModel fromViewModel, DocumentViewModel toViewmodel)
        {
            Polyline link = new Polyline();
            

            var toGvm = _nodes.First(gvm => gvm.DocumentViewModel.DataDocument.Equals(toViewmodel.DataDocument));
            Point toPoint = new Point
            {
                X = toGvm.XPosition,
                Y = toGvm.YPosition
            };

            var fromGvm = _nodes.First(gvm => gvm.DocumentViewModel.DataDocument.Equals(fromViewModel.DataDocument));
            Point fromPoint = new Point
            {
                X = fromGvm.XPosition,
                Y = fromGvm.YPosition
            };

            link.Points.Add(fromPoint);
            link.Points.Add(toPoint);
            link.Fill = Application.Current.Resources["BorderHighlight"] as SolidColorBrush;

            xScrollViewCanvas.Children.Add(link);

        }

        private void CollectionController_FieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            //var properArgs = (ListController<DocumentController>.ListFieldUpdatedEventArgs) args;

            //switch (properArgs.ListAction)
            //{
            //    case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add:
            //        AddNodes(properArgs);
            //        break;
            //    case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Remove:
            //        break;
            //    case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Replace:
            //        break;
            //    case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Clear:
            //        break;
            //}
        }

        private void AddNodes(ObservableCollection<DocumentViewModel> newDocs)
        {
            foreach (var newDoc in newDocs)
            {
                if (!CollectionDocuments.Contains(newDoc.DocumentController.GetDataDocument()))
                {
                    CollectionDocuments.Add(newDoc.DocumentController.GetDataDocument());
                    _nodes.Add(new GraphNodeViewModel(newDoc, _randInt.Next(0, (int)xScrollViewCanvas.Width), _randInt.Next(0, (int)xScrollViewCanvas.Height)));
                }
            }
        }

        private void CollectionGraphView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var offsetX = e.NewSize.Width - e.PreviousSize.Width;
            var offsetY = e.NewSize.Height - e.PreviousSize.Height;
            xScrollViewCanvas.Width = xScrollViewer.ActualWidth;
            xScrollViewCanvas.Height = xScrollViewer.ActualHeight;

            foreach (var node in _nodes)
            {
                var x = node.XPosition;
                var y = node.YPosition;
                node.XPosition = x + (offsetX * (x / e.PreviousSize.Width));
                node.YPosition = y + (offsetY * (y / e.PreviousSize.Height));
            }
        }
    }
}
