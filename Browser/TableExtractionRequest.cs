using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Dash.Browser;
using Newtonsoft.Json.Linq;

namespace Dash
{
    public class TableExtractionRequest : BrowserRequest
    {
        public string data { get; set; }

        private JsonToDashUtil _parser = new JsonToDashUtil();

        public override async Task Handle(BrowserView browser)
        {
            var token = JToken.Parse(data);
            if (!(token is JArray tables))
            {
                return;
            }

            double x = 400;
            foreach (var table in tables)
            {
                if (!(table is JArray rows))
                {
                    continue;
                }
                var tab = ParseTable(rows);
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () => { MainPage.Instance.AddFloatingDoc(tab, new Point(400, 300), new Point(x, 400)); });
                x += 450;
            }
        }

        private DocumentController ParseTable(JArray rows)
        {
            var prototype = new DocumentController();

            var docs = new List<DocumentController>();

            foreach (var row in rows)
            {
                if (!(row is JObject obj))
                {
                    continue;
                }

                var doc = ParseRow(obj, prototype);
                docs.Add(doc);
            }

            return new CollectionNote(new Point(), CollectionView.CollectionViewType.Schema, collectedDocuments: docs).Document;
        }

        private DocumentController ParseRow(JObject obj, DocumentController proto)
        {
            var doc = proto.MakeDelegate();

            foreach (var kvp in obj)
            {
                var key = new KeyController(kvp.Key);
                var val = _parser.ParseValue(kvp.Value);
                doc.SetField(key, val, true);
            }

            return doc;
        }
    }
}
