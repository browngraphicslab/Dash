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
using Windows.UI.Xaml;
using Dash.Controllers;
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
                    string colTitle = collectionDoc.GetDataDocument().GetDereferencedField(KeyStore.TitleKey, null)
                        .ToString();

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

                var minMax = MinVal(dataDocs);

                //list of each line of text that must be added to file - one string for each doc
                var fileText = new List<string>();

                foreach (var doc in dataDocs)
                {
                    string docType = doc.DocumentType.Type;
                    //create diffrent output for different document types by calling helper functions
                    string newText;
                    switch (docType) 
                    {
                        case "Rich Text Box":
                            newText = TextToTxt(doc, minMax);
                            break;
                        case "Image Box":
                            newText = ImageToTxt(doc, minMax);
                            break;
                        case "Background Box":
                            newText = BackgroundBoxToTxt(doc, minMax);
                            break; 
                        case "Collection Box":
                            newText = CollectionToTxt(doc, minMax);
                            break;
                        case "Key Value Document Box":
                            newText = KeyValToTxt(doc, minMax);
                            break;
                        default:
                            newText = null;
                            break;
                    }

                    //add text to list for specificed cases
                    if (newText != null)
                    {
                        Debug.WriteLine("<span>" + newText + "</span>");

                        fileText.Add("<span>" + newText + "</span>");
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

        private static List<double> MinVal(List<DocumentController> docs)
        {
            //TODO: same but for y

            double min = Double.PositiveInfinity;
            double max = Double.NegativeInfinity;
            // This finds the minimum x value saved in this collection in Dash
            //and saves Dash width
            foreach (var doc in docs)
            {
                if (doc.GetField(KeyStore.PositionFieldKey) != null)
                {
                    var pt1 = doc.GetField(KeyStore.PositionFieldKey).DereferenceToRoot<PointController>(null);
                    var x = pt1.Data.X;
                    if (x < min)
                    {
                        min = x;
                    }

                    if (x > max)
                    {
                        max = x;
                    }
                }
            }

            var minMax = new List<double>();
            minMax.Add(min);
            minMax.Add(max);
            return minMax;
        }

        private static double getMargin(DocumentController doc, List<double> minMax)
        {
            //TODO: if I scale margin, it looks funny not to scale width / height 
            var marginLeft = 0.0;
            if (doc.GetField(KeyStore.PositionFieldKey) != null)
            {
                var pt1 = doc.GetField(KeyStore.PositionFieldKey).DereferenceToRoot<PointController>(null);

                var min = minMax[0];
                var max = minMax[1];

                var x =  pt1.Data.X - min;

                var DASHWIDTH = Math.Abs(max - min + 50);
                marginLeft = (x * PAGEWIDTH) / (DASHWIDTH);
               // var y = 
            }

            return marginLeft;
        }

        private static double dashToHtml(double val, List<double> minMax)
        {
            var min = minMax[0];
            var max = minMax[1];

            var DASHWIDTH = Math.Abs(max - min + 50);
            return (val * PAGEWIDTH) / (DASHWIDTH);
        }


        private static string TextToTxt(DocumentController doc, List<double> minMax)
        {
            var rawText = doc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null);
            if (rawText != null)
            {
                var text = rawText.Data;
                var marginLeft = getMargin(doc, minMax);

                return "<p style=\"position: fixed; left: " + marginLeft + "px; \">" + text + "</p>";
            }
            else
            {
                return "";
            }
        }

        private static string ImageToTxt(DocumentController doc, List<double> minMax)
        {
            //string version of the image uri
            var uri = doc.GetDereferencedField<ImageController>(KeyStore.DataKey, null).Data.ToString();

            //get image width and height
            var stringWidth = doc.GetField(KeyStore.WidthFieldKey).DereferenceToRoot(null).ToString();

            var marginLeft = getMargin(doc, minMax);

            //return uri with HTML image formatting
            return "<img src=\"" + uri + "\" width=\"" + dashToHtml(Convert.ToDouble(stringWidth), minMax) 
                   + "px\" style=\"position: fixed; left: " + marginLeft + "px; \">";
        }

        private static string CollectionToTxt(DocumentController col, List<double> minMax)
        {
            //get text that must be added to file for this collection
            var colText = CollectionContent(col);
            //make sure there isn't already reference to colTitle
            string colTitle = col.GetDataDocument().GetDereferencedField(KeyStore.TitleKey, null)
                .ToString();
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

            var marginLeft = getMargin(col, minMax);

            //return link to page you just created
            return "<a href=\"./" + colTitle + ".html\" style=\"position: fixed; left: " + marginLeft + "px; \">" + colTitle + "</a>";
        }

        private static string BackgroundBoxToTxt(DocumentController doc, List<double> minMax)
        {
            var text = "";
            // get shape of box
            var shape = doc.GetDereferencedField(KeyStore.AdornmentShapeKey, null).ToString();
            var colorC = doc.GetDereferencedField(KeyStore.BackgroundColorKey, null).ToString();
            //convert color to colro code used by html
            var color = "#" + colorC.Substring(3, 6) + colorC.Substring(1, 2);
            var width = doc.GetDereferencedField(KeyStore.WidthFieldKey, null).ToString();
            var height = doc.GetDereferencedField(KeyStore.HeightFieldKey, null).ToString();
            var marginLeft = getMargin(doc, minMax);

            //convert width and height in proportion to other elements
            //convert width and height in proportion to other elements
            width = dashToHtml(Convert.ToDouble(width), minMax).ToString();
            height = dashToHtml(Convert.ToDouble(height), minMax).ToString();

            if (shape == "Elliptical")
            {
                text = "<div style=\"height:" + height + "px; width: " + width + "px; border-radius: 50%; " +
                       "position: fixed; left: " + marginLeft + "px; background-color: " + color + ";\"></div>";
            }
            else if (shape == "Rectangular")
            {
                text = "<div style=\"height:" + height + "px; width:" + width + "px; " +
                       "position: fixed; left: " + marginLeft + "px; background-color: " + color + ";\"></div>";
            }
            else if (shape == "Rounded")
            {
                text = "<div style=\"height:" + height + "px; width:" + width + "px; border-radius: 15%; " +
                       "position: fixed; left: " + marginLeft + "px; background-color: " + color + ";\"></div>";
            }
            return text;
        }

        private static string KeyValToTxt(DocumentController doc, List<double> minMax)
        {
            //make table with document fields
            var marginLeft = getMargin(doc, minMax);
            var text = "<table style=\"position: fixed; left: " + marginLeft + "px; width: 70px; border-collapse: collapse;\">";
            var tdStyle = "style=\"border: 1px solid #dddddd; padding: 8px; \"";

            var data = doc.GetDataDocument();
       
            var backgr = data.GetDereferencedField(KeyStore.BackgroundColorKey, null);
            if (backgr != null)
            {
                text = text + "<tr> <td " + tdStyle + "> Background Color </td> <td " + tdStyle + ">" + backgr + "</td></tr>";
            }

            var adorns = data.GetDereferencedField(KeyStore.AdornmentShapeKey, null);
            if (adorns != null)
            {
                text = text + "<tr> <td " + tdStyle + "> Adornment Shape </td> <td " + tdStyle + ">" + adorns + "</td></tr>";
            }

            var dataf = data.GetDereferencedField(KeyStore.DataKey, null);
            if (dataf != null)
            {
                text = text + "<tr> <td " + tdStyle + "> Data </td> <td " + tdStyle + ">" + dataf + "</td></tr>";
            }

            var title = data.GetDereferencedField(KeyStore.TitleKey, null);
            if (title != null)
            {
                text = text + "<tr> <td " + tdStyle + "> Title </td> <td " + tdStyle + ">" + title + "</td></tr>";
            }

            DateTimeController time = (DateTimeController)data.GetDereferencedField(KeyStore.ModifiedTimestampKey, null);
            if (time != null)
            {
                text = text + "<tr> <td " + tdStyle + "> Modified Time </td> <td " + tdStyle + ">" + time.Data + "</td></tr>";
            }

            var doctext = data.GetDereferencedField(KeyStore.DocumentTextKey, null);
            if (doctext != null)
            {
                text = text + "<tr> <td " + tdStyle + "> Document Text </td> <td " + tdStyle + ">" + doctext + "</td></tr>";
            }


            return text + "</table>";
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
                        mergedText = mergedText + word;
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
