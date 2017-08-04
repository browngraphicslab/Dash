using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public abstract class BaseCollectionViewModel : ViewModelBase, ICollectionViewModel
    {
        private bool _isInterfaceBuilder;
        private ObservableCollection<DocumentViewModel> _documentViewModels;
        private bool _isSelected;
        private bool _isLowestSelected;
        private double _cellSize;
        private bool _canDragItems;
        private ListViewSelectionMode _itemSelectionMode;

        public event Action<bool> OnSelectionSet;
        public event Action<bool> OnLowestSelectionSet;

        protected BaseCollectionViewModel(bool isInInterfaceBuilder)
        {
            IsInterfaceBuilder = isInInterfaceBuilder;
        }

        public bool IsInterfaceBuilder
        {
            get { return _isInterfaceBuilder; }
            private set { SetProperty(ref _isInterfaceBuilder, value); }
        }

        public ObservableCollection<DocumentViewModel> DocumentViewModels
        {
            get { return _documentViewModels; }
            protected set { SetProperty(ref _documentViewModels, value); }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            protected set {
                if (SetProperty(ref _isSelected, value))
                {
                    OnSelectionSet?.Invoke(value);
                }
            }
        }

        public bool IsLowestSelected
        {
            get { return _isLowestSelected; }
            protected set {
                if (SetProperty(ref _isLowestSelected, value))
                {
                    OnLowestSelectionSet?.Invoke(value);
                }
            }
        }

        #region Grid or List Specific Variables I want to Remove

        public double CellSize
        {
            get { return _cellSize; }
            protected set { SetProperty(ref _cellSize, value); }
        }

        public bool CanDragItems
        {
            get { return _canDragItems; }
            set { SetProperty(ref _canDragItems, value); } // 
        }

        public ListViewSelectionMode ItemSelectionMode
        {
            get { return _itemSelectionMode; }
            set { SetProperty(ref _itemSelectionMode, value); }
        }

        #endregion

        public abstract void AddDocuments(List<DocumentController> documents, Context context);
        public abstract void AddDocument(DocumentController document, Context context);
        public abstract void RemoveDocuments(List<DocumentController> documents);
        public abstract void RemoveDocument(DocumentController document);


        public void SetSelected(FrameworkElement setter, bool isSelected)
        {
            Debug.Assert(ReferenceEquals(setter.DataContext, this), "selection should only be set by the views which have this as a datacontext");
            IsSelected = isSelected;
        }

        public void SetLowestSelected(FrameworkElement setter, bool isLowestSelected)
        {
            Debug.Assert(ReferenceEquals(setter.DataContext, this), "selection should only be set by the views which have this as a datacontext");
            IsLowestSelected = isLowestSelected;
        }
    }
}
