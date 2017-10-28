using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.Email;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using DashShared;
using LightBuzz.SMTP;
using Newtonsoft.Json;
using static Dash.NoteDocuments;

namespace Dash
{
    // TODO: name this to something more descriptive
    public static class Util
    {
        public static void InitializeDropShadow(UIElement shadowHost, Shape shadowTarget)
        {
            var hostVisual = ElementCompositionPreview.GetElementVisual(shadowHost);
            var compositor = hostVisual.Compositor;

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
        ///     Transforms point p to relative point in Window.Current.Content
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

            var r = to.TransformToVisual(Window.Current.Content) as MatrixTransform;
            //Debug.Assert(r != null);

            if (r == null) return new Point(0, 0);
            var m = r.Matrix;
            return new MatrixTransform {Matrix = new Matrix(1 / m.M11, 0, 0, 1 / m.M22, 0, 0)}.TransformPoint(p);
        }

        /// <summary>
        ///     Transforms point p in from-space to a point in to-space
        /// </summary>
        public static Point PointTransformFromVisual(Point p, UIElement from, UIElement to = null)
        {
            if (to == null) to = Window.Current.Content;
            return @from.TransformToVisual(to).TransformPoint(p);
        }

        /// <summary>
        ///     Given a position relative to the MainPage, returns the transformed position corresponding
        ///     to the given collection's freeform view.
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
        ///     Create TranslateTransform to translate "totranslate" by "delta" amount relative to canvas space
        /// </summary>
        /// <returns></returns>
        public static TranslateTransform TranslateInCanvasSpace(Point delta, UIElement toTranslate,
            double elemScale = 1.0)
        {
            var p = DeltaTransformFromVisual(delta, toTranslate);
            return new TranslateTransform
            {
                X = p.X * elemScale,
                Y = p.Y * elemScale
            };
        }

        /// <summary>
        ///     Forcefully bind FrameworkElement size to parent document size. Use sparingly and only when XAML is being too
        ///     stubborn
        /// </summary>
        /// <param name="toBind"></param>
        public static void ForceBindHeightToParentDocumentHeight(FrameworkElement toBind)
        {
            var parent = toBind.GetFirstAncestorOfType<DocumentView>();
            if (parent == null) return;
            parent.SizeChanged += (ss, ee) =>
            {
                toBind.Width = parent.ActualWidth;
                toBind.Height = parent.ActualHeight;
            };
        }

        public static void FixListViewBaseManipulationDeltaPropagation(ListViewBase xList)
        {
            var scrollBar = xList.GetFirstDescendantOfType<ScrollBar>();
            scrollBar.ManipulationMode = ManipulationModes.All;
            scrollBar.ManipulationDelta += (ss, ee) => ee.Handled = true;
        }

        public static HashSet<DocumentController> GetIntersection(DocumentCollectionFieldModelController setA,
            DocumentCollectionFieldModelController setB)
        {
            var result = new HashSet<DocumentController>();
            foreach (var contA in setA.GetDocuments())
            foreach (var contB in setB.GetDocuments())
            {
                if (result.Contains(contB)) continue;

                var enumFieldsA = contA.EnumFields().ToList();
                var enumFieldsB = contB.EnumFields().ToList();
                if (enumFieldsA.Count != enumFieldsB.Count) continue;

                var equal = true;
                foreach (var pair in enumFieldsA)
                    if (enumFieldsB.Select(p => p.Key).Contains(pair.Key))
                        if (pair.Value is TextFieldModelController)
                        {
                            var fmContA = pair.Value as TextFieldModelController;
                            var fmContB =
                                enumFieldsB.First(p => p.Key.Equals(pair.Key)).Value as TextFieldModelController;
                            if (!fmContA.Data.Equals(fmContB?.Data))
                            {
                                equal = false;
                                break;
                            }
                        }
                        else if (pair.Value is NumberFieldModelController)
                        {
                            var fmContA = pair.Value as NumberFieldModelController;
                            var fmContB =
                                enumFieldsB.First(p => p.Key.Equals(pair.Key)).Value as NumberFieldModelController;
                            if (!fmContA.Data.Equals(fmContB?.Data))
                            {
                                equal = false;
                                break;
                            }
                        }
                        else if (pair.Value is ImageFieldModelController)
                        {
                            var fmContA = pair.Value as ImageFieldModelController;
                            var fmContB =
                                enumFieldsB.First(p => p.Key.Equals(pair.Key)).Value as ImageFieldModelController;
                            if (!fmContA.ImageFieldModel.Data.AbsoluteUri.Equals(fmContB.ImageFieldModel.Data
                                .AbsoluteUri))
                            {
                                equal = false;
                                break;
                            }
                            throw new NotImplementedException();
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                if (equal) result.Add(contB);
            }
            return result;
        }


        /// <summary>
        ///     Serializes KeyValuePairs mapping Key to FieldModelController to json; extracts the data from FieldModelController
        ///     If there is a nested collection, nests the json recursively
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, object> JsonSerializeHelper(IEnumerable<KeyValuePair<KeyController, FieldControllerBase>> fields)
        {
            Dictionary<string, object> jsonDict = new Dictionary<string, object>();
            foreach (KeyValuePair<KeyController, FieldControllerBase> pair in fields)
            {
                object data = null;
                if (pair.Value is TextFieldModelController)
                {
                    var cont = pair.Value as TextFieldModelController;
                    data = cont.Data;
                }
                else if (pair.Value is NumberFieldModelController)
                {
                    var cont = pair.Value as NumberFieldModelController;
                    data = cont.Data;
                }
                else if (pair.Value is ImageFieldModelController)
                {
                    var cont = pair.Value as ImageFieldModelController;
                    data = cont.ImageFieldModel.Data.AbsoluteUri;
                }
                else if (pair.Value is PointFieldModelController)
                {
                    var cont = pair.Value as PointFieldModelController;
                    data = cont.Data;
                }
                // TODO refactor the CollectionKey here into DashConstants
                else if (pair.Key == KeyStore.CollectionKey)
                {
                    var collectionList = new List<Dictionary<string, object>>();
                    var collectionCont = pair.Value as DocumentCollectionFieldModelController;
                    foreach (var cont in collectionCont.GetDocuments())
                        collectionList.Add(JsonSerializeHelper(cont.EnumFields()));
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
        ///     Exports the document's key to field as json object and saves it locally as .txt
        /// </summary>
        public static async void ExportAsJson(IEnumerable<KeyValuePair<KeyController, FieldControllerBase>> fields)
        {
            var jsonDict = JsonSerializeHelper(fields);
            var json = JsonConvert.SerializeObject(jsonDict);

            var picker = new FolderPicker();
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
            var controllerList = new List<Dictionary<string, object>>();
            foreach (var cont in docContextList)
                controllerList.Add(JsonSerializeHelper(cont.EnumFields()));
            var json = JsonConvert.SerializeObject(controllerList);

            var picker = new FolderPicker();
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
        ///     Saves everything within given UIelement as .png in a specified directory
        /// </summary>
        public static async void ExportAsImage(UIElement element)
        {
            var bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(element);

            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");
            StorageFolder folder = null;
            folder = await picker.PickSingleFolderAsync();

            StorageFile file = null;
            if (folder != null)
            {
                file = await folder.CreateFileAsync("pic.png", CreationCollisionOption.ReplaceExisting);

                var pixels = await bitmap.GetPixelsAsync();
                var byteArray = pixels.ToArray();

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);

                    var displayInformation = DisplayInformation.GetForCurrentView();

                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Ignore,
                        (uint) bitmap.PixelWidth,
                        (uint) bitmap.PixelHeight,
                        displayInformation.RawDpiX,
                        displayInformation.RawDpiY,
                        pixels.ToArray());

                    await encoder.FlushAsync();
                }
            }
        }

        /// <summary>
        ///     Method that launches the Windows mail store app, shows the dialogue with selected attachment file, message body and
        ///     subject
        /// </summary>
        /// //TODO this is weird bc it requires that default app is the mail store app and if you choose anything else then it doesn't work
        public static async void SendEmail()
        {
            // TODO remove these hardcoded things idk if we even need this but maybe we do in the future  
            var recipientAddress = "help@brown.edu";
            var subject = "send help";
            var message = "brown cit 4th floor graphics lab";

            var email = new ContactEmail
            {
                Address = recipientAddress,
                Kind = ContactEmailKind.Personal
            };

            var emailMessage = new EmailMessage
            {
                Body = message,
                Subject = subject
            };

            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add("*");
            var attachmentFile = await picker.PickSingleFileAsync();
            if (attachmentFile != null)
            {
                var stream = RandomAccessStreamReference.CreateFromFile(attachmentFile);
                var attachment = new EmailAttachment(attachmentFile.Name, stream);
                emailMessage.Attachments.Add(attachment);
            }

            var emailRecipient = new EmailRecipient(email.Address);
            emailMessage.To.Add(emailRecipient);


            await EmailManager.ShowComposeNewEmailAsync(emailMessage);
        }

        /// <summary>
        ///     Method that sends email with specified fields directly instead of via an external app
        /// </summary>
        public static async void SendEmail2(string addressTo, string password, string addressFrom, string message,
            string subject, StorageFile attachment)
        {
            using (var client = new SmtpClient("smtp.gmail.com", 465, true, addressFrom, password)) // gmail
            {
                var email = new EmailMessage
                {
                    Subject = subject,
                    Body = message
                };

                email.To.Add(new EmailRecipient(addressTo));
                // TODO add CC? and BCC??  

                if (attachment != null)
                {
                    var stream = RandomAccessStreamReference.CreateFromFile(attachment);
                    email.Attachments.Add(new EmailAttachment(attachment.Name, stream));
                }
                var result = await client.SendMailAsync(email);

                //Debug.WriteLine("SMPT RESULT: " + result.ToString());

                var popupMsg = "D:";
                if (result == SmtpResult.OK)
                    popupMsg = "Sent!";
                else if (result == SmtpResult.AuthenticationFailed)
                    popupMsg =
                        "Failed to authenticate email. Check your password and make sure to enable 'Access for less secure apps' on your gmail settings LOL WAHT A PAIN IN THE ASS I KNOW";
                else
                    popupMsg = "Something went wrong :(";

                var popup = new MessageDialog(popupMsg);
                await popup.ShowAsync();
            }
        }

        /// <summary>
        ///     Fits a line to a collection of (x,y) points.
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
            double ssX;
            double sumCodeviates = 0;
            double sCo;
            double count = exclusiveEnd - inclusiveStart;

            for (var ctr = inclusiveStart; ctr < exclusiveEnd; ctr++)
            {
                var x = xVals[ctr];
                var y = yVals[ctr];
                sumCodeviates += x * y;
                sumOfX += x;
                sumOfY += y;
                sumOfXSq += x * x;
                sumOfYSq += y * y;
            }
            ssX = sumOfXSq - sumOfX * sumOfX / count;
            var rNumerator = count * sumCodeviates - sumOfX * sumOfY;
            var rDenom = (count * sumOfXSq - sumOfX * sumOfX)
                         * (count * sumOfYSq - sumOfY * sumOfY);
            sCo = sumCodeviates - sumOfX * sumOfY / count;

            var meanX = sumOfX / count;
            var meanY = sumOfY / count;
            var dblR = rNumerator / Math.Sqrt(rDenom);
            rsquared = dblR * dblR;
            yintercept = meanY - sCo / ssX * meanX;
            slope = sCo / ssX;
        }


        /// <summary>
        /// Converts a string to a field model controller
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static FieldControllerBase StringToFieldModelController(string expression)
        {
            // check for number field model controller
            var num = IsNumeric(expression);
            if (num.HasValue)
                return new NumberFieldModelController(num.Value);

            string[] imageExtensions = {"jpg", "bmp", "gif", "png"}; //  etc

            if (imageExtensions.Any(expression.EndsWith))
                return new ImageFieldModelController(new Uri(expression));
            return new TextFieldModelController(expression);
        }


        /// <summary>
        ///     Returns the double represenation of the string if possible otherwise null
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static double? IsNumeric(string expression)
        {
            var isNum = double.TryParse(expression, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out double ret);
            if (isNum)
                return ret;
            return null;
        }

        public static DocumentController BlankDoc()
        {
            var docfields = new Dictionary<KeyController, FieldControllerBase>()
            {
                [KeyStore.TitleKey] = new TextFieldModelController("Document")
            };
            var blankDocument = new DocumentController(docfields, DocumentType.DefaultType);
            var layout = new FreeFormDocument(new List<DocumentController>(), new Point(0, 0), new Size(200, 200)).Document;
            blankDocument.SetActiveLayout(layout, true, true);
            return blankDocument;
        }

        public static DocumentController BlankCollection()
        {
            var colfields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.CollectionKey] =
                new DocumentCollectionFieldModelController(),
                [KeyStore.TitleKey] = new TextFieldModelController("Collection")
            };
            var colDoc = new DocumentController(colfields, DocumentType.DefaultType);
            colDoc.SetActiveLayout(
                new CollectionBox(
                    new DocumentReferenceFieldController(colDoc.GetId(),
                        KeyStore.CollectionKey), 0, 0, 200, 200).Document, true, true);
            colDoc.SetField(KeyStore.CollectionOutputKey, new DocumentReferenceFieldController(colDoc.GetId(), KeyStore.CollectionKey), true);
            return colDoc;
        }

        public static DocumentController BlankNote()
        {
            return new NoteDocuments.RichTextNote(NoteDocuments.PostitNote.DocumentType, "Note").Document;
        }

        /// <summary>
        /// Return the union of all the keys, along with their types from a collection
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static Dictionary<KeyController, HashSet<TypeInfo>> GetTypedHeaders(DocumentCollectionFieldModelController collection)
        {
            // create the new list of headers
            var typedHeaders = new Dictionary<KeyController, HashSet<TypeInfo>>();

            // iterate over all the documents in the input collection and get their key's
            // and associated types
            foreach (var docController in collection.Data)
            {
                var actualDoc = GetDataDoc(docController);

                foreach (var field in actualDoc.EnumFields())
                {
                    if (field.Key.Name.StartsWith("_"))
                        continue;

                    if (!typedHeaders.ContainsKey(field.Key))
                        typedHeaders[field.Key] = new HashSet<TypeInfo>();
                    typedHeaders[field.Key].Add(field.Value.TypeInfo);
                }
            }
            return typedHeaders;
        }

        /// <summary>
        /// Helper method to get the data document from a document if it exists
        /// otherwise return the document itself
        /// </summary>
        /// <param name="docController"></param>
        /// <returns></returns>
        public static DocumentController GetDataDoc(DocumentController docController)
        {
            var actualDoc = docController;
            var dataDoc = docController.GetField(KeyStore.DocumentContextKey);
            if (dataDoc != null)
            {
                actualDoc = (dataDoc as DocumentFieldModelController).Data;
            }
            return actualDoc;
        }
    }
}