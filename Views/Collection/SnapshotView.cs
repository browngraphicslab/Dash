using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class SnapshotView
    {
        public string Title { get; }
        public string Image { get; }

        public int Index { get; set; }

        public SnapshotView(string t, string i, int n)
        {
            Title = t;
            Image = i;
            Index = n;
        }
    }
}
