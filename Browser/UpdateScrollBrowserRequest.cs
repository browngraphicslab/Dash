using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash.Browser;

namespace Dash
{
    public class UpdateScrollBrowserRequest : BrowserRequest
    {
        public double scroll { get; set; }

        public override void Handle(BrowserView browser)
        {
            browser.FireScrollUpdated(scroll);
            BrowserView.UpdateCurrentFromServer(tabId);
        }
    }
}
