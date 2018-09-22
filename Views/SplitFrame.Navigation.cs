using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public sealed partial class SplitFrame
    {
        public static DocumentController OpenInActiveFrame(DocumentController doc)
        {
            return ActiveFrame.OpenDocument(doc);
        }

        public static DocumentController OpenInInactiveFrame(DocumentController doc)
        {
            var frames = MainPage.Instance.MainSplitter.GetChildFrames().Where(sf => sf != ActiveFrame).ToList();
            if (frames.Count == 0)
            {
                return ActiveFrame.TrySplit(SplitDirection.Right, doc, true);
            }
            else
            {
                var frame = frames[0];
                var area = frame.ActualWidth * frame.ActualHeight;
                for (var i = 1; i < frames.Count; ++i)
                {
                    var curFrame = frames[i];
                    var curArea = curFrame.ActualWidth * curFrame.ActualHeight;
                    if (curArea > area)
                    {
                        area = curArea;
                        frame = curFrame;
                    }
                }

                return frame.OpenDocument(doc);
            }
        }

        public static DocumentController OpenDocumentInWorkspace(DocumentController document, DocumentController workspace)
        {
            Debug.Assert(workspace.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null)?.Contains(document) ?? false);

            var center = document.GetPosition() ?? new Point();
            var size = document.GetActualSize() ?? new Point();
            center.X += (size.X - ActiveFrame.ActualWidth) / 2;
            center.Y += (size.Y - ActiveFrame.ActualHeight) / 2;
            center.X = -center.X;
            center.Y = -center.Y;
            if (ActiveFrame.ViewModel.DataDocument.Equals(workspace.GetDataDocument())) //Collection is already open, so we need to animate to it
            {
                var col = ActiveFrame.ViewModel.Content as CollectionView;
                var ffv = col?.CurrentView as CollectionFreeformBase;
                ffv?.SetTransformAnimated(new TranslateTransform
                {
                    X = center.X,
                    Y = center.Y
                }, new ScaleTransform
                {
                    ScaleX = 1,
                    ScaleY = 1
                });

                return ActiveFrame.ViewModel.DocumentController;
            }
            else
            {
                workspace = ActiveFrame.OpenDocument(workspace);
                workspace.SetField<PointController>(KeyStore.PanPositionKey, center, true);
                workspace.SetField<PointController>(KeyStore.PanZoomKey, new Point(1, 1), true);
                return workspace;
            }
        }
    }
}
