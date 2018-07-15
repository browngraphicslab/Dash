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
using Windows.UI.Xaml.Media;

namespace Dash
{
	public class AnnotationManager
	{
		private UIElement    _element;
		private MenuFlyout   _linkFlyout;

	    public enum AnnotationType
	    {
            None,
            RegionBox,
            TextSelection
	    }

		public AnnotationManager(UIElement uiElement)
		{
			_element = uiElement;
			this.FormatLinkMenu();
		}

        //navigation and toggling of linked annotations to the pressed region
        public void RegionPressed(DocumentController theDoc, Point pos, DocumentController chosenDC = null)
        {
            if (chosenDC != null)
            {
                showTargetDoc(chosenDC, pos);
            }
            else
            {
                var multiToLinks = showLinks(theDoc, KeyStore.LinkToKey, chosenDC);
                var multiFromLinks = showLinks(theDoc, KeyStore.LinkFromKey, chosenDC);

                if (multiToLinks.Count + multiFromLinks.Count == 1)
                {
                    showTargetDoc(multiToLinks.Count > 0 ? multiToLinks.First() : multiFromLinks.First(), pos);
                }
                else if (_linkFlyout.Items.Count == 0)
                {
                    if (multiToLinks != null)
                        AddToLinksMenu(multiToLinks, KeyStore.LinkToKey, pos, theDoc);
                    if (multiFromLinks != null)
                        AddToLinksMenu(multiFromLinks, KeyStore.LinkFromKey, pos, theDoc);

                    if (_linkFlyout.Items.Count > 0)
                        _linkFlyout.ShowAt((FrameworkElement) _element);
                }
            }
        }

        /// <summary>
        /// shows the (first) document linked from and to the source document.  If there are multiple, then a list is returned and
        /// nothing is shown.
        /// </summary>
        /// <param name="theDoc"></param>
        /// <param name="pos"></param>
        /// <param name="toOrFromLinks"></param>
        /// <param name="chosenDC"></param>
        List<DocumentController> showLinks(DocumentController theDoc, KeyController toOrFromLinks, DocumentController chosenDC)
        {
            var linkToDoc = theDoc.GetDataDocument().GetLinks(toOrFromLinks);
            if (linkToDoc != null)
            {
                //if there are multiple links & one has not been chosen, open the link menu to let user decide which link to pursue
                if (linkToDoc.Count > 1 && chosenDC == null)
                {
                    return linkToDoc.TypedData;
                }
                else
                {
                    //if there is only 1 link, get that link
                    if (linkToDoc.Count == 1)
                    {
                        var linkToRegionDoc = linkToDoc.TypedData.First();
                        var targetDoc = linkToRegionDoc.GetDataDocument().GetLinks(toOrFromLinks)?.TypedData.First() ?? linkToRegionDoc;
                        theDoc = targetDoc?.GetRegionDefinition() ?? targetDoc;
                    }
                    //if a link has been chosen, check if that doc controller has a parent to display instead
                    else if (linkToDoc.Count > 1 && chosenDC != null)
                    {
                        var targetDoc = chosenDC;
                        theDoc = targetDoc?.GetRegionDefinition() ?? targetDoc;
                    }

                    return new List<DocumentController>(new DocumentController[] { theDoc });
                }
            }
            return new List<DocumentController>();
        }
        //finds the nearest document view of the desired document controller that is displayed on the canvas
        DocumentView FindNearestDisplayedTarget(Point where, DocumentController targetData, bool onlyOnPage = true)
        {
            var dist = double.MaxValue;
            DocumentView nearest = null;
            var itemsPanelRoot = ((CollectionFreeformView) _element.GetFirstAncestorOfType<CollectionView>().CurrentView).xItemsControl
                .ItemsPanelRoot;
            if (itemsPanelRoot != null)
                foreach (var presenter in
                    itemsPanelRoot.Children.Select(c => c as ContentPresenter))
                {
                    var dvm = presenter.GetFirstDescendantOfType<DocumentView>();
                    if (dvm?.ViewModel.DataDocument.Id == targetData?.Id)
                    {
                        var mprect = dvm.GetBoundingRect(MainPage.Instance);
                        var center = new Point((mprect.Left + mprect.Right) / 2, (mprect.Top + mprect.Bottom) / 2);
                        if (!onlyOnPage || MainPage.Instance.GetBoundingRect().Contains(center))
                        {
                            var d = Math.Sqrt((where.X - center.X) * (where.X - center.X) +
                                              (where.Y - center.Y) * (where.Y - center.Y));
                            if (d < dist)
                            {
                                d = dist;
                                nearest = dvm;
                            }
                        }
                    }
                }

            return nearest;
        }

        private void showTargetDoc(DocumentController theDoc, Point pos)
        {
            //find nearest linked doc that is currently displayed
            var cvm = _element.GetFirstAncestorOfType<CollectionView>()?.ViewModel;
            var nearestOnScreen = FindNearestDisplayedTarget(pos, theDoc?.GetDataDocument(), true);
            var nearestOnCollection = FindNearestDisplayedTarget(pos, theDoc?.GetDataDocument(), false);
            var docview = _element.GetFirstAncestorOfType<DocumentView>();
            var pt = new Point(docview.ViewModel.XPos + docview.ActualWidth, docview.ViewModel.YPos);

            if (nearestOnCollection != null && !nearestOnCollection.Equals(_element.GetFirstAncestorOfType<DocumentView>()))
            {
                if (MainPage.Instance.IsCtrlPressed())
                {
                    var viewCopy = theDoc.GetViewCopy(pt);
                    Actions.DisplayDocument(_element.GetFirstAncestorOfType<CollectionView>()?.ViewModel,
                        viewCopy);
                    // ctrl-clicking on a hyperlink creates a view copy next to the document. The view copy is marked transient so that if
                    // the hyperlink anchor is clicked again the view copy will be removed instead of hidden.
                    viewCopy.SetField<NumberController>(KeyStore.TransientKey, 1, true);
                }
                else if (nearestOnScreen != null)
                {
                    // remove hyperlink targets marked as Transient, otherwise hide the document so that it will be redisplayed in the same location.
                    if (nearestOnScreen.ViewModel.DocumentController
                            .GetDereferencedField<NumberController>(KeyStore.TransientKey, null)?.Data == 1)
                        cvm.RemoveDocument(nearestOnScreen.ViewModel.DocumentController);
                    else
                        Actions.HideDocument(cvm, nearestOnScreen.ViewModel.DocumentController);

                }
                else
                {
                    //navigate to the linked doc
                    MainPage.Instance.NavigateToDocumentInWorkspace(nearestOnCollection.ViewModel.DocumentController, true);

                    //images have additional highlighting features that should be implemented
                    if (_element is EditableImage)
                    {
                        var image = (EditableImage)_element;
                        image.AnnotationManager.UpdateHighlight(nearestOnCollection);
                    }
                }
            }
            else
            {
                //toggle the visibility of the linked doc
                if (theDoc != null)
                {
                    if (!Actions.UnHideDocument(_element.GetFirstAncestorOfType<CollectionView>()?.ViewModel, theDoc))
                    {
                        Actions.DisplayDocument(_element.GetFirstAncestorOfType<CollectionView>()?.ViewModel, theDoc.GetViewCopy(pt));
                    }

                } //if working with RichTextView, check web context as well
                else if (_element is RichTextView)
                {
                    var richTextView = (RichTextView)_element;
                    richTextView.CheckWebContext(nearestOnCollection, pt, theDoc);
                }
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
