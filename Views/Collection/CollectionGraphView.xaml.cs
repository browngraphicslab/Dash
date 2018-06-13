using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionGraphView : UserControl, ICollectionView
    {
        private DocumentController _parentDocument;
        private ObservableCollection<Ellipse> _nodes;
        private ObservableCollection<DocumentController> CollectionDocuments { get; }

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

        private ObservableDictionary<DocumentViewModel, DocumentViewModel> Connections { get; set; }

        public CollectionGraphView()
        {
            InitializeComponent();
            AdjacencyLists = new ObservableDictionary<DocumentViewModel, ObservableCollection<DocumentViewModel>>();
            Connections = new ObservableDictionary<DocumentViewModel, DocumentViewModel>();
            _nodes = new ObservableCollection<Ellipse>();

            Loaded += CollectionGraphView_Loaded;
            Unloaded += CollectionGraphView_Unloaded;
        }

        private void CollectionGraphView_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += CollectionGraphView_DataContextChanged;
            CollectionGraphView_DataContextChanged(null, null);

            // set scrollviewer to be the same dimensions as the screen
            xScrollViewer.Width = ActualWidth;
            xScrollViewer.Height = ActualHeight;
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
                cvm.CollectionController.FieldModelUpdated += CollectionController_FieldModelUpdated;
                ViewModel = cvm;

                // set the parentDocument which is the document holding this collection
                ParentDocument = this.GetFirstAncestorOfType<DocumentView>()?.ViewModel?.DocumentController;
                if (ParentDocument != null)
                {
                    GenerateGraph();
                }
            }
        }

        private void GenerateGraph()
        {
            //var context = new Context(ParentDocument);

            AdjacencyLists.Clear();
            foreach (var dvm in ViewModel.DocumentViewModels)
            {
                Position(dvm);
            }
        }

        private void Position(DocumentViewModel dvm)
        {
            var toConnections = dvm.DataDocument.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count ?? 1;
            var fromConnections = dvm.DataDocument.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count ?? 1;

            if (toConnections != 1 || fromConnections != 1)
            {

            }

            var node = new Ellipse
            {
                Width = (toConnections + fromConnections) * 10,
                Height = Width,
                Fill = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0))
            };
            _nodes.Add(node);
        }

        private void CollectionController_FieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            var properArgs = (ListController<DocumentController>.ListFieldUpdatedEventArgs) args;

            switch (properArgs.ListAction)
            {
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add:
                    AddNodes(properArgs);
                    break;
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Remove:
                    break;
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Replace:
                    break;
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Clear:
                    break;
            }
        }

        private void AddNodes(ListController<DocumentController>.ListFieldUpdatedEventArgs properArgs)
        {

        }
    }
}
