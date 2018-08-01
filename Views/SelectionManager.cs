using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
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
        private static IList<DocumentView> SelectedDocs { get; set; } = new List<DocumentView>();


        public static IList<DocumentView> GetSelectedDocs()
        {
            return new List<DocumentView>(SelectedDocs);
        }

        public static bool IsSelected(DocumentView doc)
        {
            return SelectedDocs.Contains(doc);
        }

        public delegate void SelectionChangedHandler(DocumentSelectionChangedEventArgs args);
        public static event SelectionChangedHandler SelectionChanged;

        /// <summary>
        /// Selects the given document
        /// </summary>
        /// <param name="doc">The document to select</param>
        /// <param name="toggle">Whether or not to toggle the selection of the given document.
        /// This is roughly equivalent to whether Shift is pressed when selecting.</param>
        public static void Select(DocumentView doc, bool toggle)
        {
            if (!toggle)
            {
                bool alreadySelected = false;
                var deselected = new List<DocumentView>();
                foreach (var documentView in SelectedDocs)
                {
                    if (documentView == doc)
                    {
                        alreadySelected = true;
                    }
                    else
                    {
                        DeselectHelper(documentView);
                        deselected.Add(documentView);
                    }
                }

                var args = new DocumentSelectionChangedEventArgs(deselected, alreadySelected ? new List<DocumentView>() : new List<DocumentView>{doc});

                SelectedDocs = new List<DocumentView>{doc};
                if (!alreadySelected)
                {
                    SelectHelper(doc);
                }

                OnSelectionChanged(args);
            }
            else
            {
                if (SelectedDocs.Contains(doc))
                {
                    DeselectHelper(doc);
                    SelectedDocs.Remove(doc);
                    OnSelectionChanged(new DocumentSelectionChangedEventArgs(new List<DocumentView>{doc}, new List<DocumentView>()));
                }
                else
                {
                    SelectHelper(doc);
                    SelectedDocs.Add(doc);
                    OnSelectionChanged(new DocumentSelectionChangedEventArgs(new List<DocumentView>(), new List<DocumentView>{doc}));
                }
            }
        }

        /// <summary>
        /// Selects the given documents
        /// </summary>
        /// <param name="views">The documents to select</param>
        /// <param name="keepPrevious">Whether or not to deselect the previously selected documents. 
        /// False to deselect previous documents, true to keep them selected. 
        /// This will often be roughly equivalent to whether Shift is pressed</param>
        public static void SelectDocuments(IEnumerable<DocumentView> views, bool keepPrevious)
        {
            var selectedDocs = new List<DocumentView>();
            var documentViews = views.ToList();
            foreach (var documentView in documentViews)
            {
                if (SelectedDocs.Contains(documentView))
                {
                    continue;
                }

                SelectHelper(documentView);
                selectedDocs.Add(documentView);
                if (keepPrevious)
                {
                    SelectedDocs.Add(documentView);
                }
            }

            var deselectedDocs = new List<DocumentView>();
            if (!keepPrevious)
            {
                foreach (var documentView in SelectedDocs)
                {
                    if (!documentViews.Contains(documentView))
                    {
                        DeselectHelper(documentView);
                        deselectedDocs.Add(documentView);
                    }
                }

                SelectedDocs = documentViews;
            }

            OnSelectionChanged(new DocumentSelectionChangedEventArgs(deselectedDocs, selectedDocs));
        }

        public static void Deselect(DocumentView view)
        {
            if (SelectedDocs.Contains(view))
            {
                SelectedDocs.Remove(view);
                DeselectHelper(view);
                OnSelectionChanged(new DocumentSelectionChangedEventArgs(new List<DocumentView>{view}, new List<DocumentView>()));
            }
        }

        public static void DeselectAll()
        {
            foreach (var documentView in SelectedDocs)
            {
                DeselectHelper(documentView);
            }
            var args = new DocumentSelectionChangedEventArgs(new List<DocumentView>(SelectedDocs), new List<DocumentView>());
            SelectedDocs.Clear();
            OnSelectionChanged(args);
        }

        private static void SelectHelper(DocumentView view)
        {
            view.SetSelectionBorder(true);
        }

        private static void DeselectHelper(DocumentView view)
        {
            view.SetSelectionBorder(false);
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

        private static void OnSelectionChanged(DocumentSelectionChangedEventArgs args)
        {
            if (args.DeselectedViews.Count == 0 && args.SelectedViews.Count == 0)
            {
                return;
            }
            SelectionChanged?.Invoke(args);
        }
    }
}
