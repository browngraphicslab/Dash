﻿using System.Threading.Tasks;
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
        public override Task Handle(BrowserView browser)
        {
            browser.FireUrlUpdated(url);
            browser.FireScrollUpdated(scroll);
            browser.FireTitleUpdated(title);

            if (current)
            {
                BrowserView.UpdateCurrentFromServer(tabId);
            }

            return base.Handle(browser);
        }
    }
}
