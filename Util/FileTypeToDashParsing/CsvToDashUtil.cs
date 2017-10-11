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

            // wait for the user to configure the csv data 
            var csvConfig = await GetCsvConfigFromUser(headers);

            // if the user chose not to configure the csv data just don't return anything at all
            if (csvConfig == null)
            {
                return null;
            }

            csv.Read();

            // parse the csvConfig into easier to understand variables
            var allDocTypes = csvConfig.DocToColumnMaps.Select(docToColMap => docToColMap.DocumentType).ToImmutableHashSet();
            var dataDocTypes = csvConfig.DataDocTypes.ToImmutableHashSet();
            var labelDocTypes = allDocTypes.Except(dataDocTypes).ToImmutableHashSet();
            var unmappedHeaders = csvConfig.Headers.ToList();

            // create dictionary of column labels to document types for efficiency
            var headerToDocTypeMap = new Dictionary<string, DocumentType>();
            foreach (var docToColumnMap in csvConfig.DocToColumnMaps)
            {
                foreach (var header in docToColumnMap.MappedHeaders)
                {
                    headerToDocTypeMap[header] = docToColumnMap.DocumentType;
                }
            }

            // list of all the prototype documents we use
            var protoDocs = new Dictionary<DocumentType, DocumentController>();

            // add prototypes of all the label docs to the proto docs
            AddProtoLabelDocsToProtoDocs(csvConfig, labelDocTypes, csv, protoDocs);

            // create a field dictionary to use for data documetns
            var labelAndHeaderFieldDict = CreateFieldDictForDataDocs(unmappedHeaders, csv, labelDocTypes, protoDocs);

            // add prototypes of all the data docs to the proto docs
            AddProtoDataDocsToProtoDocs(csvConfig, dataDocTypes, labelAndHeaderFieldDict, csv, protoDocs);

            // mapping of label document types to the values to document controllers. This is used
            // to maintain a one to one relationship between label values and label document controllers
            // we seed it with empty dictionaries associated with each label document type to make our
            // lives easier later
            var labelToValue = new Dictionary<DocumentType, Dictionary<string, DocumentController>>();
            foreach (var labelDocType in labelDocTypes)
            {
                labelToValue[labelDocType] = new Dictionary<string, DocumentController>();
            }

            int rowsRead = 0;
            while (csv.Read())
            {
                rowsRead++;
                Debug.WriteLine($"Rows Read {rowsRead}");
                var labelDocuments = new List<DocumentController>();

                // iterate over the headers associated with label docs and add them to the field dictionary
                foreach (var header in csv.FieldHeaders.Where(h => labelDocTypes.Contains(headerToDocTypeMap[h])))
                {
                    var docType = headerToDocTypeMap[header];
                    var docProto = protoDocs[docType];

                    // read the string value associated with the header from the csv
                    var cellValue = csv[header];

                    // if we have never encountered the value before then create a document for it
                    if (!labelToValue[docType].ContainsKey(cellValue))
                    {
                        var newLabelDoc = docProto.MakeDelegate();
                        // set the field on the new label to the field we are parsing
                        var valueKey = newLabelDoc.EnumFields().First(kvp => kvp.Key.Name.Equals(header)).Key;
                        var valueField = ParseStringToFieldModelController(cellValue);
                        newLabelDoc.SetField(valueKey, valueField, true);
                        labelToValue[docType][cellValue] = newLabelDoc;
                    }

                    labelDocuments.Add(labelToValue[docType][cellValue]);
                }

                // field dictionary for t
                var fieldDict = new Dictionary<KeyController, FieldModelController>();

                // iterate over the headers associated with data docs 
                foreach (var header in csv.FieldHeaders.Where(h => dataDocTypes.Contains(headerToDocTypeMap[h])))
                {
                    var docType = headerToDocTypeMap[header];
                    var docProto = protoDocs[docType];
                    // read the string value associated with the header from the csv
                    var cellValue = csv[header];

                    foreach (var unmappedHeader in unmappedHeaders)
                    {
                        var key = docProto.EnumFields().First(kvp => kvp.Key.Name.Equals(unmappedHeader)).Key;
                        fieldDict.Add(key, ParseStringToFieldModelController(csv[unmappedHeader]));
                    }

                    foreach (var labelDoc in labelDocuments)
                    {
                        var key = docProto.EnumFields().First(kvp => kvp.Key.Name.Equals(labelDoc.DocumentType.Type)).Key;
                        fieldDict.Add(key, new DocumentFieldModelController(labelDoc));
                    }

                    fieldDict.Add(KeyStore.HeaderKey, new TextFieldModelController(header));
                    fieldDict.Add(KeyStore.DataKey, ParseStringToFieldModelController(cellValue));
                    var newDataDoc = docProto.MakeDelegate();
                    newDataDoc.SetFields(fieldDict, true);
                }
            }

            var outputDoc = new DocumentController(new Dictionary<KeyController, FieldModelController>(), new DocumentType());
            outputDoc.SetActiveLayout(new DefaultLayout().Document, true, true);
            var activeLayout = outputDoc.GetActiveLayout().Data;
            outputDoc.SetActiveLayout(activeLayout, true, false);
            var defaultLayoutFields = CourtesyDocument.DefaultLayoutFields(new Point(), new Size(200, 200));
            defaultLayoutFields.Add(CollectionBox.CollectionViewTypeKey, new TextFieldModelController(CollectionView.CollectionViewType.Schema.ToString()));
            activeLayout.SetFields(defaultLayoutFields, true);
            foreach (var protoDataDoc in protoDocs.Where(kvp => dataDocTypes.Contains(kvp.Key)).Select(kvp => kvp.Value))
            {
                outputDoc.SetField(new KeyController(DashShared.Util.GenerateNewId(), protoDataDoc.DocumentType.Type),
                    protoDataDoc.GetDelegates(), true);
            }
            return outputDoc;
        }

        private static void AddProtoDataDocsToProtoDocs(CsvImportHelperViewModel csvConfig, ImmutableHashSet<DocumentType> dataDocTypes,
            Dictionary<KeyController, FieldModelController> labelAndHeaderFieldDict, CsvReader csv, Dictionary<DocumentType, DocumentController> protoDocs)
        {
            foreach (var docMap in csvConfig.DocToColumnMaps.Where(dmap => dataDocTypes.Contains(dmap.DocumentType)))
            {
                var documentType = docMap.DocumentType;
                // create a copy of the field dictionary
                var fieldDictionary = labelAndHeaderFieldDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                fieldDictionary.Add(KeyStore.HeaderKey, new TextFieldModelController("").GetDefaultController());
                fieldDictionary.Add(KeyStore.DataKey, ParseStringToFieldModelController(csv[docMap.MappedHeaders.First()]).GetDefaultController());
                AddDocToProtoDocs(fieldDictionary, documentType, protoDocs);
            }
        }

        private static Dictionary<KeyController, FieldModelController> CreateFieldDictForDataDocs(List<string> unmappedHeaders, CsvReader csv,
            ImmutableHashSet<DocumentType> labelDocTypes, Dictionary<DocumentType, DocumentController> protoDocs)
        {
            // create a field dictionary to store keys associated with unmapped headers and labels
            var labelAndHeaderFieldDict = new Dictionary<KeyController, FieldModelController>();

            // add each of the unmapped headers as default field model controllers
            foreach (var header in unmappedHeaders)
            {
                var fieldValue = csv[header];
                var fieldController = ParseStringToFieldModelController(fieldValue);
                var keyController = new KeyController(DashShared.Util.GenerateNewId(), header);
                labelAndHeaderFieldDict.Add(keyController,
                    fieldController.GetDefaultController());
            }

            // add each of the label docs as default documentFieldModels
            foreach (var labelDocType in labelDocTypes)
            {
                var protoController = protoDocs.First(kvp => kvp.Value.DocumentType.Equals(labelDocType)).Value;
                labelAndHeaderFieldDict.Add(new KeyController(DashShared.Util.GenerateNewId(), labelDocType.Type),
                    new DocumentFieldModelController(protoController));
            }
            return labelAndHeaderFieldDict;
        }

        private static void AddProtoLabelDocsToProtoDocs(CsvImportHelperViewModel csvConfig, ImmutableHashSet<DocumentType> labelDocTypes,
            CsvReader csv, Dictionary<DocumentType, DocumentController> protoDocs)
        {
            // iterate through the label documents creating prototypes for each
            foreach (var docMap in csvConfig.DocToColumnMaps.Where(dmap => labelDocTypes.Contains(dmap.DocumentType)))
            {
                var documentType = docMap.DocumentType;

                var fieldDictionary = new Dictionary<KeyController, FieldModelController>();

                // add each of the columns mapped to that label document as fields of that label document
                foreach (var header in docMap.MappedHeaders)
                {
                    var fieldValue = csv[header];
                    var fieldController = ParseStringToFieldModelController(fieldValue);
                    var keyController = new KeyController(DashShared.Util.GenerateNewId(), header);
                    fieldDictionary.Add(keyController,
                        fieldController.GetDefaultController());
                }

                AddDocToProtoDocs(fieldDictionary, documentType, protoDocs);
            }
        }

        private static async Task<CsvImportHelperViewModel> GetCsvConfigFromUser(string[] headers)
        {
            var csvImporter = new CSVImportHelper(new CsvImportHelperViewModel(headers));
            MainPage.Instance.DisplayElement(csvImporter,
                new Point(MainPage.Instance.MainDocView.ActualWidth / 2, MainPage.Instance.MainDocView.ActualHeight / 2),
                MainPage.Instance.xCanvas);
            var csvConfig = await csvImporter.GetConfigFromUser();
            return csvConfig;
        }

        private static string[] GetHeadersFromCsv(CsvReader csv)
        {
            csv.ReadHeader(); // TODO can we check to see if the csv has a header or not? otherwise this fails, what happens when it doesn't
            var headers = csv.FieldHeaders;
            return headers;
        }

        private static void AddDocToProtoDocs(Dictionary<KeyController, FieldModelController> fieldDictionary, DocumentType documentType, Dictionary<DocumentType, DocumentController> protoDocs)
        {
            var protoDoc = new DocumentController(fieldDictionary, documentType);
            protoDoc.SetActiveLayout(new DefaultLayout().Document, true, true);
            protoDocs.Add(documentType, protoDoc);
        }

        private static bool IsLabelField(string fieldHeader, IEnumerable<DocumentType> labelDocTypes)
        {
            return labelDocTypes.Select(dt => dt.Type).Contains(fieldHeader);
        }

        private static FieldModelController ParseStringToFieldModelController(string fieldValue)
        {
            JToken token;
            try
            {
                token = JToken.Parse(fieldValue);
            }
            catch (Exception)
            {
                try
                {
                    fieldValue = fieldValue.Replace(@"'", @"\'");
                    token = JToken.Parse("'" + fieldValue + "'");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            var fieldController = new JsonToDashUtil().ParseValue(token);
            return fieldController;
        }
    }
}
