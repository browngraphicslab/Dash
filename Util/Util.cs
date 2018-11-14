using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
using Windows.System;

namespace Dash
{
    // TODO: name this to something more descriptive
    //If you get an error that Util is already defined, you made a class somewhere that is in the Util namespace.
    //Change that namespace to just be Dash and it will be fixed
    public static class Util
    {
        public static void InitializeDropShadow(UIElement shadowHost, Shape shadowTarget)
        {
            var hostVisual = ElementCompositionPreview.GetElementVisual(shadowHost);
            var compositor = hostVisual.Compositor;

            // Create a drop shadow
            var dropShadow = compositor.CreateDropShadow();

            dropShadow.Color = Color.FromArgb(150, 0, 0, 0);
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
            return new MatrixTransform { Matrix = new Matrix(1 / m.M11, 0, 0, 1 / m.M22, 0, 0) }.TransformPoint(p);
        }

        /// <summary>
        ///     Transforms point p in from-space to a point in to-space
        /// </summary>
        public static Point PointTransformFromVisual(Point p, UIElement from, UIElement to = null)
        {
            if (to == null)
                to = Window.Current.Content;
            if (from == null)
                from = Window.Current.Content;
            return @from.TransformToVisual(to).TransformPoint(p);
        }

        /// <summary>
        /// Transforms rect in from-spcae to a rect in to-space
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static Rect RectTransformFromVisual(Rect rect, UIElement from, UIElement to = null)
        {
            if (to == null) to = Window.Current.Content;
            var topLeftPoint = @from.TransformToVisual(to).TransformPoint(new Point(rect.Left, rect.Top));
            var bottomRightPoint = @from.TransformToVisual(to).TransformPoint(new Point(rect.Right, rect.Bottom));
            return new Rect(topLeftPoint, bottomRightPoint);
        }

        /// <summary>
        ///     Given a position relative to the MainPage, returns the transformed position corresponding
        ///     to the given collection's freeform view.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="freeform"></param>
        /// <param name="absolutePosition"></param>
        /// <returns></returns>
        public static Point GetCollectionFreeFormPoint(CollectionFreeformBase freeform, Point absolutePosition)
        {
            if (freeform == null) return absolutePosition;

            GeneralTransform r = MainPage.Instance.xOuterGrid.TransformToVisual(freeform.GetItemsControl().ItemsPanelRoot);
            Debug.Assert(r != null);
            return r.TransformPoint(absolutePosition);
        }

        /// <summary>
        ///     Create TranslateTransform to translate "totranslate" by "delta" amount relative to canvas space
        /// </summary>
        /// <returns></returns>
        public static TranslateTransform TranslateInCanvasSpace(Point delta, UIElement toTranslate)
        {
            var p = DeltaTransformFromVisual(delta, toTranslate);
            return new TranslateTransform
            {
                X = p.X,
                Y = p.Y
            };
        }

        public static void FixListViewBaseManipulationDeltaPropagation(ListViewBase xList)
        {
            var scrollBar = xList.GetFirstDescendantOfType<ScrollBar>();
            scrollBar.ManipulationMode = ManipulationModes.All;
            scrollBar.ManipulationDelta += (ss, ee) => ee.Handled = true;
        }

        public static HashSet<DocumentController> GetIntersection(ListController<DocumentController> setA,
            ListController<DocumentController> setB)
        {
            var result = new HashSet<DocumentController>();
            foreach (var contA in setA)
            {
                foreach (var contB in setB)
                {
                    if (result.Contains(contB)) continue;

                    var enumFieldsA = contA.EnumFields().ToList();
                    var enumFieldsB = contB.EnumFields().ToList();
                    if (enumFieldsA.Count != enumFieldsB.Count) continue;

                    var equal = true;
                    foreach (var pair in enumFieldsA)
                    {
                        if (enumFieldsB.Select(p => p.Key).Contains(pair.Key))
                        {
                            if (pair.Value is TextController)
                            {
                                var fmContA = pair.Value as TextController;
                                var fmContB =
                                enumFieldsB.First(p => p.Key.Equals(pair.Key)).Value as TextController;
                                if (!fmContA.Data.Equals(fmContB?.Data))
                                {
                                    equal = false;
                                    break;
                                }
                            }
                            else if (pair.Value is NumberController)
                            {
                                var fmContA = pair.Value as NumberController;
                                var fmContB =
                                enumFieldsB.First(p => p.Key.Equals(pair.Key)).Value as NumberController;
                                if (!fmContA.Data.Equals(fmContB?.Data))
                                {
                                    equal = false;
                                    break;
                                }
                            }
                            else if (pair.Value is ImageController)
                            {
                                var fmContA = pair.Value as ImageController;
                                var fmContB =
                                enumFieldsB.First(p => p.Key.Equals(pair.Key)).Value as ImageController;
                                if (!fmContA.ImageFieldModel.Data.AbsoluteUri.Equals(fmContB.ImageFieldModel.Data
                                    .AbsoluteUri))
                                {
                                    equal = false;
                                    break;
                                }
                                throw new NotImplementedException();
                            }
                            else if (pair.Value is PointController)
                            {
                                var fmContA = pair.Value as PointController;
                                var fmContB =
                                enumFieldsB.First(p => p.Key.Equals(pair.Key)).Value as PointController;
                                if (!fmContA.Data.Equals(fmContB.Data))
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

        public static async Task<StorageFile> SaveSoftwareBitmapToFile(SoftwareBitmap softwareBitmap, StorageFile outputFile)
        {
            using (var stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                // Create an encoder with the desired format
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                // Set the software bitmap
                encoder.SetSoftwareBitmap(softwareBitmap);

                // Set additional encoding parameters, if needed
                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                encoder.IsThumbnailGenerated = true;

                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception err)
                {
                    const int WINCODEC_ERR_UNSUPPORTEDOPERATION = unchecked((int)0x88982F81);
                    switch (err.HResult)
                    {
                    case WINCODEC_ERR_UNSUPPORTEDOPERATION:
                        // If the encoder does not support writing a thumbnail, then try again
                        // but disable thumbnail generation.
                        encoder.IsThumbnailGenerated = false;
                        break;
                    default:
                        throw;
                    }
                }

                if (encoder.IsThumbnailGenerated == false)
                {
                    await encoder.FlushAsync();
                }
            }
            return outputFile;
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
                if (pair.Value is TextController)
                {
                    var cont = pair.Value as TextController;
                    data = cont.Data;
                }
                else if (pair.Value is NumberController)
                {
                    var cont = pair.Value as NumberController;
                    data = cont.Data;
                }
                else if (pair.Value is ImageController)
                {
                    var cont = pair.Value as ImageController;
                    data = cont.ImageFieldModel.Data.AbsoluteUri;
                }
                else if (pair.Value is PointController)
                {
                    var cont = pair.Value as PointController;
                    data = cont.Data;
                }
                // TODO refactor the CollectionKey here into DashConstants
                else if (pair.Value is ListController<DocumentController>)
                {
                    var collectionList = new List<Dictionary<string, object>>();
                    var collectionCont = pair.Value as ListController<DocumentController>;
                    foreach (var cont in collectionCont)
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
        public static async Task<string> ExportAsImage(UIElement element, string imgName = "pic.png", bool saveLocal = false)
        {
            var bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(element);

            StorageFolder folder = null;
            if (saveLocal)
            {
                folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            }
            else
            {
                var picker = new FolderPicker();
                picker.SuggestedStartLocation = PickerLocationId.Desktop;
                picker.FileTypeFilter.Add("*");
                folder = await picker.PickSingleFolderAsync();
            }

            StorageFile file = null;
            if (folder != null)
            {
                file = await folder.CreateFileAsync(imgName, CreationCollisionOption.GenerateUniqueName);

                var pixels = await bitmap.GetPixelsAsync();
                var byteArray = pixels.ToArray();

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);

                    var displayInformation = DisplayInformation.GetForCurrentView();

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

                if (saveLocal)
                {
                    return file.Name;
                }
            }

            return null;
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

        public static DocumentController AdornmentWithPosition(BackgroundShape.AdornmentShape shape, Point pos, double width = 200, double height = 200)
        {
            return new BackgroundNote(shape, pos, new Size(width, height)).Document;
        }

        public static DocumentController AdornmentWithPosandColor(Color color, BackgroundShape.AdornmentShape shape, Point pos,
            double width = 200, double height = 200)
        {
            var note = new BackgroundNote(shape, pos, new Size(width, height));
            note.SetAdornmentColor(color);
            return note.Document;
        }

        // TODO remove this method or match it up with the methods in Actions.cs
        public static DocumentController AdornmentDoc()
        {
            return AdornmentWithPosition(BackgroundShape.AdornmentShape.Elliptical, new Point(0, 0));
        }

        // TODO remove this method or match it up with the methods in Actions.cs
        public static DocumentController BlankCollection()
        {
            return new CollectionNote(new Point(), CollectionViewType.Freeform).Document;
        }

        // TODO remove this method or match it up with the methods in Actions.cs
        public static DocumentController BlankNote()
        {
            return new RichTextNote().Document;
        }

        /// <summary>
        /// Return the union of all the keys, along with their types from a collection
        /// </summary>
        /// <param name="a"> First point </param>
        /// <param name="b"> Seconds point </param>
        /// <returns> True if the points have equal x, y and false otherwise</returns>
        public static bool PointEquals(Point a, Point b)
        {
            var ax = double.IsNaN(a.X);
            var ay = double.IsNaN(a.Y);
            var bx = double.IsNaN(b.X);
            var by = double.IsNaN(b.Y);
            if (!ax && !ay) return a == b;
            if (ax == ay) return bx && by;
            return ax ? bx : by;
        }

        /// <summary>
        /// Return the union of all the keys, along with their types from a collection
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static Dictionary<KeyController, HashSet<TypeInfo>> GetDisplayableTypedHeaders(IEnumerable<DocumentController> collection)
        {
            // create the new list of headers
            var typedHeaders = new Dictionary<KeyController, HashSet<TypeInfo>>();

            // iterate over all the documents in the input collection and get their key's
            // and associated types
            foreach (var docController in collection)
            {
                var actualDoc = docController.GetDataDocument();

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
        /// Given 2 points which represent vectors, returns a point that represents the projection of the argument point onto the first.
        /// </summary>
        /// <param name="a"> The vector to project onto (as point) </param>
        /// <param name="b"> The vector to be projected (as point) </param>
        /// <returns></returns>
        public static Point PointProjectArg(this Point a, Point b)
        {
            var dotProduct = a.X * b.X + a.Y * b.Y;
            var aMagSq = Math.Pow(a.X, 2) + Math.Pow(a.Y, 2);
            return new Point(a.X * dotProduct / aMagSq, a.Y * dotProduct / aMagSq);
        }
        /// <summary>
        ///  clamps a point to lie within a rectangle
        /// </summary>
        /// <param name="point"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static Point Clamp(Point point, Rect rect)
        {
            if (point.X < rect.Left)
            {
                point.X = rect.Left;
            }
            else if (point.X > rect.Right)
            {
                point.X = rect.Right;
            }

            if (point.Y < rect.Top)
            {
                point.Y = rect.Top;
            }
            else if (point.Y > rect.Bottom)
            {
                point.Y = rect.Bottom;
            }


            return point;
        }



        public static string KeyCodeToUnicode(VirtualKey key, bool shiftState, bool capState)
        {
            var virtualKeyCode = (uint)key;

            string character = null;

            // take care of symbols
            if (key == VirtualKey.Space)
            {
                character = " ";
            }
            if (key == VirtualKey.Multiply)
            {
                character = "*";
            }
            // TODO take care of more symbols

            //Take care of letters
            if (virtualKeyCode >= 65 && virtualKeyCode <= 90)
            {
                if ((!shiftState && !capState) || (shiftState && capState))
                {
                    character = key.ToString().ToLower();
                }
                else
                {
                    character = key.ToString();
                }
            }

            //Take care of numbers
            if (virtualKeyCode >= 48 && virtualKeyCode <= 57)
            {
                character = (virtualKeyCode - 48).ToString();
                if ((shiftState != false || capState != false) &&
                    (!shiftState || !capState))
                {
                    switch ((virtualKeyCode - 48))
                    {
                    case 1: character = "!"; break;
                    case 2: character = "@"; break;
                    case 3: character = "#"; break;
                    case 4: character = "$"; break;
                    case 5: character = "%"; break;
                    case 6: character = "^"; break;
                    case 7: character = "&"; break;
                    case 8: character = "*"; break;
                    case 9: character = "("; break;
                    case 0: character = ")"; break;
                    default: break;
                    }
                }
            }

            if (virtualKeyCode >= 186 && virtualKeyCode <= 222)
            {
                var shifted = ((shiftState != false || capState != false) &&
                    (!shiftState || !capState));
                switch (virtualKeyCode)
                {
                case 186: character = shifted ? ":" : ";"; break;
                case 187: character = shifted ? "=" : "+"; break;
                case 188: character = shifted ? "<" : ","; break;
                case 189: character = shifted ? "_" : "-"; break;
                case 190: character = shifted ? ">" : "."; break;
                case 191: character = shifted ? "?" : "/"; break;
                case 192: character = shifted ? "~" : "`"; break;
                case 219: character = shifted ? "{" : "["; break;
                case 220: character = shifted ? "|" : "\\"; break;
                case 221: character = shifted ? "}" : "]"; break;
                case 222: character = shifted ? "\"" : "'"; break;
                }

            }
            //Take care of numpad numbers
            if (virtualKeyCode >= 96 && virtualKeyCode <= 105)
            {
                character = (virtualKeyCode - 96).ToString();
            }

            return character;
        }
    }
}
