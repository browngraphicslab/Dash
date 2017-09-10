using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Shapes;
using LightBuzz.SMTP;


namespace Dash
{
    // TODO: name this to something more descriptive
    public static class Util
    {

        public static void InitializeDropShadow(UIElement shadowHost, Shape shadowTarget)
        {
            Visual hostVisual = ElementCompositionPreview.GetElementVisual(shadowHost);
            Compositor compositor = hostVisual.Compositor;

            // Create a drop shadow
            var dropShadow = compositor.CreateDropShadow();

            dropShadow.Color = Color.FromArgb(90, 0, 0, 0);
            dropShadow.BlurRadius = 15.0f;
            dropShadow.Offset = new Vector3(0f, 0f, 0f);
            // Associate the shape of the shadow with the shape of the target element
            dropShadow.Mask = shadowTarget.GetAlphaMask();

            // Create a Visual to hold the shadow
            var shadowVisual = compositor.CreateSpriteVisual();
            shadowVisual.Shadow = dropShadow;

            // Add the shadow as a child of the host in the visual tree
            ElementCompositionPreview.SetElementChildVisual(shadowHost, shadowVisual);

            // Make sure size of shadow host and shadow visual always stay in sync
            var bindSizeAnimation = compositor.CreateExpressionAnimation("hostVisual.Size");
            bindSizeAnimation.SetReferenceParameter("hostVisual", hostVisual);

            shadowVisual.StartAnimation("Size", bindSizeAnimation);
        }

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

            if (r == null) return new Point(0, 0);
            var m = r.Matrix;
            return new MatrixTransform { Matrix = new Matrix(1 / m.M11, 0, 0, 1 / m.M22, 0, 0) }.TransformPoint(p);
        }

        /// <summary>
        /// Transforms point p in from-space to a point in to-space 
        /// </summary>
        public static Point PointTransformFromVisual(Point p, UIElement from, UIElement to = null)
        {
            if (to == null) to = Window.Current.Content;
            return from.TransformToVisual(to).TransformPoint(p);
        }

        /// <summary>
        /// Given a position relative to the MainPage, returns the transformed position corresponding 
        /// to the given collection's freeform view.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="absolutePosition"></param>
        /// <returns></returns>
        public static Point GetCollectionFreeFormPoint(CollectionFreeformView freeForm, Point absolutePosition)
        {
            //Debug.Assert(freeForm != null);
            if (freeForm != null)
            {
                var r = MainPage.Instance.xCanvas.TransformToVisual(freeForm.xItemsControl.ItemsPanelRoot);
                Debug.Assert(r != null);
                return r.TransformPoint(absolutePosition);
            }
            return absolutePosition;
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
                    foreach (KeyValuePair<KeyController, FieldModelController> pair in enumFieldsA)
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
                                if (!fmContA.ImageFieldModel.Data.AbsoluteUri.Equals(fmContB.ImageFieldModel.Data.AbsoluteUri))
                                {
                                    equal = false;
                                    break;
                                }
                                else throw new NotImplementedException();
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
        public static Dictionary<string, object> JsonSerializeHelper(IEnumerable<KeyValuePair<KeyController, FieldModelController>> fields)
        {
            Dictionary<string, object> jsonDict = new Dictionary<string, object>();
            foreach (KeyValuePair<KeyController, FieldModelController> pair in fields)
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
                    data = cont.ImageFieldModel.Data.AbsoluteUri;
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
        public static async void ExportAsJson(IEnumerable<KeyValuePair<KeyController, FieldModelController>> fields)
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
        /// Fits a line to a collection of (x,y) points.
        /// </summary>
        /// <param name="xVals">The x-axis values.</param>
        /// <param name="yVals">The y-axis values.</param>
        /// <param name="inclusiveStart">The inclusive inclusiveStart index.</param>
        /// <param name="exclusiveEnd">The exclusive exclusiveEnd index.</param>
        /// <param name="rsquared">The r^2 value of the line.</param>
        /// <param name="yintercept">The y-intercept value of the line (i.e. y = ax + b, yintercept is b).</param>
        /// <param name="slope">The slop of the line (i.e. y = ax + b, slope is a).</param>
        public static void LinearRegression(double[] xVals, double[] yVals,
            int inclusiveStart, int exclusiveEnd,
            out double rsquared, out double yintercept,
            out double slope)
        {
            Debug.Assert(xVals.Length == yVals.Length);
            double sumOfX = 0;
            double sumOfY = 0;
            double sumOfXSq = 0;
            double sumOfYSq = 0;
            double ssX = 0;
            double ssY = 0;
            double sumCodeviates = 0;
            double sCo = 0;
            double count = exclusiveEnd - inclusiveStart;

            for (int ctr = inclusiveStart; ctr < exclusiveEnd; ctr++)
            {
                double x = xVals[ctr];
                double y = yVals[ctr];
                sumCodeviates += x * y;
                sumOfX += x;
                sumOfY += y;
                sumOfXSq += x * x;
                sumOfYSq += y * y;
            }
            ssX = sumOfXSq - ((sumOfX * sumOfX) / count);
            ssY = sumOfYSq - ((sumOfY * sumOfY) / count);
            double RNumerator = (count * sumCodeviates) - (sumOfX * sumOfY);
            double RDenom = (count * sumOfXSq - (sumOfX * sumOfX))
                            * (count * sumOfYSq - (sumOfY * sumOfY));
            sCo = sumCodeviates - ((sumOfX * sumOfY) / count);

            double meanX = sumOfX / count;
            double meanY = sumOfY / count;
            double dblR = RNumerator / Math.Sqrt(RDenom);
            rsquared = dblR * dblR;
            yintercept = meanY - ((sCo / ssX) * meanX);
            slope = sCo / ssX;
        }
    }
}
