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
        public ObservableCollection<KeyValuePair<DocumentViewModel, TransformGroupData>> PinnedNodes = new ObservableCollection<KeyValuePair<DocumentViewModel,TransformGroupData>>();
        public ObservableCollection<IntWrapper> PinNumbers = new ObservableCollection<IntWrapper>();

        public void AddToPinnedNodesCollection(DocumentViewModel viewModel, TransformGroupData zoom)
        {
            PinnedNodes.Add(new KeyValuePair<DocumentViewModel, TransformGroupData>(viewModel, zoom));
            PinNumbers.Add(new IntWrapper(PinnedNodes.Count));
        }

        public void RemovePinFromPinnedNodesCollection(KeyValuePair<DocumentViewModel, TransformGroupData> pair)
        {
            PinNumbers.RemoveAt(PinnedNodes.Count-1);
            if (PinnedNodes.Contains(pair))
                PinnedNodes.Remove(pair);
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
