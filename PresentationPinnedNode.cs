using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class PresentationPinnedNode
    {
        /// <summary>
        /// Key value pair of DocumentViewModel and the scale of its parent collection at the time it was pinned to the presentation
        /// </summary>
        public KeyValuePair<DocumentViewModel, double> Data { get; set; }

        public PresentationPinnedNode(DocumentViewModel viewModel, double scale)
        {
            Data = new KeyValuePair<DocumentViewModel, double>(viewModel, scale);
        }
    }
}
