using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public abstract class BaseCollectionViewModel : BaseSelectionElementViewModel, ICollectionViewModel
    {
        private bool _isInterfaceBuilder;
        private ObservableCollection<DocumentViewModel> _documentViewModels;
        private double _cellSize;
        private bool _canDragItems;
        private ListViewSelectionMode _itemSelectionMode;

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
    }
}
