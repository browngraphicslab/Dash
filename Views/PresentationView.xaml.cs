﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Point = Windows.Foundation.Point;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class PresentationView
    {
        public PresentationViewModel ViewModel => DataContext as PresentationViewModel;

        private int LastSelectedIndex { get; set; }

        public bool IsPresentationPlaying = false;
        private PresentationViewTextBox _textbox;
        private bool _giveTextBoxFocusUponFlyoutClosing = false;
        private bool _repeat = false;
        private List<UIElement> _paths = new List<UIElement>();

        private PointController _panZoom;
        private PointController _panPos;
        private DocumentController _startCollection;

        public PresentationView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            xHelpPrompt.Text = "Pinned items will appear here.\rAdd content from right-click\rcontext menu.";
        }

        private PresentationViewModel _oldViewModel;
        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (_oldViewModel == args.NewValue) return;

            if (_oldViewModel != null)
            {
                _oldViewModel.PinnedNodes.CollectionChanged -= PinnedNodes_CollectionChanged;
            }

            _oldViewModel = ViewModel;

            if (ViewModel == null) return;

            var loopBinding = new FieldBinding<BoolController, BoolController>()
            {
                Document = MainPage.Instance.MainDocument.GetDataDocument(),
                Key = KeyStore.PresLoopOnKey,
                Mode = BindingMode.TwoWay
            };
            xLoopButton.AddFieldBinding(ToggleButton.IsCheckedProperty, loopBinding);

            var lineVisBinding = new FieldBinding<BoolController, BoolController>()
            {
                Document = MainPage.Instance.MainDocument.GetDataDocument(),
                Key = KeyStore.PresLinesVisibleKey,
                Mode = BindingMode.TwoWay
            };
            xShowLinesButton.AddFieldBinding(ToggleButton.IsCheckedProperty, lineVisBinding);

            ViewModel.PinnedNodes.CollectionChanged += PinnedNodes_CollectionChanged;
            if (ViewModel.PinnedNodes.Count == 0) xHelpPrompt.Visibility = Visibility.Visible;

            //remove all paths
            DrawLines();
            RemoveLines();
        }

        private void PinnedNodes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => DrawLinesWithNewDocs();

        private void PlayStopButton_Click(object sender, RoutedEventArgs e) => PlayStopClick();

        public void PlayStopClick()
        {
            // can't play/stop if there's nothing in it
            if (xPinnedNodesListView.Items.Count != 0)
            {
                if (IsPresentationPlaying)
                {
                    // if it's currently playing, then it means the user just clicked the stop button. Reset.
                    IsPresentationPlaying = false;

                    xPlayStopButton.Icon = new SymbolIcon(Symbol.Play);
                    xPlayStopButton.Label = "Play";
                    xPinnedNodesListView.SelectionMode = ListViewSelectionMode.None;

                    _startCollection.SetField(KeyStore.PanZoomKey, _panZoom, true);
                    _startCollection.SetField(KeyStore.PanPositionKey, _panPos, true);
                    SplitFrame.OpenInActiveFrame(_startCollection);
                }
                else
                {
                    // zoom to first item in the listview

                    xPinnedNodesListView.SelectionMode = ListViewSelectionMode.Single;
                    xPinnedNodesListView.SelectedIndex = 0;
                    NavigateToDocument((DocumentController)xPinnedNodesListView.SelectedItem);

                    _startCollection = MainPage.Instance.MainDocument.GetDataDocument().GetField<DocumentController>(KeyStore.LastWorkspaceKey);
                    _panZoom = _startCollection.GetField(KeyStore.PanZoomKey)?.Copy() as PointController;
                    _panPos = _startCollection.GetField(KeyStore.PanPositionKey)?.Copy() as PointController;
                    NavigateToDocument((DocumentController) xPinnedNodesListView.SelectedItem);

                    IsPresentationPlaying = true;
                    xPlayStopButton.Icon = new SymbolIcon(Symbol.Stop);
                    xPlayStopButton.Label = "Stop";
                }
            }

            // back/next/reset buttons change appearance depending on state of presentation
            ResetBackNextButtons();

            if (!_repeat && IsPresentationPlaying)
            {
                //disable back button
                IsBackEnabled(false);
                if (ViewModel.PinnedNodes.Count == 1) IsNextEnabled(false);
            }

            if (_repeat && ViewModel.PinnedNodes.Count == 1)
            {
                IsBackEnabled(false);
                IsNextEnabled(false);

            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = xPinnedNodesListView.SelectedIndex;

            // only move back if there is a step to go back to
            if (selectedIndex > 0)
            {
                IsNextEnabled(true);
                xPinnedNodesListView.SelectedIndex = selectedIndex - 1;
            }
            else if (_repeat)
            {
                xPinnedNodesListView.SelectedIndex = xPinnedNodesListView.Items.Count - 1;
            }
            if(selectedIndex == 1 && !_repeat)
            {
                //disable back button
                IsBackEnabled(false);
            }

            LastSelectedIndex = xPinnedNodesListView.SelectedIndex;

            NavigateToDocument((DocumentController) xPinnedNodesListView.SelectedItem);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = xPinnedNodesListView.SelectedIndex;

            // can only move forward if there's a node to move forward to
            if (selectedIndex != xPinnedNodesListView.Items.Count - 1)
            {
                xBackButton.Opacity = 1;
                xBackButton.IsEnabled = true;
                xPinnedNodesListView.SelectedIndex = selectedIndex + 1;
            }
            else if (_repeat)
            {
                xPinnedNodesListView.SelectedIndex = 0;
            }
            if (selectedIndex == xPinnedNodesListView.Items.Count - 2 && !_repeat)
            {
                //end presentation
                IsNextEnabled(false);
            }

            LastSelectedIndex = xPinnedNodesListView.SelectedIndex;

            NavigateToDocument((DocumentController) xPinnedNodesListView.SelectedItem);
        }

        // ON TRASH CLICK: remove from viewmodel
        private void DeletePin(object sender, RoutedEventArgs e) => FullPinDelete((sender as Button)?.DataContext as DocumentController);

        public void FullPinDelete(DocumentController doc)
        {
            ViewModel.RemovePinFromPinnedNodesCollection(doc);

            DrawLinesWithNewDocs();

            int selectedIndex = xPinnedNodesListView.SelectedIndex;
            if (selectedIndex == xPinnedNodesListView.Items?.Count - 1 && !_repeat)
            {
                //end presentation
                IsNextEnabled(false);
            }
            if (selectedIndex == 0 && !_repeat)
            {
                //disable back button
                IsBackEnabled(false);
            }
        }

        // if we click a node, we should navigate to it immediately. Note that IsItemClickable is always enabled.
        private void PinnedNode_Click(object sender, ItemClickEventArgs e)
        {
            var dc = (DocumentController) e.ClickedItem;
            NavigateToDocument(dc);
        }

        // helper method for moving the mainpage screen
        private static void NavigateToDocument(DocumentController dc)
        {
            //if navigation failed, it wasn't in current workspace or something
            if (!SplitFrame.TryNavigateToDocument(dc))
            {
                var tree = DocumentTree.MainPageTree;
                var docNode = tree.FirstOrDefault(dn => dn.ViewDocument.Equals(dc));
                if (docNode != null)//TODO This doesn't handle documents in collections that aren't in the document "visual tree", so diff workspaces doesn't really work (also change in AnnotationManager)
                {
                    SplitFrame.OpenDocumentInWorkspace(docNode.ViewDocument, docNode.Parent.ViewDocument);
                }
                else
                {
                    SplitFrame.OpenInActiveFrame(dc);
                }
            }
        } 

        // these buttons are only enabled when the presentation is playing
        private void ResetBackNextButtons()
        {
            IsBackEnabled(IsPresentationPlaying);
            IsNextEnabled(IsPresentationPlaying);
            IsResetEnabled(IsPresentationPlaying);
        }

        // if user strays in middle of presentation, hitting this will bring them back to the selected node
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            xPinnedNodesListView.SelectedIndex = LastSelectedIndex;
            NavigateToDocument((DocumentController) xPinnedNodesListView.SelectedItem);
        }

        private void PinnedNodesListView_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var listView = (ListView) sender;
            PinnedNodeFlyout.ShowAt(listView, e.GetPosition(listView));
            var source = (FrameworkElement) e.OriginalSource;
            _textbox = source.GetFirstDescendantOfType<PresentationViewTextBox>() ?? source.GetFirstAncestorOfType<PresentationViewTextBox>();
        }

        private void Edit_OnClick(object sender, RoutedEventArgs e) => _giveTextBoxFocusUponFlyoutClosing = true;

        private void Reset_OnClick(object sender, RoutedEventArgs e) => _textbox.ResetTitle();

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
            var docs = xPinnedNodesListView.Items;

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
            var docViewA = MainPage.Instance.MainSplitter.GetFirstDescendantOfType<CollectionFreeformBase>()
                .GetCanvas();
            var docViewB = MainPage.Instance.MainSplitter.GetFirstDescendantOfType<CollectionFreeformBase>()
                .GetCanvas();
            var allCollections = MainPage.Instance.MainSplitter.GetDescendantsOfType<CollectionFreeformBase>()
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
            if (MainPage.Instance.CurrPresViewState == MainPage.PresentationViewState.Collapsed) return;

            //if pins changed, updating won't work
            if (_paths.Count / 2 != xPinnedNodesListView.Items.Count - 1)
            {
                DrawLines();
            }

            //draw lines between members of presentation 
            var docs = xPinnedNodesListView.Items;

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

        public void DrawLines()
        {
            if (MainPage.Instance.CurrPresViewState == MainPage.PresentationViewState.Collapsed) return;

            var canvas = MainPage.Instance.xCanvas;
            //only recalcualte if you need to 

            RemoveLines();
            _paths = new List<UIElement>();

            //draw lines between members of presentation 
            var docs = xPinnedNodesListView.Items;

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
                Canvas.SetZIndex(path, -1);
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
                Canvas.SetZIndex(arrow, -1);

                _paths.Add(arrow);
                canvas.Children.Add(arrow);
            }
        }

        public void RemoveLines()
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
            var isChecked = xShowLinesButton.IsChecked;
            if (isChecked != null && (bool) !isChecked || MainPage.Instance.CurrPresViewState == MainPage.PresentationViewState.Collapsed) return;

            //show lines
            foreach (DocumentController viewModelPinnedNode in ViewModel.PinnedNodes)
            {
                viewModelPinnedNode.RemoveFieldUpdatedListener(KeyStore.PositionFieldKey, DocFieldUpdated);
                viewModelPinnedNode.RemoveFieldUpdatedListener(KeyStore.ActualSizeKey, DocFieldUpdated);
            }

            foreach (DocumentController viewModelPinnedNode in ViewModel.PinnedNodes)
            {
                viewModelPinnedNode.AddFieldUpdatedListener(KeyStore.PositionFieldKey, DocFieldUpdated);
                viewModelPinnedNode.AddFieldUpdatedListener(KeyStore.ActualSizeKey, DocFieldUpdated);
            }

            DrawLines();
        }

        private void ShowLinesButton_OnChecked(object sender, RoutedEventArgs e) => ShowLines();

        public void ShowLines()
        {
            //show lines
            var allCollections = MainPage.Instance.MainSplitter.GetDescendantsOfType<CollectionFreeformBase>();
            //xShowLinesButton.Background = new SolidColorBrush(Colors.LightGray);

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

        private void ShowLinesButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            //hide lines
            var allCollections = MainPage.Instance.MainSplitter.GetDescendantsOfType<CollectionFreeformBase>();
            //xShowLinesButton.Background = new SolidColorBrush(Colors.White);

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
        #endregion

        private void RepeatButton_OnChecked(object sender, RoutedEventArgs e)
        {
            _repeat = true;

            xNextButton.IsEnabled = true;
            xNextButton.Opacity = 1;
            xBackButton.IsEnabled = true;
            xBackButton.Opacity = 1;
        }

        private void RepeatButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            _repeat = false;

            int selectedIndex = xPinnedNodesListView.SelectedIndex;
            if (selectedIndex == xPinnedNodesListView.Items?.Count - 1)
            {
                //end presentation
                IsNextEnabled(false);
            }
            if (selectedIndex == 0)
            {
                //disable back button
                IsBackEnabled(false);
            }
        }

        private void XClosePresentation_OnClick(object sender, RoutedEventArgs e)
        {
            //close presentation
            MainPage.Instance.SetPresentationState(false);
            TryPlayStopClick();
        }

        private async void Timeline_OnCompleted(object sender, object e)
        {
            await Task.Delay(350);
            xTransportControls.MinHeight = 0;
            xTransportControls.Opacity = 1;
        }

        public void TryHighlightMatches(DocumentView viewRef)
        {
            DocumentController viewRefDoc = viewRef.ViewModel?.LayoutDocument;

            var ind = 0;
            foreach (DocumentController viewDoc in ViewModel?.PinnedNodes)
            {
                if (viewDoc == viewRefDoc) break;
                ind++;
            }

            if (ind == ViewModel.PinnedNodes.Count) return;

            xNumberList.SelectionMode = ListViewSelectionMode.None;
            xNumberList.SelectedIndex = ind;
            if (xNumberList.SelectedItem is PresentationNumberViewModel selectedNum) selectedNum.FontWeight = FontWeights.ExtraBold;
        }

        public void ClearHighlightedMatch()
        {
            if (xNumberList.Items != null)
            {
                foreach (object vm in xNumberList.Items)
                {
                    (vm as PresentationNumberViewModel).FontWeight = FontWeights.Normal;
                }
            }
            xNumberList.SelectedIndex = -1;
        }

        internal void SimulateAnimation(bool expand)
        {
            if (expand)
            {
                xTransportControls.Height = 60;
                xPinnedNodesListView.Opacity = 1;
                xPresentationTitle.Opacity = 1;
                xSettingsPanel.Opacity = 1;
            }
            else
            {
                xTransportControls.Height = 0;
                xPinnedNodesListView.Opacity = 0;
                xPresentationTitle.Opacity = 0;
                xSettingsPanel.Opacity = 0;
            }
        }

        public void IsBackEnabled(bool enabled)
        {
            xBackButton.Opacity = enabled ? 1 : 0.3;
            xBackButton.IsEnabled = enabled;
        }

        public void IsNextEnabled(bool enabled)
        {
            xNextButton.Opacity = enabled ? 1 : 0.3;
            xNextButton.IsEnabled = enabled;
        }

        public void IsResetEnabled(bool enabled)
        {
            xResetButton.Opacity = enabled ? 1 : 0.3;
            xResetButton.IsEnabled = enabled;
        }

        public void TryPlayStopClick()
        {
            if (IsPresentationPlaying) PlayStopClick();
        }
    }
}
