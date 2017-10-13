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

            // skip to the next row
            csv.Read();

            // set up a prototype document that models each of the rows
            var protoDoc = new DocumentController(new Dictionary<KeyController, FieldModelController>(), new DocumentType(DashShared.Util.GenerateNewId(), uniquePath ?? DashShared.Util.GenerateNewId()));
            var protoFieldDict = new Dictionary<KeyController, FieldModelController>(); // dictionary of fields to set on the prototype document
            var headerToFieldMap = new Dictionary<string, FieldModelController>();
            var headerToKeyMap = new Dictionary<string, KeyController>();
            // generate a default field model controller for each of the fields
            foreach (var header in headers)
            {
                var key = new KeyController(DashShared.Util.GenerateNewId(), header);
                var field = Util.StringToFieldModelController(csv[header]).GetDefaultController();
                protoFieldDict.Add(key, field);
                headerToFieldMap.Add(header, field);
                headerToKeyMap.Add(header, key);
            }
            protoDoc.SetFields(protoFieldDict.ToList(), true);

            // go through the entire csv generating a delegate of the prototype document to represent each row
            // and set the fields on that delegate to the values found in the cell of the row
            var rowDocs = new List<DocumentController>();
            do
            {
                var delgate = protoDoc.MakeDelegate();
                foreach (var header in headers)
                {
                    delgate.SetField(headerToKeyMap[header], Util.StringToFieldModelController(csv[header]), true);
                }
                rowDocs.Add(delgate);
            } while (csv.Read());
      
            var outputDoc = new DocumentController(new Dictionary<KeyController, FieldModelController>(), new DocumentType(DashShared.Util.GenerateNewId(), "CSV Collection"));
            SetDefaultActiveLayout(outputDoc); // set active layout on the output doc

            outputDoc.SetField(KeyStore.DataKey, new DocumentCollectionFieldModelController(rowDocs), true);

            return outputDoc;
        }

        /// <summaryl>
        /// Set the active layout on the output doc
        /// </summary>
        /// <param name="doc"></param>
        private static void SetDefaultActiveLayout(DocumentController doc)
        {
            doc.SetActiveLayout(new DefaultLayout().Document, true, true);
            var defaultLayoutFields = CourtesyDocument.DefaultLayoutFields(new Point(), new Size(200, 200));
            defaultLayoutFields.Add(KeyStore.CollectionViewTypeKey,
                new TextFieldModelController(CollectionView.CollectionViewType.Schema.ToString()));
            doc.GetActiveLayout().Data.SetFields(defaultLayoutFields, true);
        }

        /// <summary>
        /// Get all the headers form a csv reader
        /// </summary>
        /// <param name="csv"></param>
        private static string[] GetHeadersFromCsv(ICsvReader csv)
        {
            csv.ReadHeader(); // TODO can we check to see if the csv has a header or not? otherwise this fails, what happens when it doesn't
            var headers = csv.FieldHeaders;
            return headers;
        }
    }
}
