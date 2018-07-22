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

        public SnapshotView(string t, string i)
        {
            Title = t;
            Image = i;
        }
    }
}
