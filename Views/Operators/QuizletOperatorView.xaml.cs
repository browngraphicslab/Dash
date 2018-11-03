using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using DashShared;
using Newtonsoft.Json;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class QuizletOperatorView : UserControl
    {
        private DocumentController _operatorDoc;

        public QuizletOperatorView()
        {
            this.InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // datacontext is a reference to the operator field
            var refToOp = DataContext as FieldReference;

            // get the document containing the operator
            _operatorDoc = refToOp?.GetDocumentController(null);

            _operatorDoc?.AddFieldUpdatedListener(QuizletOperator.TitleKey, OnTitleFieldUpdated);

            var titleText = _operatorDoc.GetDereferencedField<TextController>(QuizletOperator.TitleKey, null)?.Data;
            if (titleText != null)
            {
                xTitleInput.Text = titleText;
            }

        }

        private void OnTitleFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            var tfmc = args.NewValue.DereferenceToRoot<TextController>(null);
            if (xTitleInput.Text != tfmc.Data)
            {
                xTitleInput.Text = tfmc.Data;
            }
        }

        private void TextBox_OnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            _operatorDoc.SetField(QuizletOperator.TitleKey, new TextController(xTitleInput.Text), true);
        }

        /// <summary>
        ///  fired when the user wants to send data to quizlet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSendTapped(object sender, TappedRoutedEventArgs e)
        {
            // get the input collection
            var collection = _operatorDoc.GetDereferencedField<ListController<DocumentController>>(QuizletOperator.CollectionKey, null);

            // get keys associated with fields we want to send to quizlet
            var termKey = GetKeyFromOp(QuizletOperator.TermKey);
            var imageKey = GetKeyFromOp(QuizletOperator.ImageKey);

            if (collection == null) return;

            var data = new List<(string term, string definition, string image)>();

            // iterate over all the docs
            foreach (var doc in collection.TypedData)
            { 
                var dataDoc = doc?.GetDataDocument();

                if (dataDoc == null) continue;

                var term = string.Empty;
                var definition = string.Empty;
                 var image = string.Empty;
                if (termKey != null)
                {
                    term = dataDoc.GetField<TextController>(termKey)?.Data ?? string.Empty;
                }
                if (imageKey != null)
                {
                    image = dataDoc.GetField<DocumentController>(imageKey)?.GetDataDocument()?.GetField<ImageController>(KeyStore.DataKey)?.Data?.ToString() ?? dataDoc.GetField<ImageController>(imageKey)?.Data?.ToString() ?? string.Empty;
                }

                data.Add((term, definition, image));
            }

            var setTitle = _operatorDoc.GetDereferencedField<TextController>(QuizletOperator.TitleKey, null)?.Data ?? "Quiz Exported From Dash";
            ExportToQuizlet(data, setTitle);
        }

        /// <summary>
        /// Assumes the passed in key has a text controller on the opdoc associated with it, and returns
        /// the key from database that has the same id as the string in the text controller
        /// </summary>
        private KeyController GetKeyFromOp(KeyController keyController)
        {
            var outputKeyId = _operatorDoc.GetDereferencedField<TextController>(keyController, null)?.Data;
            KeyController outputKey = null;
            if (outputKeyId != null)
            {
                outputKey = RESTClient.Instance.Fields.GetController<KeyController>(outputKeyId);
            }
            return outputKey;
        }


        private static readonly HttpClient _client = new HttpClient();

        private Task<HttpResponseMessage> MakeTokenRequest(string code)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "Y1Q0cTJocmpNNDpnd0dwdzJId0FVQTdyWWtDaFRkaGdG"); //Set token header authorization
            var values = new Dictionary<string, string>
            {
                {"grant_type", "authorization_code" },
                {"code", code}
            };

            var content = new FormUrlEncodedContent(values);
            return _client.PostAsync("https://api.quizlet.com/oauth/token", content);
        }

        private async Task<HttpResponseMessage> MakeRemoteImageRequest(List<(string term, string definition, string image)> setData)
        {
            var imageForm = new MultipartFormDataContent();

            for (int i = 0; i < setData.Count; i++)
            {
                var triplet = setData[i];
                if (!String.IsNullOrEmpty(triplet.image))
                {
                    string url = triplet.image;

                    var stream = await _client.GetStreamAsync(url);
                    var extension = ".jpg"; //TODO: get the image extension dynamically
                    //System.IO.FileStream fs = stream as System.IO.FileStream;
                    //var extension = (fs != null) ? System.IO.Path.GetExtension(fs.Name) : "jpg";
                    var img = new StreamContent(stream);

                    imageForm.Add(img, "imageData[]", $"img_{i}{extension}");
                }
            }
            var res = await _client.PostAsync("https://api.quizlet.com/2.0/images", imageForm); //Create the set
            return res;
        }

        private async Task<HttpResponseMessage> MakeLocalImageRequest(List<(string term, string definition, string image)> setData)
        {
            var imageForm = new MultipartFormDataContent();

            for (int i = 0; i < setData.Count; i++)
            {
                var triplet = setData[i];
                if (!String.IsNullOrEmpty(triplet.image))
                {
                    string imgUri = triplet.image;
                    //var imgUri = ViewModel.DocumentController.GetDataDocument(null).GetField<ImageController>(KeyStore.DataKey).Data.ToString();

                    if (imgUri == null) break;

                    //get the part after file:///
                    if (imgUri.StartsWith(@"file:///")) imgUri = imgUri.Substring(8);

                    await Task.Run(() =>
                    {

                        var fs = new System.IO.FileStream(imgUri, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                        var extension = System.IO.Path.GetExtension(fs.Name) ?? ".jpg";
                        var img = new StreamContent(fs);

                        imageForm.Add(img, "imageData[]", $"img_{i}{extension}");
                    });
                }
            }
            var res = await _client.PostAsync("https://api.quizlet.com/2.0/images", imageForm); //Create the set
            return res;
        }

        private Task<HttpResponseMessage> MakeSetRequest(List<(string term, string definition, string image)> setData, string setTitle, string langTerm = "en", string langDef = "en")
        {

            var newSetForm = new MultipartFormDataContent();
            newSetForm.Add(new StringContent(setTitle), "title");

            foreach (var triplet in setData)
            {
                newSetForm.Add(new StringContent(triplet.term), "terms[]");
                newSetForm.Add(new StringContent(triplet.definition), "definitions[]");
                newSetForm.Add(new StringContent(triplet.image), "images[]");
            }

            newSetForm.Add(new StringContent(langTerm), "lang_terms");
            newSetForm.Add(new StringContent(langDef), "lang_definitions");

            return _client.PostAsync("https://api.quizlet.com/2.0/sets", newSetForm); //Create the set
        }

        public async void ExportToQuizlet(List<(string term, string definition, string image)> setData, string setTitle, string langTerm = "en", string langDef = "en")
        {

            if (!setData.Any()) return;

            var token = "GnRF3QpEcnbE5cBNxjBep9ysxvTspXerfcpfSKQw";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); //Set token header authorization

            // Upload images and get the image IDs 
            var imageResponse = await MakeLocalImageRequest(setData);
            var imageJsonString = imageResponse.Content.ReadAsStringAsync().Result;
            if (!imageResponse.IsSuccessStatusCode)
            {
                Debug.WriteLine(imageResponse.StatusCode);
                return;
            }
            var imageJson = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(imageJsonString);
            var imageIds = imageJson.Select(imgDict => imgDict["id"]).ToList();


            //Update the data to refer to image IDs rather than URI
            for (int i = 0, j = 0; i < setData.Count(); i++)
            {
                var oldTriplet = setData[i];
                var newImageId = String.IsNullOrEmpty(oldTriplet.image) || j >= imageIds.Count() ? String.Empty : imageIds[j++];
                setData[i] = (oldTriplet.term, oldTriplet.definition, newImageId);
            }

            // Make the request to create a set using the terms
            var setResponse = await MakeSetRequest(setData, setTitle, langTerm, langDef);
            var newSetJsonString = setResponse.Content.ReadAsStringAsync().Result;

            if (!setResponse.IsSuccessStatusCode)
            {
                Debug.WriteLine(setResponse.StatusCode);
                return;
            }

            dynamic newSetJson = JsonConvert.DeserializeObject(newSetJsonString);
            var newSetUrl = newSetJson["url"];


            BrowserView.OpenTab(newSetUrl.ToString());
        }

        /*
        private void BrowserView_CurrentTabChanged(object sender, BrowserView e)
        {
           if (_authenticationUrl == e.Url) return;

           var redirectUrl = "https://quizlet.com/browngfx";

           if (e.Url.Contains(redirectUrl))
           {
               WwwFormUrlDecoder decoder = new WwwFormUrlDecoder(new Uri(e.Url).Query);
               var state = decoder.GetFirstValueByName("state");
               var code = decoder.GetFirstValueByName("code");

                    
                    // Using the code, make a request for a Quizlet authentication token
                    var tokenResponse = await MakeTokenRequest(code);
                    var tokenJson = tokenResponse.Content.ReadAsStringAsync().Result;
            
                Debug.Assert(_quizletState == state);
               BrowserView.CurrentTabChanged -= BrowserView_CurrentTabChanged;

               ExportQuizlet(code, state);
           }
        }
*/

    }
}
