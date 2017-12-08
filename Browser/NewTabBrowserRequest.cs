using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Browser
{
    public class NewTabBrowserRequest : BrowserRequest
    {
        public string url { get; set; }

        public NewTabBrowserRequest()
        {
            this.type = "newBrowser";
            var a = this.Serialize();
        }

        public override void Handle(BrowserView browser)
        {
            
        }
    }
}
