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
using Microsoft.Toolkit.Uwp.UI.Extensions;
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
        private bool _repeat = false;
        private List<UIElement> _paths = new List<UIElement>();

        public PresentationView()
        {
            this.InitializeComponent();
            DataContext = new PresentationViewModel();

            ShowLinesButton.Background = new SolidColorBrush(Colors.White);

     
            //remove all paths
            DrawLines();
            RemoveLines();
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
                    NavigateToDocument((DocumentController) PinnedNodesListView.SelectedItem);

                    IsPresentationPlaying = true;
                    PlayStopButton.Icon = new SymbolIcon(Symbol.Stop);
                    PlayStopButton.Label = "Stop";
                }
            }

            // back/next/reset buttons change appearance depending on state of presentation
            ResetBackNextButtons();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = PinnedNodesListView.SelectedIndex;

            // only move back if there is a step to go back to
            if (selectedIndex != 0)
                PinnedNodesListView.SelectedIndex = selectedIndex - 1;
            else if (_repeat)
                PinnedNodesListView.SelectedIndex = PinnedNodesListView.Items.Count - 1;

            NavigateToDocument((DocumentController) PinnedNodesListView.SelectedItem);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = PinnedNodesListView.SelectedIndex;

            // can only move forward if there's a node to move forward to
            if (selectedIndex != PinnedNodesListView.Items.Count - 1)
                PinnedNodesListView.SelectedIndex = selectedIndex + 1;
            else if (_repeat)
                PinnedNodesListView.SelectedIndex = 0;

            NavigateToDocument((DocumentController) PinnedNodesListView.SelectedItem);
        }

        // remove from viewmodel
        private void DeletePin(object sender, RoutedEventArgs e)
        {
            ViewModel.RemovePinFromPinnedNodesCollection((sender as Button).DataContext as DocumentController);

            DrawLinesWithNewDocs();
        }

        // if we click a node, we should navigate to it immediately. Note that IsItemClickable is always enabled.
        private void PinnedNode_Click(object sender, ItemClickEventArgs e)
        {
            DocumentController dc = (DocumentController) e.ClickedItem;
            NavigateToDocument(dc);
        }

        // helper method for moving the mainpage screen
        private void NavigateToDocument(DocumentController dc)
        {
            MainPage.Instance.NavigateToDocumentInWorkspaceAnimated(dc, true);
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
            NavigateToDocument((DocumentController) PinnedNodesListView.SelectedItem);
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
       


        private double distSqr(Point a, Point b)
        {
            return ((a.Y - b.Y)* (a.Y - b.Y) + (a.X - b.X) * (a.X - b.X));
        }

        private List<Point> GetFourBeizerPoints(int i)
        {
            var canvas = MainPage.Instance.xCanvas;
            var docs = PinnedNodesListView.Items;

            //use bounds to find closest sides on each neighboring doc
            //get midpoitns of every side of both docs
            var docA = (docs[i] as DocumentController);
            var docAPos = docA.GetField<PointController>(KeyStore.PositionFieldKey).Data;
            var docASize = docA.GetField<PointController>(KeyStore.ActualSizeKey).Data;
            var docAsides = new List<Tuple<Point, Point>>();
            var docAy = docAPos.Y + docASize.Y / 2;
            var docAx = docAPos.X + docASize.X / 2;
            var docALeft = docAPos.X;
            var docARight = docAPos.X + docASize.X;
            var docATop = docAPos.Y;
            var docABottom = docAPos.Y + docASize.Y;

            var docB = (docs[i + 1] as DocumentController);
            var docBPos = docB.GetField<PointController>(KeyStore.PositionFieldKey).Data;
            var docBSize = docB.GetField<PointController>(KeyStore.ActualSizeKey).Data;
            var docBsides = new List<Tuple<Point, Point>>();
            var docBy = docBPos.Y + docBSize.Y / 2;
            var docBx = docBPos.X + docBSize.X / 2;
            var docBLeft = docBPos.X;
            var docBRight = docBPos.X + docBSize.X;
            var docBTop = docBPos.Y;
            var docBBottom = docBPos.Y + docBSize.Y;

            var offset = Math.Sqrt(distSqr(new Point(docAx, docAy), new Point(docBx, docBy))) / 10;

            //the order goes left, top, right, bottom - in regualr UWP fashion
            docAsides.Add(Tuple.Create(new Point(docALeft, docAy), new Point(docALeft - offset, docAy)));
            docAsides.Add(Tuple.Create(new Point(docAx, docATop), new Point(docAx, docATop - offset)));
            docAsides.Add(Tuple.Create(new Point(docARight, docAy), new Point(docARight + offset, docAy)));
            docAsides.Add(Tuple.Create(new Point(docAx, docABottom), new Point(docAx, docABottom + offset)));
           
             //the order goes left, top, right, bottom - in regualr UWP fashion
             docBsides.Add(Tuple.Create(new Point(docBLeft, docBy), new Point(docBLeft - offset, docBy)));
            docBsides.Add(Tuple.Create(new Point(docBx, docBTop), new Point(docBx, docBTop - offset)));
            docBsides.Add(Tuple.Create(new Point(docBRight, docBy), new Point(docBRight + offset, docBy)));
            docBsides.Add(Tuple.Create(new Point(docBx, docBBottom), new Point(docBx, docBBottom + offset)));

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

            //get right collection
            Canvas docViewA = MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionFreeformBase>()
                .GetCanvas();
            Canvas docViewB = MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionFreeformBase>()
                .GetCanvas();
            var allCollections = MainPage.Instance.MainDocView.GetDescendantsOfType<CollectionFreeformBase>()
                .Reverse();
            foreach (var col in allCollections)
            {
                foreach (var doc in col.GetImmediateDescendantsOfType<DocumentView>())
                {
                    if (Equals(docs[i] as DocumentController, doc.ViewModel.DocumentController))
                    {
                        docViewA = col.GetCanvas();
                    }

                    if (Equals(docs[i + 1] as DocumentController, doc.ViewModel.DocumentController))
                    {
                        docViewB = col.GetCanvas();
                    }
                }
            }

            //TransformToVisual gets a transform that can transform coords from background to xCanvas coord system
            startPoint = docViewA.TransformToVisual(canvas).TransformPoint(startPoint);
            endPoint = docViewB.TransformToVisual(canvas).TransformPoint(endPoint);
            startControlPt = docViewA.TransformToVisual(canvas).TransformPoint(startControlPt);
            endControlPt = docViewB.TransformToVisual(canvas).TransformPoint(endControlPt);

            return new List<Point>()
            {
                startPoint,
                endPoint,
                startControlPt,
                endControlPt
            };
        }

        private void UpdatePaths()
        {
            //if pins changed, updating won't work
            if (_paths.Count / 2 != PinnedNodesListView.Items.Count - 1)
            {
                DrawLines();
            }

            //draw lines between members of presentation 
            var docs = PinnedNodesListView.Items;

            for (var i = 0; i < docs?.Count - 1; i++)
            {
                var points = GetFourBeizerPoints(i);
                Point startPoint = points[0];
                Point endPoint = points[1];
                Point startControlPt = points[2];
                Point endControlPt = points[3];

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

                var oldPath = ((_paths[2 * i] as Windows.UI.Xaml.Shapes.Path)?.Data as PathGeometry);
                oldPath.Figures = figures;

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
                var arrowPoints = new PointCollection
                {
                    endPoint,
                    arrowPtA,
                    arrowPtB
                };

                var oldPath2 = _paths[2 * i + 1] as Polygon;
                oldPath2.Points = arrowPoints;
            }
        }

        private void DrawLines()
        {
            var canvas = MainPage.Instance.xCanvas;
            //only recalcualte if you need to 

            RemoveLines();
            _paths = new List<UIElement>();

            //draw lines between members of presentation 
            var docs = PinnedNodesListView.Items;

            for (var i = 0; i < docs?.Count - 1; i++)
            {
                var points = GetFourBeizerPoints(i);
                Point startPoint = points[0];
                Point endPoint = points[1];
                Point startControlPt = points[2];
                Point endControlPt = points[3];

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

        private void RemoveLines()
        {
            //remove all paths
            var canvas = MainPage.Instance.xCanvas;
            foreach (var path in _paths)
            {
                canvas.Children.Remove(path);
            }

        }
        private void DocFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context c)
        {
            UpdatePaths();
        }

        public void DrawLinesWithNewDocs()
        {
            if ((ShowLinesButton.Background as SolidColorBrush).Color.ToString() != "#FFFFFFFF")
            {
                //show lines
                foreach (var viewModelPinnedNode in ViewModel.PinnedNodes)
                {
                    viewModelPinnedNode.RemoveFieldUpdatedListener(KeyStore.PositionFieldKey, DocFieldUpdated);
                    viewModelPinnedNode.RemoveFieldUpdatedListener(KeyStore.ActualSizeKey, DocFieldUpdated);
                }

                foreach (var viewModelPinnedNode in ViewModel.PinnedNodes)
                {
                    viewModelPinnedNode.AddFieldUpdatedListener(KeyStore.PositionFieldKey, DocFieldUpdated);
                    viewModelPinnedNode.AddFieldUpdatedListener(KeyStore.ActualSizeKey, DocFieldUpdated);
                }

                DrawLines();
            }
        }

        private void ShowLinesButton_OnClick(object sender, RoutedEventArgs e)
        {
            var allCollections = MainPage.Instance.MainDocView.GetDescendantsOfType<CollectionFreeformBase>();
        

            if ((ShowLinesButton.Background as SolidColorBrush).Color.ToString() == "#FFFFFFFF")
            {
                //show lines
                ShowLinesButton.Background = new SolidColorBrush(Colors.LightGray);

                DrawLines();

                foreach (var viewModelPinnedNode in ViewModel.PinnedNodes)
                {
                    viewModelPinnedNode.AddFieldUpdatedListener(KeyStore.PositionFieldKey, DocFieldUpdated);
                    viewModelPinnedNode.AddFieldUpdatedListener(KeyStore.ActualSizeKey, DocFieldUpdated);
                }

                foreach (var coll in allCollections)
                {
                    var track = coll.ViewModel.ContainerDocument;

                    track.AddFieldUpdatedListener(KeyStore.PanZoomKey, DocFieldUpdated);
                    track.AddFieldUpdatedListener(KeyStore.PanPositionKey, DocFieldUpdated);
                }
            }
            else
            {
                //hide lines
                ShowLinesButton.Background = new SolidColorBrush(Colors.White);

                //remove all paths
                RemoveLines();

                foreach (var viewModelPinnedNode in ViewModel.PinnedNodes)
                {
                    viewModelPinnedNode.RemoveFieldUpdatedListener(KeyStore.PositionFieldKey, DocFieldUpdated);
                    viewModelPinnedNode.RemoveFieldUpdatedListener(KeyStore.ActualSizeKey, DocFieldUpdated);
                }

                foreach (var coll in allCollections)
                {
                    var track = coll.ViewModel.ContainerDocument;

                    track.RemoveFieldUpdatedListener(KeyStore.PanZoomKey, DocFieldUpdated);
                    track.RemoveFieldUpdatedListener(KeyStore.PanPositionKey, DocFieldUpdated);
                }
            }
        }
        #endregion

        private void RepeatButton_OnChecked(object sender, RoutedEventArgs e)
        {
            _repeat = true;
        }

        private void RepeatButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            _repeat = false;
        }
    }
}
