using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;

namespace Dash
{
	public class AnnotationManager
	{
		private UIElement _element = null;
		private DocumentView _docview;
		private MenuFlyout _linkFlyout;
		public bool _isLinkMenuOpen = false;

		public AnnotationManager(UIElement uiElement, DocumentView docView)
		{
			_element = uiElement;
			_docview = docView;
			this.FormatLinkMenu();
		}

		//navigation and toggling of linked annotations to the pressed region
		public void RegionPressed(DocumentController theDoc, Windows.Foundation.Point pos, DocumentController chosenDC = null)
		{
			//get "linked to" docs and "linked from" docs
			var linkFromDoc = theDoc.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.LinkFromKey, null);
			var linkToDoc   = theDoc.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.LinkToKey, null);
			if (linkFromDoc != null)
			{
				//if there is only 1 link, get that link
				if (linkFromDoc.Count == 1)
				{
					var targetDoc = linkFromDoc.TypedData.First().GetDataDocument()
						.GetDereferencedField<ListController<DocumentController>>(KeyStore.LinkFromKey, null).TypedData
						.First();
					targetDoc =
						targetDoc?.GetDereferencedField<DocumentController>(KeyStore.RegionDefinitionKey, null) ??
						targetDoc;
					theDoc = targetDoc;
				} 
				//if there are multiple links & one has not been chosen, open the link menu to let user decide which link to pursue
				else if (linkFromDoc.Count > 1 && chosenDC == null)
				{
					this.OpenLinkMenu(linkFromDoc.TypedData, KeyStore.LinkFromKey, pos, theDoc);
					return;
				}
				//if a link has been chosen, check if that doc controller has a parent to display instead
				else if (linkFromDoc.Count > 1 && chosenDC != null)
				{
					var targetDoc = chosenDC;
					targetDoc =
						targetDoc?.GetDereferencedField<DocumentController>(KeyStore.RegionDefinitionKey, null) ??
						targetDoc;
					theDoc = targetDoc;
				}

			} //same procedure for links TO the doc
			else if (linkToDoc != null)
			{
				if (linkToDoc.Count == 1)
				{
					var targetDoc = linkToDoc.TypedData.First().GetDataDocument()
						.GetDereferencedField<ListController<DocumentController>>(KeyStore.LinkToKey, null).TypedData
						.First();
					targetDoc =
						targetDoc?.GetDereferencedField<DocumentController>(KeyStore.RegionDefinitionKey, null) ??
						targetDoc;
					theDoc = targetDoc;
				}
				else if (linkToDoc.Count > 1 && chosenDC == null)
				{
					this.OpenLinkMenu(linkToDoc.TypedData, KeyStore.LinkToKey, pos, theDoc);
					Debug.WriteLine("LINK TO DOC CONTAINS MORE THAN 1");
					return;
				}
				else if (linkToDoc.Count > 1 && chosenDC != null)
				{
					var targetDoc = chosenDC;
					targetDoc = targetDoc?.GetDereferencedField<DocumentController>(KeyStore.RegionDefinitionKey,
									null) ?? targetDoc;
					theDoc = targetDoc;
				}
			} 
			//else, there are no links and nothing should happen
			else
			{
				return;
			}
			
			//find nearest linked doc that is currently displayed
			var cvm = _element.GetFirstAncestorOfType<CollectionView>()?.ViewModel;
			var nearestOnScreen = FindNearestDisplayedTarget(pos, theDoc?.GetDataDocument(), true);
			var nearestOnCollection = FindNearestDisplayedTarget(pos, theDoc?.GetDataDocument(), false);
			if (_docview == null) _docview = _element.GetFirstAncestorOfType<DocumentView>();
			var pt = new Point(_docview.ViewModel.XPos + _docview.ActualWidth, _docview.ViewModel.YPos);

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
						var image = (EditableImage) _element;
						image.UpdateHighlight(nearestOnCollection);
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

			//finds the nearest document view of the desired document controller that is displayed on the canvas
			DocumentView FindNearestDisplayedTarget(Point where, DocumentController targetData, bool onlyOnPage = true)
			{
				double dist = double.MaxValue;
				DocumentView nearest = null;
				foreach (var presenter in
					(_element.GetFirstAncestorOfType<CollectionView>().CurrentView as CollectionFreeformView).xItemsControl
					.ItemsPanelRoot.Children.Select((c) => (c as ContentPresenter)))
				{
					var dvm = presenter.GetFirstDescendantOfType<DocumentView>();
					if (dvm.ViewModel.DataDocument.Id == targetData?.Id)
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
		}

		//creates & adds handlers to the link menu
		private void FormatLinkMenu()
		{
			_linkFlyout = new MenuFlyout();

			_linkFlyout.Closed += (s, e) =>
			{
				_isLinkMenuOpen = false;
				_linkFlyout.Items.Clear();
			};

			_linkFlyout.Opening += (s, e) =>
			{
				_isLinkMenuOpen = true;
			};
		}

		//opens a flyout menu of all the links associated to the region
		//clicking a link will then choose the desired link to pursue
		private void OpenLinkMenu(List<DocumentController> linksList, KeyController directionKey, Point point, DocumentController theDoc)
		{
			if (_isLinkMenuOpen == false)
			{
				//add all links as menu items
				foreach (DocumentController linkedDoc in linksList)
				{
					//format new item
					var linkItem = new MenuFlyoutItem();
					var dc = linkedDoc.GetDataDocument()
						.GetDereferencedField<ListController<DocumentController>>(directionKey, null).TypedData.First();
					linkItem.Text = dc.Title;
					linkItem.Click += (s, e) =>
					{
						this.RegionPressed(theDoc, point, dc);
					};

					// Add the item to the menu.
					_linkFlyout.Items.Add(linkItem);

				}

				//var selectedText = (FrameworkElement) xRichEditBox.Document.Selection;
				_linkFlyout.ShowAt((FrameworkElement)_element);
			}

		}
	}
}
