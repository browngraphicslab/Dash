using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash.Browser;

namespace Dash
{
    public class SetTabImageBrowserRequest : BrowserRequest
    {
        public string data { get; set; }

        public override void Handle(BrowserView browser)
        {
            browser.FireImageUpdated(data);
            base.Handle(browser);
        }
    }
}
