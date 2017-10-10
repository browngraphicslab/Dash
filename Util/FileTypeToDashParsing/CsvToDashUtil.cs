using System;
using System.Collections.Generic;
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

            var csv = new CsvReader(streamReader);
            csv.ReadHeader(); // TODO can we check to see if the csv has a header or not? otherwise this fails, what happens when it doesn't
            var headers = csv.FieldHeaders;

            MainPage.Instance.DisplayElement(new CSVImportHelper(new CsvImportHelperViewModel(headers)), 
                new Point(MainPage.Instance.MainDocView.ActualWidth/2, MainPage.Instance.MainDocView.ActualHeight/2), 
                MainPage.Instance.xCanvas);


            //MainPage.Instance.xCanvas.Children.Add(new CSVImportHelper(new CsvImportHelperViewModel(headers)));

            var records = new List<Dictionary<string, dynamic>>();
            while (csv.Read())
            {
                var record = new Dictionary<string, dynamic>();
                for (int i = 0; i < headers.Length; i++)
                {
                    double double_field;
                    string string_field;
                    if (csv.TryGetField(i, out double_field))
                    {
                        record[headers[i]] = double_field;
                    }
                    else if (csv.TryGetField(i, out string_field))
                    {
                        record[headers[i]] = string_field;
                    }
                    else
                    {
                        Debug.WriteLine("Failed to get field");
                    }
                }
                records.Add(record);
            }
            var resultDict = new Dictionary<string, List<Dictionary<string, dynamic>>>()
            {
                ["CSVRecords"] = records,
            };

            var json = JsonConvert.SerializeObject(resultDict);
            return  new JsonToDashUtil().ParseJsonString(json, item.Path);
        }
    }
}
