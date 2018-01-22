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

            LocalContextVisible = true;
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
        }

        public void DisplayBelow(bool showContext)
        {
            if (!showContext && _localContext.View != null)
            {
                // TODO hide the context
                xDocHolder.Children.Remove(_localContext.View);
                _localContext.View = null;
                GC.Collect();

                xLowerLine.Visibility = Visibility.Collapsed;
                xUpperLine.Visibility = Visibility.Visible;
                Thickness margin = xWebHolderTop.Margin;
                margin.Top = -40;
                xWebHolderTop.Margin = margin;
                xDocumentPreview.Width = 80;
                xDocumentPreview.Height = 30;
                Thickness margin2 = xDocGrid.Margin;
                margin.Left = -40;
                xDocGrid.Margin = margin;
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
                    xDocHolder.Children.Add(_localContext.View);
                    Canvas.SetLeft(_localContext.View, -_localContext.ActualWidth / 2 - EllipseSize / 2);
                    if (xDocumentPreview != null) Canvas.SetTop(_localContext.View, xDocumentPreview.ActualHeight);
                }
                else if (_localContext.View.Source != null && !_localContext.View.Source.Equals(source))
                {
                    _localContext.View.Source = source;
                }

                xLowerLine.Visibility = Visibility.Visible;
                xUpperLine.Visibility = Visibility.Collapsed;
                Thickness margin = xWebHolderTop.Margin;
                margin.Top = 40;
                xWebHolderTop.Margin = margin;
                xDocumentPreview.Width = 250;
                xDocumentPreview.Height = 160;
                Thickness margin2 = xDocGrid.Margin;
                margin.Left = -120;
                xDocGrid.Margin = margin;
            }
        }

        private void TimelineElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //LocalContextVisible = !LocalContextVisible;
            _displayState = (_displayState + 1) % 3;

            UpdateView();
            
        }

        private void UpdateView()
        {
            var displayBelow = false;
            if (_displayState == 0)
            {
                displayBelow = true;
            }

            bool displayAll = true;
            if (_displayState == 2)
            {
                displayAll = false;
            }
            DisplayAll(displayAll);

            //_localContextVisible = !_localContextVisible;
            DisplayBelow(displayBelow);
        }

        private void DisplayAll(bool display)
        {
            if(display)
            {
                xDisplay.Visibility = Visibility.Visible;
                Thickness margin = xEllipse.Margin;
                margin.Top = -200;
                xEllipse.Margin = margin;
            } else
            {
                xDisplay.Visibility = Visibility.Collapsed;
                Thickness margin = xEllipse.Margin;
                margin.Top = 0;
                xEllipse.Margin = margin;
            }
        }

        private void DocumentPreviewSizeChanged(object sender, SizeChangedEventArgs e)
        {

            try
            {
                if(_localContext.View != null)
                {
                    Canvas.SetTop(_localContext.View, xDocumentPreview.ActualHeight);

                }
                Canvas.SetLeft(xDocumentPreview, -xDocumentPreview.ActualWidth / 2 - EllipseSize / 2);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
                throw;
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
