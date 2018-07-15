using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
using Point = Windows.Foundation.Point;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class PresentationView : UserControl
    {
        public PresentationViewModel ViewModel => DataContext as PresentationViewModel;
        public bool IsPresentationPlaying = false;
        private PresentationViewTextBox _textbox;
        private bool _giveTextBoxFocusUponFlyoutClosing = false;

        public PresentationView()
        {
            this.InitializeComponent();
            DataContext = new PresentationViewModel();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = PinnedNodesListView.SelectedIndex;

            // only move back if there is a step to go back to
            if (selectedIndex != 0)
            {
                PinnedNodesListView.SelectedItem = PinnedNodesListView.Items[selectedIndex - 1];
                NavigateToDocument((DocumentViewModel) PinnedNodesListView.SelectedItem);
            }
        }

        private void PlayStopButton_Click(object sender, RoutedEventArgs e)
        {
            // can't play/stop if there's nothing in it
            if (PinnedNodesListView.Items.Count != 0)
            {
                if (IsPresentationPlaying)
                {
                    // if it's currently playing, then it means the user just clicked the stop button. Reset.
                    IsPresentationPlaying = false;
                    PlayStopButton.Icon = new SymbolIcon(Symbol.Play);
                    PlayStopButton.Label = "Play";
                    PinnedNodesListView.SelectionMode = ListViewSelectionMode.None;
                }
                else
                {
                    // zoom to first item in the listview
                    PinnedNodesListView.SelectionMode = ListViewSelectionMode.Single;
                    PinnedNodesListView.SelectedItem = PinnedNodesListView.Items[0];
                    NavigateToDocument((DocumentViewModel)PinnedNodesListView.SelectedItem);

                    IsPresentationPlaying = true;
                    PlayStopButton.Icon = new SymbolIcon(Symbol.Stop);
                    PlayStopButton.Label = "Stop";
                }
            }

            // back/next/reset buttons change appearance depending on state of presentation
            ResetBackNextButtons();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = PinnedNodesListView.SelectedIndex;

            // can only move forward if there's a node to move forward to
            if (selectedIndex != PinnedNodesListView.Items.Count - 1)
            {
                PinnedNodesListView.SelectedItem = PinnedNodesListView.Items[selectedIndex + 1];
                NavigateToDocument((DocumentViewModel) PinnedNodesListView.SelectedItem);
            }
        }

        // remove from viewmodel
        private void DeletePin(object sender, RoutedEventArgs e)
        {
            ViewModel.RemovePinFromPinnedNodesCollection((sender as Button).Tag as DocumentViewModel);
        }

        // if we click a node, we should navigate to it immediately. Note that IsItemClickable is always enabled.
        private void PinnedNode_Click(object sender, ItemClickEventArgs e)
        {
            DocumentViewModel viewModel = (DocumentViewModel) e.ClickedItem;
            NavigateToDocument(viewModel);
        }

        // helper method for moving the mainpage screen
        private void NavigateToDocument(DocumentViewModel viewModel)
        {
            MainPage.Instance.NavigateToDocumentInWorkspaceAnimated(viewModel.DocumentController);
        }

        // these buttons are only enabled when the presentation is playing
        private void ResetBackNextButtons()
        {
            BackButton.IsEnabled = IsPresentationPlaying;
            NextButton.IsEnabled = IsPresentationPlaying;
            ResetButton.IsEnabled = IsPresentationPlaying;
            BackButton.Opacity = IsPresentationPlaying ? 1 : 0.3;
            NextButton.Opacity = IsPresentationPlaying ? 1 : 0.3;
            ResetButton.Opacity = IsPresentationPlaying ? 1 : 0.3;
        }

        // if user strays in middle of presentation, hitting this will bring them back to the selected node
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToDocument((DocumentViewModel) PinnedNodesListView.SelectedItem);
        }

        private void PinnedNodesListView_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ListView listView = (ListView) sender;
            PinnedNodeFlyout.ShowAt(listView, e.GetPosition(listView));
            var source = (FrameworkElement) e.OriginalSource;
            _textbox = source.GetFirstDescendantOfType<PresentationViewTextBox>() ?? source.GetFirstAncestorOfType<PresentationViewTextBox>();
        }

        private void Edit_OnClick(object sender, RoutedEventArgs e)
        {
            _giveTextBoxFocusUponFlyoutClosing = true;
        }

        private void Reset_OnClick(object sender, RoutedEventArgs e)
        {
            _textbox.ResetTitle();
        }

        private void Flyout_Closed(object sender, object e)
        {
            if (_giveTextBoxFocusUponFlyoutClosing)
            {
                _textbox.TriggerEdit();
                _giveTextBoxFocusUponFlyoutClosing = false;
            }
        }

        #region Presenation Lines
        private List<UIElement> _paths = new List<UIElement>();


        private double distSqr(Point a, Point b)
        {
            return ((a.Y - b.Y)* (a.Y - b.Y) + (a.X - b.X) * (a.X - b.X));
        }

        private void drawLines()
        {
            var canvas = MainPage.Instance.xCanvas;
            var docView = MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionFreeformBase>().GetCanvas();

            //draw lines between members of presentation 
            var docs = PinnedNodesListView.Items?.ToList();

            for (var i = 0; i < docs?.Count - 1; i++)
            {
                //use bounds to find closest sides on each neighboring doc
                //get midpoitns of every side of both docs
                var docA = (docs[i] as DocumentViewModel).Bounds;
                var docAsides = new List<Tuple<Point, Point>>();
                var docAy = docA.Y + docA.Height / 2;
                var docAx = docA.X + docA.Width / 2;

                var docB = (docs[i + 1] as DocumentViewModel).Bounds;
                var docBsides = new List<Tuple<Point, Point>>();
                var docBy = docB.Y + docB.Height / 2;
                var docBx = docB.X + docB.Width / 2;

                var offset = Math.Sqrt(distSqr(new Point(docAx, docAy), new Point(docBx, docBy))) / 10;

                //the order goes left, top, right, bottom - in regualr UWP fashion
                docAsides.Add(Tuple.Create(new Point(docA.Left, docAy), new Point(docA.Left - offset, docAy)));
                docAsides.Add(Tuple.Create(new Point(docAx, docA.Top), new Point(docAx, docA.Top - offset)));
                docAsides.Add(Tuple.Create(new Point(docA.Right, docAy), new Point(docA.Right + offset, docAy)));
                docAsides.Add(Tuple.Create(new Point(docAx, docA.Bottom), new Point(docAx, docA.Bottom + offset)));


                //the order goes left, top, right, bottom - in regualr UWP fashion
                docBsides.Add(Tuple.Create(new Point(docB.Left, docBy), new Point(docB.Left - offset, docBy)));
                docBsides.Add(Tuple.Create(new Point(docBx, docB.Top), new Point(docBx, docB.Top - offset)));
                docBsides.Add(Tuple.Create(new Point(docB.Right, docBy), new Point(docB.Right + offset, docBy)));
                docBsides.Add(Tuple.Create(new Point(docBx, docB.Bottom), new Point(docBx, docB.Bottom + offset)));

                var minDist = Double.PositiveInfinity;
                Point startPoint;
                Point endPoint;
                Point startControlPt;
                Point endControlPt;

                //get closest two sides between docs
                foreach (var aside in docAsides)
                {
                    foreach (var bside in docBsides)
                    {
                        var dist = distSqr(aside.Item1, bside.Item1);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            startPoint = aside.Item1;
                            startControlPt = aside.Item2;
                            endPoint = bside.Item1;
                            endControlPt = bside.Item2;
                        }
                    }
                }

                //TransformToVisual gets a transform that can transform coords from background to xCanvas coord system
                startPoint = docView.TransformToVisual(canvas).TransformPoint(startPoint);
                endPoint = docView.TransformToVisual(canvas).TransformPoint(endPoint);
                startControlPt = docView.TransformToVisual(canvas).TransformPoint(startControlPt);
                endControlPt = docView.TransformToVisual(canvas).TransformPoint(endControlPt);

                //create nest of elements to show segment
                var segment =
                    new BezierSegment() {Point1 = startControlPt, Point2 = endControlPt, Point3 = endPoint};

                var segments = new PathSegmentCollection {segment};
                var pathFig = new PathFigure
                {
                    StartPoint = startPoint,
                    Segments = segments
                };

                var figures = new PathFigureCollection {pathFig};
                var pathGeo = new PathGeometry {Figures = figures};

                var path = new Windows.UI.Xaml.Shapes.Path
                {
                    Data = pathGeo,
                    Stroke = new SolidColorBrush(Windows.UI.Colors.Black),
                    StrokeThickness = 2

                };

                _paths.Add(path);
                canvas.Children.Add(path);

                //make arrow points
                var diffX = endControlPt.X - endPoint.X;
                var diffY = endControlPt.Y - endPoint.Y;
                Point arrowPtA;
                Point arrowPtB;
                var arrowWidth = 10;
                if (Math.Abs(diffX) > Math.Abs(diffY))
                {
                    var sign = diffX / Math.Abs(diffX);
                    //arrow should come from x direction
                    arrowPtA = new Point(endPoint.X + sign * arrowWidth, endPoint.Y + arrowWidth);
                    arrowPtB = new Point(endPoint.X + sign * arrowWidth, endPoint.Y - arrowWidth);
                }
                else
                {
                    var sign = diffY / Math.Abs(diffY);
                    arrowPtA = new Point(endPoint.X + arrowWidth, endPoint.Y + sign * arrowWidth);
                    arrowPtB = new Point(endPoint.X - arrowWidth, endPoint.Y + sign * arrowWidth);
                }

                //make arrow
                var arrow = new Polygon
                {
                    Points = new PointCollection
                    {
                        endPoint,
                        arrowPtA,
                        arrowPtB
                    },
                    Fill = new SolidColorBrush(Colors.Black)
                };

                _paths.Add(arrow);
                canvas.Children.Add(arrow);
            }
        }

        private void removeLines()
        {
            //remove all paths
            var canvas = MainPage.Instance.xCanvas;
            foreach (var path in _paths)
            {
                canvas.Children.Remove(path);
            }

        }
        protected void DocFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context c)
        {
            removeLines();
            drawLines();
        }

        private void ShowLinesButton_OnClick(object sender, RoutedEventArgs e)
        {
            var track = MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionFreeformBase>().ViewModel.ContainerDocument;         

            if ((ShowLinesButton.Background as SolidColorBrush).Color.ToString() == "#FFFFFFFF")
            {
                //show lines
                ShowLinesButton.Background = new SolidColorBrush(Colors.LightGray);

                drawLines();

                track.AddFieldUpdatedListener(KeyStore.PanZoomKey, DocFieldUpdated);
                track.AddFieldUpdatedListener(KeyStore.PanPositionKey, DocFieldUpdated);
            }
            else
            {
                //hide lines
                ShowLinesButton.Background = new SolidColorBrush(Colors.White);

                //remove all paths
                removeLines();
              
                track.RemoveFieldUpdatedListener(KeyStore.PanZoomKey, DocFieldUpdated);
                track.RemoveFieldUpdatedListener(KeyStore.PanPositionKey, DocFieldUpdated);
            }
        }
        #endregion
    }
}
