using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using DashShared;
using Flurl.Util;
using Newtonsoft.Json.Serialization;

namespace Dash
{
    //Windows.Storage.ApplicationData.Current.LocalFolder;

    public static class ExportToTxt
    {

        public static void CollectionToTxt(DocumentController collection)
        {
            //Get all the Document Controller in the collection
            //The document controllers are saved as the Data Field in each collection
            var dataDocs = collection.GetField(KeyStore.DataKey).DereferenceToRoot<ListController<DocumentController>>(null).TypedData;

          //create a list of key value pairs that link each doc to its point
            List<KeyValuePair<DocumentController, List<double>>> docToPt = new List<KeyValuePair<DocumentController, List<double>>>();

            foreach (var doc in dataDocs)
            {
                String docType = doc.DocumentType.Type;
                //create diffrent output for different document types by calling helper functions
                switch (docType)
                {
                    case "Rich Text Box":
                        String a = TextToTxt(doc);
                        break;
                    case "Image Box":
                        Console.WriteLine("Case 1");
                        break;
                    case "Background Box":
                        Console.WriteLine("Case 1");
                        break;
                    case "Collection Box":
                        Console.WriteLine("Case 1");
                        break;
                    default:
                        Console.WriteLine("Default case");
                        break;
                }

                //Get an ImmutableList of KetValue Pairs with doc properites - add a breaker point and look at properties to see what extentions to add
                IEnumerable <KeyValuePair<String, Object>> docPostion = doc.GetField(KeyStore.PositionFieldKey).DereferenceToRoot(null).ToKeyValuePairs().ToImmutableList();
                object rawpoint = docPostion.ElementAt(1).Value;
                double xPt = (double)(rawpoint.GetType().GetProperty("X").GetValue(rawpoint, null));
                double yPt = (double)(rawpoint.GetType().GetProperty("Y").GetValue(rawpoint, null));

                //now add data to docToPt List
                List<double> point = new List<double>();
                point.Add(xPt);
                point.Add(yPt);
                docToPt.Add(new KeyValuePair<DocumentController, List<double>>(doc, point));
            }
        

           // var orderedDocs = OrderElements(docToPt);
            SaveData();
        }

        private static List<DocumentController> OrderElements(List<KeyValuePair<DocumentController, List<double>>> docPtData)
        {
            IEnumerable<DocumentController> a = ContentController<FieldModel>.GetControllers<DocumentController>();

            //reorder list of elems based on position
            foreach (var doc in a)
            {
                // Context context = doc;
                // var deepestDelegateID = context?.GetDeepestDelegateOf(DocumentId) ?? DocumentId;


                Debug.WriteLine(doc);

                Point pt = doc.GetPositionField().Data;
                double xpt = pt.X;
                double ypt = pt.Y;
                Debug.WriteLine("x - " + xpt + " y - " + ypt);
            }
            return null;
        }


        private static String TextToTxt(DocumentController doc)
        {
            return doc.Title;
        }

        private static String ImageToTxt(DocumentController doc)
        {
            return "b";
        }


        private static async void SaveData()
        {
            String filename = "Sample.md";

            StorageFile stFile;
            if (!(Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons")))
            {
                FileSavePicker savePicker = new FileSavePicker();
                savePicker.DefaultFileExtension = ".md";
                savePicker.SuggestedFileName = "Dash";
                savePicker.FileTypeChoices.Add("Markdown Document", new List<string>() { ".md" });
                stFile = await savePicker.PickSaveFileAsync();
                await Windows.Storage.FileIO.WriteTextAsync(stFile, "Swift as a shadow");
            }
            else
            {
                StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;
                stFile = await local.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                await Windows.Storage.FileIO.WriteTextAsync(stFile, "Swift as a shadow");
            }
        }
    }
}
