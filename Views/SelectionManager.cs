using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private static List<DocumentController> _selectedRegions = new List<DocumentController>();

        public delegate void SelectionChangedHandler(DocumentSelectionChangedEventArgs args);
        public static event SelectionChangedHandler SelectionChanged;

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
            SelectionChanged?.Invoke(args);
        }

        public static void SelectRegion(DocumentController doc)
        {
            _selectedRegions.Add(doc);
        }

        public static bool IsRegionSelected(DocumentController doc)
        {
            return _selectedRegions.Contains(doc);
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
                }
            }
            SelectionChanged?.Invoke(args);
        }

        private static void SelectHelper(DocumentView doc)
        {
            _selectedDocs.Add(doc);
            doc.SetSelectionBorder(true);
        }

        public static void Deselect(DocumentView doc)
        {
            if (DeselectHelper(doc))
            {
                SelectionChanged?.Invoke(new DocumentSelectionChangedEventArgs(new List<DocumentView>{doc}, new List<DocumentView>()));
            }
        }

        public static void DeselectRegions(DocumentController doc)
        {
            _selectedRegions.Remove(doc);
            var args = new RegionSelectionChangedEventArgs();
            args.DeselectedRegions.Add(doc);
            RegionSelectionChanged?.Invoke(args);
        }

        /*
         * This method deselects everything that's currently selected.
         */
        public static void DeselectAll()
        {
            if (_selectedDocs.Count > 0)
            {
                var args = new DocumentSelectionChangedEventArgs(new List<DocumentView>(_selectedDocs), new List<DocumentView>());
                DeselectAllHelper();
                SelectionChanged?.Invoke(args);
            }
        }

        public static void DeselectAllRegions()
        {
            var args = new RegionSelectionChangedEventArgs {DeselectedRegions = _selectedRegions.ToList()};
            RegionSelectionChanged?.Invoke(args);
            _selectedRegions.Clear();
        }

        private static void DeselectAllHelper()
        {
            foreach (var documentView in _selectedDocs)
            {
                documentView.SetSelectionBorder(false);
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
