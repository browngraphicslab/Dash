using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using DashShared;
using Windows.UI.Xaml.Controls;
using Windows.System;

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
        #region variables

        private DocumentViewModel _documentViewModel;
        private DocumentViewModel _displayViewModel;
        private DocumentContext _documentContext;

        public DocumentViewModel DocumentViewModel
        {
            get => _documentViewModel;
            set => SetProperty(ref _documentViewModel, value);
        }

        public DocumentViewModel DisplayViewModel
        {
            get => _displayViewModel;
            set => SetProperty(ref _displayViewModel, value);
        }

        public DocumentContext DocumentContext
        {
            get => _documentContext;
            set => SetProperty(ref _documentContext, value);
        }

        public double TitleY;
        public double PositionX;


        public enum DisplayType { Above, Below, Hidden};

        public DisplayType CurrDisplay;

        #endregion

        #region constructors
        public TimelineElementViewModel()
        {
        }

        public TimelineElementViewModel(DocumentContext documentContext, DocumentViewModel documentViewModel)
        {
            DocumentContext = documentContext;
            DocumentViewModel = documentViewModel;
        }
        #endregion
    }


    public sealed partial class CollectionTimelineView : ICollectionView
    {

        private readonly Dictionary<DocumentViewModel, FieldControllerBase.FieldUpdatedHandler> _docViewModelToHandler =
            new Dictionary<DocumentViewModel, FieldControllerBase.FieldUpdatedHandler>();
        
        private readonly List<DocumentViewModel> _trackedViewModels = new List<DocumentViewModel>();

        public TimelineMetadata Metadata { get; }
        public CollectionViewModel ViewModel { get; set; }
        public event Action MetadataUpdated;

        private readonly ObservableCollection<TimelineElementViewModel> _contextList;

        // timeline element layout
        public List<double> DisplayedXPositions { get; private set; }
        private double CurrentXPosition;
        public static double LastDisplayedPosition = 0;

        private double CurrentTopY; // how tall the element is vertically

        private double _minGap = 30; // the minimum width between timeline elements
        private double _maxGap = 300; // the maximum width between timeline elements

        public double Scale; // the current scale


        /// <summary>
        /// Constructor
        /// </summary>
        public CollectionTimelineView()
        {
            this.InitializeComponent();

            _contextList = new ObservableCollection<TimelineElementViewModel>();
            DisplayedXPositions = new List<double>();

            DataContextChanged += OnDataContextChanged;
            Unloaded += CollectionTimelineView_Unloaded;

            Metadata = new TimelineMetadata
            {
                LeftRightMargin = 160
            };

            Loaded += CollectionTimelineView_Loaded;
            PointerWheelChanged += CollectionTimelineView_PointerWheelChanged;
        }


        /// <summary>
        /// Changes timeline scale (zooms in and out)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CollectionTimelineView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control))
            {
                // If the user scrolled up, scale up by 1.07; if the user scrolled down, scale down by 1.07
                var scaleFactor = e.GetCurrentPoint(this).Properties.MouseWheelDelta > 0 ? 1.07f : 1 / 1.07f;

                if (!(scaleFactor < 1 && Scale * scaleFactor < .85))
                {
                    // Find scroll offset to maintain the scale center roughly at the position of the cursor
                    var scrollShift = (e.GetCurrentPoint(this).Position.X) * Scale * (scaleFactor - 1);

                    // Change scale of timeline and update view and scroll position
                    Scale *= scaleFactor;
                    SetTimelineFormatting();

                    var scrollTo = xScrollViewer.HorizontalOffset + scrollShift;
                    xScrollViewer.ChangeView(scrollTo, null, null, true);
                }
                e.Handled = true;
            }
        }

        private void CollectionTimelineView_Loaded(object sender, RoutedEventArgs e)
        {
            Scale = .9;
            SetTimelineFormatting();
        }

        private void CollectionTimelineView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {            
            SetTimelineFormatting();
        }


        #region Timeline formatting
        
        /// <summary>
        /// Formats the timeline based on current size
        /// </summary>
        private void SetTimelineFormatting()
        {
            // refresh this list
            DisplayedXPositions = new List<double>();

            

            // set scrollviewer to be the same dimensions as the screen
            xScrollViewer.Width = ActualWidth;
            xScrollViewer.Height = ActualHeight;

            // find new width and layout elements and timeline
            var scaledWidth = Scale * ActualWidth;
          
            LayoutTimelineElements(scaledWidth);

            Metadata.ActualHeight = ActualHeight;
            Metadata.ActualWidth = scaledWidth;
            MetadataUpdated?.Invoke();

            SetTimelineWidth(scaledWidth);
        }



        #region Timeline Element Positioning

        /// <summary>
        /// Spaces out all the timeline elements
        /// </summary>
        private void LayoutTimelineElements(double width)
        {
            if(_contextList.Count < 1)
            {
                return;
            }

            // reset position trackers and position elements
            CurrentXPosition = 0;
            CurrentTopY = 30;
            foreach (var element in _contextList)
            {
                PositionElement(element);
            }

            // rescale and reposition elements, and set display type (above or below)
            var offset = _contextList[0].PositionX - 100;
            var scaleFactor = width / _contextList[_contextList.Count - 1].PositionX;
            foreach (var element in _contextList)
            {
                element.PositionX -= offset;
                element.PositionX *= scaleFactor;
                SetDisplayType(element);
            }
        }

        private void SetDisplayType(TimelineElementViewModel element)
        {
            // display or hide element based on layout
            if (DisplayElement(element.PositionX))
            {
                LastDisplayedPosition = element.PositionX;
                element.CurrDisplay = TimelineElementViewModel.DisplayType.Below;
                DisplayedXPositions.Add(element.PositionX);

            }
            else
            {
                element.CurrDisplay = TimelineElementViewModel.DisplayType.Above;
            }
        }

        /// <summary>
        /// Positions a specific element along the timeline
        /// </summary>
        /// <param name="element"></param>
        private void PositionElement(TimelineElementViewModel element)
        {
            // laying out horizontally
            var x = CalculateXPosition(element);
            var gapDistance = x - CurrentXPosition;

            // adjust gaps if necessary
            if (gapDistance < _minGap)
            {
                x = CurrentXPosition + _minGap;
            }
            else if (gapDistance > _maxGap)
            {
                x = CurrentXPosition + _maxGap;
            }
            CurrentXPosition = x;
            element.PositionX = x;

            //stacking vertically
            element.TitleY = CurrentTopY;
            CurrentTopY += 40;
            if (CurrentTopY > 200) CurrentTopY = 30;
        }

        /// <summary>
        /// Finds the expected x position of the element from the timestamp
        /// </summary>
        /// <param name="context"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        private double CalculateXPosition(TimelineElementViewModel context)
        {
            var totalTime = Metadata.MaxTime - Metadata.MinTime;
            //Debug.Assert(totalTime != 0);
            var normOffset = (double)(context.DocumentContext.CreationTimeTicks - Metadata.MinTime) / totalTime;
            var offset = normOffset * (Metadata.ActualWidth - 2 * Metadata.LeftRightMargin) + Metadata.LeftRightMargin;
            return offset;
        }

        /// <summary>
        /// Returns whether or not to display a timeline element
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private bool DisplayElement(double x)
        {
            // return false to hide current timeline element if it is too close to another displayed element
            foreach (var pos in DisplayedXPositions)
            {
                if (Math.Abs(x - pos) < 200)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

    
        /// <summary>
        /// sets the width of the timeline shape
        /// </summary>
        /// <param name="width"></param>
        private void SetTimelineWidth(double width)
        {
            xHorizontalLine.Width = width;
            Canvas.SetLeft(xVerticalLineRight, width + 80);
            xScrollViewCanvas.Width = width;
        }


        private void UpdateMetadataMinAndMax()
        {
            // if context list is empty we can't update anything
            if (!_contextList.Any()) return;
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

        #endregion

        #region ContextManagement

        private void CollectionTimelineView_Unloaded(object sender, RoutedEventArgs e)
        {
            RemoveViewModelEvents(ViewModel);
            CurrentXPosition = 0;
            Unloaded -= CollectionTimelineView_Unloaded;
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            RemoveViewModelEvents(ViewModel);
            ViewModel = DataContext as CollectionViewModel;;
            // make the new ViewModel listen to events
            AddViewModelEvents(ViewModel);
            Initialize(ViewModel);
        }

        private void Initialize(CollectionViewModel viewModel)
        {
            if (viewModel != null)
            {
                foreach (var dvm in viewModel.DocumentViewModels)
                {
                    var docContexts = GetWebContextFromDocViewModel(dvm)?.TypedData
                        .Select(i => i.Data.CreateObject<DocumentContext>());
                    if (docContexts != null)
                        foreach (var dc in docContexts)
                            _contextList.Add(new TimelineElementViewModel(dc, dvm));
                    else
                    {
                        // if there is no web context stored for a document, create a context from the ModifiedTimestamp instead
                        var dateObject = (DateTime)(dvm.DataDocument.GetDereferencedField(KeyStore.ModifiedTimestampKey, null).GetValue(new Context()));
                        var documentTicks = dateObject.Ticks;
                        _contextList.Add(new TimelineElementViewModel(new DocumentContext() { CreationTimeTicks = documentTicks}, dvm));
                    }
                }
                UpdateMetadataMinAndMax();
            }
        }

        private void AddViewModelEvents(CollectionViewModel viewModel)
        {
            if (viewModel != null)
            {
                viewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;
                viewModel.DocumentViewModels.CollectionChanged += DocumentViewModels_CollectionChanged;
            }
        }

        private void RemoveViewModelEvents(CollectionViewModel viewModel)
        {
            if (viewModel != null)
            {
                viewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;
            }
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
                vm.DataDocument.RemoveFieldUpdatedListener(KeyStore.WebContextKey, _docViewModelToHandler[vm]);
            }
        }

        private void TrackViewModels(IEnumerable<DocumentViewModel> viewModels)
        {
            foreach (var vm in viewModels)
            {
                _trackedViewModels.Add(vm);
                var dataDocument = vm.DataDocument;

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
            var webContextList =
                vm.DataDocument.GetDereferencedField<ListController<TextController>>(KeyStore.WebContextKey, null);
            webContextList?.TypedData.Select(i => i.Data.CreateObject<DocumentContext>());
            return webContextList;
        }

        #endregion

        #region Selection
        
        
        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        #endregion
    }
}