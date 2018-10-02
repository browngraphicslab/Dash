using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash.Browser;

namespace Dash
{
    public class TableExtractionRequest : BrowserRequest
    {
        public string data { get; set; }

        public override async Task Handle(BrowserView browser)
        {
            var doc = new JsonToDashUtil().ParseJsonString(data, "");
        }
    }
}
