using System.Threading.Tasks;
using DashShared;

namespace Dash.Browser
{
    public class BrowserRequest : EntityBase
    {
        public int tabId { get; set; }

        public virtual Task Handle(BrowserView browser)
        {
            return Task.CompletedTask;
        }

        public void Send()
        {
            BrowserView.SendToServer(this);
        }
    }
}
