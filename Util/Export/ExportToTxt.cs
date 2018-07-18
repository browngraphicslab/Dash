using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Controllers;
using DashShared;
using Flurl.Util;
using Newtonsoft.Json.Serialization;
using System.IO;

namespace Dash
{
    //Windows.Storage.ApplicationData.Current.LocalFolder;

    public class ExportToTxt
    {
        private StorageFolder folder;
        private List<String> UsedUrlNames = new List<string>();

        //define the max width and height coordinates in html and dash for conversion
        private double PAGEWIDTH = 900.0;
        private double PAGEHEIGHT = 900.0;

        private int imgCount = 0;
        private int vidCount = 0;
        private int audCount = 0;

        public async void DashToTxt(IEnumerable<DocumentController> collectionDataDocs)
        {

            //allow the user to pick a folder to save all the files
            StorageFolder outerFolder = await PickFolder();

            if (outerFolder != null)
            {
                // Get the current date in good string format for file name
                DateTime thisDay = DateTime.Now;
                var dayR = thisDay.Date.ToString();
                string day = dayR.Split(' ')[0].Replace('/', '.');
                string timeR = thisDay.TimeOfDay.ToString();
                string time = timeR.Split('.')[0].Replace(':', '.');
                string daytimeString = "export_" + day + "_" + time;

                //create a folder inside selected folder to save this export
                folder = await outerFolder.CreateFolderAsync(daytimeString, CreationCollisionOption.ReplaceExisting);

                //save names of all collections for linking pruposes
                List<String> colNames = new List<string>();

                //create one file in this folder for each collection
                foreach (var collectionDoc in collectionDataDocs)
                {
                    //CollectionToTxt returns the content that must be added to a file
                    var colContent = await CollectionContent(collectionDoc);
                    FieldControllerBase rawTitle = collectionDoc.GetDataDocument().GetDereferencedField(KeyStore.TitleKey, null);
                    string colTitle;
                    if (rawTitle == null)
                    {
                        colTitle = "Untitled";
                    }
                    else
                    {
                        colTitle = rawTitle.ToString();
                    }

                    colTitle = SafeTitle(colTitle);

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
                CreateIndex(colNames, folder.Name);
            }
        }

        public async Task<List<string>> CollectionContent(DocumentController collection)
        {
            //Get all the Document Controller in the collection
            //The document controllers are saved as the Data Field in each collection
            // var dataDocs = collection.GetField(KeyStore.DataKey).DereferenceToRoot<ListController<DocumentController>>(null).TypedData;
            if (collection.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null) != null)
            {
                var dataDocs = collection.GetDataDocument()
                    .GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).TypedData;

                OrderElements(dataDocs);

                var minMax = BorderVal(dataDocs);

                //list of each line of text that must be added to file - one string for each doc
                var fileText = new List<string>();
                fileText.Add("<body background=\"https://s3.amazonaws.com/spoonflower/public/design_thumbnails/0611/7656/windowpane2in_c21_shop_thumb.png\" > ");

                foreach (var doc in dataDocs)
                {
                    string docType = doc.DocumentType.Type;
                    //create diffrent output for different document types by calling helper functions
                    string newText;
                    //TODO TFS: This switch statement should switch on DocumentType instead of the string contained in DocumentType
                    switch (docType) 
                    {
                        case "Rich Text Box":
                            newText = TextToTxt(doc, minMax);
                            break;
                        case "Image Box":
                            newText = ImageToTxt(doc, minMax);
                            break;
                        case "Video Box":
                            newText = VideoToTxt(doc, minMax);
                            break;
                        case "Audio Box":
                            newText = AudioToTxt(doc, minMax);
                            break;
                        case "Background Shape":
                            newText = BackgroundShapeToTxt(doc, minMax);
                            break; 
                        case "Collection Box":
                            newText = await CollectionToTxt(doc, minMax);
                            break;
                        case "Key Value Document Box":
                            newText = KeyValToTxt(doc, minMax);
                            break;
                        default:
                            newText = "";
                            break;
                    }

                    //add text to list for specificed cases
                    if (newText != null)
                    {
                        fileText.Add("<span>" + newText + "</span>");
                    }
                }

                fileText.Add("</body>");
                return fileText;
            }
            else
            {
                return new List<string>();
            }  
        }

        private void OrderElements(List<DocumentController> docs)
        {
            // This shows calling the Sort(Comparison(T) overload using 
            // an anonymous method for the Comparison delegate. 
            docs.Sort(delegate (DocumentController doc1, DocumentController doc2)
            {
                // NOTE if no positionfield set we default to 0
                var y1 = 0.0;
                var y2 = 0.0;
                //get the y points of each doc
                //check that doc has PostionFieldKey
                var pt1 = doc1.GetPosition();
                if (pt1 != null)
                {
                    y1 = pt1.Value.Y;
                }

                var pt2 = doc2.GetPosition();
                if (pt2 != null)
                {
                    y2 = pt2.Value.Y;
                }

                //return 1 if doc1 first and -1 if doc2 first
                if (y1 >= y2) return 1;
                else return -1;
            });
        }

        private List<double> BorderVal(List<DocumentController> docs)
        {
            double minX = Double.PositiveInfinity;
            double maxX = Double.NegativeInfinity;

            double minY = Double.PositiveInfinity;
            double maxY = Double.NegativeInfinity;
            // This finds the minimum and max values saved in this collection in Dash
            //and saves Dash width
            foreach (var doc in docs)
            {
                if (doc.GetField(KeyStore.PositionFieldKey) != null)
                {
                    var pt1 = doc.GetField(KeyStore.PositionFieldKey).DereferenceToRoot<PointController>(null);
                    var x = pt1.Data.X;
                    var y = pt1.Data.Y;

                    var rawWidth = doc.GetField(KeyStore.WidthFieldKey).DereferenceToRoot(null).ToString();
                    double width = 0;
                    if (rawWidth != "NaN")
                    {
                        width = Convert.ToDouble(rawWidth);
                    }

                    if (x < minX)
                    {
                        minX = x;
                    }
                    if (y < minY)
                    {
                        minY = y;
                    }

                    if (x + width > maxX)
                    {
                        maxX = x + width;
                    }
                    if (y > maxY)
                    {
                        maxY = y;
                    }
                }
            }

            var minMax = new List<double>();


            //add numbers for margins
            minMax.Add(minX - (minX * .1));
            minMax.Add(maxX);
            minMax.Add(minY - (Math.Abs(minY) * .1));
            minMax.Add(maxY + (Math.Abs(maxY) * .1));
            return minMax;
        }

        private List<double> getMargin(DocumentController doc, List<double> minMax)
        {
            var marginLeft = 0.0;
            var marginTop = 0.0;
            var pt1 = doc.GetPosition();
            if (pt1 != null)
            {
                var minX = minMax[0];
                var maxX = minMax[1];

                var x =  pt1.Value.X - minX;

                var DASHWIDTH = Math.Abs(maxX - minX + 50);
                marginLeft = (x * PAGEWIDTH) / (DASHWIDTH);

                var minY = minMax[2];
                var maxY = minMax[3];

                var y = pt1.Value.Y - minY;

                var DASHHEIGHT = Math.Abs(maxY - minY);
                marginTop = (y * PAGEHEIGHT) / (DASHHEIGHT);
            }

            var margins = new List<double>();
            margins.Add(marginLeft);
            margins.Add(marginTop);
            return margins;
        }

        private double dashToHtml(double val, List<double> minMax)
        {
            var min = minMax[0];
            var max = minMax[1];

            var DASHWIDTH = Math.Abs(max - min + 50);
            return (val * PAGEWIDTH) / (DASHWIDTH);
        }


        private string TextToTxt(DocumentController doc, List<double> minMax)
        {
            var rawText = doc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null);
            if (rawText != null)
            {
                var text = rawText.Data;
                var margins = getMargin(doc, minMax);

                var stringWidth = doc.GetField(KeyStore.WidthFieldKey);

                if (stringWidth == null) {
                    return "<p style=\"position: fixed; left: " + margins[0] + "px; top: " + margins[1] + "px;  z-index: 2; \">" + text + "</p>";

                } else {
                    return "<p style=\"width: " + dashToHtml(Convert.ToDouble(stringWidth.DereferenceToRoot(null).ToString()), minMax)
                       + "px; position: fixed; left: " + margins[0] + "px; top: " + margins[1] + "px;  z-index: 2; \">" + text + "</p>";
                }
            }
            else
            {
                return "";
            }
        }

        private string ImageToTxt(DocumentController doc, List<double> minMax)
        {
            //string version of the image uri
            var uriRaw = doc.GetDereferencedField<ImageController>(KeyStore.DataKey, null);
            if (uriRaw != null)
            {
                var olduri = uriRaw.ToString();
                
                //create image with unique title
                var imgTitle = "img" + imgCount + ".jpg";
                imgCount++;
                CopyFile(olduri, imgTitle, "imgs", imgCount);

                var uri = "imgs\\" + imgTitle;

                //get image width and height
                var stringWidth = doc.GetField(KeyStore.WidthFieldKey).DereferenceToRoot(null).ToString();

                var margins = getMargin(doc, minMax);

                //return uri with HTML image formatting
                return "<img src=\"" + uri + "\" width=\"" + dashToHtml(Convert.ToDouble(stringWidth), minMax)
                       + "px\" style=\"position: fixed; left: " + margins[0] + "px; top: " + margins[1] + "px; \">";
            }
            else
            {
                return "";
            }
        }

        private string VideoToTxt(DocumentController doc, List<double> minMax)
        {
            //string version of the image uri
            var uriRaw = doc.GetDereferencedField(KeyStore.DataKey, null);
            if (uriRaw != null)
            {
                var olduri = uriRaw.ToString();
                
                //create image with unique title
                var vidTitle = "vid" + vidCount + ".mp4";
                vidCount++;
                CopyFile(olduri, vidTitle, "vids", vidCount);

                var uri = "vids\\" + vidTitle; 


                //get image width and height
                var stringWidth = doc.GetField(KeyStore.WidthFieldKey).DereferenceToRoot(null).ToString();

                var margins = getMargin(doc, minMax);

                //return uri with HTML video formatting
                return "<video width=\"" + dashToHtml(Convert.ToDouble(stringWidth), minMax) + "px\" " +
                       "style=\"position: fixed; left: " + margins[0] + "px; top: " + margins[1] +
                       "px;  z-index: 1; \" controls>" +
                       "<source src=\"" + uri + "\" >" +
                       "Your browser does not support the video tag. </video>";
            }
            else
            {
                return "";
            } 
        }

        private string AudioToTxt(DocumentController doc, List<double> minMax)
        {
            //string version of the image uri
            var uriRaw = doc.GetDereferencedField(KeyStore.DataKey, null);
            if (uriRaw != null)
            {
                var olduri = uriRaw.ToString();

                //create image with unique title
                var audTitle = "aud" + audCount + ".mp3";
                audCount++;
                CopyFile(olduri, audTitle, "auds", audCount);

                var uri = "auds\\" + audTitle;


                //get image width and height
                var stringWidth = doc.GetField(KeyStore.WidthFieldKey).DereferenceToRoot(null).ToString();

                var margins = getMargin(doc, minMax);

                //return uri with HTML video formatting
                return "<audio width=\"" + dashToHtml(Convert.ToDouble(stringWidth), minMax) + "px\" " +
                       "style=\"position: fixed; left: " + margins[0] + "px; top: " + margins[1] +
                       "px;  z-index: 1; \" controls>" +
                       "<source src=\"" + uri + "\" >" +
                       "Your browser does not support audio. </audio>";
            }
            else
            {
                return "";
            }
        }

        private async Task<string> CollectionToTxt(DocumentController col, List<double> minMax)
        {
            //get text that must be added to file for this collection
            var colText = await CollectionContent(col);
            //make sure there isn't already reference to colTitle
            string colTitle = col.GetDataDocument().GetDereferencedField(KeyStore.TitleKey, null)
                .ToString();
            colTitle = SafeTitle(colTitle);
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

            var margins = getMargin(col, minMax);

            // TODO extract collection stuff into a new method
            var colWidth = dashToHtml(Convert.ToDouble(col.GetDereferencedField(KeyStore.WidthFieldKey, null).ToString()), minMax);

            // create a collectionview off the screen
            var collView = col.MakeViewUI(null) as CollectionView;
            Debug.Assert(collView != null);    
            MainPage.Instance.xCanvas.Children.Add(collView);
            Canvas.SetLeft(collView, -10000);
            Canvas.SetTop(collView, -10000);

            // when the currentview of the collectionview is loaded (i.e. freeform grid etc...)
            // render the currentview to a bitmap and save it for export
            // if you don't wait for currentviewloaded you get a white screen since only the background
            // of the collection has loaded
            collView.CurrentViewLoaded += async delegate
            {
                var bitmap = new RenderTargetBitmap();
                await bitmap.RenderAsync(collView.CurrentView);
               
                var file = await folder.CreateFileAsync($"{colTitle}.png", CreationCollisionOption.ReplaceExisting);

                var pixels = await bitmap.GetPixelsAsync();

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);

                    var displayInformation = DisplayInformation.GetForCurrentView();

                    encoder.SetPixelData(
                        BitmapPixelFormat.Rgba8,
                        BitmapAlphaMode.Ignore,
                        (uint)bitmap.PixelWidth,
                        (uint)bitmap.PixelHeight,
                        displayInformation.RawDpiX,
                        displayInformation.RawDpiY,
                        pixels.ToArray());

                    await encoder.FlushAsync();
                }
                MainPage.Instance.xCanvas.Children.Remove(collView); // remove the collection from the canvas (cleanup)
            };

            //return link to the image you just created
            return "<a href=\"./" + colTitle + ".html\" style=\"position: fixed; left: " + margins[0] + "px; top: " + margins[1] + "px; \">" +
                   "<div style=\"width: "+ colWidth + "px; border: 1px solid black; \">" + "<img style=\"width: 100%;\" src=\"" + colTitle + ".png\"/>" + "</div></a>";
        }
        
        private string BackgroundShapeToTxt(DocumentController doc, List<double> minMax)
        {
            var text = "";

            //var shape = doc.GetDereferencedField(KeyStore.AdornmentShapeKey, null).ToString();
            DocumentController data = doc.GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, null);

            // get shape of box
            var shape = data.GetDereferencedField(KeyStore.DataKey, null).ToString();
            var colorC = data.GetDereferencedField(KeyStore.BackgroundColorKey, null).ToString();


            //convert color to colro code used by html
            var color = "#" + colorC.Substring(3, 6) + colorC.Substring(1, 2);
            var width = doc.GetDereferencedField(KeyStore.WidthFieldKey, null).ToString();
            var height = doc.GetDereferencedField(KeyStore.HeightFieldKey, null).ToString();
            var margins = getMargin(doc, minMax);

            //convert width and height in proportion to other elements
            //convert width and height in proportion to other elements
            width = dashToHtml(Convert.ToDouble(width), minMax).ToString();
            height = dashToHtml(Convert.ToDouble(height), minMax).ToString();

            if (shape == "Elliptical")
            {
                text = "<div style=\"height:" + height + "px; width: " + width + "px; border-radius: 50%; " +
                       "position: fixed; left: " + margins[0] + "px; top: " + margins[1] + "px; background-color: " + color + ";\"></div>";
            }
            else if (shape == "Rectangular")
            {
                text = "<div style=\"height:" + height + "px; width:" + width + "px; " +
                       "position: fixed; left: " + margins[0] + "px; top: " + margins[1] + "px; background-color: " + color + ";\"></div>";
            }
            else if (shape == "Rounded")
            {
                text = "<div style=\"height:" + height + "px; width:" + width + "px; border-radius: 15%; " +
                       "position: fixed; left: " + margins[0] + "px; top: " + margins[1] + "px; background-color: " + color + ";\"></div>";
            }
            return text;
        }

        private string KeyValToTxt(DocumentController doc, List<double> minMax)
        {
            //make table with document fields
            var margins = getMargin(doc, minMax);
            var text = "<table style=\"position: fixed; left: " + margins[0] + "px; top: " + margins[1] + "px; width: 70px; border-collapse: collapse;\">";
            var tdStyle = "style=\"border: 1px solid #dddddd; padding: 8px; z-index: -1;\"";

            var data = doc.GetDataDocument();

            foreach (var kvp in data.EnumDisplayableFields())
            {
                text = text + "<tr> <td " + tdStyle + "> " + kvp.Key.Name + " </td> <td " + tdStyle + ">" + kvp.Value + "</td></tr>";
            }

            return text + "</table>";
        }

        private async Task<StorageFolder> PickFolder()
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

        private string SafeTitle(string title)
        {
            return title.Replace('/', 'a').Replace('\\', 'a').Replace(':', 'a').Replace('?', 'a')
                .Replace('*', 'a').Replace('"', 'a').Replace('<', 'a').Replace('>', 'a').Replace('|', 'a')
                .Replace('#', 'a');
        }


        private async void CreateFile(List<string> text, string title)
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
                    
                    //add String to file
                    await Windows.Storage.FileIO.WriteTextAsync(stFile, mergedText);
                   
                }
            }
        }

        private async void CopyFile(string rawUrl, string title, string type, int count)
        {
            if (folder != null)
            {
                if (count == 1)
                {
                    //create folder to save media - images, videos, etc
                    await folder.CreateFolderAsync(type, CreationCollisionOption.ReplaceExisting);
                }
                
                StorageFolder localFolder =
                    Windows.Storage.ApplicationData.Current.LocalFolder;

                var parts = rawUrl.Split('/');
                string url = parts[parts.Length - 1];

                StorageFile imgFile = await localFolder.GetFileAsync(url);
                StorageFolder imgFolder = await folder.GetFolderAsync(type);

                //copy imgFile from rawUrl to type folder in export folder
                await imgFile.CopyAsync(imgFolder, title);
            }
        }


        private async void CreateIndex(List<string> subCollections, String name)
        {
            List<String> htmlContent = new List<string>();
            htmlContent.Add("<center><h1 style=\"font-size: 70px; margin-bottom: -30px;\">Dash</h1></center><br>");
            htmlContent.Add("<center><h2>" + name + "</h2></center><br>");
            foreach (var colName in subCollections)
            {
                //make link to this collection
                htmlContent.Add("<center><a href=\"./" + colName + ".html\" style=\"font-size: 30px; \">" + colName + "</a></center><br>");
            }

            CreateFile(htmlContent, "index");
        }
    }
}
