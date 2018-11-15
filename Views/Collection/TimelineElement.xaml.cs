using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Annotations;
using Dash.Controllers;

namespace Dash
{
    /// <summary>
    ///     The TimelineElement component displays a document in the CollectionTimelineView.
    /// </summary>
    public sealed partial class TimelineElement : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        ///     The width and height of the context preview
        /// </summary>
        private const double ContextPreviewActualHeight = 250;

        private const double ContextPreviewActualWidth = 200;
        private readonly double _offsetX = 200;
        private readonly double _offsetY = 492;

        /// <summary>
        ///     A reference to the context preview
        /// </summary>
        private UIElement _contextPreview;

        private double _ellipseSize = 18;

        private long _time;

        /// <summary>
        ///     Constructor
        /// </summary>
        public TimelineElement()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += TimelineElement_Unloaded;
            Loaded += TimelineElement_Loaded;
        }

        private TimelineElementViewModel _oldViewModel;
        public TimelineElementViewModel ViewModel { get => DataContext as TimelineElementViewModel; private set => DataContext = value; }
        public CollectionTimelineView ParentTimeline { get; private set; }

        // remove field listener when unloaded
        private void TimelineElement_Unloaded(object sender, RoutedEventArgs e)
        {
            ParentTimeline.MetadataUpdated -= UpdateTimelinePosition;
        }

        #region loading

        private void TimelineElement_Loaded(object sender, RoutedEventArgs e)
        {
            ParentTimeline = this.GetFirstAncestorOfType<CollectionTimelineView>();
            ParentTimeline.MetadataUpdated += UpdateTimelinePosition;

            // get the sort key value and display it
            var date = ViewModel.DocumentViewModel.DocumentController.GetDataDocument()?
                .GetDereferencedField<DateTimeController>(ViewModel.SortKey, null)?.Data ?? DateTime.Now;
            xTimeBlock.Text = date.ToShortDateString();
            xDateBlock.Text = date.ToShortTimeString();

            // initializes timeline element, position, and display
            UpdateTimelinePosition();
            LoadContext();
        }

        // loads the display used for the timeline element
        private void LoadContext()
        {
            if (_contextPreview == null && ViewModel.DocumentContext.GetImage() != null)
            {
                _contextPreview = new ContextPreview(ViewModel.DocumentContext)
                {
                    Width = ContextPreviewActualWidth,
                    Height = ContextPreviewActualHeight
                };
                xDocHolder.Children.Add(_contextPreview);
            }
            else
            {
                xLowerLine2.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        ///     updates the text that is displayed by the timeline. called when either the timeline element's data context has
        ///     changed or when the timeline element viewmodel's data context has changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (ViewModel == _oldViewModel)
            {
                return;
            }

            _oldViewModel = ViewModel;
            if (ViewModel != null)
            {
                xTitleTextBlock.AddFieldBinding(TextBlock.TextProperty, new FieldBinding<TextController>
                {
                    Document = ViewModel.DocumentViewModel.DocumentController,
                    Key = KeyStore.TitleKey,
                    Mode = BindingMode.OneWay
                });
            }
        }

        #endregion

        #region Update Elements

        private void UpdateTimelinePosition()
        {
            if (!(DataContext is TimelineElementViewModel vm)) return;

            // set vertical stacking height
            xTopY.Height = new GridLength(vm.TitleY);

            // find x and y position
            var x = vm.PositionX - _offsetX;
            var y = _offsetY;

            RenderTransform = new TranslateTransform
            {
                X = x,
                Y = y
            };

            UpdateView();
        }


        private void TimelineElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // toggle view and update
            ViewModel.CurrDisplay = ViewModel.CurrDisplay == TimelineElementViewModel.DisplayType.Above ? TimelineElementViewModel.DisplayType.Below : TimelineElementViewModel.DisplayType.Above;

            UpdateView();
        }


        private void UpdateView()
        {
            if (ViewModel.CurrDisplay == TimelineElementViewModel.DisplayType.Below)
            {
                xTopViewGrid.Visibility = Visibility.Collapsed;
                xBottomViewGrid.Visibility = Visibility.Visible;
                xDateTimeStackPanel.Visibility = Visibility.Visible;
            }
            else if (ViewModel.CurrDisplay == TimelineElementViewModel.DisplayType.Above)
            {
                xTopViewGrid.Visibility = Visibility.Visible;
                xBottomViewGrid.Visibility = Visibility.Collapsed;
                xDateTimeStackPanel.Visibility = Visibility.Collapsed;
            }
            else if (ViewModel.CurrDisplay == TimelineElementViewModel.DisplayType.Hidden)
            {
                xTopViewGrid.Visibility = Visibility.Collapsed;
                xBottomViewGrid.Visibility = Visibility.Collapsed;
            }
        }

        #endregion


        #region property changed

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
