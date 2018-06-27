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
        public ObservableCollection<PresentationPinnedNode> PinnedNodes = new ObservableCollection<PresentationPinnedNode>();
        public ObservableCollection<IntWrapper> PinNumbers = new ObservableCollection<IntWrapper>();

        public void AddToPinnedNodesCollection(DocumentViewModel viewModel, double scale)
        {
            PinnedNodes.Add(new PresentationPinnedNode(viewModel, scale));
            PinNumbers.Add(new IntWrapper(PinnedNodes.Count));
        }

        public void RemovePinFromPinnedNodesCollection(PresentationPinnedNode docScale)
        {
            PinNumbers.RemoveAt(PinnedNodes.Count-1);
            if (PinnedNodes.Contains(docScale))
                PinnedNodes.Remove(docScale);
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
