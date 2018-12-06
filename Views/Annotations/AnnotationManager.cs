using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;

namespace Dash
{
    public class AnnotationManager
    {
        static public void FollowLink(DocumentView originatingView, DocumentController link, LinkDirection direction, IEnumerable<ILinkHandler> linkHandlers, LinkBehavior? overrideBehavior = null)
        {
            var linkContext = link.GetDataDocument().GetDereferencedField<BoolController>(KeyStore.LinkContextKey, null)?.Data ?? true;
            var document    = link.GetLinkedDocument(direction);

            switch (overrideBehavior ?? link.GetDataDocument().GetLinkBehavior())
            {
            case LinkBehavior.Dock: MainPage.Instance.DockLink(link, direction, linkContext); break;
            case LinkBehavior.Float: MainPage.Instance.AddFloatingDoc(document); break;
            case LinkBehavior.Overlay:  //default behavior of highlighting and toggling link visibility and docking when off screen
            case LinkBehavior.Annotate: linkHandlers.TakeWhile(hdlr => hdlr.HandleLink(link, direction) != LinkHandledResult.HandledClose).ToList(); break;
            case LinkBehavior.Follow:   //navigate to link
                if (!linkContext || !SplitFrame.TryNavigateToDocument(document))
                {
                    var docNode = DocumentTree.MainPageTree.FirstOrDefault(dn => dn.ViewDocument.Equals(document));
                    if (!linkContext || docNode == null)
                    {
                        SplitFrame.OpenInActiveFrame(document);
                    }
                    else
                    {
                        SplitFrame.OpenDocumentInWorkspace(docNode.ViewDocument, docNode.Parent.ViewDocument);
                    }
                }
                break;
            }
        }
        public static void FollowRegion(DocumentView originatingView, DocumentController region, IEnumerable<ILinkHandler> linkHandlers, Point flyoutPosition, string linkType = null)
        {
            var linksTo    = region.GetDataDocument().GetLinks(KeyStore.LinkToKey)  .Where((l) => l.GetDataDocument().GetLinkTag()?.Data.Equals("Travelog") != true || linkType == "Travelog").ToList();
            var linksFrom  = region.GetDataDocument().GetLinks(KeyStore.LinkFromKey).Where((l) => l.GetDataDocument().GetLinkTag()?.Data.Equals("Travelog") != true || linkType == "Travelog").ToList();
            if (region.GetDataDocument().GetRegions() is ListController<DocumentController> subregions)
            {
                foreach (var subregion in subregions.Select((sr) => sr.GetDataDocument()))
                {
                    linksTo.AddRange(subregion.GetLinks(KeyStore.LinkToKey));
                    linksFrom.AddRange(subregion.GetLinks(KeyStore.LinkFromKey));
                }
            }
            if ((linksTo?.Count ?? 0) + (linksFrom?.Count ?? 0) > 0)
            {
                var linkFlyout = MainPage.Instance.IsShiftPressed() ? new MenuFlyout() : null;
                linksTo.Where((l) => matchesLinkType(l, linkType)).ToList().ForEach(linkTo => {
                    processMatchingLink(linkTo, LinkDirection.ToDestination, originatingView, linkHandlers, linkFlyout);
                });

                linkFlyout?.Items?.Add(new MenuFlyoutSeparator());

                linksFrom.Where((l) => matchesLinkType(l, linkType)).ToList().ForEach(linkFrom => {
                    processMatchingLink(linkFrom, LinkDirection.ToSource, originatingView, linkHandlers, linkFlyout);
                });
                
                linkFlyout?.ShowAt(originatingView, flyoutPosition);
            }
	    }

        static private bool matchesLinkType(DocumentController link, string linkType)
        {
            return linkType == null || (link.GetDataDocument().GetLinkTag()?.Data.Equals(linkType) ?? false);
        }

        static private void processMatchingLink(DocumentController link, LinkDirection direction, DocumentView originatingView,  IEnumerable<ILinkHandler> linkHandlers, MenuFlyout linkFlyout)
        {
            if (linkFlyout == null)
            {
                FollowLink(originatingView, link, direction, linkHandlers);
            }
            else
            {
                var targetTitle = link.GetDataDocument().GetLinkedDocument(LinkDirection.ToSource).Title;
                var item        = new MenuFlyoutItem { Text = targetTitle, DataContext = link };
                var itemHdlr    = new RoutedEventHandler((s, e) => FollowLink(originatingView, link, direction, linkHandlers));
                item.Click += itemHdlr;
                linkFlyout.Items?.Add(item);
            }
        }

    }
}
