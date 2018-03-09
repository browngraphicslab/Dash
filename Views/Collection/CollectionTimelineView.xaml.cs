﻿using System;
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
        private readonly ObservableCollection<TimelineElementViewModel> _contextList;

        private readonly Dictionary<DocumentViewModel, FieldControllerBase.FieldUpdatedHandler> _docViewModelToHandler =
            new Dictionary<DocumentViewModel, FieldControllerBase.FieldUpdatedHandler>();
        
        private readonly List<DocumentViewModel> _trackedViewModels = new List<DocumentViewModel>();

        private double CurrentXPosition;
        private double CurrentTopY;

        public double Scale;


        public List<double> DisplayedXPositions { get; private set; }

        public TimelineMetadata Metadata { get; }
        public BaseCollectionViewModel ViewModel { get; private set; }
        public event Action MetadataUpdated;

        public static double LastDisplayedPosition = 0;

        private double _minGap = 30;
        private double _maxGap = 300;

        private double _scrollScaleAmount = 1;


        /// <summary>
        /// Constructor
        /// </summary>

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

            PointerWheelChanged += CollectionTimelineView_PointerWheelChanged;
        }

       

        private void CollectionTimelineView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control))
            {
                


                var scaleFactor = e.GetCurrentPoint(this).Properties.MouseWheelDelta > 0 ? 1.07f : 1 / 1.07f;
                var scale = Scale;

                if (!(scaleFactor < 1 && Scale * scaleFactor < .85))
                {


                    Scale *= scaleFactor;

                    var scrollShift = (80 + e.GetCurrentPoint(this).Position.X) * scale * (scaleFactor - 1);


                    SetTimelineFormatting();

                    var scrollTo = xScrollViewer.HorizontalOffset + 20;

                    xScrollViewer.ChangeView(scrollTo, null, null, false);

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

        /// <summary>
        /// Formats the timeline based on current size
        /// </summary>
        private void SetTimelineFormatting()
        {
            DisplayedXPositions = new List<double>();


            var scaledWidth = Scale * ActualWidth;

            xScrollViewer.Width = ActualWidth;
            xScrollViewer.Height = ActualHeight;


            var adjustedWidth = scaledWidth - 160;
            var minWidth = 300;
            if (adjustedWidth < minWidth)
            {
                adjustedWidth = minWidth;
            }


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

            CurrentXPosition = 0;
            CurrentTopY = 30;
            foreach (var element in _contextList)
            {
                PositionElement(element);
            }

            var offset = _contextList[0].PositionX - 100;

            var scaleFactor = width / _contextList[_contextList.Count - 1].PositionX;
            foreach (var element in _contextList)
            {
                element.PositionX -= offset;
                element.PositionX *= scaleFactor;
            }
        }

       

        /// <summary>
        /// Positions a specific element along the timeline
        /// </summary>
        /// <param name="element"></param>
        private void PositionElement(TimelineElementViewModel element)
        {
            var x = CalculateXPosition(element, Metadata); //ehh fix
            var gapDistance = x - CurrentXPosition;

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


            if (DisplayElement(x))
            {
                LastDisplayedPosition = x;
                element.CurrDisplay = TimelineElementViewModel.DisplayType.Below;
                DisplayedXPositions.Add(x);
                
            }
            else
            {
                element.CurrDisplay = TimelineElementViewModel.DisplayType.Above;
            }

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
        private double CalculateXPosition(TimelineElementViewModel context, TimelineMetadata metadata)
        {
            var totalTime = metadata.MaxTime - metadata.MinTime;
            Debug.Assert(totalTime != 0);
            var normOffset = (double)(context.DocumentContext.CreationTimeTicks - metadata.MinTime) / totalTime;
            var offset = normOffset * (metadata.ActualWidth - 2 * metadata.LeftRightMargin) + metadata.LeftRightMargin;
            return offset;
        }

        /// <summary>
        /// Returns whether or not to display a timeline element
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private bool DisplayElement(double x)
        {
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

    

        private void SetTimelineWidth(double width)
        {
            xHorizontalLine.Width = width;
            Canvas.SetLeft(xVerticalLineRight, width + 80);
            xScrollViewCanvas.Width = width;
        }


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