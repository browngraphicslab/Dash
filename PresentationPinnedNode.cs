using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class PresentationPinnedNode
    {
        public KeyValuePair<DocumentViewModel, double> Data { get; set; }

        public PresentationPinnedNode(DocumentViewModel viewModel, double scale)
        {
            Data = new KeyValuePair<DocumentViewModel, double>(viewModel, scale);
        }
    }
}
