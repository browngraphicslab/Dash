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
            if (rows.Count() > 0)
            {
                var prototype = new CollectionNote(new Point(), CollectionView.CollectionViewType.Stacking, 200, 200).Document;
                prototype.GetDataDocument().SetTitle("Prototype Row Record");
                prototype.SetField(KeyStore.DataKey, new ListController<DocumentController>(), true);

                foreach (var c in rows.FirstOrDefault())
                {
                    prototype.GetDataDocument().SetField<TextController>(new KeyController(c.Key.Trim()), "<" + c.Key.Trim() + ">", true);
                }
                
                var listOfColumns = new List<KeyController>();
                int count = 0;
                KeyController primaryKey = null;
                foreach (var keyValuePair in rows.First())
                {
                    var key = new KeyController(keyValuePair.Key.Trim());
                    primaryKey = primaryKey ?? key;
                    prototype.GetDataDocument().SetField<TextController>(key, "<" +key.Name + ">", true);
                    listOfColumns.Add(key);
                    var newDataBoxCol = new DataBox(new DocumentReferenceController(prototype.GetDataDocument(), key), 0, 35 * count++, double.NaN, double.NaN).Document;
                    CollectionViewModel.RouteDataBoxReferencesThroughCollection(prototype, new List<DocumentController>(new DocumentController[] { newDataBoxCol }));
                    prototype.AddToListField(KeyStore.DataKey, newDataBoxCol);
                    newDataBoxCol.SetTitle(key.Name);
                }
                var docs = rows.Select((jobj) => ParseRow(jobj, primaryKey, prototype, parser));
                var cnote = new CollectionNote(where, CollectionView.CollectionViewType.Schema, collectedDocuments: docs).Document;
                cnote.GetDataDocument().SetTitle("Table " + rows.Count());
                cnote.GetDataDocument().SetField(KeyStore.CollectionItemLayoutPrototypeKey, prototype, true);
                cnote.SetField<ListController<KeyController>>(KeyStore.SchemaDisplayedColumns, listOfColumns, true);

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
                if (key.Equals(primaryKey))  // bcz: should we reference the first column instead of copying its value?
                {
                    datadoc.SetTitle(val.ToString());
                }
            }
            doc.SetField(KeyStore.LayoutPrototypeKey, proto, true);

            return doc;
        }
    }
}
