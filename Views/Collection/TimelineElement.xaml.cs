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

        private static double _webViewActualHeight = 160;
        private static double _webViewActualWidth =  250;
        private static double _webViewScaleFactor = .3;

        private readonly ContextWebView _localContext = new ContextWebView(null, _webViewScaleFactor, _webViewActualWidth/_webViewScaleFactor, _webViewActualHeight/_webViewScaleFactor);

        private bool _localContextVisible;
        private double _ellipseSize = 18;

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
            UpdateTimelinePosition();

            LocalContextVisible = true;
            ShowLocalContext(true);
        }

        private void UpdateTimelinePosition()
        {
            var x = CalculateXPosition(ViewModel, ParentTimeline.Metadata);
            var y = CalculateYPosition(ViewModel, ParentTimeline.Metadata);
            RenderTransform = new TranslateTransform()
            {
                X = x,
                Y = y
            };
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
        }

        public void ShowLocalContext(bool showContext)
        {
            if (!showContext && _localContext.View != null)
            {
                // TODO hide the context
                xWebHolder.Children.Remove(_localContext.View);
                _localContext.View = null;
                GC.Collect();

                xLowerLine.Visibility = Visibility.Collapsed;
            }

            if (showContext)
            {
                if (ViewModel == null) return;
                var source = new Uri(ViewModel.DocumentContext.Url);

                if (_localContext.View == null)
                {
                    _localContext.View = new WebAndPdfView(source)
                    {
                        Width = _localContext.Width,
                        Height = _localContext.Height,
                        RenderTransform = new ScaleTransform { ScaleX = _localContext.ScaleFactor, ScaleY = _localContext.ScaleFactor }
                    };
                    xWebHolder.Children.Add(_localContext.View);
                    Canvas.SetLeft(_localContext.View, -_localContext.ActualWidth / 2 - EllipseSize / 2);
                    if (xDocumentPreview != null) Canvas.SetTop(_localContext.View, xDocumentPreview.ActualHeight);
                }
                else if (!_localContext.View.Source.Equals(source))
                {
                    _localContext.View.Source = source;
                }

                xLowerLine.Visibility = Visibility.Visible;
            }
        }

        private void TimelineElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //LocalContextVisible = !LocalContextVisible;
            
            _localContextVisible = !_localContextVisible;
            ShowLocalContext(_localContextVisible);
        }

        private void DocumentPreviewSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(_localContext.View != null)
            {
                Canvas.SetTop(_localContext.View, xDocumentPreview.ActualHeight);

            }
            Canvas.SetLeft(xDocumentPreview, -xDocumentPreview.ActualWidth / 2 - EllipseSize / 2);

        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
