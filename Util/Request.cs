using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using Newtonsoft.Json.Linq;
using HttpClient = Windows.Web.Http.HttpClient;
using HttpMethod = Windows.Web.Http.HttpMethod;
using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;

namespace Dash
{
    public class Request
    {
        protected HttpMethod RequestType; // POST, GET, etc.
        protected HttpMethod AuthRequestType;
        protected HttpRequestMessage Message;
        protected HttpFormUrlEncodedContent MessageBody;
        protected HttpFormUrlEncodedContent AuthMessageBody;
        protected HttpResponseMessage Response;
        protected HttpRequestMessage TokenMsg;

        protected Uri AuthUri;
        protected Uri ApiUri;

        protected string Secret, Key;

        protected IEnumerable<KeyValuePair<string, string>> Headers, Parameters, AuthHeaders, AuthParameters;

        protected HttpClient Client;

        public Request(HttpMethod method, Uri apiUri)
        {
            Message = new HttpRequestMessage(method, apiUri);
            ApiUri = apiUri;
            RequestType = method;
            Message.Headers.UserAgent.TryParseAdd("Dash");
            
        }

        public Request SetAuthUri(Uri uri)
        {
            AuthUri = uri;
            return this;
        }

        public Request SetAuthMethod(HttpMethod method)
        {
            AuthRequestType = method;
            return this;
        }

        public Request SetHeaders(IEnumerable<KeyValuePair<string, string>> headers)
        {
            foreach (KeyValuePair<string, string> entry in headers)
            {
                
                // add custom header properties to request
                Message.Headers.Add(entry);
            }
            return this;
        }

        public Request SetMessageBody(HttpFormUrlEncodedContent messageBody)
        {
            MessageBody = messageBody;
            
            return this;
        }

        public Request SetAuthHeaders(IEnumerable<KeyValuePair<string, string>> authHeaders)
        {
            AuthHeaders = authHeaders;
            return this;
        }

        public Request SetAuthMessageBody(HttpFormUrlEncodedContent authMessageBody)
        {
            AuthMessageBody = authMessageBody;
            return this;
        }

        public Request SetKey(string key)
        {
            Key = key;
            return this;
        }

        public Request SetSecret(string secret)
        {
            Secret = secret;
            return this;
        }

        public async Task<Request> TrySetResponse()
        {
            // if get, we add parameters to the URI either in URL for GET or in body for POST requests
            if (RequestType == HttpMethod.Get)
            {
                string messageBody = MessageBody.ToString();
                if (!string.IsNullOrWhiteSpace(messageBody))
                    Message.RequestUri = new Uri(ApiUri.OriginalString + "?" + messageBody);
            }
            else
            {
                Message.Content = MessageBody;
            }
            if (AuthUri != null &&
                !string.IsNullOrWhiteSpace(AuthUri.AbsolutePath) &&
                !string.IsNullOrWhiteSpace(Key) &&
                !string.IsNullOrWhiteSpace(Secret))
            {
                TokenMsg = new HttpRequestMessage(AuthRequestType, AuthUri);
                //var byteArray = Encoding.ASCII.GetBytes("my_client_id:my_client_secret");
                //var header = new HttpCredentialsHeaderValue("Basic", Convert.ToBase64String(byteArray)); //TODO is this needed

                // apply auth headers & parameters
                TokenMsg.Headers.Authorization = new HttpCredentialsHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(
                        string.Format("{0}:{1}", Key, Secret))));
                foreach (KeyValuePair<string, string> entry in AuthHeaders)
                {
                    if (!TokenMsg.Headers.UserAgent.TryParseAdd(entry.Key + "=" + entry.Value))
                        return null;
                }
                if (AuthRequestType == HttpMethod.Get)
                {
                    string messageBody = AuthMessageBody.ToString();
                    if (!string.IsNullOrWhiteSpace(messageBody))
                        TokenMsg.RequestUri = new Uri(AuthUri.OriginalString + "?" + messageBody);
                }
                else
                {
                    TokenMsg.Content = AuthMessageBody;
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
                DocumentController documentController = new JsonToDashUtil().ParseJsonString(Response.Content.ToString(),
                    Message.RequestUri.ToString());
                var dcfm = documentController.EnumFields()
                    .FirstOrDefault(keyFieldPair => keyFieldPair.Value is ListController<DocumentController>).Value as
                ListController<DocumentController>;
                if (dcfm != null) return new List<DocumentController>(dcfm.GetElements());
            }
            catch (Exception)
            {
                Debug.Fail("the json util failed");
            }
            return null;
        }

        public DocumentController GetResult()
        {
            return new JsonToDashUtil().ParseJsonString(Response.Content.ToString(),
                Message.RequestUri.ToString());
        }
    }
}
