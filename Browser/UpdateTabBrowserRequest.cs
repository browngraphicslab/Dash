using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash.Browser;

namespace Dash
{
    public class UpdateTabBrowserRequest : BrowserRequest
    {
        public bool current { get; set; }
        public string url { get; set; }
        public string title { get; set; }
        public int index { get; set; }
        public double scroll { get; set; }
        public override void Handle(BrowserView browser)
        {
            browser.FireUrlUpdated(url);
            browser.FireScrollUpdated(scroll);
            browser.FireTitleUpdated(title);

            if (current)
            {
                BrowserView.UpdateCurrentFromServer(tabId);
            }
        }
    }
}
