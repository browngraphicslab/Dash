using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Casting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Dash
{
    public class DocumentSelectionChangedEventArgs
    {
        public List<DocumentView> DeselectedViews, SelectedViews;

        public DocumentSelectionChangedEventArgs()
        {
            DeselectedViews = new List<DocumentView>();
            SelectedViews = new List<DocumentView>();
        }

        public DocumentSelectionChangedEventArgs(List<DocumentView> deselectedViews, List<DocumentView> selectedViews)
        {
            DeselectedViews = deselectedViews;
            SelectedViews = selectedViews;
        }
    }

    public static class SelectionManager
    {
        public static IEnumerable<DocumentView> SelectedDocs => _selectedDocs.Where(dv => dv?.ViewModel?.DocumentController != null).ToList();
        private static List<DocumentView> _selectedDocs = new List<DocumentView>();
        public static DocumentController SelectedRegion;

        public delegate void SelectionChangedHandler(DocumentSelectionChangedEventArgs args);
        public static event SelectionChangedHandler SelectionChanged;

        static SelectionManager()
        {
            //SelectionChanged += e => DeselectRegion();
        }

        public static void ToggleSelection(DocumentView doc)
        {
            if (_selectedDocs.Contains(doc))
                Deselect(doc);
            else
                Select(doc);
        }

        public static void Select(DocumentView doc)
        {
            var args = new DocumentSelectionChangedEventArgs();
            SelectHelper(doc);
            args.SelectedViews.Add(doc);
            var pdf = doc.GetFirstDescendantOfType<CustomPdfView>();
            if (pdf != null)
            {
                pdf.ShowPdfControls();
            }


        }

        public static void SelectRegion(DocumentController region)
        {
            // todo: this is a hack, should refactor elsewhere later
            if (SelectedRegion != null)
            {
                var toLinks = SelectedRegion.GetDataDocument().GetLinks(KeyStore.LinkToKey)?.TypedData;
                if (toLinks != null)
                {
                    foreach (var dc in toLinks)
                    {
                        var docCtrl = dc.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.LinkToKey, null)?.TypedData.First();
                        if (docCtrl == null) return;
                        docCtrl.ToggleHidden();
                        MainPage.Instance.HighlightDoc(docCtrl, false, 2);
                    }
                }
            }

            SelectedRegion = region;

            if (region != null)
            {
                var newToLinks = region.GetDataDocument().GetLinks(KeyStore.LinkToKey)?.TypedData;
                if (newToLinks == null) return;
                foreach (var dc in newToLinks)
                {
                    var docCtrl = dc.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.LinkToKey, null)?.TypedData.First();
                    if (docCtrl == null) return;
                    docCtrl.ToggleHidden();
                    MainPage.Instance.HighlightDoc(docCtrl, false, 1);
                }
            }
        }

        public static void SelectDocuments(List<DocumentView> docs)
        {
            var args = new DocumentSelectionChangedEventArgs();
            foreach (var doc in docs)
            {
                if (!_selectedDocs.Contains(doc))
                {
                    SelectHelper(doc);
                    args.SelectedViews.Add(doc);
                    var pdf = doc.GetFirstDescendantOfType<CustomPdfView>();
                    if (pdf != null)
                    {
                        pdf.ShowPdfControls();
                    }
                }
            }
            SelectionChanged?.Invoke(args);
        }

        private static void SelectHelper(DocumentView doc)
        {
            if (SelectedRegion != null && !doc.ViewModel.LayoutDocument.Equals(SelectedRegion.GetRegionDefinition()) && !doc.ViewModel.DataDocument.Equals(SelectedRegion)) { }
                SelectRegion(null);
            _selectedDocs.Add(doc);
            doc.SetSelectionBorder(true);

        }

        public static void Deselect(DocumentView doc)
        {
            if (DeselectHelper(doc))
            {
                SelectionChanged?.Invoke(new DocumentSelectionChangedEventArgs(new List<DocumentView> { doc }, new List<DocumentView>()));
            }
            var pdf = doc.GetFirstDescendantOfType<CustomPdfView>();
            if (pdf != null)
            {
                pdf.HidePdfControls();
            }
        }

        /*
         * This method deselects everything that's currently selected.
         */
        public static void DeselectAll()
        {
            if (_selectedDocs.Count > 0)
            {
                DeselectAllHelper();
                var args = new DocumentSelectionChangedEventArgs(new List<DocumentView>(_selectedDocs), new List<DocumentView>());
                SelectionChanged?.Invoke(args);
            }
        }

        private static void DeselectAllHelper()
        {
            foreach (var documentView in _selectedDocs)
            {
                documentView.SetSelectionBorder(false);
                var pdf = documentView.GetFirstDescendantOfType<CustomPdfView>();
                if (pdf != null)
                {
                    pdf.HidePdfControls();
                }
            }
            _selectedDocs.Clear();
        }

        private static bool DeselectHelper(DocumentView doc)
        {
            doc.SetSelectionBorder(false);
            return _selectedDocs.Remove(doc);
        }

        public static IEnumerable<DocumentView> GetSelectedDocumentsInCollection(CollectionFreeformBase collection)
        {
            return SelectedDocs.Where(doc => Equals(doc.ParentCollection?.CurrentView, collection));
        }

        /*
         * Returns itself if nothing is selected.
         */
        public static List<DocumentView> GetSelectedSiblings(DocumentView view)
        {
            if (view.ParentCollection != null && view.ParentCollection.CurrentView is CollectionFreeformBase cfb)
            {
                var marqueeDocs = GetSelectedDocumentsInCollection(cfb).ToList();
                if (marqueeDocs.Contains(view))
                    return marqueeDocs;
            }
            return new List<DocumentView>(new[] { view });
        }
    }
}
