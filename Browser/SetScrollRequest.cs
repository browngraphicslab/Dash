using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash.Browser;

namespace Dash
{
    public class SetScrollRequest : BrowserRequest
    {
        public double scroll { get; set; }
    }
}
