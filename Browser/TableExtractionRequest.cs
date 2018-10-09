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
        
        public override async Task Handle(BrowserView browser)
        {
            var tab = await ProcessTableData(data, new Point());
            if (tab != null)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () => MainPage.Instance.AddFloatingDoc(tab, new Point(400,300), new Point(400,400)));
            }
        }

        public static async Task<DocumentController> ProcessTableData(string data, Point where)
        {
            var token = JToken.Parse(data);
            if (token is JArray tables)
            {
                var tab = ParseTable(tables.OfType<JObject>(), new JsonToDashUtil(), where);
                return tab;
            }
            return null;
        }

        private static DocumentController ParseTable(IEnumerable<JObject> rows, JsonToDashUtil parser, Point where)
        {
            var columns = rows.FirstOrDefault()?.GetEnumerator();
            if (columns != null)
            {
                var prototype = new CollectionNote(new Point(), CollectionView.CollectionViewType.Stacking, 200, 200).Document;
                prototype.GetDataDocument().SetTitle("Prototype Row Record");
                foreach (var c in rows.FirstOrDefault())
                {
                    prototype.GetDataDocument().SetField<TextController>(new KeyController(c.Key.Trim()), "<" + c.Key.Trim() + ">", true);
                }

                columns.MoveNext();
                var primaryKey = new KeyController(columns.Current.Key.Trim() ?? "<empty>"); // choose a better primary key -- this should become the document's title, too.

                var protobox = new DataBox(new DocumentReferenceController(prototype.GetDataDocument(), primaryKey), 0, 0, double.NaN, 50).Document;
                protobox.SetHorizontalAlignment(Windows.UI.Xaml.HorizontalAlignment.Center);
                prototype.SetField(KeyStore.DataKey, new ListController<DocumentController>(protobox), true);
                CollectionViewModel.RouteDataBoxReferencesThroughCollection(prototype, new List<DocumentController>(new DocumentController[] { protobox }));

                var docs = rows.Select((jobj) => ParseRow(jobj, primaryKey, prototype, parser));
                var cnote = new CollectionNote(where, CollectionView.CollectionViewType.Schema, collectedDocuments: docs).Document;
                cnote.GetDataDocument().SetField(KeyStore.CollectionItemLayoutPrototypeKey, prototype, true);
                return cnote;
            }
            return null;
        }

        private static DocumentController ParseRow(JObject obj, KeyController primaryKey, DocumentController proto, JsonToDashUtil parser)
        {
            var doc = proto.GetDataInstance();
            var datadoc = doc.GetDataDocument();
            
            foreach (var kvp in obj)
            {
                var key = new KeyController(kvp.Key.Trim());
                var val = parser.ParseValue(kvp.Value);
                datadoc.SetField(key, val, true);
                if (key.Equals(primaryKey))
                {
                    datadoc.SetTitle(val.ToString());
                }
            }
            doc.SetField(KeyStore.LayoutPrototypeKey, proto, true);

            return doc;
        }
    }
}
