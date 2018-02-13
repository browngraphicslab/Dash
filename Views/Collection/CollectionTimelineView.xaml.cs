using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using DashShared;
using Microsoft.Toolkit.Uwp.UI;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public class TimelineMetadata
    {
        public long MaxTime { get; set; }
        public long MinTime { get; set; }
        public double ActualWidth { get; set; }
        public double ActualHeight { get; set; }
        public double LeftRightMargin { get; set; }
    }

    public class TimelineElementViewModel : ViewModelBase
    {
        private DocumentViewModel _documentViewModel;
        private DocumentContext _documentContext;

        public DocumentViewModel DocumentViewModel
        {
            get => _documentViewModel;
            set => SetProperty(ref _documentViewModel, value);
        }

        public DocumentContext DocumentContext
        {
            get => _documentContext;
            set => SetProperty(ref _documentContext, value);
        }


        public TimelineElementViewModel()
        {
        }

        public TimelineElementViewModel(DocumentContext documentContext, DocumentViewModel documentViewModel)
        {
            DocumentContext = documentContext;
            DocumentViewModel = documentViewModel;
        }
    }


    public sealed partial class CollectionTimelineView : SelectionElement, ICollectionView
    {
        private readonly ObservableCollection<TimelineElementViewModel> _contextList;

        private readonly Dictionary<DocumentViewModel, FieldControllerBase.FieldUpdatedHandler> _docViewModelToHandler =
            new Dictionary<DocumentViewModel, FieldControllerBase.FieldUpdatedHandler>();


        private readonly List<DocumentViewModel> _trackedViewModels = new List<DocumentViewModel>();

        public double CurrentXPosition { get; set; }

        public List<double> DisplayedXPositions { get; private set; }


        public CollectionTimelineView()
        {
            this.InitializeComponent();
            _contextList = new ObservableCollection<TimelineElementViewModel>();
            DataContextChanged += OnDataContextChanged;

            Unloaded += CollectionTimelineView_Unloaded;

            Metadata = new TimelineMetadata
            {
                LeftRightMargin = 160
            };

            DisplayedXPositions = new List<double>();

            Loaded += CollectionTimelineView_Loaded;
        }

        private void CollectionTimelineView_Loaded(object sender, RoutedEventArgs e)
        {
            CollectionTimelineView_OnSizeChanged(null, null);
        }

        public TimelineMetadata Metadata { get; }
        public BaseCollectionViewModel ViewModel { get; private set; }
        public event Action MetadataUpdated;


        private void UpdateMetadataMinAndMax()
        {
            try
            {
                
                
                Metadata.MinTime = _contextList.Min(vm => vm.DocumentContext.CreationTimeTicks);
                Metadata.MaxTime = _contextList.Max(vm => vm.DocumentContext.CreationTimeTicks);
                
                MetadataUpdated?.Invoke();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private void CollectionTimelineView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            DisplayedXPositions = new List<double>();
            SetTimelineFormatting();
        }

        private void SetTimelineFormatting()
        {
            xScrollViewer.Width = ActualWidth;
            xScrollViewer.Height = ActualHeight - 80;
           // Canvas.SetTop(xItemsControl, 17 - ActualHeight / 2);

            CurrentXPosition = 0;
            Metadata.ActualHeight = ActualHeight;
            Metadata.ActualWidth = ActualWidth;
            MetadataUpdated?.Invoke();

            var width = CurrentXPosition + 100;
            var minWidth = ActualWidth - 160;
            if (width < minWidth)
            {
                width = minWidth;
            }

            SetTimelineWidth(width);
        }

        private void SetTimelineWidth(double width)
        {
            xHorizontalLine.Width = width;
            Canvas.SetLeft(xVerticalLineRight, width + 80);
            xScrollViewCanvas.Width = width;
        }

        #region ContextManagement

        private void CollectionTimelineView_Unloaded(object sender, RoutedEventArgs e)
        {
            RemoveViewModelEvents(ViewModel);
            CurrentXPosition = 0;
            Unloaded -= CollectionTimelineView_Unloaded;
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var vm = DataContext as BaseCollectionViewModel;

            // remove events from the old ViewModel (null checks itself)
            RemoveViewModelEvents(ViewModel);

            // update the ViewModel variable to the current view model and set its selection
            ViewModel = vm;
            ViewModel.SetSelected(this, IsSelected);

            // make the new ViewModel listen to events
            AddViewModelEvents(ViewModel);
            Initialize(ViewModel);
        }

        private void Initialize(ICollectionViewModel viewModel)
        {
            foreach (var dvm in viewModel.DocumentViewModels)
            {
                var docContexts = GetWebContextFromDocViewModel(dvm)?.TypedData
                    .Select(i => i.Data.CreateObject<DocumentContext>());
                if (docContexts != null)
                    foreach (var dc in docContexts)
                        _contextList.Add(new TimelineElementViewModel(dc, dvm));
            }
            UpdateMetadataMinAndMax();
        }

        private void AddViewModelEvents(ICollectionViewModel viewModel)
        {
            if (viewModel != null)
                viewModel.DocumentViewModels.CollectionChanged += DocumentViewModels_CollectionChanged;
        }

        private void RemoveViewModelEvents(ICollectionViewModel viewModel)
        {
            if (viewModel != null)
                viewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;
        }

        private void DocumentViewModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    TrackViewModels(e.NewItems.Cast<DocumentViewModel>());
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    UntrackViewModels(e.OldItems.Cast<DocumentViewModel>());
                    break;
                case NotifyCollectionChangedAction.Replace:
                    UntrackViewModels(e.OldItems.Cast<DocumentViewModel>());
                    TrackViewModels(e.NewItems.Cast<DocumentViewModel>());
                    break;
                case NotifyCollectionChangedAction.Reset:
                    UntrackViewModels(new List<DocumentViewModel>(_trackedViewModels));
                    TrackViewModels(e.NewItems.Cast<DocumentViewModel>());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UntrackViewModels(IEnumerable<DocumentViewModel> viewModels)
        {
            foreach (var vm in viewModels)
            {
                _trackedViewModels.Remove(vm);
                var dataDocument = vm.DocumentController.GetDataDocument(null);
                dataDocument.RemoveFieldUpdatedListener(KeyStore.WebContextKey, _docViewModelToHandler[vm]);
            }
        }

        private void TrackViewModels(IEnumerable<DocumentViewModel> viewModels)
        {
            foreach (var vm in viewModels)
            {
                _trackedViewModels.Add(vm);
                var dataDocument = vm.DocumentController.GetDataDocument(null);

                void Handler(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
                {
                    var properArgs = args as ListController<TextController>.ListFieldUpdatedEventArgs;
                    Debug.Assert(properArgs != null, "Make sure the way we store webContexts hasn't changed");
                    switch (properArgs.ListAction)
                    {
                        case ListController<TextController>.ListFieldUpdatedEventArgs.ListChangedAction.Add:
                            foreach (var dc in properArgs.ChangedDocuments.Select(i => i.Data
                                .CreateObject<DocumentContext>()))
                                _contextList.Add(new TimelineElementViewModel(dc, vm));
                            UpdateMetadataMinAndMax();
                            break;
                        case ListController<TextController>.ListFieldUpdatedEventArgs.ListChangedAction.Remove:
                            foreach (var dc in properArgs.ChangedDocuments.Select(i => i.Data
                                .CreateObject<DocumentContext>()))
                                _contextList.Add(new TimelineElementViewModel(dc, vm));
                            UpdateMetadataMinAndMax();
                            break;
                        case ListController<TextController>.ListFieldUpdatedEventArgs.ListChangedAction.Replace:
                            throw new NotImplementedException();
                        case ListController<TextController>.ListFieldUpdatedEventArgs.ListChangedAction.Clear:
                            throw new NotImplementedException();
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                dataDocument.AddFieldUpdatedListener(KeyStore.WebContextKey, Handler);
                _docViewModelToHandler[vm] = Handler;
            }
        }

        private ListController<TextController> GetWebContextFromDocViewModel(DocumentViewModel vm)
        {
            var dataDocument = vm.DocumentController.GetDataDocument(null);
            var webContextList =
                dataDocument.GetDereferencedField<ListController<TextController>>(KeyStore.WebContextKey, null);
            webContextList?.TypedData.Select(i => i.Data.CreateObject<DocumentContext>());
            return webContextList;
        }

        #endregion

        #region Selection

        protected override void OnActivated(bool isSelected)
        {
            ViewModel.SetSelected(this, isSelected);
            ViewModel.UpdateDocumentsOnSelection(isSelected);
        }

        protected override void OnLowestActivated(bool isLowestSelected)
        {
            ViewModel.SetLowestSelected(this, isLowestSelected);
        }

        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            if (ViewModel.IsInterfaceBuilder)
                return;
            OnSelected();
        }

        // TODO not sure how this should be implemented or if it would ever get called (part of the interface...)
        public void ToggleSelectAllItems()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}