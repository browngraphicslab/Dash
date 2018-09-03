using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;

namespace Dash
{
    public enum LinkTargetPlacement
    {
       Default, Overlay
    }
    public class AnnotationManager
	{
		private FrameworkElement    _element;
		private readonly MenuFlyout   _linkFlyout = new MenuFlyout();
		private List<DocumentController> _lastHighlighted = new List<DocumentController>();

		public AnnotationManager(FrameworkElement uiElement)
		{
			_element = uiElement;
			_linkFlyout.Closed += (s, e) => _linkFlyout.Items?.Clear();
		}

        //TODO This can be made static and can take in a framework element instead of IEnumerable<ILinkHandler>
	    public void FollowRegion(DocumentController region, IEnumerable<ILinkHandler> linkHandlers, Point flyoutPosition, string linkType=null)
        {
            _linkFlyout.Items?.Clear();
            var linksTo = region.GetDataDocument().GetLinks(KeyStore.LinkToKey);
	        var linksFrom = region.GetDataDocument().GetLinks(KeyStore.LinkFromKey);
            var subregions = region.GetDataDocument().GetRegions()?.TypedData;
            if (subregions != null)
            {
                foreach (var subregion in subregions.Select((sr) => sr.GetDataDocument()))
                {
                    var sublinksTo   = subregion.GetLinks(KeyStore.LinkToKey);
                    var sublinksFrom = subregion.GetLinks(KeyStore.LinkFromKey);
                    linksTo.AddRange(sublinksTo);
                    linksFrom.AddRange(sublinksFrom);
                }
            }
            int linkToCount = linksTo?.Count ?? 0;
            int linkFromCount = linksFrom?.Count ?? 0;
            int linkCount = linkToCount + linkFromCount;
            if (linkCount == 0) return;

	        if (linkCount == 1)
	        {
                var link = linkToCount == 0 ? linksFrom?[0] : linksTo?[0];
                if (linkType == null || (link.GetDataDocument().GetLinkTag()?.Data.Equals(linkType) ?? false))
                    FollowLink(link, linkToCount != 0 ? LinkDirection.ToDestination : LinkDirection.ToSource, linkHandlers);
	        }
	        else if (!MainPage.Instance.IsShiftPressed())
            {
                if (linksTo != null)
                {
                    foreach (DocumentController linkTo in linksTo)
                        if (linkType == null || (linkTo.GetDataDocument().GetLinkTag()?.Data.Equals(linkType) ?? false))
                        {
                            FollowLink(linkTo, LinkDirection.ToDestination, linkHandlers);
                        }
                }

                _linkFlyout.Items?.Add(new MenuFlyoutSeparator());

                if (linksFrom != null)
                {
                    foreach (var linkFrom in linksFrom)
                        if (linkType == null || (linkFrom.GetDataDocument().GetLinkTag()?.Data.Equals(linkType) ?? false))
                        {
                            FollowLink(linkFrom, LinkDirection.ToSource, linkHandlers);
                        }
                }
            }
	        else // There are multiple links, so we need to show a flyout to determine which link to follow
	        {
                RoutedEventHandler defaultHdlr = null;
	            if (linksTo != null)
                {
                    foreach (DocumentController linkTo in linksTo)
                    if (linkType == null || (linkTo.GetDataDocument().GetLinkTag()?.Data.Equals(linkType) ?? false))
	                    {
		                    var targetTitle = linkTo.GetDataDocument().GetLinkedDocument(LinkDirection.ToDestination)
			                    .Title;

		                    var item = new MenuFlyoutItem
		                    {
			                    Text = targetTitle,
			                    DataContext = linkTo
		                    };
		                    var itemHdlr = new RoutedEventHandler((s, e) => FollowLink(linkTo, LinkDirection.ToDestination, linkHandlers));
		                    item.Click += itemHdlr;
		                    defaultHdlr = itemHdlr;
		                    _linkFlyout.Items?.Add(item);
						}
                }

                _linkFlyout.Items?.Add(new MenuFlyoutSeparator());

	            if (linksFrom != null)
                {
                    foreach (var linkFrom in linksFrom)
                    if (linkType == null || (linkFrom.GetDataDocument().GetLinkTag()?.Data.Equals(linkType) ?? false))
                    {
	                    var targetTitle = linkFrom.GetDataDocument().GetLinkedDocument(LinkDirection.ToSource)
		                    .Title;

						var item = new MenuFlyoutItem
	                    {
		                    Text = targetTitle,
		                    DataContext = linkFrom
	                    };
	                    var itemHdlr = new RoutedEventHandler((s, e) => FollowLink(linkFrom, LinkDirection.ToSource, linkHandlers));
	                    item.Click += itemHdlr;
	                    defaultHdlr = itemHdlr;
	                    _linkFlyout.Items?.Add(item);
                    }
                }

                if (_linkFlyout.Items.Count == 2)
                    defaultHdlr(null, null);
                else _linkFlyout.ShowAt(_element, flyoutPosition);
			}
	    }

	    private void FollowLink(DocumentController link, LinkDirection direction, IEnumerable<ILinkHandler> linkHandlers)
	    {
            //show link description floating doc if operator output is true
	        var linkOperator = link.GetDataDocument().GetDereferencedField<BoolController>(LinkDescriptionTextOperator.ShowDescription, null);
	        if (linkOperator?.Data ?? false)
	        {
	            MainPage.Instance.AddFloatingDoc(link);
            }
            
            var linkContext = link.GetDataDocument().GetDereferencedField<BoolController>(KeyStore.LinkContextKey, null)?.Data ?? true;

            var document = link.GetLinkedDocument(direction);

            switch (link.GetDataDocument().GetLinkBehavior())
            {
                case LinkBehavior.Zoom:
                    //navigate to link
                    if (linkContext)
                    {
                        if (!MainPage.Instance.NavigateToDocumentInWorkspaceAnimated(document, false))
                        {
                            var tree = DocumentTree.MainPageTree;
                            if (tree.Nodes.ContainsKey(document))//TODO This doesn't handle documents in collections that aren't in the document "visual tree"
                            {
                                var docNode = tree.Nodes[document];
                                MainPage.Instance.SetCurrentWorkspaceAndNavigateToDocument(docNode.Parent.ViewDocument, docNode.ViewDocument);
                            }
                            else
                            {
                                MainPage.Instance.SetCurrentWorkspace(document);
                            }
                        }
                    }
                    else
                    {
                        MainPage.Instance.SetCurrentWorkspace(document);
                    }

                    break;
                case LinkBehavior.Overlay:
                case LinkBehavior.Annotate:
                    //default behavior of highlighting and toggling link visibility and docking when off screen
                    foreach (var linkHandler in linkHandlers)
                    {
                        var status = linkHandler.HandleLink(link, direction);

                        if (status == LinkHandledResult.HandledClose) break;
                        if (status == LinkHandledResult.HandledRemainOpen)
                        {
                            void LinkFlyoutOnClosing(FlyoutBase flyoutBase, FlyoutBaseClosingEventArgs args)
                            {
                                args.Cancel = true;
                                _linkFlyout.Closing -= LinkFlyoutOnClosing;
                            }

                            _linkFlyout.Closing += LinkFlyoutOnClosing;
                        }
                    }
                    break;
                case LinkBehavior.Dock:
                    MainPage.Instance.Dock_Link(link, direction, linkContext);
                    break;
                case LinkBehavior.Float:
                    MainPage.Instance.AddFloatingDoc(document);
                    break;
                default:
                    break;

	        }

        }

	    #region Old annotation stuff

        //      //navigation and toggling of linked annotations to the pressed region
        //      public void RegionPressed(DocumentController theDoc, Point pos, DocumentController chosenDC = null)
        //{
        //	if (chosenDC != null)
        //          {
        //		// navigate to the doc if ctrl is pressed, unless if it's super far away, in which case dock it. FollowDocument will take care of that.
        //		// I think chosenDC is only not-null when it's selected from the LinkFlyoutMenu, which only triggers under ctrl anyways.
        //              FollowDocument(chosenDC, pos);
        //           SelectionManager.SelectRegionFromParent(chosenDC);
        //          }
        //          else
        //	{
        //		var toLinks = GetLinks(theDoc, false);
        //		var fromLinks = GetLinks(theDoc, true);

        //		// choose link to follow by showing flyout
        //		if (MainPage.Instance.IsCtrlPressed())
        //              {
        //               if (_linkFlyout.Items == null || _linkFlyout.Items.Count != 0) return;

        //               if (toLinks?.Count + fromLinks?.Count == 1)
        //			{
        //				var dc = toLinks.Count > 0 ? toLinks.First() : fromLinks.First();
        //				SelectionManager.SelectRegionFromParent(theDoc);
        //				dc = dc.GetDataDocument()
        //	                .GetDereferencedField<ListController<DocumentController>>(
        //		                toLinks.Count > 0 ? KeyStore.LinkToKey : KeyStore.LinkFromKey, null).TypedData.First();
        //                FollowDocument(dc, pos);
        //				return;
        //               }

        //			if (toLinks != null)
        //                AddToLinksMenu(toLinks, KeyStore.LinkToKey, pos, theDoc);
        //               if (fromLinks != null)
        //                AddToLinksMenu(fromLinks, KeyStore.LinkFromKey, pos, theDoc);


        //               if (_linkFlyout.Items.Count > 0)
        //                _linkFlyout.ShowAt(_element);
        //              }
        //		// shows everything if it's not selected already. Otherwise, it'll toggle.
        //		else
        //		{
        //			if (theDoc.Equals(SelectionManager.SelectedRegion))
        //			{
        //				ToggleAnnotationVisibility(theDoc);
        //			}
        //			else
        //			{
        //				SelectionManager.SelectRegionFromParent(theDoc);
        //			}
        //		}

        //          }
        //      }

        //// follows the document in the workspace, and heuristically determines if it's too far away and should be docked
        //   private void FollowDocument(DocumentController target, Point pos)
        //   {
        //    DocumentController docToFollow = target;

        //	// is a region
        //	if (target.GetRegionDefinition() != null)
        //		docToFollow = target.GetRegionDefinition();

        //	var nearestOnScreen = FindNearestDisplayedTarget(pos, docToFollow?.GetDataDocument(), true);
        //	var nearestOnCollection = FindNearestDisplayedTarget(pos, docToFollow?.GetDataDocument(), false);

        //    var toFollow = nearestOnScreen ?? nearestOnCollection;
        //    if (toFollow == null) return;
        //	// true if it's in sight, false if hidden
        //    var intersectWithParentCollection = RectHelper.Intersect(toFollow.GetBoundingRect(MainPage.Instance),
        //	                                        toFollow.GetFirstAncestorOfType<CollectionFreeformView>()
        //		                                        .GetBoundingRect(MainPage.Instance)) !=
        //                                        Rect.Empty || toFollow.GetFirstAncestorOfType<CollectionFreeformView>().Equals(MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionFreeformView>());

        //	if (nearestOnScreen == null || !intersectWithParentCollection)
        //	{
        //		// calculate distance of how off-screen it is
        //		var distPoint = MainPage.Instance.GetDistanceFromMainDocCenter(docToFollow);
        //		var dist = Math.Sqrt(distPoint.X * distPoint.X + distPoint.Y * distPoint.Y);
        //	    var threshold = MainPage.Instance.MainDocView.ActualWidth * 1.5;

        //		// not visible in its current collectionview, or too far away: dock it
        //		if (dist > threshold || !intersectWithParentCollection)
        //		{
        //			// see if it's already docked
        //			var dv = MainPage.Instance.DockManager.GetDockedView(target);

        //			// if not docked
        //			if (dv == null)
        //			{
        //				var dir = distPoint.X > 0 ? DockDirection.Left : DockDirection.Right;
        //				MainPage.Instance.DockManager.Dock(target, dir);
        //			}

        //			// if it's already docked, then highlight it instead of docking it again
        //			else
        //			{
        //				dv.FlashSelection();
        //			}
        //		}
        //		// should always be true
        //		else if (dist <= threshold)
        //		{
        //			MainPage.Instance.NavigateToDocumentInWorkspace(nearestOnCollection.ViewModel.DocumentController, true, false);
        //		}
        //    }

        //    var va = toFollow.GetFirstDescendantOfType<IVisualAnnotatable>();
        //    va?.GetAnnotationManager().SelectRegionFromParent(target);
        //    //if (va is PdfView pdf)
        //    //{
        //	   // pdf.ScrollToRegion(target);
        //    //}

        //    UpdateHighlight(new List<DocumentController> {toFollow.ViewModel.DocumentController});
        //}

        ////called when the selected region changes
        //private void UpdateHighlight(List<DocumentController> toHighlight)
        //{
        //	foreach (var unhighlight in _lastHighlighted)
        //	{
        //		MainPage.Instance.HighlightDoc(unhighlight, false, 2);
        //	}

        //	_lastHighlighted.Clear();

        //	// cycle through and show everything
        //	foreach (var dc in toHighlight)
        //	{
        //		MainPage.Instance.HighlightDoc(dc, false, 1);
        //		_lastHighlighted.Add(dc);
        //	}
        //}

        //List<DocumentController> GetLinks(DocumentController theDoc, bool getFromLinks)
        //      {
        //       var links = getFromLinks ? theDoc.GetDataDocument().GetLinks(KeyStore.LinkFromKey)?.TypedData : theDoc.GetDataDocument().GetLinks(KeyStore.LinkToKey)?.TypedData;

        //       // does this list exist? If not, return an empty list since there's nothing in it
        //       return links ?? new List<DocumentController>();
        //      }

        //      //finds the nearest document view of the desired document controller that is displayed on the canvas
        //      DocumentView FindNearestDisplayedTarget(Point where, DocumentController targetData, bool onlyOnPage = true)
        //      {
        //       var collection = _element.GetFirstAncestorOfType<CollectionView>();
        //       DocumentView nearest = null;

        //       if (collection != null) nearest = NearestOnCollection(where, targetData, collection, onlyOnPage);

        //	// means something was found
        //       if (nearest != null) return nearest;

        //       // haven't found a doc of the matching criteria on this current collection, so queue up all the collections and start searching
        //	var q = new Queue<CollectionView>();
        //       var mainCollection = MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>();
        //	q.Enqueue(mainCollection);
        //       foreach (var nestedCollection in mainCollection.GetDescendantsOfType<CollectionView>())
        //       {
        //        q.Enqueue(nestedCollection);
        //       }

        //	// iterate through every document looking for it
        //       var dist = double.MaxValue;
        //	while (q.Count != 0)
        //       {
        //        var curr = q.Dequeue();
        //        var nearestOnThisCollection = NearestOnCollection(where, targetData, curr, onlyOnPage);
        //        if (nearestOnThisCollection == null) continue;
        //        var d = GetDistanceFromDocument(where, nearestOnThisCollection);
        //        if (d < dist)
        //        {
        //	        dist = d;
        //	        nearest = nearestOnThisCollection;
        //        }
        //       }

        //    return nearest;
        //      }

        //private DocumentView NearestOnCollection(Point where, DocumentController targetData, CollectionView collection, bool onlyOnPage = true)
        //{
        //	var dist = double.MaxValue;
        //	DocumentView nearest = null;

        //	// TODO expand this to work with treeviews too...?
        //	var itemsPanelRoot = (collection.CurrentView as CollectionFreeformView)?.xItemsControl.ItemsPanelRoot;
        //	if (itemsPanelRoot == null) return null;
        //	foreach (var presenter in itemsPanelRoot.Children.Select(c => c as ContentPresenter))
        //	{
        //		var dvm = presenter.GetFirstDescendantOfType<DocumentView>();
        //		if (dvm?.ViewModel.DataDocument.Id != targetData?.Id) continue;
        //		var mprect = dvm.GetBoundingRect(MainPage.Instance);
        //		if (onlyOnPage && RectHelper.Intersect(MainPage.Instance.GetBoundingRect(), mprect).IsEmpty) continue;

        //		var d = GetDistanceFromDocument(where, dvm);

        //		if (d < dist)
        //		{
        //			dist = d;
        //			nearest = dvm;
        //		}
        //	}
        //	return nearest;
        //}

        //private double GetDistanceFromDocument(Point where, DocumentView view)
        //{
        //	var mprect = view.GetBoundingRect(MainPage.Instance);
        //	var center = new Point((mprect.Left + mprect.Right) / 2, (mprect.Top + mprect.Bottom) / 2);
        //	return Math.Sqrt((@where.X - center.X) * (@where.X - center.X) + (@where.Y - center.Y) * (@where.Y - center.Y));
        //}

        //// figures out what to do once a region has been tapped again after beign selected (shows/hides its regions)
        //private void ToggleAnnotationVisibility(DocumentController region)
        //{
        //	if (region == null) return;
        //	var toLinks = region.GetDataDocument().GetLinks(KeyStore.LinkToKey)?.TypedData;
        //	if (toLinks == null) return;
        //	foreach (var dc in toLinks)
        //	{
        //		var docCtrl = dc.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.LinkToKey, null)?.TypedData.First();
        //		if (docCtrl == null) return;
        //		var isVisible = docCtrl.GetDereferencedField<BoolController>(KeyStore.AnnotationVisibilityKey, null);
        //		if (isVisible == null) return;
        //		docCtrl.ToggleHidden();
        //	}
        //}

        ////opens a flyout menu of all the links associated to the region
        ////clicking a link will then choose the desired link to pursue
        //private void AddToLinksMenu(List<DocumentController> linksList, KeyController directionKey, Point point, DocumentController theDoc)
        //{
        //	//add all links as menu items
        //	foreach (var linkedDoc in linksList.Select((ldoc) => ldoc.GetDataDocument()))
        //          {
        //              var dc = linkedDoc.GetDereferencedField<ListController<DocumentController>>(directionKey, null)?.TypedData.First() ?? linkedDoc;
        //              //format new item
        //              var linkItem = new MenuFlyoutItem() { Text = dc.Title };
        //              linkItem.BorderBrush = new SolidColorBrush(directionKey.Equals(KeyStore.LinkFromKey) ? Colors.Red : Colors.Green);
        //              linkItem.BorderThickness = new Thickness(1);
        //		linkItem.Click += (s, e) => this.RegionPressed(theDoc, point, dc);

        //		// Add the item to the menu.
        //           _linkFlyout.Items?.Add(linkItem);
        //          }
        //}

#endregion
    }
}
