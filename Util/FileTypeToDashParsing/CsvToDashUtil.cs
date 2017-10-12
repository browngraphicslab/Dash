using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using CsvHelper;
using Newtonsoft.Json;
using Dash.Controllers.Operators;
using DashShared;
using Flurl.Util;
using Newtonsoft.Json.Linq;
using static Dash.NoteDocuments;

namespace Dash
{
    public class CsvToDashUtil : IFileParser
    {
        public async Task<DocumentController> ParseFileAsync(IStorageFile item, string uniquePath = null)
        {
            // set up streams for the csvReader
            var stream = await item.OpenStreamForReadAsync();
            var streamReader = new StreamReader(stream);

            // read the headers of the csv
            var csv = new CsvReader(streamReader);
            var headers = GetHeadersFromCsv(csv);

            csv.Read();

            var protoDoc = new DocumentController(new Dictionary<KeyController, FieldModelController>(), new DocumentType(DashShared.Util.GenerateNewId(), uniquePath ?? DashShared.Util.GenerateNewId()));
            var protoFieldDict = new Dictionary<KeyController, FieldModelController>();
            var headerToFieldMap = new Dictionary<string, FieldModelController>();
            var headerToKeyMap = new Dictionary<string, KeyController>();
            foreach (var header in headers)
            {
                var key = new KeyController(DashShared.Util.GenerateNewId(), header);
                var field = Util.StringToFieldModelController(csv[header]).GetDefaultController();
                protoFieldDict.Add(key, field);
                headerToFieldMap.Add(header, field);
                headerToKeyMap.Add(header, key);
            }

            protoDoc.SetFields(protoFieldDict.ToList(), true);
            protoDoc.SetActiveLayout(new DefaultLayout().Document, true, true);
            var defaultLayoutFields = CourtesyDocument.DefaultLayoutFields(new Point(), new Size(200, 200));
            protoDoc.GetActiveLayout().Data.SetFields(defaultLayoutFields, true);

            var outputDocs = new List<DocumentController>();

            do
            {
                var delgate = protoDoc.MakeDelegate();
                foreach (var header in headers)
                {
                    delgate.SetField(headerToKeyMap[header], Util.StringToFieldModelController(csv[header]), true);
                }
                outputDocs.Add(delgate);
            } while (csv.Read());

            var outputDic = new Dictionary<KeyController, FieldModelController>
            {
                [CollectionBox.CollectionViewTypeKey] = new DocumentCollectionFieldModelController(outputDocs)
            };

            var outputDoc = new DocumentController(new Dictionary<KeyController, FieldModelController>(), new DocumentType());
            outputDoc.SetActiveLayout(new DefaultLayout().Document, true, true);
            defaultLayoutFields = CourtesyDocument.DefaultLayoutFields(new Point(), new Size(200, 200));
            defaultLayoutFields.Add(CollectionBox.CollectionViewTypeKey, new TextFieldModelController(CollectionView.CollectionViewType.Schema.ToString()));
            outputDoc.GetActiveLayout().Data.SetFields(defaultLayoutFields, true);

            outputDoc.SetField(KeyStore.DataKey, new DocumentCollectionFieldModelController(outputDocs), true);

            return outputDoc;
        }

        private static string[] GetHeadersFromCsv(CsvReader csv)
        {
            csv.ReadHeader(); // TODO can we check to see if the csv has a header or not? otherwise this fails, what happens when it doesn't
            var headers = csv.FieldHeaders;
            return headers;
        }
    }
}
