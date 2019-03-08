using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI;
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
        public enum PresentationViewState
        {
            Expanded,
            Collapsed
        }
        public PresentationViewModel ViewModel => DataContext as PresentationViewModel;

        private DocumentController _toPinNext = null;

        public void SetPinAtLocation(DocumentController doc)
        {
            _toPinNext = doc;
        }

        private int LastSelectedIndex { get; set; }

        public bool IsPresentationPlaying = false;
        private PresentationViewTextBox _textbox;
        private DocumentController _document;
        private bool _giveTextBoxFocusUponFlyoutClosing = false;
        private bool _repeat = false;
        private List<UIElement> _paths = new List<UIElement>();

        private PointController _panZoom;
        private PointController _panPos;
        private DocumentController _startCollection;

        public string CurrTitle
        {
            get => ViewModel?.CurrPres?.Title ?? "New Presentation";
            set => ViewModel.CurrPres.SetTitle(value);
        }

        public ObservableCollection<DocumentController> Presentations => ViewModel?.Presentations;

        public PresentationView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            xHelpPrompt.Text = "Pinned items will appear here.\rAdd content from right-click\rcontext menu.";
            xTitle.PropertyChanged += XTitleBox_PropertyChanged;
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (FocusManager.GetFocusedElement() is RichEditBox)
            {
                return;
            }
            if (IsPresentationPlaying)
            {
                switch (args.VirtualKey)
                {
                case VirtualKey.Right:
                    if (this.IsCtrlPressed())
                    {
                        SplitFrame.ActiveFrame.GoForward();
                    }
                    else
                    {
                        this.NextButton_Click(null, null);
                    }
                    args.Handled = true;
                    break;
                case VirtualKey.Left:
                    if (this.IsCtrlPressed())
                    {
                        SplitFrame.ActiveFrame.GoBack();
                    }
                    else
                    {
                        this.BackButton_Click(null, null);
                    }
                    args.Handled = true;
                    break;
                case VirtualKey.Up:
                    var doc = SplitFrame.ActiveFrame.DocumentController;
                    var paths = DocumentTree.GetPathsToDocuments(doc);
                    if (paths.Any())
                    {
                        var path = paths[0];
                        if (path.Count > 2)
                        {
                            var col = path[path.Count - 2];
                            SplitFrame.ActiveFrame.OpenDocument(doc, col);
                        }
                    }
                    args.Handled = true;
                    break;
                }
            }
        }

        public PresentationViewState CurrPresViewState
        {
            get => MainPage.Instance.MainDocument.GetDataDocument().GetField<BoolController>(KeyStore.PresentationViewVisibleKey)?.Data ?? false ? PresentationViewState.Expanded : PresentationViewState.Collapsed;
            set
            {
                bool state = value == PresentationViewState.Expanded;
                MainPage.Instance.MainDocument.GetDataDocument().SetField<BoolController>(KeyStore.PresentationViewVisibleKey, state, true);
            }
        }

        public void PinToPresentation(DocumentController doc)
        {
            ViewModel.AddToPinnedNodesCollection(doc);
            if (CurrPresViewState == PresentationViewState.Collapsed)
            {
                xHelpPrompt.Opacity = 0;
                xHelpPrompt.Visibility = Visibility.Collapsed;
                SetPresentationState(true);
            }
            DrawLinesWithNewDocs();
        }
        public void SetPresentationState(bool expand, bool animate = true)
        {
            //    TogglePresentationMode(expand);
            if (expand)
            {
                CurrPresViewState = PresentationViewState.Expanded;
                if (animate)
                {
                    MainPage.Instance.xPresentationExpand.Begin();
                    MainPage.Instance.xPresentationExpand.Completed += (s, e) =>
                    {
                        xContentIn.Begin();
                        xHelpIn.Begin();
                    };
                    xContentIn.Completed += (s, e) => xSettingsIn.Begin();
                    xSettingsIn.Completed += (s, e) =>
                    {
                        if (xShowLinesButton.IsChecked ?? false) ShowLines();
                    };
                }
                else
                {
                    MainPage.Instance.xUtilTabColumn.MinWidth = 300;
                    SimulateAnimation(true);
                }
            }
            else
            {
                CurrPresViewState = PresentationViewState.Collapsed;
                //open presentation
                if (animate)
                {
                    //TryPlayStopClick();
                    xSettingsOut.Begin();
                    xContentOut.Begin();
                    xHelpOut.Begin();
                    MainPage.Instance.xPresentationRetract.Begin();
                }
                else
                {
                    MainPage.Instance.xUtilTabColumn.MinWidth = 0;
                    SimulateAnimation(false);
                }

                xShowLinesButton.Background = new SolidColorBrush(Colors.White);
                RemoveLines();
            }
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

            var loopBinding = new FieldBinding<BoolController>()
            {
                Document = MainPage.Instance.MainDocument.GetDataDocument(),
                Key = KeyStore.PresLoopOnKey,
                Mode = BindingMode.TwoWay
            };
            xLoopButton.AddFieldBinding(ToggleButton.IsCheckedProperty, loopBinding);

            var lineVisBinding = new FieldBinding<BoolController>()
            {
                Document = MainPage.Instance.MainDocument.GetDataDocument(),
                Key = KeyStore.PresLinesVisibleKey,
                Mode = BindingMode.TwoWay
            };
            xShowLinesButton.AddFieldBinding(ToggleButton.IsCheckedProperty, lineVisBinding);

            //TODO: NEED TO ADD LISTENER PER CURRENT PRES, otherwise pinned nodes is null
            ViewModel.PinnedNodes.CollectionChanged += PinnedNodes_CollectionChanged;
            if (ViewModel.PinnedNodes.Count == 0) xHelpPrompt.Visibility = Visibility.Visible;

            //remove all paths
            DrawLines();
            RemoveLines();
        }

        private void PinnedNodes_CollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            DrawLinesWithNewDocs();
            ViewModel.UpdateList();
        }

        private void PlayStopButton_Click(object sender, RoutedEventArgs e)
        {
            PlayStopClick();
        }

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
                    //xPinnedNodesListView.SelectionMode = ListViewSelectionMode.None;

                    //_startCollection.SetField(KeyStore.PanZoomKey, _panZoom, true);
                    //_startCollection.SetField(KeyStore.PanPositionKey, _panPos, true);
                    //SplitFrame.OpenInActiveFrame(_startCollection);

                    foreach (PresentationItemViewModel item in xPinnedNodesListView.Items)
                    {
                        item.Document.SetField<BoolController>(KeyStore.HiddenKey, false, true);

                        item.Document.SetField<NumberController>(KeyStore.OpacityKey, 1, true);
                    }
                }
                else
                {
                    // zoom to first item in the listview

                    xPinnedNodesListView.SelectionMode = ListViewSelectionMode.Single;
                    int nextIndex = xPinnedNodesListView.SelectedIndex;
                    if (nextIndex >= 0)
                    {
                        nextIndex--;
                    }
                    nextIndex = GetNextIndex(nextIndex);
                    xPinnedNodesListView.SelectedIndex = nextIndex;

                    //int nextIndex = GetNextIndex(-1);
                    //xPinnedNodesListView.SelectedIndex = nextIndex;
                    foreach (PresentationItemViewModel item in xPinnedNodesListView.Items)
                    {
                        if (item.Document.GetField<BoolController>(KeyStore.PresentationVisibleKey)?.Data ?? false)
                        {
                            item.Document.SetField<BoolController>(KeyStore.HiddenKey, true, true);
                        }
                    }

                    for (int i = 0; i <= nextIndex; ++i)
                    {
                        ToDocument(ViewModel.PinnedNodes[i].Document, false);
                    }

                    UpdateTransform(nextIndex);

                    _startCollection = MainPage.Instance.MainDocument.GetDataDocument().GetField<DocumentController>(KeyStore.LastWorkspaceKey);

                    _panZoom = _startCollection.GetField(KeyStore.PanZoomKey)?.Copy() as PointController;
                    _panPos = _startCollection.GetField(KeyStore.PanPositionKey)?.Copy() as PointController;

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

        private static bool HideAfter(DocumentController doc)
        {
            return doc.GetField<BoolController>(KeyStore.PresentationHideKey)?.Data ?? false;
        }

        private static bool HideBefore(DocumentController doc)
        {
            return doc.GetField<BoolController>(KeyStore.PresentationVisibleKey)?.Data ?? false;
        }

        private static bool ShouldFade(DocumentController doc)
        {
            return doc.GetField<BoolController>(KeyStore.PresentationFadeKey)?.Data ?? false;
        }

        private static bool ShouldNavigate(DocumentController doc)
        {
            return doc.GetField<BoolController>(KeyStore.PresentationNavigateKey)?.Data ?? false;
        }

        private static bool IsGrouped(DocumentController doc)
        {
            return doc.GetField<BoolController>(KeyStore.PresentationGroupUpKey)?.Data ?? false;
        }

        private void ToDocument(DocumentController doc, bool reverse)
        {
            if (HideBefore(doc))
            {
                doc.SetHidden(reverse);
            }

            //if (ShouldNavigate(doc) && !reverse)
            //{
            //    NavigateToDocument(doc);
            //}
        }

        private void FromDocument(DocumentController doc, bool reverse)
        {
            if (HideAfter(doc))
            {
                doc.SetHidden(!reverse);
            }

            if (ShouldFade(doc))
            {
                doc.SetField<NumberController>(KeyStore.OpacityKey, reverse ? 1 : 0.5, true);
            }

            //if (ShouldNavigate(doc) && reverse)
            //{
            //    NavigateToDocument(doc);
            //}
        }

        private void UpdateTransform(int currentIndex)
        {
            for (int i = currentIndex; i >= 0; i--)
            {
                var doc = ViewModel.PinnedNodes[i].Document;
                if (ShouldNavigate(doc))
                {
                    NavigateToDocument(doc);
                    break;
                }
            }
        }

        int GetNextIndex(int currentIndex)
        {
            int numNodes = ViewModel.PinnedNodes.Count;
            currentIndex++;
            if (currentIndex >= numNodes)
            {
                return -1;
            }
            while (currentIndex + 1 < numNodes)
            {
                var doc = ViewModel.PinnedNodes[currentIndex + 1].Document;
                if (!IsGrouped(doc))
                {
                    break;
                }
                currentIndex++;
            }

            return currentIndex;
        }

        int GetPreviousIndex(int currentIndex)
        {
            while (currentIndex >= 0)
            {
                var doc = ViewModel.PinnedNodes[currentIndex].Document;
                if (!IsGrouped(doc))
                {
                    break;
                }
                currentIndex--;
            }

            return currentIndex - 1;

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = xPinnedNodesListView.SelectedIndex;

            int nextIndex = GetPreviousIndex(selectedIndex);
            if (nextIndex == -1)
            {
                if (_repeat)
                {
                    nextIndex = ViewModel.PinnedNodes.Count - 1;
                }
                else
                {
                    return;
                }
            }

            int nextNextIndex = GetPreviousIndex(nextIndex);

            IsNextEnabled(true);
            xPinnedNodesListView.SelectedIndex = nextIndex;


            if (nextNextIndex == -1)
            {
                if (!_repeat)
                {
                    //end presentation
                    IsBackEnabled(false);
                }
            }

            for (int i = selectedIndex; i > nextIndex; --i)
            {
                ToDocument(ViewModel.PinnedNodes[i].Document, true);
            }

            for (int i = nextIndex; i > nextNextIndex; --i)
            {
                FromDocument(ViewModel.PinnedNodes[i].Document, true);
            }

            UpdateTransform(nextIndex);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = xPinnedNodesListView.SelectedIndex;

            int nextIndex = GetNextIndex(selectedIndex);
            if (nextIndex == -1)
            {
                if (_repeat)
                {
                    nextIndex = 0;
                }
                else
                {
                    return;
                }
            }

            int nextNextIndex = GetNextIndex(nextIndex);
            int prevIndex = GetPreviousIndex(selectedIndex);

            IsBackEnabled(true);
            xPinnedNodesListView.SelectedIndex = nextIndex;


            if (nextNextIndex == -1 && !_repeat)
            {
                //end presentation
                IsNextEnabled(false);
            }

            if (prevIndex != -1)
            {
                for (int i = prevIndex + 1; i <= selectedIndex; ++i)
                {
                    FromDocument(ViewModel.PinnedNodes[i].Document, false);
                }
            }

            for (int i = selectedIndex + 1; i <= nextIndex; ++i)
            {
                ToDocument(ViewModel.PinnedNodes[i].Document, false);
            }

            UpdateTransform(nextIndex);
        }


        // ON TRASH CLICK: remove from viewmodel
        private void DeletePin(object sender, RoutedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel) ((FrameworkElement) sender).DataContext;
            ViewModel.RemoveNode(itemViewModel);
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

        public void FullPinDelete(DocumentController doc)
        {
            if (ViewModel.RemovePinFromPinnedNodesCollection(doc))
            {
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
        }

        // if we click a node, we should navigate to it immediately. Note that IsItemClickable is always enabled.
        private void PinnedNode_Click(object sender, ItemClickEventArgs e)
        {
            var itemVM = (PresentationItemViewModel)e.ClickedItem;
            if (_toPinNext != null)
            {
                ViewModel.AddToPinnedNodesCollection(_toPinNext, index:ViewModel.PinnedNodes.IndexOf(itemVM) + 1);
                _toPinNext = null;
            }
            else
            {
                var dc = itemVM.Document;

                NavigateToDocument(dc);
            }
        }

        // helper method for moving the mainpage screen
        private static void NavigateToDocument(DocumentController dc)
        {
            bool zoom = dc.GetField<BoolController>(KeyStore.PresContextZoomKey)?.Data ?? true;
            var parent = dc.GetRegionDefinition();
            if (parent != null)
            {
                var region = dc;
                dc = parent;
                dc.GotoRegion(region);
            }
            if (zoom)
            {
                SplitFrame.OpenInActiveFrame(dc);
            }
            else
            {
                //if navigation failed, it wasn't in current workspace or something
                if (!SplitFrame.TryNavigateToDocument(dc))
                {
                    var tree = DocumentTree.MainPageTree;
                    var docNode = tree.FirstOrDefault(dn => dn.ViewDocument.Equals(dc));
                    if (docNode != null) //TODO This doesn't handle documents in collections that aren't in the document "visual tree", so diff workspaces doesn't really work (also change in AnnotationManager)
                    {
                        SplitFrame.OpenDocumentInWorkspace(docNode.ViewDocument, docNode.Parent.ViewDocument);
                    }
                    else
                    {
                        SplitFrame.OpenInActiveFrame(dc);
                    }
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
            NavigateToDocument(((PresentationItemViewModel)xPinnedNodesListView.SelectedItem).Document);
        }

        private void PinnedNodesListView_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            _document = (((FrameworkElement)e.OriginalSource).DataContext as PresentationItemViewModel)?.Document;
            if (_document == null)
            {
                return;
            }
            var listView = (ListView)sender;
            PinnedNodeFlyout.ShowAt(listView, e.GetPosition(listView));
            var source = (FrameworkElement)e.OriginalSource;
            _textbox = source.GetFirstDescendantOfType<PresentationViewTextBox>() ??
                       source.GetFirstAncestorOfType<PresentationViewTextBox>();

            bool zoomContext = _document.GetField<BoolController>(KeyStore.PresContextZoomKey)?.Data ?? false;
            Fullscreen.Background = zoomContext ? new SolidColorBrush(Colors.LightSteelBlue) : new SolidColorBrush(Colors.Transparent);

        }

        private void Edit_OnClick(object sender, RoutedEventArgs e)
        {
            _giveTextBoxFocusUponFlyoutClosing = true;
        }

        private void Reset_OnClick(object sender, RoutedEventArgs e)
        {
            _textbox.ResetTitle();
        }

        private void Fullscreen_OnClick(object sender, RoutedEventArgs e)
        {
            BoolController zoomContext = _document.GetFieldOrCreateDefault<BoolController>(KeyStore.PresContextZoomKey);
            zoomContext.Data = !zoomContext.Data;
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
            return ((a.Y - b.Y) * (a.Y - b.Y) + (a.X - b.X) * (a.X - b.X));
        }

        private List<Point> GetFourBeizerPoints(int i)
        {
            var canvas = MainPage.Instance.xCanvas;
            var docs = xPinnedNodesListView.Items;

            //use bounds to find closest sides on each neighboring doc
            //get midpoitns of every side of both docs
            var docA = ((PresentationItemViewModel) docs[i]).Document;
            var docAPos = docA.GetField<PointController>(KeyStore.PositionFieldKey).Data;
            var docASize = docA.GetField<PointController>(KeyStore.ActualSizeKey).Data;
            var docAsides = new List<Tuple<Point, Point>>();
            double docAy = docAPos.Y + docASize.Y / 2;
            double docAx = docAPos.X + docASize.X / 2;
            double docALeft = docAPos.X;
            double docARight = docAPos.X + docASize.X;
            double docATop = docAPos.Y;
            double docABottom = docAPos.Y + docASize.Y;

            var docB = ((PresentationItemViewModel) docs[i + 1]).Document;
            var docBPos = docB.GetField<PointController>(KeyStore.PositionFieldKey).Data;
            var docBSize = docB.GetField<PointController>(KeyStore.ActualSizeKey).Data;
            var docBsides = new List<Tuple<Point, Point>>();
            double docBy = docBPos.Y + docBSize.Y / 2;
            double docBx = docBPos.X + docBSize.X / 2;
            double docBLeft = docBPos.X;
            double docBRight = docBPos.X + docBSize.X;
            double docBTop = docBPos.Y;
            double docBBottom = docBPos.Y + docBSize.Y;

            double offset = Math.Sqrt(distSqr(new Point(docAx, docAy), new Point(docBx, docBy))) / 10;

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

            double minDist = Double.PositiveInfinity;
            Point startPoint;
            Point endPoint;
            Point startControlPt;
            Point endControlPt;

            //get closest two sides between docs
            foreach (var aside in docAsides)
            {
                foreach (var bside in docBsides)
                {
                    double dist = distSqr(aside.Item1, bside.Item1);
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
            var docViewA = MainPage.Instance.MainSplitter.GetFirstDescendantOfType<CollectionFreeformView>().GetTransformedCanvas();
            var docViewB = MainPage.Instance.MainSplitter.GetFirstDescendantOfType<CollectionFreeformView>().GetTransformedCanvas();
            var allCollections = MainPage.Instance.MainSplitter.GetDescendantsOfType<CollectionFreeformView>().Reverse();
            foreach (var col in allCollections)
            {
                foreach (var doc in col.GetImmediateDescendantsOfType<DocumentView>())
                {
                    if (Equals(docs[i], doc.ViewModel.DocumentController))
                    {
                        docViewA = col.GetTransformedCanvas();
                    }

                    if (Equals(docs[i + 1], doc.ViewModel.DocumentController))
                    {
                        docViewB = col.GetTransformedCanvas();
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
            if (CurrPresViewState == PresentationViewState.Collapsed) return;

            //if pins changed, updating won't work
            if (_paths.Count / 2 != xPinnedNodesListView.Items.Count - 1)
            {
                DrawLines();
            }

            //draw lines between members of presentation 
            var docs = xPinnedNodesListView.Items;

            for (int i = 0; i < docs?.Count - 1; i++)
            {
                var points = GetFourBeizerPoints(i);
                Point startPoint = points[0];
                Point endPoint = points[1];
                Point startControlPt = points[2];
                Point endControlPt = points[3];

                //create nest of elements to show segment
                var segment =
                    new BezierSegment()
                    {
                        Point1 = startControlPt,
                        Point2 = endControlPt,
                        Point3 = endPoint
                    };

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
                double diffX = endControlPt.X - endPoint.X;
                double diffY = endControlPt.Y - endPoint.Y;
                Point arrowPtA;
                Point arrowPtB;
                int arrowWidth = 10;
                if (Math.Abs(diffX) > Math.Abs(diffY))
                {
                    double sign = diffX / Math.Abs(diffX);
                    //arrow should come from x direction
                    arrowPtA = new Point(endPoint.X + sign * arrowWidth, endPoint.Y + arrowWidth);
                    arrowPtB = new Point(endPoint.X + sign * arrowWidth, endPoint.Y - arrowWidth);
                }
                else
                {
                    double sign = diffY / Math.Abs(diffY);
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
            if (CurrPresViewState == PresentationViewState.Collapsed) return;

            var canvas = MainPage.Instance.xCanvas;
            //only recalcualte if you need to 

            RemoveLines();
            _paths = new List<UIElement>();

            //draw lines between members of presentation 
            var docs = xPinnedNodesListView.Items;

            for (int i = 0; i < docs?.Count - 1; i++)
            {
                var points = GetFourBeizerPoints(i);
                Point startPoint = points[0];
                Point endPoint = points[1];
                Point startControlPt = points[2];
                Point endControlPt = points[3];

                //create nest of elements to show segment
                var segment =
                    new BezierSegment()
                    {
                        Point1 = startControlPt,
                        Point2 = endControlPt,
                        Point3 = endPoint
                    };
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
                double diffX = endControlPt.X - endPoint.X;
                double diffY = endControlPt.Y - endPoint.Y;
                Point arrowPtA;
                Point arrowPtB;
                int arrowWidth = 10;
                if (Math.Abs(diffX) > Math.Abs(diffY))
                {
                    double sign = diffX / Math.Abs(diffX);
                    //arrow should come from x direction
                    arrowPtA = new Point(endPoint.X + sign * arrowWidth, endPoint.Y + arrowWidth);
                    arrowPtB = new Point(endPoint.X + sign * arrowWidth, endPoint.Y - arrowWidth);
                }
                else
                {
                    double sign = diffY / Math.Abs(diffY);
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

        private void DocFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            UpdatePaths();
        }

        public void DrawLinesWithNewDocs()
        {
            bool? isChecked = xShowLinesButton.IsChecked;
            if (isChecked != null && (bool)!isChecked ||
                CurrPresViewState == PresentationViewState.Collapsed) return;

            //show lines
            foreach (var viewModelPinnedNode in ViewModel.PinnedNodes)
            {
                viewModelPinnedNode.Document.RemoveFieldUpdatedListener(KeyStore.PositionFieldKey, DocFieldUpdated);
                viewModelPinnedNode.Document.RemoveFieldUpdatedListener(KeyStore.ActualSizeKey, DocFieldUpdated);
            }

            foreach (var viewModelPinnedNode in ViewModel.PinnedNodes)
            {
                viewModelPinnedNode.Document.AddFieldUpdatedListener(KeyStore.PositionFieldKey, DocFieldUpdated);
                viewModelPinnedNode.Document.AddFieldUpdatedListener(KeyStore.ActualSizeKey, DocFieldUpdated);
            }

            DrawLines();
        }

        private void ShowLinesButton_OnChecked(object sender, RoutedEventArgs e)
        {
            ShowLines();
        }

        public void ShowLines()
        {
            //show lines
            var allCollections = MainPage.Instance.MainSplitter.GetDescendantsOfType<CollectionFreeformView>();
            //xShowLinesButton.Background = new SolidColorBrush(Colors.LightGray);

            DrawLines();

            foreach (var viewModelPinnedNode in ViewModel.PinnedNodes)
            {
                viewModelPinnedNode.Document.AddFieldUpdatedListener(KeyStore.PositionFieldKey, DocFieldUpdated);
                viewModelPinnedNode.Document.AddFieldUpdatedListener(KeyStore.ActualSizeKey, DocFieldUpdated);
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
            var allCollections = MainPage.Instance.MainSplitter.GetDescendantsOfType<CollectionFreeformView>();
            //xShowLinesButton.Background = new SolidColorBrush(Colors.White);

            //remove all paths
            RemoveLines();

            foreach (var viewModelPinnedNode in ViewModel.PinnedNodes)
            {
                viewModelPinnedNode.Document.RemoveFieldUpdatedListener(KeyStore.PositionFieldKey, DocFieldUpdated);
                viewModelPinnedNode.Document.RemoveFieldUpdatedListener(KeyStore.ActualSizeKey, DocFieldUpdated);
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
            SetPresentationState(false);
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

            int ind = 0;
            foreach (var viewDoc in ViewModel?.PinnedNodes)
            {
                if (viewDoc.Document == viewRefDoc) break;
                ind++;
            }

            if (ind == ViewModel.PinnedNodes.Count) return;

        }

        public void ClearHighlightedMatch()
        {
        }

        internal void SimulateAnimation(bool expand)
        {
            if (expand)
            {
                xTransportControls.Height = 60;
                xPinnedNodesListView.Opacity = 1;
                xSettingsPanel.Opacity = 1;
            }
            else
            {
                xTransportControls.Height = 0;
                xPinnedNodesListView.Opacity = 0;
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

        //opens settings menu to change the working presentation
        private void XSettingsGrid_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //fixes bug with combobox not updating
            xPresentations.DisplayMemberPath = "Title";

            xSettingsFlyout.ShowAt(xSettingsIcon);
        }

        private void XPresentations_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //update CurrPres accordingly
            ViewModel.PinnedNodes.CollectionChanged -= PinnedNodes_CollectionChanged;
            ViewModel.CurrPres.GetDataDocument().SetField<ListController<DocumentController>>(KeyStore.DataKey, ViewModel.PinnedNodes.Select(pn => pn.Document), true);
            ViewModel.CurrPres = (xPresentations.SelectedItem as DocumentController);
            if (ViewModel?.CurrPres != null) xTitle.Text = ViewModel.CurrPres.Title;
            ViewModel.PinnedNodes.CollectionChanged += PinnedNodes_CollectionChanged;
        }

        /// <summary>
        /// Create new pres & set it to be the current presentation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XNewPresButton_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.SetCurrentPresentation(ViewModel.MakeNewPres());
            xTitle.Text = ViewModel.CurrPres.Title;
        }

        /// <summary>
        /// Typing into the title box should change the name of the current presentation to the string entered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XTitleBox_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ViewModel.RenamePres(ViewModel.CurrPres, xTitle.Text);

        }

        private void XDropGrid_OnDragEnter(object sender, DragEventArgs e)
        {
            var dragModel = e.DataView.GetDragModel();
            if (!(dragModel is DragDocumentModel ddm))
            {
                e.AcceptedOperation = DataPackageOperation.None;
                return;
            }

            if (ddm.DraggedDocuments.Count == 0 || (ddm.DraggedDocuments.Count == 1 && ddm.DraggedDocuments[0].GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null) == null))
            {
                e.AcceptedOperation = DataPackageOperation.None;
                return;
            }

            XDropGrid.Visibility = Visibility.Visible;
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private void XDropGrid_OnDragLeave(object sender, DragEventArgs e)
        {
            XDropGrid.Visibility = Visibility.Collapsed;
        }

        private void XDropGrid_OnDrop(object sender, DragEventArgs e)
        {
            XDropGrid.Visibility = Visibility.Collapsed;
            if (!(e.DataView.GetDragModel() is DragDocumentModel dragModel)) return;

            IEnumerable<DocumentController> docs = null;

            if (dragModel.DraggedDocuments.Count == 1 && dragModel.DraggedDocuments[0].GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null) is var list)
            {
                docs = list;
            }
            else if (dragModel.DraggedDocuments.Count > 1)
            {
                docs = dragModel.DraggedDocuments;
            }

            if (docs != null)
            {
                foreach (var documentController in docs)
                {
                    ViewModel.AddToPinnedNodesCollection(documentController);
                }
            }
        }

        private void XDeletePresentationButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.DeletePresentation(ViewModel.CurrPres);
        }

        private void ViewChecked(object sender, RoutedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel) ((FrameworkElement) sender).DataContext;
            itemViewModel?.Document.SetField<BoolController>(KeyStore.PresentationVisibleKey, true, true);
        }

        private void ViewUnchecked(object sender, RoutedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel) ((FrameworkElement) sender).DataContext;
            itemViewModel?.Document.SetField<BoolController>(KeyStore.PresentationVisibleKey, false, true);
        }

        private void FadeChecked(object sender, RoutedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel) ((FrameworkElement) sender).DataContext;
            itemViewModel?.Document.SetField<BoolController>(KeyStore.PresentationFadeKey, true, true);
        }

        private void FadeUnchecked(object sender, RoutedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel) ((FrameworkElement) sender).DataContext;
            itemViewModel?.Document.SetField<BoolController>(KeyStore.PresentationFadeKey, false, true);
        }

        private void GroupUnchecked(object sender, RoutedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel) ((FrameworkElement) sender).DataContext;
            itemViewModel?.Document.SetField<BoolController>(KeyStore.PresentationGroupUpKey, false, true);
        }

        private void GroupChecked(object sender, RoutedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel) ((FrameworkElement) sender).DataContext;
            itemViewModel?.Document.SetField<BoolController>(KeyStore.PresentationGroupUpKey, true, true);
        }

        private void NavigateChecked(object sender, RoutedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel) ((FrameworkElement) sender).DataContext;
            itemViewModel?.Document.SetField<BoolController>(KeyStore.PresentationNavigateKey, true, true);
        }

        private void NavigateUnchecked(object sender, RoutedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel) ((FrameworkElement) sender).DataContext;
            itemViewModel?.Document.SetField<BoolController>(KeyStore.PresentationNavigateKey, false, true);
        }

        private void HideChecked(object sender, RoutedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel) ((FrameworkElement) sender).DataContext;
            itemViewModel?.Document.SetField<BoolController>(KeyStore.PresentationHideKey, true, true);
        }

        private void HideUnchecked(object sender, RoutedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel)((FrameworkElement)sender).DataContext;
            itemViewModel?.Document.SetField<BoolController>(KeyStore.PresentationHideKey, false, true);
        }

        private void ContextUnchecked(object sender, RoutedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel)((FrameworkElement)sender).DataContext;
            itemViewModel?.Document.SetField<BoolController>(KeyStore.PresContextZoomKey, true, true);
        }

        private void ContextChecked(object sender, RoutedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel)((FrameworkElement)sender).DataContext;
            itemViewModel?.Document.SetField<BoolController>(KeyStore.PresContextZoomKey, false, true);
        }

        private void NavigateLoaded(object sender, DataContextChangedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel)((FrameworkElement)sender).DataContext;
            ((ToggleButton)sender).IsChecked = itemViewModel?.Document?.GetField<BoolController>(KeyStore.PresentationNavigateKey)?.Data ?? false;
        }

        private void ViewLoaded(object sender, DataContextChangedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel)((FrameworkElement)sender).DataContext;
            ((ToggleButton)sender).IsChecked = itemViewModel?.Document?.GetField<BoolController>(KeyStore.PresentationVisibleKey)?.Data ?? false;
        }

        private void GroupLoaded(object sender, DataContextChangedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel)((FrameworkElement)sender).DataContext;
            ((ToggleButton)sender).IsChecked = itemViewModel?.Document?.GetField<BoolController>(KeyStore.PresentationGroupUpKey)?.Data ?? false;
        }

        private void HideLoaded(object sender, DataContextChangedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel)((FrameworkElement)sender).DataContext;
            ((ToggleButton)sender).IsChecked = itemViewModel?.Document?.GetField<BoolController>(KeyStore.PresentationHideKey)?.Data ?? false;
        }

        private void FadeLoaded(object sender, DataContextChangedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel)((FrameworkElement)sender).DataContext;
            ((ToggleButton)sender).IsChecked = itemViewModel?.Document?.GetField<BoolController>(KeyStore.PresentationFadeKey)?.Data ?? false;
        }

        private void ContextLoaded(object sender, DataContextChangedEventArgs e)
        {
            var itemViewModel = (PresentationItemViewModel)((FrameworkElement)sender).DataContext;
            ((ToggleButton)sender).IsChecked = !(itemViewModel?.Document?.GetField<BoolController>(KeyStore.PresContextZoomKey)?.Data ?? true);
        }
    }
}
