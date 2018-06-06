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
        public ObservableCollection<DocumentViewModel> PinnedNodes = new ObservableCollection<DocumentViewModel>();
        public ObservableCollection<IntWrapper> PinNumbers = new ObservableCollection<IntWrapper>();

        public void AddToPinnedNodesCollection(DocumentViewModel viewModel)
        {
            PinnedNodes.Add(viewModel);
            PinNumbers.Add(new IntWrapper(PinnedNodes.Count));
        }

        public void RemovePinFromPinnedNodesCollection(DocumentViewModel viewModel)
        {
            PinNumbers.RemoveAt(PinnedNodes.Count-1);
            PinnedNodes.Remove(viewModel);
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
