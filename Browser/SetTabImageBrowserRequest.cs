using System.Threading.Tasks;
using Dash.Browser;

namespace Dash
{
    public class SetTabImageBrowserRequest : BrowserRequest
    {
        public string data { get; set; }

        public override Task Handle(BrowserView browser)
        {
            //browser.FireImageUpdated(data);
            return base.Handle(browser);
        }
    }
}
