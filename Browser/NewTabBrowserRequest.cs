using DashShared;

namespace Dash.Browser
{
    public class NewTabBrowserRequest : BrowserRequest
    {
        public string url { get; set; }

        public NewTabBrowserRequest()
        {
            var a = this.Serialize();
        }
    }
}
