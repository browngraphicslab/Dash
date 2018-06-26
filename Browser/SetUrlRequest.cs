using System.Threading.Tasks;
using Dash.Browser;

namespace Dash
{
    public class SetUrlRequest : BrowserRequest
    {
        public string url { get; set; }
        public override Task Handle(BrowserView browser)
        {
            browser.FireUrlUpdated(url);
            return base.Handle(browser);
        }
    }
}
