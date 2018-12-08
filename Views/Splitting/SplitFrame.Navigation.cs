using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public partial class SplitFrame
    {
        #region Navigation
        public static DocumentController OpenInActiveFrame(DocumentController doc)
        {
            var newDoc = ActiveFrame.OpenDocument(doc);
            return newDoc;
        }

        public static DocumentController OpenInInactiveFrame(DocumentController doc)
        {
            var frames = MainPage.Instance.MainSplitter.GetChildFrames().Where(sf => sf != ActiveFrame).ToList();
            if (frames.Count == 0)
            {
                return ActiveFrame.Split(SplitDirection.Right, doc, true);
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

                //if (frame.ActualWidth < MainPage.Instance.ActualWidth / 2)
                //{
                //    var columns = frame.GetFirstAncestorOfType<SplitManager>().GetFirstAncestorOfType<SplitManager>().Columns;
                //    var frameCol = Grid.GetColumn(frame.GetFirstAncestorOfType<SplitManager>());
                //    columns[frameCol].Width = new GridLength(1, GridUnitType.Star);
                //    columns[frameCol - 2].Width = new GridLength(1, GridUnitType.Star);
                //}

                return frame.OpenDocument(doc);
            }
        }

        private void AnimateToDocument(DocumentController document)
        {
            var position = document.GetPosition() ?? new Point();
            var center = position;
            var size = document.GetActualSize() ?? new Point();
            center.X += (size.X - ActualWidth) / 2;
            center.Y += (size.Y - ActualHeight) / 2;
            center.X *= -1;
            center.Y *= -1;

            var widthRatio = ActualWidth / size.X;
            var heightRatio = ActualHeight / size.Y;
            widthRatio = Math.Clamp(widthRatio, 0.2, 6);
            heightRatio = Math.Clamp(heightRatio, 0.2, 6);
            var scale = Math.Min(widthRatio, heightRatio);
            scale *= 0.8;

            var col = ViewModel.Content as CollectionView;
            var ffv = col?.CurrentView as CollectionFreeformBase;
            ffv?.SetTransformAnimated(new TranslateTransform
            {
                X = center.X,
                Y = center.Y
            }, new ScaleTransform
            {
                CenterX = position.X + size.X / 2,
                CenterY = position.Y + size.Y / 2,
                ScaleX = scale,
                ScaleY = scale
            });
        }

        public static DocumentController OpenInInactiveWorkspace(DocumentController doc, DocumentController workspace)
        {
            var frames = MainPage.Instance.MainSplitter.GetChildFrames().Where(sf => sf != ActiveFrame).ToList();
            if (frames.Count == 0)
            {
                return ActiveFrame.Split(SplitDirection.Right, doc, true);
            }
                var frame = frames[0];
                var area = frame.ActualWidth * frame.ActualHeight;
                for (var i = 1; i < frames.Count; ++i)
                {
                    // TODO: prioritize frames with workspace already open
                    var curFrame = frames[i];
                    var curArea = curFrame.ActualWidth * curFrame.ActualHeight;
                    if (curArea > area)
                    {
                        area = curArea;
                        frame = curFrame;
                    }
                }

                //if (frame.ActualWidth < MainPage.Instance.ActualWidth / 2)
                //{
                //    var columns = frame.GetFirstAncestorOfType<SplitManager>().GetFirstAncestorOfType<SplitManager>().Columns;
                //    var frameCol = Grid.GetColumn(frame.GetFirstAncestorOfType<SplitManager>());
                //    columns[frameCol].Width = new GridLength(1, GridUnitType.Star);
                //    columns[frameCol - 2].Width = new GridLength(1, GridUnitType.Star);
                //}

                return frame.OpenDocument(doc,workspace);
        }

        public static DocumentController OpenDocumentInWorkspace(DocumentController document, DocumentController workspace)
        {
            return ActiveFrame.OpenDocument(document, workspace);
        }

        public static bool TryNavigateToDocument(DocumentController document, bool useDataDoc = false)
        {
            var workspace = ActiveFrame.ViewModel.DocumentController;
            if (useDataDoc)
            {
                var dataDoc = document.GetDataDocument();
                document = workspace.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null)
                    ?.FirstOrDefault(doc => doc.GetDataDocument().Equals(dataDoc));
                if (document == null)
                {
                    return false;
                }
            }
            else
            {
                if (!(workspace.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null)
                          ?.Contains(document) ?? false))
                {
                    return false;
                }
            }

            ActiveFrame.AnimateToDocument(document);
            return true;
        }

        #endregion

        #region Highlighting

        private static void HighlightViewModel(DocumentViewModel viewModel, HighlightMode highlightMode)
        {
            switch (highlightMode)
            {
            case HighlightMode.Highlight:
                viewModel.SetHighlight(true);
                break;
            case HighlightMode.Unhighlight:
                viewModel.SetHighlight(false);
                break;
            case HighlightMode.ToggleHighlight:
                viewModel.ToggleHighlight();
                break;
            }
        }

        public static void HighlightDoc(DocumentController document, HighlightMode mode, bool useDataDoc = false, bool unhighlightOthers = true)
        {
            HighlightDocs(new List<DocumentController> { document }, mode, useDataDoc, unhighlightOthers);
        }

        public static void HighlightDocs(IEnumerable<DocumentController> documents, HighlightMode mode, bool useDataDoc = false, bool unhighlightOthers = true)
        {
            var docs = new HashSet<DocumentController>(useDataDoc ? documents.Select(doc => doc.GetDataDocument()) : documents);
            var dvms = MainPage.Instance.MainSplitter.GetChildFrames().Select(sf => sf.ViewModel).ToList();
            while (dvms.Any())
            {
                var dvm = dvms[dvms.Count - 1];
                dvms.RemoveAt(dvms.Count - 1);

                if (docs.Contains(useDataDoc ? dvm.DataDocument : dvm.DocumentController))
                {
                    HighlightViewModel(dvm, mode);
                }
                else if (unhighlightOthers && mode == HighlightMode.Highlight)
                {
                    HighlightViewModel(dvm, HighlightMode.Unhighlight);
                }

                if (dvm.Content.DataContext is CollectionViewModel cvm)
                {
                    dvms.AddRange(cvm.DocumentViewModels);
                }
            }
        }

        public static void UnhighlightAllDocs()
        {
            var dvms = MainPage.Instance.MainSplitter.GetChildFrames().Select(sf => sf.ViewModel).ToList();
            while (dvms.Any())
            {
                var dvm = dvms[dvms.Count - 1];
                dvms.RemoveAt(dvms.Count - 1);

                HighlightViewModel(dvm, HighlightMode.Unhighlight);

                if (dvm.Content.DataContext is CollectionViewModel cvm)
                {
                    dvms.AddRange(cvm.DocumentViewModels);
                }
            }

        }

        public enum HighlightMode
        {
            Highlight,
            Unhighlight,
            ToggleHighlight
        }

        #endregion
    }
}
