using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Views
{
    public class PresentationViewModel
    {
        public ObservableCollection<DocumentController> PinnedNodes = new ObservableCollection<DocumentController>();
        public ObservableCollection<IntWrapper> PinNumbers = new ObservableCollection<IntWrapper>();
        private ListController<DocumentController> _listController = null;

        public PresentationViewModel()
        {

        }
        
        public PresentationViewModel(ListController<DocumentController> lc)
        {
            _listController = lc;
            PinnedNodes = new ObservableCollection<DocumentController>(_listController.TypedData);
            for (int i = 1; i < PinnedNodes.Count; i++)
            {
                PinNumbers.Add(new IntWrapper(i));
            }
        }

        public void AddToPinnedNodesCollection(DocumentController dc)
        {
            if (_listController == null)
            {
                _listController = new ListController<DocumentController>();
                MainPage.Instance.MainDocument.SetField<ListController<DocumentController>>(KeyStore.PresentationItemsKey, _listController,
                    true);
            }
            PinnedNodes.Add(dc);
            PinNumbers.Add(new IntWrapper(PinnedNodes.Count));
            _listController.Add(dc);
        }

        public void RemovePinFromPinnedNodesCollection(DocumentController dc)
        {
            PinNumbers.RemoveAt(PinnedNodes.Count-1);
            PinnedNodes.Remove(dc);
            _listController.Remove(dc);
        }
    }

    public class IntWrapper
    {
        public int Number { get; set; }

        public IntWrapper(int num)
        {
            Number = num;
        }
    }
}
