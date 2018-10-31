using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Dash.Controllers;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

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
        private DocumentContext _documentContext;
        private KeyController _sortKey;

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

        public KeyController SortKey
        {
            get => _sortKey;
            set => SetProperty(ref _sortKey, value);
        }

        public double TitleY;
        public double PositionX;

        public enum DisplayType
        {
            Above,
            Below,
            Hidden
        }

        public DisplayType CurrDisplay;

        #endregion

        #region constructors

        public TimelineElementViewModel()
        {
        }

        public TimelineElementViewModel(DocumentContext documentContext, DocumentViewModel documentViewModel,
            KeyController sortKey)
        {
            DocumentContext = documentContext;
            DocumentViewModel = documentViewModel;
            SortKey = sortKey;
        }

        #endregion
    }


    public sealed partial class CollectionTimelineView : ICollectionView
    {
        public UserControl UserControl => this;
        private readonly ObservableCollection<TimelineElementViewModel> _contextList;
        private readonly double _maxGap = 300; // the maximum width between timeline elements
        private readonly double _minGap = 30; // the minimum width between timeline elements
        private double CurrentTopY; // how tall the element is vertically
        private double CurrentXPosition;
        public double Scale = 0.9; // the current scale


        /// <summary>
        ///     Constructor
        /// </summary>
        public CollectionTimelineView()
        {
            InitializeComponent();

            _contextList = new ObservableCollection<TimelineElementViewModel>();
            DisplayedXPositions = new List<double>();

            DataContextChanged += OnDataContextChanged;
            Unloaded += CollectionTimelineView_Unloaded;

            Metadata = new TimelineMetadata
            {
                LeftRightMargin = 160
            };

            //Todo: make sortkey work for other keys
            SortKey = KeyStore.DateModifiedKey;

            Loaded += CollectionTimelineView_Loaded;
            PointerWheelChanged += CollectionTimelineView_PointerWheelChanged;
        }
        public void SetupContextMenu(MenuFlyout contextMenu)
        {

        }

        public TimelineMetadata Metadata { get; }
        public KeyController SortKey { get; set; }

        // timeline element layout
        public List<double> DisplayedXPositions { get; private set; }

        private CollectionViewModel _oldViewModel;
        public CollectionViewModel ViewModel { get => DataContext as CollectionViewModel; set => DataContext = value; }
        public event Action MetadataUpdated;


        /// <summary>
        ///     Changes timeline scale (zooms in and out)
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
                    var scrollShift = e.GetCurrentPoint(this).Position.X * Scale * (scaleFactor - 1);

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
            SetTimelineFormatting();
        }

        private void CollectionTimelineView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetTimelineFormatting();
        }
        public void OnDocumentSelected(bool selected)
        {
        }

        #region Selection

        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        public void SetDropIndicationFill(Brush fill) { }


        #endregion


        #region Timeline formatting

        /// <summary>
        ///     Formats the timeline based on current size
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
        ///     Spaces out all the timeline elements
        /// </summary>
        public void LayoutTimelineElements(double width)
        {
            if (_contextList.Count < 1) return;

            // reset position trackers and position elements
            CurrentXPosition = 0;
            CurrentTopY = 30;

            // gets the value of the sort key (currently modified time) and turns it into ticks to order increasingly by
            // PositionElement only works when elements are passed in in an increasing order
            var sortedElements = _contextList.OrderBy(vm =>
                vm.DocumentViewModel.DocumentController.GetDataDocument().GetDereferencedField<DateTimeController>(SortKey,null).Data.Ticks).ToList();
            foreach (var element in sortedElements)
            {
                PositionElement(element);
            }

            // rescale and reposition elements, and set display type (above or below)
            var offset = sortedElements[0].PositionX - 100;
            var scaleFactor = width / sortedElements[sortedElements.Count - 1].PositionX;
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
            if (ShouldDisplayElement(element.PositionX))
            {
                element.CurrDisplay = TimelineElementViewModel.DisplayType.Below;
                DisplayedXPositions.Add(element.PositionX);
            }
            else
            {
                element.CurrDisplay = TimelineElementViewModel.DisplayType.Above;
            }
        }

        /// <summary>
        ///     Positions a specific element along the timeline
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

            // stacking vertically
            element.TitleY = CurrentTopY;
            CurrentTopY += 40;
            if (CurrentTopY > 200) CurrentTopY = 30;
        }

        /// <summary>
        ///     Finds the expected x position of the element from the modified time of the element
        /// </summary>
        /// <param name="tevm"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        private double CalculateXPosition(TimelineElementViewModel tevm)
        {
            var totalTime = Metadata.MaxTime - Metadata.MinTime;
            // if the max and min time are the same, use arbitrary small constant (10)
            if (totalTime == 0) totalTime = 10;

            var normOffset =
                (double)(tevm.DocumentViewModel.DocumentController.GetDataDocument().GetDereferencedField<DateTimeController>(SortKey, null).Data.Ticks - Metadata.MinTime) / totalTime;
            var offset = normOffset * (Metadata.ActualWidth - 2 * Metadata.LeftRightMargin) + Metadata.LeftRightMargin;
            return offset;
        }

        /// <summary>
        ///     Returns whether or not to display a timeline element
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private bool ShouldDisplayElement(double x)
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
        ///     sets the width of the timeline shape
        /// </summary>
        /// <param name="width"></param>
        private void SetTimelineWidth(double width)
        {
            xHorizontalLine.Width = width;
            Canvas.SetLeft(xVerticalLineRight, width + 80);
            xScrollViewCanvas.Width = width;
        }

        /// <summary>
        ///     updates the start and end points of the timeline relative to other points
        /// </summary>
        private void UpdateTimeline()
        {
            // if context list is empty we can't update anything
            if (!_contextList.Any()) return;
            try
            {
                // lambda f(x) that retrieves value of key from viewmodel
                Func<TimelineElementViewModel, long> getValues = vm =>
                    vm.DocumentViewModel.DocumentController.GetDataDocument().GetDereferencedField<DateTimeController>(SortKey, null).Data.Ticks;
                // find the earliest and latest modified times in document
                Metadata.MinTime = _contextList.Min(getValues);
                Metadata.MaxTime = _contextList.Max(getValues);

                MetadataUpdated?.Invoke();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            SetTimelineFormatting();
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
            if (_oldViewModel == ViewModel) return;
            _oldViewModel = ViewModel;

            RemoveViewModelEvents(ViewModel);
            ViewModel = DataContext as CollectionViewModel;
            // make the new ViewModel listen to events
            AddViewModelEvents(ViewModel);
            Initialize(ViewModel);
        }

        private void Initialize(CollectionViewModel viewModel)
        {
            if (viewModel != null)
            {
                _contextList.Clear();
                foreach (var dvm in viewModel.DocumentViewModels)
                {
                    // add all document viewmodels as timeline element view models
                    _contextList.Add(new TimelineElementViewModel(new DocumentContext(), dvm, SortKey));
                    // add an event listener for the document to listen to when sortkey changes
                    //TODO This event handler is probably never removed
                    dvm.DataDocument.AddFieldUpdatedListener(SortKey, SortKeyModified);
                }

                UpdateTimeline();
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
                viewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;
        }

        private void DocumentViewModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                // Todo
                case NotifyCollectionChangedAction.Add:
                    AddViewModels(e.NewItems.Cast<DocumentViewModel>());
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveViewModels(e.OldItems.Cast<DocumentViewModel>());
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Remove a list of viewmodels from the document and re-organize the timeline
        /// </summary>
        /// <param name="removedViewModels"></param>
        private void RemoveViewModels(IEnumerable<DocumentViewModel> removedViewModels)
        {
            foreach (var vm in removedViewModels)
            {
                // use document viewmodels to find the right timeline element viewmodel to remove
                _contextList.Remove(_contextList.First(i => i.DocumentViewModel.Equals(vm)));
                vm.DataDocument.RemoveFieldUpdatedListener(SortKey, SortKeyModified);
            }

            UpdateTimeline();
        }

        /// <summary>
        ///     Create a list of viewmodels from the document and re-organize the timeline
        /// </summary>
        /// <param name="newViewModels"></param>
        private void AddViewModels(IEnumerable<DocumentViewModel> newViewModels)
        {
            foreach (var vm in newViewModels)
            {
                _contextList.Add(new TimelineElementViewModel(new DocumentContext(), vm, SortKey));
                vm.DataDocument.AddFieldUpdatedListener(SortKey, SortKeyModified);
            }

            UpdateTimeline();
        }

        // Sort the modified key
        private void SortKeyModified(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            UpdateTimeline();
        }
        #endregion
    }
}
