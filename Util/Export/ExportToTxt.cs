using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using DashShared;
using Flurl.Util;

namespace Dash
{
    //Windows.Storage.ApplicationData.Current.LocalFolder;

    public static class ExportToTxt
    {

        public static void CollectionToTxt(DocumentController collection)
        {
            //Get all the Document Controller in the collection
            //The document cotnrollers are saved as the Data Field in each collection
            var dataDocs = collection.GetField(KeyStore.DataKey).DereferenceToRoot<ListController<DocumentController>>(null).TypedData;

            foreach (var viewDoc in dataDocs)
            {
                // Debug.WriteLine($"{viewDoc.GetField(KeyStore.PositionFieldKey)}");
                // var dataDoc = viewDoc.GetDataDocument();
                IEnumerable<KeyValuePair<String, Object>> docPostion = viewDoc.GetField(KeyStore.PositionFieldKey).DereferenceToRoot(null).ToKeyValuePairs().ToImmutableList();
               // docPostion.
                //Debug.WriteLine(a);

             
            }

           // var orderedDocs = OrderElements(viewDocs);
           // SaveData();
        }

        private static List<DocumentController> OrderElements(List<DocumentController> elems)
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

        private static void allDocsToTxt()
        {

        }

        private static void TextToTxt()
        {

        }

        private static void ImgToTxt()
        {

        }


        private static async void SaveData()
        {
            String filename = "Sample.txt";

            StorageFile stFile;
            if (!(Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons")))
            {
                FileSavePicker savePicker = new FileSavePicker();
                savePicker.DefaultFileExtension = ".txt";
                savePicker.SuggestedFileName = "Dash";
                savePicker.FileTypeChoices.Add("Txt Document", new List<string>() { ".txt" });
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
