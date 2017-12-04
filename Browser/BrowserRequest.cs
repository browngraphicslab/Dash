using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Browser
{
    public abstract class BrowserRequest : EntityBase
    {
        public string type { get; set; }
        public string tabId { get; set; }
        public abstract void Handle(BrowserView browser);

        public void Send()
        {
            BrowserView.SendToServer(this);
        }
    }
}
