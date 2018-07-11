using System;
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

namespace Dash
{
	public class AnnotationManager
	{
		private FrameworkElement    _element;
		private MenuFlyout   _linkFlyout;

		public AnnotationManager(FrameworkElement uiElement)
		{
			_element = uiElement;
			FormatLinkMenu();
		}

        //navigation and toggling of linked annotations to the pressed region
        public void RegionPressed(DocumentController theDoc, Point pos, DocumentController chosenDC = null)
		{
			var isAlreadySelected = SelectionManager.IsRegionSelected(theDoc);
			SelectionManager.SelectRegion(theDoc);

			if (chosenDC != null)
            {
	            chosenDC = chosenDC.GetField<ListController<DocumentController>>(KeyStore.LinkToKey).TypedData.First();

				// navigate to the doc if ctrl is pressed, unless if it's super far away, in which case dock it. FollowDocument will take care of that.
				if (MainPage.Instance.IsCtrlPressed())
                    FollowDocument(chosenDC, pos);
                // otherwise, select it
                else
                {
	                TriggerVisibilityBehaviorOnAnnotation(chosenDC, isAlreadySelected, pos);
                }
            }
            else
            {
                // choose link to follow by showing flyout
                if (MainPage.Instance.IsCtrlPressed())
                {

                }
				// shows everything
				else
				{
	                var toLinks = GetLinks(theDoc, false);

					Debug.WriteLine(toLinks.Count);
					// cycle through and show everything
					foreach (var dc in toLinks)
					{
						TriggerVisibilityBehaviorOnAnnotation(GetLinks(dc, false).First(), isAlreadySelected, pos);
					}

	                //// link flyouts' items are always cleared upon closing, so this should always be true when it's closed.
	                //if (_linkFlyout.Items.Count == 0)
	                //{
		               // if (multiToLinks != null)
			              //  AddToLinksMenu(multiToLinks, KeyStore.LinkToKey, pos, theDoc);
		               // if (multiFromLinks != null)
			              //  AddToLinksMenu(multiFromLinks, KeyStore.LinkFromKey, pos, theDoc);

		               // if (_linkFlyout.Items.Count > 0)
			              //  _linkFlyout.ShowAt(_element);
	                //}
				}

            }
        }

        // follows the document in the workspace, and heuristically determines if it's too far away and should be docked
	    private void FollowDocument(DocumentController target, Point pos)
	    {
	        var cvm = _element.GetFirstAncestorOfType<CollectionView>()?.ViewModel;
	        var nearestOnScreen = FindNearestDisplayedTarget(pos, target?.GetDataDocument(), true);
	        var nearestOnCollection = FindNearestDisplayedTarget(pos, target?.GetDataDocument(), false);
	        var docview = _element.GetFirstAncestorOfType<DocumentView>();
	        var pt = new Point(docview.ViewModel.XPos + docview.ActualWidth, docview.ViewModel.YPos);

            // TODO: figure out the distance/docking thing
            MainPage.Instance.NavigateToDocumentInWorkspace(nearestOnCollection.ViewModel.DocumentController, true);

	        //images have additional highlighting features that should be implemented
		    if (!(_element is IVisualAnnotatable)) return;

		    var element = (IVisualAnnotatable)_element;
		    element.GetAnnotationManager().UpdateHighlight(nearestOnCollection);
	    }
        
        List<DocumentController> GetLinks(DocumentController theDoc, bool getFromLinks)
        {
	        var links = getFromLinks ? theDoc.GetDataDocument().GetLinks(KeyStore.LinkFromKey).TypedData : theDoc.GetDataDocument().GetLinks(KeyStore.LinkToKey).TypedData;

	        // does this list exist? If not, return an empty list since there's nothing in it
	        return links ?? new List<DocumentController>();
        }

        //finds the nearest document view of the desired document controller that is displayed on the canvas
        DocumentView FindNearestDisplayedTarget(Point where, DocumentController targetData, bool onlyOnPage = true)
        {
            var dist = double.MaxValue;
            DocumentView nearest = null;
            var itemsPanelRoot = ((CollectionFreeformView) _element.GetFirstAncestorOfType<CollectionView>().CurrentView).xItemsControl
                .ItemsPanelRoot;
	        if (itemsPanelRoot == null) return nearest;
	        foreach (var presenter in
		        itemsPanelRoot.Children.Select(c => c as ContentPresenter))
	        {
		        var dvm = presenter.GetFirstDescendantOfType<DocumentView>();
		        if (dvm?.ViewModel.DataDocument.Id != targetData?.Id) continue;
		        var mprect = dvm.GetBoundingRect(MainPage.Instance);
		        var center = new Point((mprect.Left + mprect.Right) / 2, (mprect.Top + mprect.Bottom) / 2);
		        if (onlyOnPage && !MainPage.Instance.GetBoundingRect().Contains(center)) continue;
		        var d = Math.Sqrt((@where.X - center.X) * (@where.X - center.X) +
		                          (@where.Y - center.Y) * (@where.Y - center.Y));
		        if (!(d < dist)) continue;
		        d = dist;
		        nearest = dvm;
	        }

	        return nearest;
		}

		// figures out what to do once a link's home region has been tapped based on the current selection status
		private void TriggerVisibilityBehaviorOnAnnotation(DocumentController target, bool isRegionCurrentlySelected, Point pos)
		{
			// toggle visibility
			if (isRegionCurrentlySelected)
				target.TogglePinUnpin();
			else
				ShowOrHideDocument(target, pos, true);
		}

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

			_linkFlyout.Closed += (s, e) => _linkFlyout.Items.Clear();
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
				_linkFlyout.Items.Add(linkItem);
			}
		}
	}
}
