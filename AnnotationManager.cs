using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Dash;
using Dash.Views;
using iText.StyledXmlParser.Jsoup.Select;

namespace Dash
{
	public class AnnotationManager
	{
		private FrameworkElement    _element;
		private MenuFlyout   _linkFlyout;
		public enum AnnotationType
		{
			None,
			RegionBox,
			TextSelection,
			Ink
		}

		public AnnotationManager(FrameworkElement uiElement)
		{
			_element = uiElement;
			FormatLinkMenu();
		}

        //navigation and toggling of linked annotations to the pressed region
        public void RegionPressed(DocumentController theDoc, Point pos, DocumentController chosenDC = null)
		{
			if (chosenDC != null)
            {
				// navigate to the doc if ctrl is pressed, unless if it's super far away, in which case dock it. FollowDocument will take care of that.
				// I think chosenDC is only not-null when it's selected from the LinkFlyoutMenu, which only triggers under ctrl anyways.
                FollowDocument(chosenDC, pos);
            }
            else
			{
				var toLinks = GetLinks(theDoc, false);
				var fromLinks = GetLinks(theDoc, true);

				// choose link to follow by showing flyout
				if (MainPage.Instance.IsCtrlPressed())
                {
	                if (_linkFlyout.Items == null || _linkFlyout.Items.Count != 0) return;

	                if (toLinks?.Count + fromLinks?.Count == 1)
	                {
		                var dc = toLinks.Count > 0 ? toLinks.First() : fromLinks.First();
		                dc = dc.GetDataDocument()
			                .GetDereferencedField<ListController<DocumentController>>(
				                toLinks.Count > 0 ? KeyStore.LinkToKey : KeyStore.LinkFromKey, null).TypedData.First();
		                FollowDocument(dc, pos);
		                return;
	                }

					if (toLinks != null)
		                AddToLinksMenu(toLinks, KeyStore.LinkToKey, pos, theDoc);
	                if (fromLinks != null)
		                AddToLinksMenu(fromLinks, KeyStore.LinkFromKey, pos, theDoc);


	                if (_linkFlyout.Items.Count > 0)
		                _linkFlyout.ShowAt(_element);
                }
				// shows everything
				else
				{
					// cycle through and show everything
					foreach (var dc in toLinks)
					{
						
					}
				}

            }
        }

        // follows the document in the workspace, and heuristically determines if it's too far away and should be docked
	    private void FollowDocument(DocumentController target, Point pos)
	    {
		    DocumentController docToFollow = target;

			// is a region
			if (target.GetRegionDefinition() != null)
				docToFollow = target.GetRegionDefinition();

			var nearestOnScreen = FindNearestDisplayedTarget(pos, docToFollow?.GetDataDocument(), true);
			var nearestOnCollection = FindNearestDisplayedTarget(pos, docToFollow?.GetDataDocument(), false);

		    var toFollow = nearestOnScreen ?? nearestOnCollection;
		    if (toFollow == null) return;
			// true if it's in sight, false if hidden
		    var intersectWithParentCollection = RectHelper.Intersect(toFollow.GetBoundingRect(MainPage.Instance),
			                                        toFollow.GetFirstAncestorOfType<CollectionFreeformView>()
				                                        .GetBoundingRect(MainPage.Instance)) !=
		                                        Rect.Empty || toFollow.GetFirstAncestorOfType<CollectionFreeformView>().Equals(MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionFreeformView>());

			if (nearestOnScreen == null || !intersectWithParentCollection)
			{
				// calculate distance of how off-screen it is
				var distPoint = MainPage.Instance.GetDistanceFromMainDocCenter(docToFollow);
				var dist = Math.Sqrt(distPoint.X * distPoint.X + distPoint.Y * distPoint.Y);
			    var threshold = MainPage.Instance.MainDocView.ActualWidth * 1.5;

				// not visible in its current collectionview, or too far away: dock it
				if (dist > threshold || !intersectWithParentCollection)
				{
					// see if it's already docked
					var dv = MainPage.Instance.DockManager.GetDockedView(target);

					// if not docked
					if (dv == null)
					{
						var dir = distPoint.X > 0 ? DockDirection.Left : DockDirection.Right;
						MainPage.Instance.DockManager.Dock(target, dir);
					}

					// if it's already docked, then highlight it instead of docking it again
					else
					{
						dv.FlashSelection();
					}
				}
				// should always be true
				else if (dist <= threshold)
				{
					MainPage.Instance.NavigateToDocumentInWorkspace(nearestOnCollection.ViewModel.DocumentController, true, false);
				}
		    }

		    // we only want to pan when the document isn't currently on the screen

		    var viewToFollow = nearestOnScreen ?? nearestOnCollection;
		    var va = viewToFollow.GetFirstDescendantOfType<IVisualAnnotatable>();
		    va?.GetAnnotationManager().SelectRegion(target);
		    if (va is CustomPdfView pdf)
		    {
				// TODO make it scroll to the region's position as well
		    }

		    //images have additional highlighting features that should be implemented
			if (!(_element is IVisualAnnotatable)) return;

			var element = (IVisualAnnotatable)_element;
			element.GetAnnotationManager().UpdateHighlight(viewToFollow);
		}
        
        List<DocumentController> GetLinks(DocumentController theDoc, bool getFromLinks)
        {
	        var links = getFromLinks ? theDoc.GetDataDocument().GetLinks(KeyStore.LinkFromKey)?.TypedData : theDoc.GetDataDocument().GetLinks(KeyStore.LinkToKey)?.TypedData;

	        // does this list exist? If not, return an empty list since there's nothing in it
	        return links ?? new List<DocumentController>();
        }

        //finds the nearest document view of the desired document controller that is displayed on the canvas
        DocumentView FindNearestDisplayedTarget(Point where, DocumentController targetData, bool onlyOnPage = true)
        {
	        var collection = _element.GetFirstAncestorOfType<CollectionView>();
	        DocumentView nearest = null;

	        if (collection != null) nearest = NearestOnCollection(where, targetData, collection, onlyOnPage);

			// means something was found
	        if (nearest != null) return nearest;

	        // haven't found a doc of the matching criteria on this current collection, so queue up all the collections and start searching
			var q = new Queue<CollectionView>();
	        var mainCollection = MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>();
			q.Enqueue(mainCollection);
	        foreach (var nestedCollection in mainCollection.GetDescendantsOfType<CollectionView>())
	        {
		        q.Enqueue(nestedCollection);
	        }

			// iterate through every document looking for it
	        var dist = double.MaxValue;
			while (q.Count != 0)
	        {
		        var curr = q.Dequeue();
		        var nearestOnThisCollection = NearestOnCollection(where, targetData, curr, onlyOnPage);
		        if (nearestOnThisCollection == null) continue;
		        var d = GetDistanceFromDocument(where, nearestOnThisCollection);
		        if (d < dist)
		        {
			        dist = d;
			        nearest = nearestOnThisCollection;
		        }
	        }

		    return nearest;
        }

		private DocumentView NearestOnCollection(Point where, DocumentController targetData, CollectionView collection, bool onlyOnPage = true)
		{
			var dist = double.MaxValue;
			DocumentView nearest = null;
			
			// TODO expand this to work with treeviews too...?
			var itemsPanelRoot = (collection.CurrentView as CollectionFreeformView)?.xItemsControl.ItemsPanelRoot;
			if (itemsPanelRoot == null) return null;
			foreach (var presenter in itemsPanelRoot.Children.Select(c => c as ContentPresenter))
			{
				var dvm = presenter.GetFirstDescendantOfType<DocumentView>();
				if (dvm?.ViewModel.DataDocument.Id != targetData?.Id) continue;
				var mprect = dvm.GetBoundingRect(MainPage.Instance);
				if (onlyOnPage && RectHelper.Intersect(MainPage.Instance.GetBoundingRect(), mprect).IsEmpty) continue;

				var d = GetDistanceFromDocument(where, dvm);

				if (d < dist)
				{
					dist = d;
					nearest = dvm;
				}
			}
			return nearest;
		}

		private double GetDistanceFromDocument(Point where, DocumentView view)
		{
			var mprect = view.GetBoundingRect(MainPage.Instance);
			var center = new Point((mprect.Left + mprect.Right) / 2, (mprect.Top + mprect.Bottom) / 2);
			return Math.Sqrt((@where.X - center.X) * (@where.X - center.X) + (@where.Y - center.Y) * (@where.Y - center.Y));
		}

		// TODO: figure out this interaction once region selection is working
		// figures out what to do once a link's home region has been tapped based on the current selection status
		private void TriggerVisibilityBehaviorOnAnnotation(DocumentController target, bool isRegionCurrentlySelected, Point pos)
		{
			// toggle visibility
			if (isRegionCurrentlySelected)
				target.TogglePinUnpin();
			else
				ShowOrHideDocument(target, pos, true);
		}

		// TODO: figure out this interaction once region selection is working
		// shows the document
		private void ShowOrHideDocument(DocumentController target, Point pos, bool toVisible)
        {
	        if (target != null)
		        target.SetHidden(!toVisible);
	        else if (_element is RichTextView rtv)
	        {
		        //find nearest linked doc that is currently displayed
		        var nearestOnCollection = FindNearestDisplayedTarget(pos, target?.GetDataDocument(), false);
		        var docview = _element.GetFirstAncestorOfType<DocumentView>();
		        var pt = new Point(docview.ViewModel.XPos + docview.ActualWidth, docview.ViewModel.YPos);

		        rtv.CheckWebContext(nearestOnCollection, pt, target);
	        }
        }

        //creates & adds handlers to the link menu
        private void FormatLinkMenu()
		{
			_linkFlyout = new MenuFlyout();

			_linkFlyout.Closed += (s, e) => _linkFlyout.Items?.Clear();
			
		}

		//opens a flyout menu of all the links associated to the region
		//clicking a link will then choose the desired link to pursue
		private void AddToLinksMenu(List<DocumentController> linksList, KeyController directionKey, Point point, DocumentController theDoc)
		{
			//add all links as menu items
			foreach (var linkedDoc in linksList.Select((ldoc) => ldoc.GetDataDocument()))
            {
                var dc = linkedDoc.GetDereferencedField<ListController<DocumentController>>(directionKey, null)?.TypedData.First() ?? linkedDoc;
                //format new item
                var linkItem = new MenuFlyoutItem() { Text = dc.Title };
                linkItem.BorderBrush = new SolidColorBrush(directionKey.Equals(KeyStore.LinkFromKey) ? Colors.Red : Colors.Green);
                linkItem.BorderThickness = new Thickness(1);
				linkItem.Click += (s, e) => this.RegionPressed(theDoc, point, dc);

				// Add the item to the menu.
	            _linkFlyout.Items?.Add(linkItem);
            }
		}
	}
}
