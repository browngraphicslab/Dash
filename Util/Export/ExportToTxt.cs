using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
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

        //define the max width and height coordinates in html and dash for conversion
        private static double PAGEWIDTH = 300.0;
        private static double PAGEHEIGHT = 1000.0;
        private static double DASHWIDTH = 1500.0;


        public static async void DashToTxt(IEnumerable<DocumentController> collectionDataDocs)
        {
            //TODO: other document types

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
                        //check if title ends in (x)
                        if ((colTitle.Length > 3) && (colTitle[colTitle.Length - 1].Equals(')')))
                        {
                            //2 digit number is parenthesis
                            if ((colTitle[colTitle.Length - 4].Equals('(')))
                            {
                                colTitle = colTitle.Substring(0, colTitle.Length - 4) + "(" + count + ")";
                            }
                            //one digit number
                            else if ((colTitle[colTitle.Length - 3].Equals('(')))
                            {
                                colTitle = colTitle.Substring(0, colTitle.Length - 3) + "(" + count + ")";
                            }
                        }
                        else
                        {
                            colTitle = colTitle + "(" + count + ")";
                        }
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
            if (collection.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null) != null)
            {
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
                    switch (docType) //TODO: there is also a Data Box
                    {
                        case "Rich Text Box":
                            newText = TextToTxt(doc);
                            break;
                        case "Image Box":
                            newText = ImageToTxt(doc);
                            break;
                        case "Background Box":
                            newText = BackgroundBoxToTxt(doc);
                            break; 
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
                }

                return fileText;
            }
            else
            {
                return new List<string>();
            }  
        }

        private static void OrderElements(List<DocumentController> docs)
        {
            // This shows calling the Sort(Comparison(T) overload using 
            // an anonymous method for the Comparison delegate. 
            docs.Sort(delegate (DocumentController doc1, DocumentController doc2)
            {
                var y1 = 0.0;
                var y2 = 0.0;
                //get the y points of each doc
                //check that doc has PostionFieldKey
                if (doc1.GetField(KeyStore.PositionFieldKey) != null)
                {
                    var pt1 = doc1.GetField(KeyStore.PositionFieldKey).DereferenceToRoot<PointController>(null);
                    y1 = pt1.Data.Y;
                }

                if (doc2.GetField(KeyStore.PositionFieldKey) != null)
                {
                    var pt2 = doc2.GetField(KeyStore.PositionFieldKey).DereferenceToRoot<PointController>(null);
                    y2 = pt2.Data.Y;
                }

                //return 1 if doc1 first and -1 if doc2 first
                if (y1 >= y2) return 1;
                else return -1;
            });
        }

        private static double getMargin(DocumentController doc)
        {
            var marginLeft = 0.0;
            if (doc.GetField(KeyStore.PositionFieldKey) != null)
            {
                var pt1 = doc.GetField(KeyStore.PositionFieldKey).DereferenceToRoot<PointController>(null);
                //TODO: I add 1500 to get rid of negatives, come up with better solution
                var x = pt1.Data.X + 1500.0;
                marginLeft = (x * PAGEWIDTH) / (DASHWIDTH);
               // var y = 
            }

            return marginLeft;
        }


        private static string TextToTxt(DocumentController doc)
        {
            var text = doc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null).Data;
            var marginLeft = getMargin(doc);

            return "<p style=\"margin-left: " + marginLeft + "px; \">" + text + "</p>";
        }

        private static string ImageToTxt(DocumentController doc)
        {
            //string version of the image uri
            var uri = doc.GetDereferencedField<ImageController>(KeyStore.DataKey, null).Data.ToString();

            //get image width and height
            var stringWidth = doc.GetField(KeyStore.ActualWidthKey).DereferenceToRoot(null).ToString();

            var marginLeft = getMargin(doc);

            //return uri with HTML image formatting
            return "<img src=\"" + uri + "\" width=\"" + stringWidth + "px\" style=\"margin-left: " + marginLeft + "px; \">";
        }

        private static string CollectionToTxt(DocumentController col)
        {
            var docs = col.GetDataDocument()
                .GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);


            //get text that must be added to file for this collection
            var colText = CollectionContent(col);
            //make sure there isn't already reference to colTitle
            var colTitle = col.ToString();
            int count = 1;
            while (UsedUrlNames.Contains(colTitle))
            {
                //check if title ends in (x)
                if ((colTitle.Length > 3) && (colTitle[colTitle.Length - 1].Equals(')')))
                {
                    //2 digit number is parenthesis
                    if ((colTitle[colTitle.Length - 4].Equals('(')))
                    {
                        colTitle = colTitle.Substring(0, colTitle.Length - 4) + "(" + count + ")";
                    }
                    //one digit number
                    else if ((colTitle[colTitle.Length - 3].Equals('(')))
                    {
                        colTitle = colTitle.Substring(0, colTitle.Length - 3) + "(" + count + ")";
                    }
                }
                else
                {
                    colTitle = colTitle + "(" + count + ")";
                }
                count++;
            }
            UsedUrlNames.Add(colTitle);

            //create a file in folder with colContent and titled colTitle
            CreateFile(colText, colTitle);

            var marginLeft = getMargin(col);

            //return link to page you just created
            return "<a href=\"./" + colTitle + ".html\" style=\"margin-left: " + marginLeft + "px; \">" + colTitle + "</a>";
        }

        private static string BackgroundBoxToTxt(DocumentController doc)
        {
            var text = "";
            // get shape of box
            var shape = doc.GetDereferencedField(KeyStore.AdornmentShapeKey, null).ToString();
            var colorC = doc.GetDereferencedField(KeyStore.BackgroundColorKey, null).ToString();
            //convert color to colro code used by html
            var color = "#" + colorC.Substring(3, 6) + colorC.Substring(1, 2);
            var width = doc.GetDereferencedField(KeyStore.WidthFieldKey, null).ToString();
            var height = doc.GetDereferencedField(KeyStore.HeightFieldKey, null).ToString();
            var marginLeft = getMargin(doc);

            if (shape == "Elliptical")
            {
                text = "<div style=\"height:" + height + "px; width: " + width + "px; border-radius: 50%; margin-left: " + marginLeft + "px; background-color: " + color + ";\"></div>";
            }
            else if (shape == "Rectangular")
            {
                text = "<div style=\"height:" + height + "px; width:" + width + "px; margin-left: " + marginLeft + "px; background-color: " + color + ";\"></div>";
            }
            else if (shape == "Rounded")
            {
                text = "<div style=\"height:" + height + "px; width:" + width + "px; border-radius: 15%; margin-left: " + marginLeft + "px; background-color: " + color + ";\"></div>";
            }
            return text;
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
