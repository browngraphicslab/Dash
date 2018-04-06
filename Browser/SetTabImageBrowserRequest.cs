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
