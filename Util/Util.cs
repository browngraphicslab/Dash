using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using DashShared;
using Newtonsoft.Json;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using LightBuzz.SMTP;


namespace Dash
{
    // TODO: name this to something more descriptive
    public static class Util
    {

        /// <summary>
        /// Transforms point p to relative point in Window.Current.Content 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static Point DeltaTransformFromVisual(Point p, UIElement to)
        {
            //GeneralTransform r = this.TransformToVisual(Window.Current.Content).Inverse;
            //Rect rect = new Rect(new Point(0, 0), new Point(1, 1));
            //Rect newRect = r.TransformBounds(rect);
            //Point p = new Point(rect.Width * e.Delta.Translation.X, rect.Height * e.Delta.Translation.Y);

            MatrixTransform r = to.TransformToVisual(Window.Current.Content) as MatrixTransform;
            //Debug.Assert(r != null);
            if (r != null)
            {
                Matrix m = r.Matrix;
                return new MatrixTransform { Matrix = new Matrix(1 / m.M11, 0, 0, 1 / m.M22, 0, 0) }.TransformPoint(p);
            }
            return new Point(0, 0);
        }

        public static Point PointTransformFromVisual(Point p, UIElement to)
        {
            GeneralTransform r = to.TransformToVisual(Window.Current.Content).Inverse;
            Debug.Assert(r != null);
            return r.TransformPoint(p);
        }

        /// <summary>
        /// Create TranslateTransform to translate "totranslate" by "delta" amount relative to canvas space  
        /// </summary>
        /// <returns></returns>
        public static TranslateTransform TranslateInCanvasSpace(Point delta, UIElement toTranslate, double elemScale = 1.0)
        {
            Point p = DeltaTransformFromVisual(delta, toTranslate);
            return new TranslateTransform
            {
                X = p.X * elemScale,
                Y = p.Y * elemScale
            };
        }

        public static HashSet<DocumentController> GetIntersection(DocumentCollectionFieldModelController setA, DocumentCollectionFieldModelController setB)
        {
            HashSet<DocumentController> result = new HashSet<DocumentController>();
            foreach (DocumentController contA in setA.GetDocuments())
            {
                foreach (DocumentController contB in setB.GetDocuments())
                {
                    if (result.Contains(contB)) continue;

                    var enumFieldsA = contA.EnumFields().ToList();
                    var enumFieldsB = contB.EnumFields().ToList();
                    if (enumFieldsA.Count != enumFieldsB.Count) continue;

                    bool equal = true;
                    foreach (KeyValuePair<Key, FieldModelController> pair in enumFieldsA)
                    {
                        if (enumFieldsB.Select(p => p.Key).Contains(pair.Key))
                        {
                            if (pair.Value is TextFieldModelController)
                            {
                                TextFieldModelController fmContA = pair.Value as TextFieldModelController;
                                TextFieldModelController fmContB = enumFieldsB.First(p => p.Key.Equals(pair.Key)).Value as TextFieldModelController;
                                if (!fmContA.Data.Equals(fmContB?.Data))
                                {
                                    equal = false;
                                    break;
                                }
                            }
                            else if (pair.Value is NumberFieldModelController)
                            {
                                NumberFieldModelController fmContA = pair.Value as NumberFieldModelController;
                                NumberFieldModelController fmContB = enumFieldsB.First(p => p.Key.Equals(pair.Key)).Value as NumberFieldModelController;
                                if (!fmContA.Data.Equals(fmContB?.Data))
                                {
                                    equal = false;
                                    break;
                                }
                            }
                            else if (pair.Value is ImageFieldModelController)
                            {
                                ImageFieldModelController fmContA = pair.Value as ImageFieldModelController;
                                ImageFieldModelController fmContB = enumFieldsB.First(p => p.Key.Equals(pair.Key)).Value as ImageFieldModelController;
                                if (!fmContA.Data.UriSource.AbsoluteUri.Equals(fmContB?.Data.UriSource.AbsoluteUri))
                                {
                                    equal = false;
                                    break;
                                }
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                        }
                    }
                    if (equal) result.Add(contB);
                }
            }
            return result;
        }


        /// <summary>
        /// Serializes KeyValuePairs mapping Key to FieldModelController to json; extracts the data from FieldModelController 
        /// If there is a nested collection, nests the json recursively 
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, object> JsonSerializeHelper(IEnumerable<KeyValuePair<Key, FieldModelController>> fields)
        {
            Dictionary<string, object> jsonDict = new Dictionary<string, object>();
            foreach (KeyValuePair<Key, FieldModelController> pair in fields)
            {
                object data = null;
                if (pair.Value is TextFieldModelController)
                {
                    TextFieldModelController cont = pair.Value as TextFieldModelController;
                    data = cont.Data;
                }
                else if (pair.Value is NumberFieldModelController)
                {
                    NumberFieldModelController cont = pair.Value as NumberFieldModelController;
                    data = cont.Data;
                }
                else if (pair.Value is ImageFieldModelController)
                {
                    ImageFieldModelController cont = pair.Value as ImageFieldModelController;
                    data = cont.Data.UriSource.AbsoluteUri;
                }
                else if (pair.Value is PointFieldModelController)
                {
                    PointFieldModelController cont = pair.Value as PointFieldModelController;
                    data = cont.Data;
                }
                // TODO refactor the CollectionKey here into DashConstants
                else if (pair.Key == DocumentCollectionFieldModelController.CollectionKey)
                {
                    var collectionList = new List<Dictionary<string, object>>();
                    DocumentCollectionFieldModelController collectionCont = pair.Value as DocumentCollectionFieldModelController;
                    foreach (DocumentController cont in collectionCont.GetDocuments())
                    {
                        collectionList.Add(JsonSerializeHelper(cont.EnumFields()));
                    }
                    jsonDict[pair.Key.Name] = collectionList;
                    continue;
                }
                else
                {
                    // TODO throw this at some point 
                    data = "";
                }
                jsonDict[pair.Key.Name] = data;
            }
            return jsonDict;
        }

        /// <summary>
        /// Exports the document's key to field as json object and saves it locally as .txt 
        /// </summary>
        public static async void ExportAsJson(IEnumerable<KeyValuePair<Key, FieldModelController>> fields)
        {
            Dictionary<string, object> jsonDict = JsonSerializeHelper(fields);
            string json = JsonConvert.SerializeObject(jsonDict);

            FolderPicker picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");
            StorageFolder folder = null;
            folder = await picker.PickSingleFolderAsync();

            StorageFile file = null;
            if (folder != null)
            {
                file = await folder.CreateFileAsync("sample.json", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, json);
            }
        }

        public static async void ExportAsJson(List<DocumentController> docContextList)
        {
            List<Dictionary<string, object>> controllerList = new List<Dictionary<string, object>>();
            foreach (DocumentController cont in docContextList)
            {
                controllerList.Add(JsonSerializeHelper(cont.EnumFields()));
            }
            string json = JsonConvert.SerializeObject(controllerList);

            FolderPicker picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");
            StorageFolder folder = null;
            folder = await picker.PickSingleFolderAsync();

            StorageFile file = null;
            if (folder != null)
            {
                file = await folder.CreateFileAsync("sample.json", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, json);
            }
        }

        /// <summary>
        /// Saves everything within given UIelement as .png in a specified directory 
        /// </summary>
        public static async void ExportAsImage(UIElement element)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(element);

            FolderPicker picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");
            StorageFolder folder = null;
            folder = await picker.PickSingleFolderAsync();

            StorageFile file = null;
            if (folder != null)
            {
                file = await folder.CreateFileAsync("pic.png", CreationCollisionOption.ReplaceExisting);

                var pixels = await bitmap.GetPixelsAsync();
                byte[] byteArray = pixels.ToArray();

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);

                    var displayInformation = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();

                    encoder.SetPixelData(
                            BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Ignore,
                            (uint)bitmap.PixelWidth,
                            (uint)bitmap.PixelHeight,
                            displayInformation.RawDpiX,
                            displayInformation.RawDpiY,
                            pixels.ToArray());

                    await encoder.FlushAsync();
                }
            }
        }

        /// <summary>
        /// Method that launches the Windows mail store app, shows the dialogue with selected attachment file, message body and subject 
        /// </summary>
        ///             //TODO this is weird bc it requires that default app is the mail store app and if you choose anything else then it doesn't work 
        public static async void SendEmail()
        {
            // TODO remove these hardcoded things idk if we even need this but maybe we do in the future  
            string recipientAddress = "help@brown.edu";
            string subject = "send help";
            string message = "brown cit 4th floor graphics lab";

            var email = new Windows.ApplicationModel.Contacts.ContactEmail
            {
                Address = recipientAddress,
                Kind = Windows.ApplicationModel.Contacts.ContactEmailKind.Personal
            };

            var emailMessage = new Windows.ApplicationModel.Email.EmailMessage
            {
                Body = message,
                Subject = subject
            };

            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add("*");
            StorageFile attachmentFile = await picker.PickSingleFileAsync();
            if (attachmentFile != null)
            {
                var stream = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromFile(attachmentFile);
                var attachment = new Windows.ApplicationModel.Email.EmailAttachment(attachmentFile.Name, stream);
                emailMessage.Attachments.Add(attachment);
            }

            var emailRecipient = new Windows.ApplicationModel.Email.EmailRecipient(email.Address);
            emailMessage.To.Add(emailRecipient);


            await Windows.ApplicationModel.Email.EmailManager.ShowComposeNewEmailAsync(emailMessage);
        }

        /// <summary>
        /// Method that sends email with specified fields directly instead of via an external app 
        /// </summary>
        public static async void SendEmail2(string addressTo, string password, string addressFrom, string message, string subject, StorageFile attachment)
        {
            using (SmtpClient client = new SmtpClient("smtp.gmail.com", 465, true, addressFrom, password)) // gmail
            {
                var email = new Windows.ApplicationModel.Email.EmailMessage
                {
                    Subject = subject,
                    Body = message
                };

                email.To.Add(new Windows.ApplicationModel.Email.EmailRecipient(addressTo));
                // TODO add CC? and BCC??  

                if (attachment != null)
                {
                    var stream = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromFile(attachment);
                    email.Attachments.Add(new Windows.ApplicationModel.Email.EmailAttachment(attachment.Name, stream));
                }
                SmtpResult result = await client.SendMailAsync(email);

                //Debug.WriteLine("SMPT RESULT: " + result.ToString());

                string popupMsg = "D:";
                if (result == SmtpResult.OK)
                {
                    popupMsg = "Sent!";
                }
                else if (result == SmtpResult.AuthenticationFailed)
                {
                    popupMsg = "Failed to authenticate email. Check your password and make sure to enable 'Access for less secure apps' on your gmail settings LOL WAHT A PAIN IN THE ASS I KNOW";
                }
                else
                {
                    popupMsg = "Something went wrong :(";
                }

                var popup = new Windows.UI.Popups.MessageDialog(popupMsg);
                await popup.ShowAsync();
            }
        }

        /// <summary>
        /// Method that returns a list of different types of FieldModelControllers
        ///                                                                 TODO what if people want to display url as textfieldmodel??? 
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<FieldModelController> RawToFieldModelControllerFactory(IEnumerable<object> rawValues, bool displayAsImage = false)
        {
            var result = new LinkedList<FieldModelController>();

            foreach (object value in rawValues)
            {
                string stringVal = value.ToString();

                double n;
                Uri outUri;

                if (double.TryParse(stringVal, out n))                             // if it's a number
                {
                    result.AddLast(new NumberFieldModelController(n));
                }
                else if (displayAsImage && Uri.TryCreate(stringVal, UriKind.Absolute, out outUri)         // if it's a url...  
                       && Uri.IsWellFormedUriString(stringVal, UriKind.Absolute))
                {
                    result.AddLast(new ImageFieldModelController(outUri));
                }
                else
                {
                    result.AddLast(new TextFieldModelController(stringVal));
                }
            }
            return result;
        }

        public static IList<DocumentController> FMControllerToCourtesyDocs(ref DocumentController doc, IEnumerable<FieldModelController> fms)
        {
            var result = new List<DocumentController>();
            string docID = doc.GetId();

            foreach (FieldModelController fm in fms)
            {
                Key key = new Key();                                                                                  // TODO should give it a unique key? 
                doc.SetField(key, fm, true);
                if (fm is TextFieldModelController || fm is NumberFieldModelController)
                {
                    result.Add(new TextingBox(new ReferenceFieldModelController(docID, key)).Document);
                }
                else if (fm is ImageFieldModelController)
                {
                    result.Add(new ImageBox(new ReferenceFieldModelController(docID, key)).Document);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return result;
        }

        // TODO move this somewhere else 
        public static DocumentController MakeListView(List<object> randomList)
        {
            IEnumerable<FieldModelController> fms = RawToFieldModelControllerFactory(randomList, true);

            DocumentType ListType = new DocumentType("testingattentionpls", "List");                         // TODO give it proper document type w/ actual guid 
            var fields = new Dictionary<Key, FieldModelController>();
            fields.Add(DashConstants.KeyStore.DataKey, new ListFieldModelController<FieldModelController>(fms));
            DocumentController Document = new DocumentController(fields, ListType);

            IList<DocumentController> viewDocs = FMControllerToCourtesyDocs(ref Document, fms);
            var layoutDoc = new ListViewLayout(viewDocs, new Point(), new Size(300, Math.Min(50 * viewDocs.Count, 400)));
            Document.SetActiveLayout(layoutDoc.Document, true, true);

            return Document; 
        }
    }
}
