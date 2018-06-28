using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public static class SelectionManager
    {
        public static IEnumerable<DocumentView> SelectedDocs => _selectedDocs.Where(dv => dv?.ViewModel?.DocumentController != null).ToList();
        private static List<DocumentView> _selectedDocs = new List<DocumentView>();
        public static event EventHandler SelectionChanged;

        public static bool Contains(DocumentView doc)
        {
            return _selectedDocs.Contains(doc);
        }

        public static void Select(DocumentView doc)
        {
            _selectedDocs.Add(doc);
            doc.SetSelectionBorder(true);
            SelectionChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void SelectDocuments(List<DocumentView> docs)
        {
            foreach (var doc in docs)
            {
                Select(doc);
            }
            SelectionChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void Deselect(DocumentView doc)
        {
            if (_selectedDocs.Count == 0) return;
            _selectedDocs.Remove(doc);
            doc.SetSelectionBorder(false);
            SelectionChanged?.Invoke(null, EventArgs.Empty);
        }

        /*
         * This method deselects everything that's currently selected, but needs to take in a CollectionFreeformBase (wherever it's being called from) in order to reset its marquees and so on.
         */
        public static void DeselectAll(CollectionFreeformBase cfbase)
        {
            foreach (var doc in _selectedDocs.ToList())
            {
                Deselect(doc);
            }
            SelectionChanged?.Invoke(null, EventArgs.Empty);
            cfbase.ResetMarquee();
        }

        public static IEnumerable<DocumentView> GetSelectedDocumentsInCollection(CollectionFreeformBase collection)
        {
            return SelectedDocs.Where(doc => doc.GetFirstAncestorOfType<CollectionFreeformBase>().Equals(collection));
        }

        /*
         * Returns itself if nothing is selected.
         */
        public static List<DocumentView> GetSelectedSiblings(DocumentView view)
        {
            if (view.ParentCollection != null)
            {
                var marqueeDocs = GetSelectedDocumentsInCollection(view.ParentCollection.CurrentView as CollectionFreeformBase);
                if (marqueeDocs != null && marqueeDocs.Contains(view))
                    return marqueeDocs.ToList();
            }
            return new List<DocumentView>(new[] { view });
        }
    }
}
