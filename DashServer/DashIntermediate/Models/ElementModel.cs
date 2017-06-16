using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash;

namespace DashIntermediate
{
    public class ElementModel
    {

        public double Left { get; set; }
        public double Top { get; set; }

        public double Width { get; set; }
        public double Height { get; set; }

        public ElementModel(double left, double top)
        {
            Left = left;
            Top = top;
        }
    }
}
