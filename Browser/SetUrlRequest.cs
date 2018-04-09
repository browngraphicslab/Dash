﻿using Dash.Browser;

namespace Dash
{
    public class SetUrlRequest : BrowserRequest
    {
        public string url { get; set; }
        public override void Handle(BrowserView browser)
        {
            browser.FireUrlUpdated(url);
        }
    }
}
