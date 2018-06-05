using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Annotations;
using Syncfusion.UI.Xaml.Controls;

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

        /// <summary>
        ///     A reference to the context preview
        /// </summary>
        private UIElement _contextPreview;

        private double _ellipseSize = 18;
        private readonly double _offsetX = 200;
        private readonly double _offsetY = 492;

        private long _time;

        /// <summary>
        ///     Constructor
        /// </summary>
        public TimelineElement()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += TimelineElement_Loaded;
        }

        public TimelineElementViewModel ViewModel { get; private set; }
        public CollectionTimelineView ParentTimeline { get; private set; }

        #region loading

        private void TimelineElement_Loaded(object sender, RoutedEventArgs e)
        {
            ParentTimeline = this.GetFirstAncestorOfType<CollectionTimelineView>();
            ParentTimeline.MetadataUpdated += UpdateTimelinePosition;

            // get the modified date value and display it
            var date = ViewModel.DocumentViewModel.DocumentController.GetDataDocument()
                .GetField(KeyStore.ModifiedTimestampKey).GetValue(new Context()).ToDateTime();
            xTimeBlock.Text = date.ToShortDateString();
            xDateBlock.Text = date.ToShortTimeString();

            UpdateTimelinePosition();
            LoadContext();
        }

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

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var vm = DataContext as TimelineElementViewModel;
            Debug.Assert(vm != null);
            ViewModel = vm;

            DocumentController thumbnailImageViewDoc = null;
            var richText = vm.DocumentViewModel.DataDocument
                .GetDereferencedField<RichTextController>(KeyStore.DataKey, null)
                ?.Data; //NoteDocuments.RichTextNote.RTFieldKey
            var docText =
                vm.DocumentViewModel.DataDocument.GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null)
                    ?.Data ?? richText?.ToString() ?? null;

            if (docText != null)
                thumbnailImageViewDoc = new PostitNote(docText).Document;
            else
                thumbnailImageViewDoc =
                    (vm.DocumentViewModel.DocumentController.GetDereferencedField(KeyStore.ThumbnailFieldKey, null) as
                         DocumentController ?? vm.DocumentViewModel.DocumentController).GetViewCopy();
            thumbnailImageViewDoc.SetLayoutDimensions(300, 500);
            ViewModel.DisplayViewModel = new DocumentViewModel(thumbnailImageViewDoc)
            {
                Width = 220,
                BackgroundBrush = new SolidColorBrush(Colors.Transparent)
            };
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
            if (ViewModel.CurrDisplay == TimelineElementViewModel.DisplayType.Above)
                ViewModel.CurrDisplay = TimelineElementViewModel.DisplayType.Below;
            else
                ViewModel.CurrDisplay = TimelineElementViewModel.DisplayType.Above;

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