using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Dash.Annotations;
using Windows.UI;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TimelineElement : UserControl, INotifyPropertyChanged
    {
        public TimelineElementViewModel ViewModel { get; private set; }
        public CollectionTimelineView ParentTimeline { get; private set; }

        /// <summary>
        /// The width and height of the context preview
        /// </summary>
        private const double ContextPreviewActualHeight = 250;
        private const double ContextPreviewActualWidth = 200;
        /// <summary>
        /// A reference to the context preview
        /// </summary>
        private UIElement _contextPreview;


        private bool _localContextVisible;
        private double _ellipseSize = 18;

        private int _displayState = 0;

        private bool LocalContextVisible
        {
            get => _localContextVisible;
            set
            {
                if (_localContextVisible != value)
                {
                    _localContextVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        private double EllipseSize
        {
            get => _ellipseSize;
            set
            {
                if (!(Math.Abs(_ellipseSize - value) > .0001)) return;
                _ellipseSize = value;
                OnPropertyChanged();
            }
        }

        public TimelineElement()
        {
            this.InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += TimelineElement_Loaded;
        }

        private void TimelineElement_Loaded(object sender, RoutedEventArgs e)
        {
            ParentTimeline = this.GetFirstAncestorOfType<CollectionTimelineView>();
            ParentTimeline.MetadataUpdated += UpdateTimelinePosition;

            //DisplayBelow(true);

            UpdateTimelinePosition();

            LoadContext();

            LocalContextVisible = true;
        }

        private void LoadContext()
        {
            if (_contextPreview == null)
            {
                _contextPreview = new ContextPreview(ViewModel.DocumentContext)
                {
                    Width = ContextPreviewActualWidth,
                    Height = ContextPreviewActualHeight,
                };
                xDocHolder.Children.Add(_contextPreview);
                //Canvas.SetLeft(_contextPreview, -ContextPreviewActualWidth / 2 - EllipseSize / 2);
                //if (xDocumentPreview != null) Canvas.SetTop(_contextPreview, xDocumentPreview.ActualHeight);
            }
        }

        public static double LastDisplayedPosition= 0;

        private double _minGap = 30;
        private double _maxGap = 300;

        private void UpdateTimelinePosition()
        {
            var x = CalculateXPosition(ViewModel, ParentTimeline.Metadata);
            var y = CalculateYPosition(ViewModel, ParentTimeline.Metadata);

            var gapDistance = x - ParentTimeline.CurrentXPosition;
            if (gapDistance < _minGap)
            {
                x = ParentTimeline.CurrentXPosition + _minGap;
            }
            else if (gapDistance > _maxGap)
            {
                x = ParentTimeline.CurrentXPosition + _maxGap;
            }


            RenderTransform = new TranslateTransform()
            {
                X = x,
                Y = y
            };

            ParentTimeline.CurrentXPosition = x;

            if(DisplayElement(x))
            {
                LastDisplayedPosition = x;
                _displayState = 0;
                ParentTimeline.DisplayedXPositions.Add(x);
            } else
            {
                _displayState = 0;
                UpdateView();
                _displayState = 1;
            }
            UpdateView();

        }

        private bool DisplayElement(double x)
        {
            foreach(var pos in ParentTimeline.DisplayedXPositions)
            {
                if(Math.Abs(x - pos) < 200)
                {
                    return false;
                }
            }
            return true;
        }

        private double CalculateYPosition(TimelineElementViewModel context, TimelineMetadata metadata)
        {
            return metadata.ActualHeight / 2 - EllipseSize / 2;
        }

        private double CalculateXPosition(TimelineElementViewModel context, TimelineMetadata metadata)
        {
            var totalTime = metadata.MaxTime - metadata.MinTime;
            Debug.Assert(totalTime != 0);
            var normOffset = (double)(context.DocumentContext.CreationTimeTicks - metadata.MinTime) / totalTime;
            var offset = normOffset * (metadata.ActualWidth - 2 * metadata.LeftRightMargin) + metadata.LeftRightMargin - EllipseSize / 2;
            return offset;
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var vm = DataContext as TimelineElementViewModel;
            Debug.Assert(vm != null);
            ViewModel = vm;



            DocumentController thumbnailImageViewDoc = null;
            var richText = vm.DocumentViewModel.DataDocument.GetDereferencedField<RichTextController>(NoteDocuments.RichTextNote.RTFieldKey, null)?.Data;
            var docText = vm.DocumentViewModel.DataDocument.GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null)?.Data ?? richText?.ReadableString ?? null;

            if (docText != null)
            {
                thumbnailImageViewDoc = new NoteDocuments.PostitNote(docText).Document;
            }
            else
            {
                thumbnailImageViewDoc = (vm.DocumentViewModel.DocumentController.GetDereferencedField(KeyStore.ThumbnailFieldKey, null) as DocumentController ?? vm.DocumentViewModel.DocumentController).GetViewCopy();
            }
            thumbnailImageViewDoc.SetLayoutDimensions(300, 500);
            ViewModel.DisplayViewModel = new DocumentViewModel(thumbnailImageViewDoc) { Undecorated = true, BackgroundBrush = new SolidColorBrush(Colors.Transparent) };
        
        //if (docText != null)
        //{
        //    thumbnailImageViewDoc = new NoteDocuments.PostitNote(docText.Substring(0, Math.Min(100, docText.Length))).Document;
        //}
        //else
        //{
        //    thumbnailImageViewDoc = (vm.DocumentViewModel.DocumentController.GetDereferencedField(KeyStore.ThumbnailFieldKey, null) as DocumentController ?? vm.DocumentViewModel.DocumentController).GetViewCopy();
        //}


        //thumbnailImageViewDoc.SetLayoutDimensions(xThumbs.ActualWidth, double.NaN);

    }



        private void TimelineElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //LocalContextVisible = !LocalContextVisible;
            _displayState = (_displayState + 1) % 3;

            UpdateView();
            
        }

        private void UpdateView()
        {
            //var displayBelow = false;
            if (_displayState == 0)
            {
                xTopViewGrid.Visibility = Visibility.Collapsed;
                xBottomViewGrid.Visibility = Visibility.Visible;
            }
            else if (_displayState == 2)
            {
                xTopViewGrid.Visibility = Visibility.Collapsed;
                xBottomViewGrid.Visibility = Visibility.Collapsed;
            }
            else if (_displayState == 1)
            {
                xTopViewGrid.Visibility = Visibility.Visible;
                xBottomViewGrid.Visibility = Visibility.Collapsed;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
