using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using MyToolkit.UI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class SplitFrame : UserControl
    {

        public DocumentView Document => XDocView;

        public static event Action<SplitFrame> ActiveDocumentChanged;

        private static SplitFrame _activeFrame;
        public static SplitFrame ActiveFrame
        {
            get => _activeFrame;
            set
            {
                if (_activeFrame == value)
                {
                    return;
                }
                _activeFrame?.SetActive(false);
                _activeFrame = value;
                _activeFrame.SetActive(true);

                OnActiveDocumentChanged(_activeFrame);

                MainPage.Instance.xDocPathView.Document = (_activeFrame.DataContext as DocumentViewModel)?.LayoutDocument;
            }
        }

        public DocumentController OpenDocument(DocumentController doc)
        {
            if (doc.GetDataDocument().Equals(ViewModel?.DataDocument))
            {
                return ViewModel.DocumentController;
            }

            if (doc.DocumentType.Equals(CollectionBox.DocumentType))
            {
                doc = !this.IsShiftPressed() ?  doc.GetViewCopy() : doc;  // bcz: think about this some more.... causes problems when trying to view the same collection twice or because of setting parameters like FitToParent
                doc.SetFitToParent(false);
                var openViewType = doc.GetDereferencedField<TextController>(KeyStore.CollectionOpenViewTypeKey, null)?.Data;
                if (openViewType != null)
                {
                    doc.SetField<TextController>(KeyStore.CollectionViewTypeKey, openViewType, true);
                }
            }

            DataContext = new DocumentViewModel(doc) { Undecorated = true, IsDimensionless = true, ResizersVisible = false };
            
            return doc;
        }

        public DocumentController OpenDocument(DocumentController document, DocumentController workspace)
        {
            Debug.Assert(workspace.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null)?.Contains(document) ?? false);

            if (ViewModel.DataDocument.Equals(workspace.GetDataDocument())) //Collection is already open, so we need to animate to it
            {
                AnimateToDocument(document);
                return ViewModel.DocumentController;
            }
            else
            {
                var center = document.GetPosition() ?? new Point();
                var size = document.GetActualSize() ?? new Point();
                center.X += (size.X - ActualWidth) / 2;
                center.Y += (size.Y - ActualHeight) / 2;
                center.X = -center.X;
                center.Y = -center.Y;
                workspace = OpenDocument(workspace);
                workspace.SetField<PointController>(KeyStore.PanPositionKey, center, true);
                workspace.SetField<PointController>(KeyStore.PanZoomKey, new Point(1, 1), true);
                return workspace;
            }
        }

        public DocumentViewModel ViewModel => DataContext as DocumentViewModel;

        private enum DragSplitMode
        {
            VerticalCollapsePrevious, VerticalCollapseNext,
            HorizontalCollapsePrevious, HorizontalCollapseNext,
            None
        }
        private DragSplitMode CurrentSplitMode { get; set; } = DragSplitMode.None;

        public DocumentController DocumentController => ViewModel?.DocumentController;

        public SplitFrame()
        {
            InitializeComponent();

            if (ActiveFrame == null)
            {
                ActiveFrame = this;
            }

            _pointerMoved = UserControl_PointerMoved;
            _draggedOver = UserControl_DragOver;
            AddHandler(DragOverEvent, _draggedOver, true);
            AddHandler(TappedEvent, new TappedEventHandler((sender, args) => ActiveFrame = this), true);
        }

        private static SolidColorBrush InactiveBrush { get; } = new SolidColorBrush(Colors.Black);
        private static SolidColorBrush ActiveBrush { get; } = new SolidColorBrush(Colors.LightBlue);

        private void SetActive(bool active)
        {
            XTopRightResizer.Fill = active ? ActiveBrush : InactiveBrush;
            XBottomLeftResizer.Fill = active ? ActiveBrush : InactiveBrush;
        }

        public DocumentController Split(SplitDirection dir, DocumentController doc = null, bool autosize = false)
        {
            if (dir == SplitDirection.InPlace)
            {
                return OpenDocument(doc ?? DocumentController);
            }

            if (doc == null && this.IsCtrlPressed())
            {
                doc = DocumentController;
            }
            return this.GetFirstAncestorOfTypeFast<SplitManager>()?.Split(this, dir, doc, autosize);
        }

        private void TopRightOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            var x = e.Cumulative.Translation.X;
            var y = e.Cumulative.Translation.Y;
            var angle = Math.Atan2(y, x);
            angle = angle * 180 / Math.PI;
            if (angle > 135 || angle < -150)
            {
                Split(SplitDirection.Right);
                var pane = (SplitPane) Parent;
                var currentDef = SplitPane.GetSplitLocation(this);
                var index = currentDef.Parent.Children.IndexOf(currentDef);
                var nextDef = currentDef.Parent.Children[index + 1];
                _manipulationDeltaHandler = (o, args) => pane.ResizeSplits(currentDef, nextDef, args.Delta.Translation);
                XTopRightResizer.ManipulationDelta += _manipulationDeltaHandler;
            }
            else if (angle <= 135 && angle > 60)
            {
                Split(SplitDirection.Up);
                var pane = (SplitPane) Parent;
                var currentDef = SplitPane.GetSplitLocation(this);
                var index = currentDef.Parent.Children.IndexOf(currentDef);
                var prevDef = currentDef.Parent.Children[index - 1];
                _manipulationDeltaHandler = (o, args) => pane.ResizeSplits(prevDef, currentDef, args.Delta.Translation);
                XTopRightResizer.ManipulationDelta += _manipulationDeltaHandler;
            }
            else if (angle <= 60 && angle > -45)
            {
                CurrentSplitMode = DragSplitMode.HorizontalCollapseNext;
            }
            else
            {
                CurrentSplitMode = DragSplitMode.VerticalCollapsePrevious;
            }

        }

        private void BottomLeftOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            var x = e.Cumulative.Translation.X;
            var y = e.Cumulative.Translation.Y;
            var angle = Math.Atan2(y, x);
            angle = angle * 180 / Math.PI;
            if (angle < 30 && angle > -45)
            {
                Split(SplitDirection.Left);

                var pane = (SplitPane) Parent;
                var currentDef = SplitPane.GetSplitLocation(this);
                var index = currentDef.Parent.Children.IndexOf(currentDef);
                var prevDef = currentDef.Parent.Children[index - 1];
                _manipulationDeltaHandler = (o, args) => pane.ResizeSplits(prevDef, currentDef, args.Delta.Translation);
                XBottomLeftResizer.ManipulationDelta += _manipulationDeltaHandler;
            }
            else if (angle <= -45 && angle > -120)
            {
                Split(SplitDirection.Down);

                var pane = (SplitPane) Parent;
                var currentDef = SplitPane.GetSplitLocation(this);
                var index = currentDef.Parent.Children.IndexOf(currentDef);
                var nextDef = currentDef.Parent.Children[index + 1];
                _manipulationDeltaHandler = (o, args) => pane.ResizeSplits(currentDef, nextDef, args.Delta.Translation);
                XBottomLeftResizer.ManipulationDelta += _manipulationDeltaHandler;
            }
            else if (angle <= -120 || angle > 135)
            {
                CurrentSplitMode = DragSplitMode.HorizontalCollapsePrevious;
            }
            else
            {
                CurrentSplitMode = DragSplitMode.VerticalCollapseNext;
            }
        }

        private ManipulationDeltaEventHandler _manipulationDeltaHandler;

        private void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (_manipulationDeltaHandler != null)
            {
                XTopRightResizer.ManipulationDelta -= _manipulationDeltaHandler;
                XBottomLeftResizer.ManipulationDelta -= _manipulationDeltaHandler;
            }

            if (CurrentSplitMode == DragSplitMode.None)
            {
                return;
            }
            var parentManager = this.GetFirstAncestorOfTypeFast<SplitManager>();
            var splitDef = SplitPane.GetSplitLocation(this);
            switch (CurrentSplitMode)
            {
            case DragSplitMode.VerticalCollapsePrevious:
            case DragSplitMode.HorizontalCollapsePrevious:
                {
                    bool vertical = CurrentSplitMode == DragSplitMode.VerticalCollapsePrevious;
                    if (splitDef.Parent != null &&
                       (vertical && splitDef.Parent.Mode == SplitMode.Vertical ||
                        !vertical && splitDef.Parent.Mode == SplitMode.Horizontal))
                    {
                        bool previous = (vertical ? e.Cumulative.Translation.Y : e.Cumulative.Translation.X) < 0;
                        if (previous)
                        {
                            var index = splitDef.Parent.Children.IndexOf(splitDef);
                            var neighborSplit = splitDef.Parent.Children[index - 1];
                            parentManager.Delete(neighborSplit, SplitDefinition.JoinOption.JoinNext);
                        }
                        else
                        {
                            parentManager.Delete(splitDef, SplitDefinition.JoinOption.JoinPrevious);
                        }
                    }

                    break;
                }
            case DragSplitMode.VerticalCollapseNext:
            case DragSplitMode.HorizontalCollapseNext:
                {
                    bool vertical = CurrentSplitMode == DragSplitMode.VerticalCollapseNext;
                    if (splitDef.Parent != null &&
                       (vertical && splitDef.Parent.Mode == SplitMode.Vertical ||
                        !vertical && splitDef.Parent.Mode == SplitMode.Horizontal))
                    {
                        bool previous = (vertical ? e.Cumulative.Translation.Y : e.Cumulative.Translation.X) < 0;
                        if (previous)
                        {
                            parentManager.Delete(this, SplitDefinition.JoinOption.JoinNext);
                        }
                        else
                        {
                            var index = splitDef.Parent.Children.IndexOf(splitDef);
                            var neighborSplit = splitDef.Parent.Children[index + 1];
                            parentManager.Delete(neighborSplit, SplitDefinition.JoinOption.JoinPrevious);
                        }
                    }

                    break;
                }
            }

            CurrentSplitMode = DragSplitMode.None;
        }

        public void Delete()
        {
            var parent = this.GetFirstAncestorOfType<SplitManager>();
            parent?.Delete(this);
        }

        private SolidColorBrush Yellow = new SolidColorBrush(Color.FromArgb(127, 255, 215, 0));
        private SolidColorBrush Transparent = new SolidColorBrush(Colors.Transparent);

        private void DropTarget_OnDragEnter(object sender, DragEventArgs e)
        {
            (sender as Shape).Fill = Yellow;
            if (e.DataView.HasDataOfType(DataTransferTypeInfo.Any))
            {
                e.AcceptedOperation = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }

            e.Handled = true;
        }

        private void DropTarget_OnDragLeave(object sender, DragEventArgs e)
        {
            (sender as Shape).Fill = new SolidColorBrush(Color.FromArgb(0x10, 0x10, 0x10, 0x10));
            e.Handled = true;
        }

        private async Task DropHandler(DragEventArgs e, SplitDirection dir)
        {
            e.Handled = true;
            using (UndoManager.GetBatchHandle())
            {
                var docsToAdd = await e.DataView.GetDroppableDocumentsForDataOfType(DataTransferTypeInfo.Any, XDocView, new Point());
                if (docsToAdd.Count != 0)
                {
                    var fromFileSystem = e.DataView.Contains(StandardDataFormats.StorageItems);

                    var dragModel        = e.DataView.GetDragModel();
                    var dragDocModel     = dragModel as DragDocumentModel;
                    var internalMove     = !MainPage.Instance.IsShiftPressed() && !MainPage.Instance.IsAltPressed() && !MainPage.Instance.IsCtrlPressed() && !fromFileSystem;
                    var isLinking        = e.AllowedOperations.HasFlag(DataPackageOperation.Link) && internalMove && dragDocModel?.DraggingLinkButton == true;
                    var isMoving         = e.AllowedOperations.HasFlag(DataPackageOperation.Move) && internalMove && dragDocModel?.DraggingLinkButton != true;
                    var isCopying        = e.AllowedOperations.HasFlag(DataPackageOperation.Copy) && (fromFileSystem || MainPage.Instance.IsShiftPressed());
                    var isSettingContext = MainPage.Instance.IsAltPressed() && !fromFileSystem;

                    e.AcceptedOperation = isSettingContext ? DataPackageOperation.None :
                                          isLinking ? DataPackageOperation.Link :
                                          isMoving ? DataPackageOperation.Move :
                                          isCopying ? DataPackageOperation.Copy :
                                          DataPackageOperation.None;

                    var docs = await CollectionViewModel.AddDroppedDocuments(this, docsToAdd, dragModel, isMoving, null);
                    var doc = docs.Count == 1 ? docs[0] :  new CollectionNote(new Point(), CollectionViewType.Freeform, collectedDocuments: docs).Document;

                    Split(dir, doc, true);
                    e.DataView.ReportOperationCompleted(e.AcceptedOperation);
                }
            }
        }

        private async void XRightDropTarget_OnDrop(object sender, DragEventArgs e)
        {
            (sender as Rectangle).Fill = Transparent;
            e.Handled = true;
            await DropHandler(e, SplitDirection.Right);
        }

        private async void XLeftDropTarget_OnDrop(object sender, DragEventArgs e)
        {
            (sender as Rectangle).Fill = Transparent;
            e.Handled = true;
            await DropHandler(e, SplitDirection.Left);
        }

        private async void XBottomDropTarget_OnDrop(object sender, DragEventArgs e)
        {
            (sender as Rectangle).Fill = Transparent;
            e.Handled = true;
            await DropHandler(e, SplitDirection.Down);
        }

        private async void XTopDropTarget_OnDrop(object sender, DragEventArgs e)
        {
            (sender as Rectangle).Fill = Transparent;
            e.Handled = true;
            await DropHandler(e, SplitDirection.Up);
        }

        private async void XCenterDropTarget_OnDrop(object sender, DragEventArgs e)
        {
            (sender as Shape).Fill = Transparent;
            e.Handled = true;
            await DropHandler(e, SplitDirection.InPlace);
        }

        private List<DocumentController> _history = new List<DocumentController>();
        private List<DocumentController> _future = new List<DocumentController>();

        private DocumentViewModel _oldViewModel;
        private bool _changingView = false;
        private void SplitFrame_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (Equals(ViewModel, _oldViewModel))
            {
                return;
            }

            if (this == ActiveFrame)//If we are the active frame, our document just changed, so the active document changed
            {
                OnActiveDocumentChanged(this);
            }

            if (_changingView)
            {
                _changingView = false;
                _oldViewModel = ViewModel;
                return;
            }

            if (_oldViewModel != null)
            {
                _future.Clear();
                _history.Add(_oldViewModel.DocumentController);
            }

            _oldViewModel = ViewModel;
        }


        public void GoBack()
        {
            if (_history.Any())
            {
                var doc = _history.Last();
                _history.RemoveAt(_history.Count - 1);
                _future.Add(DocumentController);
                _changingView = true;
                DataContext = new DocumentViewModel(doc) { IsDimensionless = true, Undecorated = true, ResizersVisible = false };
            }
        }

        public void GoForward()
        {
            if (_future.Any())
            {
                var doc = _future.Last();
                _future.RemoveAt(_future.Count - 1);
                _history.Add(DocumentController);
                _changingView = true;
                DataContext = new DocumentViewModel(doc) { IsDimensionless = true, Undecorated = true, ResizersVisible = false };
            }
        }

        private static void OnActiveDocumentChanged(SplitFrame frame)
        {
            ActiveDocumentChanged?.Invoke(frame);
            if (frame == ActiveFrame)
            MainPage.Instance.xDocPathView.Document = (frame.DataContext as DocumentViewModel)?.DocumentController;
        }

        private readonly PointerEventHandler _pointerMoved;
        private readonly DragEventHandler    _draggedOver;

        private void UserControl_DragOver(object sender, DragEventArgs e)
        {

            XRightDropTarget.Visibility = Visibility.Visible;
            XLeftDropTarget.Visibility = Visibility.Visible;
            XTopDropTarget.Visibility = Visibility.Visible;
            XBottomDropTarget.Visibility = Visibility.Visible;
            XCenterDropTarget.Visibility = Visibility.Visible;

            this.RemoveHandler(DragOverEvent, _draggedOver);
            this.AddHandler(PointerMovedEvent, _pointerMoved, true);
        }

        private void UserControl_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            XRightDropTarget.Visibility = Visibility.Collapsed;
            XLeftDropTarget.Visibility = Visibility.Collapsed;
            XTopDropTarget.Visibility = Visibility.Collapsed;
            XBottomDropTarget.Visibility = Visibility.Collapsed;
            XCenterDropTarget.Visibility = Visibility.Collapsed;
            this.AddHandler(DragOverEvent, _draggedOver, true);
            this.RemoveHandler(PointerMovedEvent, _pointerMoved);
        }
    }
}
