using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;

namespace Dash
{
    static public class AnnotationManager
    {
        static public void FollowLink(DocumentController link, LinkDirection direction, IEnumerable<ILinkHandler> linkHandlers, LinkBehavior? overrideBehavior = null)
        {
            var linkContext = link.GetDataDocument().GetDereferencedField<BoolController>(KeyStore.LinkContextKey, null)?.Data ?? true;
            var document    = link.GetLinkedDocument(direction);
            var target = document.GetRegionDefinition() ?? document;
            if (target != document)
            {
                target.GotoRegion(document);
            }

            switch (overrideBehavior ?? link.GetDataDocument().GetLinkBehavior())
            {
            case LinkBehavior.Dock:   
                    if (MainPage.Instance.MainSplitter.GetFrameWithDoc(target, true) is SplitFrame frame)
                    {
                        frame.Delete();
                    }
                    else
                    {
                        SplitFrame.OpenInInactiveFrame(target); //TODO Splitting: Deal with linkContext
                    }

                break;
            case LinkBehavior.Float:    MainPage.Instance.ToggleFloatingDoc(target); break;
            case LinkBehavior.Overlay:  //default behavior of highlighting and toggling link visibility and docking when off screen
            case LinkBehavior.Annotate: linkHandlers.TakeWhile(hdlr => hdlr.HandleLink(link, direction) != LinkHandledResult.HandledClose).ToList(); break;
            case LinkBehavior.Follow:   //navigate to link
                if (!linkContext || !SplitFrame.TryNavigateToDocument(target))
                {
                    var docNode = DocumentTree.MainPageTree.FirstOrDefault(dn => dn.ViewDocument.Equals(target));
                    if (!linkContext || docNode == null)
                    {
                        SplitFrame.OpenInActiveFrame(target);
                    }
                    else
                    {
                        SplitFrame.OpenDocumentInWorkspace(docNode.ViewDocument, docNode.Parent.ViewDocument);
                    }
                }
                break;
            }
        }
        public static void FollowRegion(FrameworkElement originatingView, DocumentController region, IEnumerable<ILinkHandler> linkHandlers, Point flyoutPosition, string linkType = null)
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
                linksTo.Where((l) => matchesLinkType(l, linkType)).ToList().ForEach(linkTo => 
                    processMatchingLink(linkTo, LinkDirection.ToDestination, linkHandlers, linkFlyout) );

                linkFlyout?.Items?.Add(new MenuFlyoutSeparator());

                linksFrom.Where((l) => matchesLinkType(l, linkType)).ToList().ForEach(linkFrom =>
                    processMatchingLink(linkFrom, LinkDirection.ToSource, linkHandlers, linkFlyout));
                
                linkFlyout?.ShowAt(originatingView, flyoutPosition);
            }
	    }

        static private bool matchesLinkType(DocumentController link, string linkType)
        {
            return linkType == null || (link.GetDataDocument().GetLinkTag()?.Data.Equals(linkType) ?? false);
        }

        static private void processMatchingLink(DocumentController link, LinkDirection direction, IEnumerable<ILinkHandler> linkHandlers, MenuFlyout linkFlyout)
        {
            if (linkFlyout == null)
            {
                FollowLink(link, direction, linkHandlers);
            }
            else
            {
                var targetTitle = link.GetDataDocument().GetLinkedDocument(LinkDirection.ToSource).Title;
                var item = new MenuFlyoutItem { Text = targetTitle, DataContext = link };
                item.Click += new RoutedEventHandler((s, e) => FollowLink(link, direction, linkHandlers)); ;
                linkFlyout.Items?.Add(item);
            }
        }

    }
}
