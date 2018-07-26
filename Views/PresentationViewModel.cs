using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class PresentationViewModel : ViewModelBase
    {
        public ObservableCollection<DocumentController> PinnedNodes
        {
            get => _pinnedNodes;
            set => SetProperty(ref _pinnedNodes, value);
        }

        public ObservableCollection<int> PinNumbers
        {
            get => _pinNumbers;
            set => SetProperty(ref _pinNumbers, value);
        }

        private ListController<DocumentController> _listController = null;
        private ObservableCollection<DocumentController> _pinnedNodes = new ObservableCollection<DocumentController>();
        private ObservableCollection<int> _pinNumbers = new ObservableCollection<int>();

        public PresentationViewModel()
        {

        }
        
        public PresentationViewModel(ListController<DocumentController> lc)
        {
            _listController = lc;
            PinnedNodes = new ObservableCollection<DocumentController>(_listController.TypedData);
            for (var i = 1; i <= PinnedNodes.Count; i++)
            {
                PinNumbers.Add(i);
            }
        }

        public void AddToPinnedNodesCollection(DocumentController dc)
        {
            if (_listController == null)
            {
                _listController = new ListController<DocumentController>();
                MainPage.Instance.MainDocument.SetField(KeyStore.PresentationItemsKey, _listController, true);
            }

            if (PinnedNodes.Contains(dc)) return;

            PinnedNodes.Add(dc);
            PinNumbers.Add(PinnedNodes.Count);
            _listController.Add(dc);
        }

        public void RemovePinFromPinnedNodesCollection(DocumentController dc)
        {
            PinNumbers.RemoveAt(PinnedNodes.Count - 1);
            PinnedNodes.Remove(dc);
            _listController.Remove(dc);
        }
    }
}
