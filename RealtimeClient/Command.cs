using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class Command
    {
        public long Time { get; set; }
        public Dictionary<string, object> Update { get; set; }
    }
}
