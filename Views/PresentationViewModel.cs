using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class PresentationViewModel
    {
        public ObservableCollection<DocumentViewModel> PinnedNodes = new ObservableCollection<DocumentViewModel>();
        public ObservableCollection<int> PinNumbers = new ObservableCollection<int>();

        public void AddToPinnedNodesCollection(DocumentViewModel viewModel)
        {
            PinnedNodes.Add(viewModel);
            PinNumbers.Add(PinnedNodes.Count);
        }

        public void RemovePinFromPinnedNodesCollection(DocumentViewModel viewModel)
        {
            PinNumbers.RemoveAt(PinnedNodes.Count-1);
            PinnedNodes.Remove(viewModel);
        }
    }
}
