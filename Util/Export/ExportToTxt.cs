using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
        private static StorageFolder folder;
        private static List<String> UsedUrlNames = new List<string>();

        public static async void DashToTxt(IEnumerable<DocumentController> collectionDataDocs)
        {
            //TODO: other document types

            //TODO: collections can be named same thing, which would screw up links

            //allow the user to pick a folder to save all the files
            folder = await PickFolder();

            if (folder != null)
            {
                //save names of all collections for linking pruposes
                List<String> colNames = new List<string>();

                //create one file in this folder for each collection
                foreach (var collectionDoc in collectionDataDocs)
                {
                    //CollectionToTxt returns the content that must be added to a file
                    var colContent = CollectionContent(collectionDoc);
                    var colTitle = collectionDoc.ToString();

                    //TODO: get rid of old number before adding new one
                    //make sure there isn't already reference to colTitle
                    int count = 1;
                    while (UsedUrlNames.Contains(colTitle))
                    {
                        colTitle = colTitle + count.ToString();
                        count++;
                    }
                    UsedUrlNames.Add(colTitle);

                    colNames.Add(colTitle);

                    //create a file in folder with colContent and titled colTitle
                    CreateFile(colContent, colTitle);
                }

                //make index.html file that refrences other collections
                CreateIndex(colNames);
            }
        }

        public static List<string> CollectionContent(DocumentController collection)
        {
            //Get all the Document Controller in the collection
            //The document controllers are saved as the Data Field in each collection
           // var dataDocs = collection.GetField(KeyStore.DataKey).DereferenceToRoot<ListController<DocumentController>>(null).TypedData;
            var dataDocs = collection.GetDataDocument()
                .GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).TypedData;

            //create a list of key value pairs that link each doc to its point
           // var docToPt = new List<KeyValuePair<DocumentController, List<double>>>();

            OrderElements(dataDocs);

            //list of each line of text that must be added to file - one string for each doc
            var fileText = new List<string>();

            foreach (var doc in dataDocs)
            {
                var docType = doc.DocumentType.Type;
                //create diffrent output for different document types by calling helper functions
                string newText;
                switch (docType)
                {
                    case "Rich Text Box":
                        newText = TextToTxt(doc);
                        break;
                    case "Image Box":
                        newText = ImageToTxt(doc);
                        break;
                /*    case "Background Box":
                        Console.WriteLine("Case 1");
                        break; */
                    case "Collection Box":
                        newText = CollectionToTxt(doc);
                        break; 
                    default:
                        newText = null;
                        break;
                }

                //add text to list for specificed cases
                if (newText != null)
                {
                    fileText.Add(newText);
                }

                /*
                //Get an ImmutableList of KeyValue Pairs with doc properites - add a breaker point and look at properties to see what extentions to add
                IEnumerable <KeyValuePair<String, Object>> docPostion = doc.GetField(KeyStore.PositionFieldKey).DereferenceToRoot(null).ToKeyValuePairs().ToImmutableList();
                object rawpoint = docPostion.ElementAt(1).Value;
                double xPt = (double)(rawpoint.GetType().GetProperty("X").GetValue(rawpoint, null));
                double yPt = (double)(rawpoint.GetType().GetProperty("Y").GetValue(rawpoint, null)); 

                //now add data to docToPt List
                List<double> point = new List<double>();
                point.Add(xPt);
                point.Add(yPt);
                docToPt.Add(new KeyValuePair<DocumentController, List<double>>(doc, point));
                */
            }

            return fileText;
        }


        private static void OrderElements(List<DocumentController> docs)
        {
            // This shows calling the Sort(Comparison(T) overload using 
            // an anonymous method for the Comparison delegate. 
            // This method treats null as the lesser of two values.
            docs.Sort(delegate (DocumentController doc1, DocumentController doc2)
            {
                //get the y points of each doc
                var pt1 = doc1.GetField(KeyStore.PositionFieldKey).DereferenceToRoot<PointController>(null);
                var y1 = pt1.Data.Y;
                var pt2 = doc2.GetField(KeyStore.PositionFieldKey).DereferenceToRoot<PointController>(null);
                var y2 = pt2.Data.Y;

                //return 1 if doc1 first and -1 if doc2 first
                if (y1 >= y2) return 1;
                else return -1;
            });
        }


        private static string TextToTxt(DocumentController doc)
        {
            return doc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null).Data;
        }

        private static string ImageToTxt(DocumentController doc)
        {
            //string version of the image uri
            var uri = doc.GetDereferencedField<ImageController>(KeyStore.DataKey, null).Data.ToString();

            //get image width and height
            var stringWidth = doc.GetField(KeyStore.ActualWidthKey).DereferenceToRoot(null).ToString();

            //return uri with HTML image formatting
            return "<img src=\"" + uri + "\" width=\"" + stringWidth + "\">";
        }

        private static string CollectionToTxt(DocumentController col)
        {
            var docs = col.GetDataDocument()
                .GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);


            //get text that must be added to file for this collection
            var colText = CollectionContent(col);
            var colTitle = col.ToString();

            //create a file in folder with colContent and titled colTitle
            CreateFile(colText, colTitle);

            //return link to page you just created
            return "<a href=\"./" + colTitle + ".html\">" + colTitle + "</a>";
        }

        private static async Task<StorageFolder> PickFolder()
        {
            StorageFolder stFolder;
            if (!(Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons")))
            {
                //create folder picker with basic properties
                var savePicker = new FolderPicker();
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                savePicker.FileTypeFilter.Add("*");
                savePicker.ViewMode = PickerViewMode.Thumbnail;

                stFolder = await savePicker.PickSingleFolderAsync();

                //return the folder that the user picked - it is a task bc async
                return stFolder;
            }
            else
            {
                //If the user can't pick a folder, it just makes a folder in data called Dash
                var local = Windows.Storage.ApplicationData.Current.LocalFolder;
                stFolder = await local.CreateFolderAsync("Dash");
                return stFolder;
            }

            
        }


        private static async void CreateFile(List<string> text, string title)
        {
            if (folder != null)
            {
                var filename = title + ".html";
                var stFile = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                if (stFile != null)
                {
                    //combine strings to one string to add to file
                    var mergedText = "";
                    foreach (var word in text)
                    {
                        mergedText = mergedText + word + "<br>";
                    }

                        //add String to file - this happens a bit after file is saved
                        await Windows.Storage.FileIO.WriteTextAsync(stFile, mergedText);
                   
                }
            }
        }
    

        private static async void CreateIndex(List<string> subCollections)
        {
            List<String> htmlContent = new List<string>();
            foreach (var colName in subCollections)
            {
                //make link to this collection
                htmlContent.Add("<a href=\"./" + colName + ".html\">" + colName + "</a>");
            }

            CreateFile(htmlContent, "index");
        }
    }
}
