using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash.Browser;

namespace Dash
{
    public class ActivateRequest : BrowserRequest
    {
        public bool activated { get; set; }

        public override async Task Handle(BrowserView browser)
        {

        }
    }
}
