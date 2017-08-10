using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using HttpClient = Windows.Web.Http.HttpClient;
using HttpMethod = Windows.Web.Http.HttpMethod;
using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;

namespace Dash
{
    public class Request
    {
        protected HttpMethod RequestType; // POST, GET, etc.
        protected HttpRequestMessage Message;
        protected HttpFormUrlEncodedContent MessageBody;
        protected HttpResponseMessage Response;
        protected HttpRequestMessage TokenMsg;

        protected Uri AuthUri;
        protected Uri ApiUri;

        protected string Secret, Key;

        protected Dictionary<string, string> Headers, Parameters, AuthHeaders, AuthParameters;

        protected HttpClient Client;

        public Request(HttpMethod method, Uri apiUri)
        {
            Message = new HttpRequestMessage(method, apiUri);
            ApiUri = apiUri;
            RequestType = method;
        }

        public Request SetAuthUri(Uri uri)
        {
            AuthUri = uri;
            return this;
        }

        public Request SetHeaders(IEnumerable<KeyValuePair<string, string>> headers)
        {
            foreach (KeyValuePair<string, string> entry in headers)
            {
                
                // add custom header properties to request
                if (!Message.Headers.UserAgent.TryParseAdd(entry.Key + "=" + entry.Value))
                    return null;
                    // TODO: have some error happen here
                    //TODO check for spaces in key or value text?
            }
            return this;
        }

        public Request SetMessageBody(HttpFormUrlEncodedContent messageBody)
        {
            MessageBody = messageBody;
            
            return this;
        }

        public Request SetAuthHeaders(Dictionary<string, string> authHeaders)
        {
            AuthHeaders = authHeaders;
            return this;
        }

        public async Task<Request> TrySetResponse()
        {
            // if get, we add parameters to the URI either in URL for GET or in body for POST requests
            if (RequestType == HttpMethod.Get)
            {
                if (!string.IsNullOrWhiteSpace(MessageBody.ToString()))
                    Message.RequestUri = new Uri(ApiUri.OriginalString + "?" + MessageBody.ToString());
            }
            else
            {
                Message.Content = MessageBody;
            }
            if (
                !(string.IsNullOrWhiteSpace(ApiUri.AbsolutePath) || string.IsNullOrWhiteSpace(Key) ||
                  string.IsNullOrWhiteSpace(Secret)))
            {
                TokenMsg = new HttpRequestMessage(HttpMethod.Post, AuthUri);
                //var byteArray = Encoding.ASCII.GetBytes("my_client_id:my_client_secret");
                //var header = new HttpCredentialsHeaderValue("Basic", Convert.ToBase64String(byteArray)); //TODO is this needed

                // apply auth headers & parameters
                TokenMsg.Content = MessageBody; //TODO is this right??
                TokenMsg.Headers.Authorization = new HttpCredentialsHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(
                        string.Format("{0}:{1}", Key, Secret))));
                foreach (KeyValuePair<string, string> entry in AuthHeaders)
                {
                    if (!TokenMsg.Headers.UserAgent.TryParseAdd(entry.Key + "=" + entry.Value))
                        return null;
                }
                // fetch token
                Response = await RequestUtil.Client.SendRequestAsync(TokenMsg);

                // parse resulting bearer token
                JObject resultObject = JObject.Parse(Response.Content.ToString());
                var token = (string)resultObject.GetValue("access_token");
                Message.Headers.Authorization = new HttpCredentialsHeaderValue("Bearer", token);
            }

            Response = await RequestUtil.Client.SendRequestAsync(Message);
            return this;
        }

        public List<DocumentController> GetResponseAsDocuments()
        {
            try
            {
                DocumentController documentController = JsonToDashUtil.Parse(Response.Content.ToString(),
                    Message.RequestUri.ToString());
                var dcfm = documentController.EnumFields()
                    .FirstOrDefault(keyFieldPair => keyFieldPair.Value is DocumentCollectionFieldModelController).Value as
                DocumentCollectionFieldModelController;
                if (dcfm != null) return new List<DocumentController>(dcfm.GetDocuments());
            }
            catch (Exception)
            {
                Debug.Fail("the json util failed");
            }
            return null;
        }

        public DocumentController GetResult()
        {
            return JsonToDashUtil.Parse(Response.Content.ToString(),
                Message.RequestUri.ToString());
        }
    }
}
