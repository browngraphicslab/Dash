using Microsoft.ProjectOxford.Vision.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using NewControls.Geometry;
using static Dash.NoteDocuments;

namespace Dash
{

    public class ManipulationDeltaData
    {
        public ManipulationDeltaData(Point position, Point translation, float scale)
        {
            Position = position;
            Translation = translation;
            Scale = scale;
        }

        public Point Position { get; }
        public Point Translation { get; }
        public float Scale { get; }
    }
      


    /// <summary>
    /// Instantiations of this class in a UserControl element will give that
    /// control's selected UIElement the ability to be moved and zoomed based on
    /// interactions with its given handleControl grid.
    /// </summary>
    public class ManipulationControls : IDisposable
    {
        public double MinScale { get; set; } = .2;
        public double MaxScale { get; set; } = 5.0;
        private bool _disabled;
        private FrameworkElement _element;
        private readonly bool _doesRespondToManipulationDelta;
        private readonly bool _doesRespondToPointerWheel;
        private bool _handle;
        public double ElementScale = 1.0;


        public delegate void OnManipulatorTranslatedHandler(TransformGroupData transformationDelta);
        public event OnManipulatorTranslatedHandler OnManipulatorTranslatedOrScaled;

        public PointerDeviceType BlockedInputType;
        public bool FilterInput;
        private bool _processManipulation;
        private bool _isManipulating;

        /// <summary>
        /// Ensure pointerwheel only changes size of documents when it's selected 
        /// </summary>
        private bool PointerWheelEnabled
        {
            get
            {
                // if we're on the lowest selecting document view then we can resize it with pointer wheel
                var docView = _element as DocumentView;
                if (docView != null) return docView.IsLowestSelected;

                /*
                var colView = _element as CollectionFreeformView;

                // hack to see if we're in the interface builder or in the compound operator editor
                // these are outside of the normal selection hierarchy so we always return true
                if (colView?.ViewModel is SimpleCollectionViewModel) return true;

                // if the collection view is a free form view, or it is the lowest
                // selected element then use the pointer
                return colView != null || colView.IsLowestSelected;*/
                return _element is CollectionFreeformView;
            }
        }

        /// <summary>
        /// Created a manipulation control to move element
        /// NOTE: bounds checking is done relative to element.Parent so the element must be in an element with the proper size for bounds checking
        /// </summary>
        /// <param name="element">The element to add manipulation to</param>
        /// <param name="doesRespondToManipulationDelta"></param>
        /// <param name="doesRespondToPointerWheel"></param>
        /// <param name="borderRegions"></param>
        public ManipulationControls(FrameworkElement element, bool doesRespondToManipulationDelta, bool doesRespondToPointerWheel, List<FrameworkElement> borderRegions=null)
        {
            _element = element;
            _doesRespondToManipulationDelta = doesRespondToManipulationDelta;
            _doesRespondToPointerWheel = doesRespondToPointerWheel;
            _processManipulation = true;

            if (_doesRespondToManipulationDelta)
            {
                element.ManipulationDelta += ElementOnManipulationDelta;
            }
            if (_doesRespondToPointerWheel)
            {
                element.PointerWheelChanged += ElementOnPointerWheelChanged;
            }
            if (borderRegions != null)
            {
                foreach (var borderRegion in borderRegions)
                {
                    borderRegion.ManipulationMode = ManipulationModes.All;
                    borderRegion.ManipulationDelta += BorderManipulateDeltaMove;
                    borderRegion.ManipulationStarted += ElementOnManipulationStarted;
                    borderRegion.AddHandler(UIElement.ManipulationCompletedEvent, new ManipulationCompletedEventHandler(BorderOnManipulationCompleted), true);
                }
            }
            element.ManipulationMode = ManipulationModes.All;
            element.ManipulationStarted += ElementOnManipulationStarted;
            element.ManipulationInertiaStarting += ElementOnManipulationInertiaStarting;
            element.AddHandler(UIElement.ManipulationCompletedEvent, new ManipulationCompletedEventHandler(ElementOnManipulationCompleted), true);
        }

        private void ElementOnManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 0.02;
        }

        #region Snapping Layouts


        /// <summary>
        /// Enum used for snapping.
        /// TODO: Move this to the top of the class definition.
        /// </summary>
        private enum Side
        {
            Top = 1,
            Bottom = ~Top,
            Left = 2,
            Right = ~Left,
        };

        /// <summary>
        /// TODO: Move this to the top of the class definition.
        /// </summary>
        private const double ALIGNING_RECTANGLE_SENSITIVITY = 15.0;

        /// <summary>
        /// TODO: Move this to the top of the class definition.
        /// </summary>
        private const double ALIGNMENT_THRESHOLD = .2;


        /// <summary>
        /// Previews the new location/position of the element
        /// </summary>
        public void Snap(bool preview)
        {
            var docRoot = _element.GetFirstAncestorOfType<DocumentView>();
            var parent = _element.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;

            if (parent == null || _element.Equals(parent))
            {
                return;
            }

            MainPage.Instance.TemporaryRectangle.Width = MainPage.Instance.TemporaryRectangle.Height = 0;

            //var currentBoundingBox = GetBoundingBox(docRoot);
            var currentBoundingBox = docRoot.ViewModel.GroupingBounds;
            var closest = GetClosestDocumentView(currentBoundingBox);
            if (preview)
                PreviewSnap(currentBoundingBox, closest);
            else
                SnapToDocumentView(docRoot, closest);
        }

        /// <summary>
        /// Snaps location of this DocumentView to the DocumentView passed in, also inheriting its width or height dimensions.
        /// </summary>
        /// <param name="closestDocumentView"></param>
        private void SnapToDocumentView(DocumentView currrentDoc, Tuple<DocumentView, Side, double> closestDocumentView)
        {
            if (closestDocumentView == null)
            {
                return;
            }

            var documentView = closestDocumentView.Item1;
            var side = closestDocumentView.Item2;
            var currentScaleAmount = currrentDoc.ViewModel.GroupTransform.ScaleAmount;

            var topLeftPoint = new Point(documentView.ViewModel.GroupTransform.Translate.X,
                documentView.ViewModel.GroupTransform.Translate.Y);
            var bottomRightPoint = new Point(documentView.ViewModel.GroupTransform.Translate.X + documentView.ActualWidth,
                documentView.ViewModel.GroupTransform.Translate.Y + documentView.ActualHeight);

            var newBoundingBox = CalculateAligningRectangleForSide(~side, topLeftPoint, bottomRightPoint, currrentDoc.ActualWidth, currrentDoc.ActualHeight);

            var translate = new Point(newBoundingBox.X, newBoundingBox.Y);

            currrentDoc.ViewModel.GroupTransform = new TransformGroupData(translate, new Point(0, 0), currentScaleAmount);

            currrentDoc.ViewModel.Width = newBoundingBox.Width;
            currrentDoc.ViewModel.Height = newBoundingBox.Height;
        }


        /// <summary>
        /// Places the TemporaryRectangle in the location where the document view being manipulation would be dragged.
        /// </summary>
        /// <param name="currentBoundingBox"></param>
        /// <param name="closestDocumentView"></param>
        private void PreviewSnap(Rect currentBoundingBox, Tuple<DocumentView, Side, double> closestDocumentView)
        {
            if (closestDocumentView == null) return;

            var docRoot = _element.GetFirstAncestorOfType<DocumentView>();

            var parent = _element.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;

            var documentView = closestDocumentView.Item1;
            var side = closestDocumentView.Item2;

            var closestBoundsInCollectionSpace = documentView.ViewModel.GroupingBounds;
            var boundingBoxCollectionSpace = CalculateAligningRectangleForSide(~side, closestBoundsInCollectionSpace, currentBoundingBox.Width, currentBoundingBox.Height);

            //Transform the rect from xCollectionCanvas (which is equivalent to xItemsControl.ItemsPanelRoot) space to screen space
            var boundingBoxScreenSpace = Util.RectTransformFromVisual(boundingBoxCollectionSpace, parent?.xItemsControl.ItemsPanelRoot);
            MainPage.Instance.TemporaryRectangle.Width = boundingBoxScreenSpace.Width;
            MainPage.Instance.TemporaryRectangle.Height = boundingBoxScreenSpace.Height;

            Canvas.SetLeft(MainPage.Instance.TemporaryRectangle, boundingBoxScreenSpace.X);
            Canvas.SetTop(MainPage.Instance.TemporaryRectangle, boundingBoxScreenSpace.Y);
        }


        private Tuple<DocumentView, Side, double> GetClosestDocumentView(Rect currentBoundingBox)
        {
            //List of all DocumentViews hit, along with a double representing how close they are
            var allDocumentViewsHit = HitTestFromSides(currentBoundingBox);

            //Return closest DocumentView (using the double that represents the confidence)
            return allDocumentViewsHit.FirstOrDefault(item => item.Item3 == allDocumentViewsHit.Max(i2 => i2.Item3)); //Sadly no better argmax one-liner 
        }

        /// <summary>
        /// Returns a list of DocumentViews hit by the side, as well as a double representing how close they are
        /// </summary>
        /// <param name="side"></param>
        /// <param name="topLeftScreenPoint"></param>
        /// <param name="bottomRightScreenPoint"></param>
        /// <returns></returns>
        private List<Tuple<DocumentView, Side, double>> HitTestFromSides(Rect currentBoundingBox)
        {

            var documentViewsAboveThreshold = new List<Tuple<DocumentView, Side, double>>();
            var parent = _element.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;
            Debug.Assert(parent != null);

            var docRoot = _element.GetFirstAncestorOfType<DocumentView>();

            var listOfSiblings = parent.DocumentViews.Where(docView => docView != docRoot);
            Side[] sides = { Side.Top, Side.Bottom, Side.Left, Side.Right };
           
            foreach (var side in sides)
            {
                var sideRect = CalculateAligningRectangleForSide(side, currentBoundingBox, ALIGNING_RECTANGLE_SENSITIVITY, ALIGNING_RECTANGLE_SENSITIVITY);
                foreach (var sibling in listOfSiblings)
                {
                    Rect intersection = sideRect;
                    intersection.Intersect(sibling.ViewModel.GroupingBounds); //Mutates intersection

                    var confidence = CalculateSnappingConfidence(side, sideRect, sibling);
                    if (!intersection.IsEmpty && confidence >= ALIGNMENT_THRESHOLD)
                    {
                        documentViewsAboveThreshold.Add(new Tuple<DocumentView, Side, double>(sibling, side, confidence));
                    }
                }
            }
            
            return documentViewsAboveThreshold;
        }

        private double CalculateSnappingConfidence(Side side, Rect hitTestRect, DocumentView otherDocumentView)
        {
            Rect otherDocumentViewBoundingBox = otherDocumentView.ViewModel.GroupingBounds; // otherDocumentView.GetBoundingBoxScreenSpace();

            var midX = hitTestRect.X + hitTestRect.Width / 2;
            var midY = hitTestRect.Y + hitTestRect.Height / 2;

            double distanceToMid = -1;

            //Get normalized x or y distance from the complementary edge of the other DocumentView and the midpoint of the hitTestRect
            switch (side)
            {
                case Side.Top:
                    distanceToMid = Math.Abs(midY - (otherDocumentViewBoundingBox.Y + otherDocumentViewBoundingBox.Height));
                    distanceToMid = 1.0f - Math.Min(1.0, distanceToMid / hitTestRect.Height);
                    return distanceToMid * GetSharedRectWidthProportion(hitTestRect, otherDocumentViewBoundingBox);
                case Side.Bottom:
                    distanceToMid = Math.Abs(otherDocumentViewBoundingBox.Y - midY);
                    distanceToMid = 1.0f - Math.Min(1.0, distanceToMid / hitTestRect.Height);
                    return distanceToMid * GetSharedRectWidthProportion(hitTestRect, otherDocumentViewBoundingBox);
                case Side.Left:
                    distanceToMid = Math.Abs(midX - (otherDocumentViewBoundingBox.X + otherDocumentViewBoundingBox.Width));
                    distanceToMid = 1.0f - Math.Min(1.0, distanceToMid / hitTestRect.Width);
                    return distanceToMid * GetSharedRectHeightProportion(hitTestRect, otherDocumentViewBoundingBox);
                case Side.Right:
                    distanceToMid = Math.Abs(otherDocumentViewBoundingBox.X - midX);
                    distanceToMid = 1.0f - Math.Min(1.0, distanceToMid / hitTestRect.Width);
                    return distanceToMid * GetSharedRectHeightProportion(hitTestRect, otherDocumentViewBoundingBox);
            }
            return distanceToMid;
        }


        private Rect CalculateAligningRectangleForSide(Side side, Point topLeftPoint, Point bottomRightPoint, double w, double h)
        {
            Point newTopLeft, newBottomRight;

            switch (side)
            {
                case Side.Top:
                    newTopLeft = new Point(topLeftPoint.X, topLeftPoint.Y - h);
                    newBottomRight = new Point(bottomRightPoint.X, topLeftPoint.Y);
                    break;
                case Side.Bottom:
                    newTopLeft = new Point(topLeftPoint.X, bottomRightPoint.Y);
                    newBottomRight = new Point(bottomRightPoint.X, bottomRightPoint.Y + h);
                    break;
                case Side.Left:
                    newTopLeft = new Point(topLeftPoint.X - w, topLeftPoint.Y);
                    newBottomRight = new Point(topLeftPoint.X, bottomRightPoint.Y);
                    break;
                case Side.Right:
                    newTopLeft = new Point(bottomRightPoint.X, topLeftPoint.Y);
                    newBottomRight = new Point(bottomRightPoint.X + w, bottomRightPoint.Y);
                    break;
            }
            return new Rect(newTopLeft, newBottomRight);
        }

        private Rect CalculateAligningRectangleForSide(Side side, Rect boundingBox, double w, double h)
        {
            Point topLeftPoint = new Point(boundingBox.X, boundingBox.Y);
            Point bottomRightPoint = new Point(boundingBox.X + boundingBox.Width, boundingBox.Y + boundingBox.Height);
            return CalculateAligningRectangleForSide(side, topLeftPoint, bottomRightPoint, w, h);
        }

        private double GetSharedRectWidthProportion(Rect source, Rect target)
        {
            var targetMin = target.X;
            var targetMax = target.X + target.Width;

            var sourceStart = Math.Max(targetMin, source.X);
            var sourceEnd = Math.Min(targetMax, source.X + source.Width);
            return (sourceEnd - sourceStart) / source.Width;
        }

        private double GetSharedRectHeightProportion(Rect source, Rect target)
        {
            var targetMin = target.Y;
            var targetMax = target.Y + target.Height;

            var sourceStart = Math.Max(targetMin, source.Y);
            var sourceEnd = Math.Min(targetMax, source.Y + source.Height);

            return (sourceEnd - sourceStart) / source.Height;
        }
        /*
        /// <summary>
        /// Returns the bounding box in screen space of a FrameworkElement.
        /// 
        /// TODO: move this to a more logical place, such as a Util class or an extension class
        /// </summary>
        /// <returns></returns>
        private Rect GetBoundingBox(FrameworkElement element)
        {
            Point topLeftObjectPoint = new Point(0, 0);
            Point bottomRightObjectPoint = new Point(element.ActualWidth, element.ActualHeight);

            var topLeftPoint = Util.PointTransformFromVisual(topLeftObjectPoint, element);
            var bottomRightPoint = Util.PointTransformFromVisual(bottomRightObjectPoint, element);

            return new Rect(topLeftPoint, bottomRightPoint);
        }
        */

        #endregion



        public void BorderOnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs manipulationCompletedRoutedEventArgs)
        {
            ManipulationCompleted(manipulationCompletedRoutedEventArgs, true);
        }

        public void ElementOnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs manipulationCompletedRoutedEventArgs)
        {
            if (manipulationCompletedRoutedEventArgs == null || !manipulationCompletedRoutedEventArgs.Handled)
                ManipulationCompleted(manipulationCompletedRoutedEventArgs, false);
        } 

        public void ManipulationCompleted(ManipulationCompletedRoutedEventArgs manipulationCompletedRoutedEventArgs, bool canSplitupDragGroup)
        {
            Snap(false);
            
            _isManipulating = false;
            var docRoot = _element.GetFirstAncestorOfType<DocumentView>();

            SplitupGroupings(canSplitupDragGroup, docRoot);

            if (manipulationCompletedRoutedEventArgs != null)
            {
                docRoot?.Dispatcher?.RunAsync(CoreDispatcherPriority.High, new DispatchedHandler(
                    () => docRoot.MoveToContainingCollection()));
                manipulationCompletedRoutedEventArgs.Handled = true;
            }
        }

        void SplitupGroupings(bool canSplitupDragGroup, DocumentView docRoot)
        {
            if (docRoot?.ParentCollection == null)
                return;
            var groupToSplit = GetGroupForDocument(docRoot.ViewModel.DocumentController);
            if (groupToSplit != null && canSplitupDragGroup)
            {
                var docsToReassign = groupToSplit.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);

                var groupsList = docRoot.ParentCollection.ParentDocument.ViewModel.DocumentController.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
                groupsList.Remove(groupToSplit);

                foreach (var dv in docsToReassign.TypedData.Select((d) => GetViewModelFromDocument(d)))
                {
                    if (dv != null)
                        SetupGroupings(dv, docRoot.ParentCollection);
                }

                foreach (var dv in docsToReassign.TypedData.Select((d) => GetViewModelFromDocument(d)))
                {
                    if (dv != null && GetGroupForDocument(dv.DocumentController) == null && 
                        !dv.DocumentController.DocumentType.Equals(BackgroundBox.DocumentType))
                        dv.BackgroundBrush = new SolidColorBrush(Colors.Transparent);
                }
            }
            else
                SetupGroupings(docRoot.ViewModel, docRoot.ParentCollection);
        }

        public void ElementOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (e != null && _isManipulating)
            {
                e.Complete();
                return;
            }
            if (e != null && e.PointerDeviceType == BlockedInputType && FilterInput)
            {
                e.Complete();
                _processManipulation = false;
                e.Handled = true;
                return;
            }
            var docRoot = _element.GetFirstAncestorOfType<DocumentView>();
            docRoot?.ToFront();
            
             SetupGroupings(docRoot.ViewModel, docRoot.ParentCollection);

            _isManipulating = true;
            _processManipulation = true;

            _numberOfTimesDirChanged = 0;
            if (e != null && (Window.Current.CoreWindow.GetKeyState(VirtualKey.RightButton) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down)
                e.Handled = true;
        }

        private void SetupGroupings(DocumentViewModel docViewModel, CollectionView parentCollection)
        {
            if (parentCollection == null)
                return;
            var groupsList = GetGroupsList(parentCollection);

            DocumentController dragGroupDocument;
            var dragDocumentList = GetDragGroupInfo(docViewModel, out dragGroupDocument);

            if (parentCollection?.CurrentView is CollectionFreeformView freeFormView)
            {
                var groups = AddConnected(dragDocumentList, dragGroupDocument, groupsList.Data.Where((gd) => !gd.Equals(dragGroupDocument)).Select((gd) => gd as DocumentController));
                parentCollection.ParentDocument.ViewModel.DocumentController.GetDataDocument(null).SetField(KeyStore.GroupingKey, new ListController<DocumentController>(groups), true);

                DocumentController newDragGroupDocument;
                _grouping = GetDragGroupInfo(docViewModel, out newDragGroupDocument).Select((gd) => GetViewModelFromDocument(gd)).ToList();
            }
        }

        List<DocumentController> GetDragGroupInfo(DocumentViewModel docViewModel, out DocumentController dragGroupDocument)
        {
            dragGroupDocument = GetGroupForDocument(docViewModel.DocumentController);
            var dragDocumentList = dragGroupDocument?.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null)?.TypedData;
            if (dragDocumentList == null)
            {
                dragGroupDocument = docViewModel.DocumentController;
                dragDocumentList = new List<DocumentController>(new DocumentController[] { dragGroupDocument });
            }
            return dragDocumentList;
        }

        ListController<DocumentController> GetGroupsList(CollectionView collectionView)
        {
            var groupsList = collectionView.ParentDocument.ViewModel.DocumentController.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
            if ((groupsList == null || groupsList.Count == 0) && collectionView.ViewModel.DocumentViewModels.Count > 0)
            {
                groupsList = new ListController<DocumentController>(collectionView.ViewModel.DocumentViewModels.Select((vm) => vm.DocumentController));
                collectionView.ParentDocument.ViewModel.DocumentController.GetDataDocument(null).SetField(KeyStore.GroupingKey, groupsList, true);
            }
            var addedItems = new List<DocumentController>();
            foreach (var d in collectionView.ViewModel.DocumentViewModels)
                if (GetGroupForDocument(d.DocumentController) == null && !groupsList.Data.Contains(d.DocumentController))
                {
                    addedItems.Add(d.DocumentController);
                }

            var removedGroups = new List<DocumentController>();
            var docsInCollection = collectionView.ViewModel.DocumentViewModels.Select((dv) => dv.DocumentController);
            foreach (var g in groupsList.TypedData)
            {
                var groupDocs = g.GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
                if (!docsInCollection.Contains((g)) && (groupDocs == null || groupDocs.TypedData.Where((gd) => docsInCollection.Contains(gd)) == null))
                {
                    removedGroups.Add(g);
                }
            }
            var newGroupsList = new List<DocumentController>(groupsList.TypedData);
            newGroupsList.AddRange(addedItems);
            newGroupsList.RemoveAll((r) => removedGroups.Contains(r));
            groupsList = new ListController<DocumentController>(newGroupsList);
            collectionView.ParentDocument.ViewModel.DocumentController.GetDataDocument(null).SetField(KeyStore.GroupingKey, groupsList, true);
            return groupsList;
        }

        DocumentController GetGroupForDocument(DocumentController dragDocument)
        {
            var docView = _element.GetFirstAncestorOfType<DocumentView>();
            var groupsList = docView.ParentCollection.ParentDocument.ViewModel.DocumentController.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);

            if (groupsList == null) return null;
            foreach (var g in groupsList.TypedData)
            {
                if (g.Equals(dragDocument))
                {
                    return null;
                }
                else
                {
                    var cfield = g.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
                    if (cfield != null && cfield.Data.Where((cd) => (cd as DocumentController).Equals(dragDocument)).Count() > 0)
                    {
                        return g;
                    }
                }
            }
            return null;
        }

        public DocumentViewModel GetViewModelFromDocument(DocumentController doc)
        {
            var parentCollection = _element.GetFirstAncestorOfType<CollectionView>();
            foreach (var dv in parentCollection.ViewModel.DocumentViewModels)
            {
                if (dv.DocumentController.Equals(doc))
                    return dv;
            }
            return null;
        }

        public List<DocumentController>  GetGroupDocumentsList(DocumentController doc, bool onlyGroups = false)
        {
            var groupList = _element.GetFirstAncestorOfType<DocumentView>().ParentCollection.ParentDocument.ViewModel.DocumentController.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);

            foreach (var g in groupList.TypedData)
            if (g.Equals(doc)) {
                if (GetViewModelFromDocument(g) != null)
                {
                    return new List<DocumentController>(new DocumentController[] { g });
                }
                else
                {
                    var cfield = g.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
                    if (cfield != null)
                    {
                        return cfield.Data.Select((cd) => cd as DocumentController).ToList();
                    }
                }
            }
            return null;
        }

        public List<DocumentController> AddConnected(List<DocumentController> dragDocumentList, DocumentController dragGroupDocument, IEnumerable<DocumentController> otherGroups)
        {
            foreach (var dragDocument in dragDocumentList)
            {
                var dragDocumentView = GetViewModelFromDocument(dragDocument);
                if (dragDocumentView == null)
                    continue;
                var dragDocumentBounds = dragDocumentView.GroupingBounds;
                foreach (var otherGroup in otherGroups)
                {
                    var otherGroupMembers = GetGroupDocumentsList(otherGroup);
                    if (otherGroupMembers == null)
                        continue;
                    foreach (var otherGroupMember in otherGroupMembers)
                    {
                        var otherDocView = GetViewModelFromDocument(otherGroupMember);
                        if (otherDocView == null)
                            continue;
                        var otherGroupMemberBounds = otherDocView.GroupingBounds;
                        otherGroupMemberBounds.Intersect(dragDocumentBounds);

                        if (otherGroupMemberBounds != Rect.Empty)
                        {
                            var group = GetGroupForDocument(otherGroupMember);
                            if (group == null) {
                                dragDocumentList.Add(otherGroupMember);
                                var newList = otherGroups.ToList();
                                var newGroup = new DocumentController();
                                newGroup.SetField(KeyStore.GroupingKey, new ListController<DocumentController>(dragDocumentList), true);
                                newList.Add(newGroup);
                                newList.Remove(otherGroup);
                                newList.Remove(dragGroupDocument);
                                var r = new Random();
                                var solid = (GetViewModelFromDocument(otherGroupMember)?.BackgroundBrush as SolidColorBrush)?.Color;
                                var brush = solid != Colors.Transparent ? new SolidColorBrush((Windows.UI.Color)solid) :
                                      new SolidColorBrush(Windows.UI.Color.FromArgb(0x33, (byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255)));
                                foreach (var d in dragDocumentList)
                                    GetViewModelFromDocument(d).BackgroundBrush = brush;
                                return newList;
                            }
                            else
                            {
                                var groupList = group.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
                                groupList.AddRange(dragDocumentList);
                                var newList = otherGroups.ToList();
                                newList.Remove(dragGroupDocument);
                                foreach (var d in dragDocumentList)
                                    GetViewModelFromDocument(d).BackgroundBrush = GetViewModelFromDocument(otherGroupMember).BackgroundBrush;
                                return newList;
                            }
                        }
                    }
                }

            }

            var sameList = otherGroups.ToList();
            sameList.Add(dragGroupDocument);
            return sameList;
        }

        public void AddAllAndHandle()
        {
            if (!_disabled) return;

            if (_doesRespondToManipulationDelta)
            {
                _element.ManipulationDelta -= EmptyManipulationDelta;
                _element.ManipulationDelta += ElementOnManipulationDelta;
            }

            if (_doesRespondToPointerWheel)
            {
                _element.PointerWheelChanged -= EmptyPointerWheelChanged;
                _element.PointerWheelChanged += ElementOnPointerWheelChanged;
            }
            _disabled = false;
        }

        public void RemoveAllButHandle()
        {
            RemoveAllSetHandle(true);
        }

        public void RemoveAllAndDontHandle()
        {
            RemoveAllSetHandle(false);
        }

        private void RemoveAllSetHandle(bool handle)
        {
            if (_disabled) return;

            if (_doesRespondToManipulationDelta)
            {
                _element.ManipulationDelta -= ElementOnManipulationDelta;
                _element.ManipulationDelta += EmptyManipulationDelta;
            }
            if (_doesRespondToPointerWheel)
            {
                _element.PointerWheelChanged -= ElementOnPointerWheelChanged;
                _element.PointerWheelChanged += EmptyPointerWheelChanged;
            }
            _handle = handle;
            _disabled = true;
        }

        // == METHODS ==

        private void ElementOnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (PointerWheelEnabled)
            {
                _processManipulation = true;
                TranslateAndScale(e);
            }

        }

        private void EmptyManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = _handle;
        }

        private void EmptyPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = _handle;
        }


        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event.
        /// </summary>
        private void BorderManipulateDeltaMove(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (e.PointerDeviceType == PointerDeviceType.Mouse &&
                (Window.Current.CoreWindow.GetKeyState(VirtualKey.RightButton) & CoreVirtualKeyStates.Down) != CoreVirtualKeyStates.Down)
            {
                return;
            }

            TranslateAndScale(new ManipulationDeltaData(e.Position, e.Delta.Translation, e.Delta.Scale));
            Snap(true);
            e.Handled = true;
        }


        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event.
        /// </summary>
        private void ElementOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (!Window.Current.CoreWindow.GetKeyState(VirtualKey.RightButton).HasFlag(CoreVirtualKeyStates.Down) &&
                !Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
            {
                return;
            }

            TranslateAndScale(new ManipulationDeltaData(e.Position, e.Delta.Translation, e.Delta.Scale), _grouping);
            DetectShake(sender, e);
            Snap(true);
            e.Handled = true;
        }

        // keeps track of whether the node has been shaken hard enough
        private static int _numberOfTimesDirChanged = 0;
        private static double _direction;
        private static DispatcherTimer _dispatcherTimer;

        // these constants adjust the sensitivity of the shake
        private static int _millisecondsToShake = 600;
        private static int _sensitivity = 4;
        public List<DocumentViewModel> _grouping;

        /// <summary>
        /// Determines whether a shake manipulation has occured based on the velocity and direction of the translation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void DetectShake(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // get the document view that's being manipulated
            var grid = sender as Grid;
            var docView = grid?.GetFirstAncestorOfType<DocumentView>();

            if (docView != null)
            {
                // calculate the speed of the translation from the velocities property of the eventargs
                var speed = Math.Sqrt(Math.Pow(e.Velocities.Linear.X, 2) + Math.Pow(e.Velocities.Linear.Y, 2));

                // calculate the direction of the velocity
                var dir = Math.Atan2(e.Velocities.Linear.Y, e.Velocities.Linear.X);
                
                // checks if a certain number of direction changes occur in a specified time span
                if (_numberOfTimesDirChanged == 0)
                {
                    StartTimer();
                    _numberOfTimesDirChanged++;
                    _direction = dir;
                }
                else if (_numberOfTimesDirChanged < _sensitivity)
                {
                    if (Math.Abs(Math.Abs(dir - _direction) - 3.14) < 1)
                    {
                        _numberOfTimesDirChanged++;
                        _direction = dir;
                    }
                }
                else
                {
                    if (Math.Abs(Math.Abs(dir - _direction) - 3.14) < 1)
                    {
                        // if we've reached enough direction changes, break the connection
                        docView.DisconnectFromLink();
                        _numberOfTimesDirChanged = 0;
                    }
                }
            }
        }

        private static void StartTimer()
        {
            if (_dispatcherTimer != null)
            {
                _dispatcherTimer.Stop();
            }
            else
            {
                _dispatcherTimer = new DispatcherTimer();
                _dispatcherTimer.Tick += dispatcherTimer_Tick;
            }
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, _millisecondsToShake);

            _dispatcherTimer.Start();

        }

        private static void dispatcherTimer_Tick(object sender, object e)
        {
            _numberOfTimesDirChanged = 0;
            _dispatcherTimer.Stop();
        }

        private void TranslateAndScale(PointerRoutedEventArgs e)
        {
            if (!_processManipulation) return;
            e.Handled = true;

            if ((e.KeyModifiers & VirtualKeyModifiers.Control) != 0)
            {

                //Get mousepoint in canvas space 
                var point = e.GetCurrentPoint(_element);

                // get the scale amount
                float scaleAmount = point.Properties.MouseWheelDelta > 0 ? 1.07f : 1 / 1.07f;

                //Clamp the scale factor 
                ElementScale *= scaleAmount;

                if (!ClampScale(scaleAmount))
                    OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(new Point(),
                        point.Position, new Point(scaleAmount, scaleAmount)));
            }
            else
            {
                var scrollAmount = e.GetCurrentPoint(_element).Properties.MouseWheelDelta / 3.0f;
                if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
                {
                    OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(new Point(scrollAmount, 0),
                        new Point(), new Point(1, 1)));
                }
                else
                {
                    OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(new Point(0, scrollAmount),
                        new Point(), new Point(1, 1)));
                }

            }
        }

        public void FitToParent()
        {
            var ff = _element as CollectionFreeformView;
            var par = ff?.Parent as FrameworkElement;
            if (par == null || ff == null)
                return;

            var rect = new Rect(new Point(), new Point(par.ActualWidth, par.ActualHeight)); //  par.GetBoundingRect();

            //if (ff.ViewModel.DocumentViewModels.Count == 1)
            //{
            //    ff.ViewModel.DocumentViewModels[0].GroupTransform = new TransformGroupData(new Point(), new Point(), new Point(1, 1));
            //    var aspect = rect.Width / rect.Height;
            //    var ffHeight = ff.ViewModel.DocumentViewModels[0].Height;
            //    var ffwidth = ff.ViewModel.DocumentViewModels[0].Width;
            //    var ffAspect = ffwidth / ffHeight;
            //    ff.ViewModel.DocumentViewModels[0].Width  = aspect > ffAspect ? rect.Height * ffAspect : rect.Width;
            //    ff.ViewModel.DocumentViewModels[0].Height = aspect < ffAspect ? rect.Width / ffAspect : rect.Height;
            //    return;
            //}

            var r = Rect.Empty;
            foreach (var dvm in ff.xItemsControl.ItemsPanelRoot.Children.Select((ic) => (ic as ContentPresenter)?.Content as DocumentViewModel))
            {
                r.Union(dvm?.Content?.GetBoundingRect(par) ?? r);
            }

            if (r != Rect.Empty)
            {
                var trans = new Point(-r.Left - r.Width / 2 + rect.Width / 2, -r.Top);
                var scaleAmt = new Point(rect.Width / r.Width, rect.Width / r.Width);
                if (rect.Width / rect.Height > r.Width / r.Height)
                {
                    scaleAmt = new Point(rect.Height / r.Height, rect.Height / r.Height);
                }
                else
                    trans = new Point(-r.Left + (rect.Width - r.Width) / 2, -r.Top + (rect.Height - r.Height) / 2);

                OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(trans, new Point(r.Left + r.Width / 2, r.Top), scaleAmt));
            }
        }

        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event.
        /// </summary>
        /// <param name="e">passed in frm routed event args</param>
        /// <param name="grouped"></param>
        public void TranslateAndScale(ManipulationDeltaData e, List<DocumentViewModel> grouped=null)
        {
            if (!_processManipulation) return;
            var handleControl = VisualTreeHelper.GetParent(_element) as FrameworkElement;

            var scaleFactor = e.Scale;
            ElementScale *= scaleFactor;

            // set up translation transform
            var translate = Util.TranslateInCanvasSpace(e.Translation, handleControl);



            //Clamp the scale factor 
            if (!ClampScale(scaleFactor))
            {
                // translate the entire group except for
                var transformGroup = new TransformGroupData(new Point(translate.X, translate.Y),
                    e.Position, new Point(scaleFactor, scaleFactor));
                if (grouped != null && grouped.Any())
                {
                    var docRoot = _element.GetFirstAncestorOfType<DocumentView>();
                    foreach (var g in grouped.Except(new List<DocumentViewModel> {docRoot.ViewModel}))
                    {
                        g?.TransformDelta(transformGroup);
                    }
                }

                OnManipulatorTranslatedOrScaled?.Invoke(transformGroup);
            }
        }

        public void Dispose()
        {
            _element.ManipulationDelta -= ElementOnManipulationDelta;
            _element.ManipulationDelta -= EmptyManipulationDelta;
            _element.PointerWheelChanged -= ElementOnPointerWheelChanged;
            _element.PointerWheelChanged -= EmptyPointerWheelChanged;
        }

        private bool ClampScale(double scaleFactor)
        {
            if (ElementScale > MaxScale)
            {
                ElementScale = MaxScale;
                return scaleFactor > 1;
            }

            if (ElementScale < MinScale)
            {
                ElementScale = MinScale;
                return scaleFactor < 1;
            }
            return false;
        }
    }
}
