﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public interface ICollectionViewModel
    {
        ObservableCollection<DocumentViewModel> DocumentViewModels { get; }
        

        List<DocumentViewModel> SelectionGroup { get;  }

        // TODO these are specific to grid view maybe shouldn't be here
        double CellSize { get; } // <----
        bool CanDragItems { get; } // <----
        ListViewSelectionMode ItemSelectionMode { get; } // <----

        void AddDocuments(List<DocumentController> documents, Context context);
        void AddDocument(DocumentController document, Context context);
        void RemoveDocuments(List<DocumentController> documents);
        void RemoveDocument(DocumentController document);
    }
}