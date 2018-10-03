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
            foreach (var rows in tables.OfType<JArray>())
            {
                var tab = ParseTable(rows.OfType<JObject>());
                if (tab != null)
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () => MainPage.Instance.AddFloatingDoc(tab, new Point(400, 300), new Point(x, 400)));
                    x += 450;
                }
            }
        }

        private DocumentController ParseTable(IEnumerable<JObject> rows)
        {
            var columns = rows.FirstOrDefault()?.GetEnumerator();
            if (columns != null)
            {
                var prototype = new CollectionNote(new Point(), CollectionView.CollectionViewType.Schema, 200, 200).Document;
                prototype.GetDataDocument().SetTitle("Prototype Row Record");
                foreach (var c in rows.FirstOrDefault())
                {
                    prototype.GetDataDocument().SetField<TextController>(new KeyController(c.Key), "<" + c.Key + ">", true);
                }

                columns.MoveNext();
                var primaryKey = new KeyController(columns.Current.Key ?? "<empty>"); // choose a better primary key -- this should become the document's title, too.

                var protobox = new DataBox(new DocumentReferenceController(prototype.GetDataDocument(), primaryKey), 0, 0, 100, 50).Document;
                CollectionViewModel.RouteDataBoxReferencesThroughCollection(prototype, new List<DocumentController>(new DocumentController[] { protobox }));
                prototype.SetField(KeyStore.DataKey, new ListController<DocumentController>(protobox), true);

                var docs = rows.Select((jobj) => ParseRow(jobj, primaryKey, prototype)).ToList().Prepend(prototype);

                return new CollectionNote(new Point(), CollectionView.CollectionViewType.Schema, collectedDocuments: docs).Document;
            }
            return null;
        }

        private DocumentController ParseRow(JObject obj, KeyController primaryKey, DocumentController proto)
        {
            var doc = proto.GetDataInstance();
            var datadoc = doc.GetDataDocument();
            
            foreach (var kvp in obj)
            {
                var key = new KeyController(kvp.Key);
                var val = _parser.ParseValue(kvp.Value);
                datadoc.SetField(key, val, true);
                if (key.Equals(primaryKey))
                {
                    datadoc.SetTitle(val.ToString());
                }
            }

            return doc;
        }
    }
}
