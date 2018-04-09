using DashShared;

namespace Dash.Browser
{
    public class BrowserRequest : EntityBase
    {
        public int tabId { get; set; }
        public virtual void Handle(BrowserView browser) { }

        public void Send()
        {
            BrowserView.SendToServer(this);
        }
    }
}
