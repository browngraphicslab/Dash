using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Web.Http;
using DashShared;

namespace Dash
{
    public struct ApiParameter
    {
        public ApiParameter(bool required, bool displayed)
        {
            Required = required;
            Displayed = displayed;
            Key = null;
        }

        public bool Required { get; set; }
        public bool Displayed { get; set; }

        public KeyController Key { get; set; }
    }

    public class ApiOperatorController : OperatorController
    {
        public static readonly DocumentType ApiType = new DocumentType("478628DA-AB98-4402-B827-F8CB625D4233", "Api");

        public static readonly KeyController UrlKey = new KeyController("Url");
        public static readonly KeyController AuthUrlKey = new KeyController("Auth Url");
        public static readonly KeyController MethodKey = new KeyController("Method");
        public static readonly KeyController AuthMethodKey = new KeyController("Auth Method");

        public static readonly KeyController AuthSecretKey = new KeyController("Auth Secret");
        public static readonly KeyController AuthKeyKey = new KeyController("Auth Key");

        public static readonly KeyController OutputKey = new KeyController("Output Document");

        public static readonly KeyController TestKey = new KeyController("Output Test");
        public static readonly KeyController Test2Key = new KeyController("Output Test1");
        public static readonly KeyController Test3Key = new KeyController("Output Test2");

        public ApiOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public ApiOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Api", "F0A2B96E-65D9-4E1D-9D3A-2660C7C5C316");

        public override FieldControllerBase GetDefaultController()
        {
            return new ApiOperatorController();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(UrlKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(MethodKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(AuthUrlKey, new IOInfo(TypeInfo.Text, false)),
            new KeyValuePair<KeyController, IOInfo>(AuthMethodKey, new IOInfo(TypeInfo.Text, false)),
            new KeyValuePair<KeyController, IOInfo>(AuthKeyKey, new IOInfo(TypeInfo.Text, false)),
            new KeyValuePair<KeyController, IOInfo>(AuthSecretKey, new IOInfo(TypeInfo.Text, false)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.Document
        };

        public override Func<ReferenceController, CourtesyDocument> LayoutFunc { get; } =
            rfmc => new ApiOperatorBox(rfmc);

        public ObservableDictionary<KeyController, ApiParameter> Parameters { get; } = new ObservableDictionary<KeyController, ApiParameter>();
        public ObservableDictionary<KeyController, ApiParameter> Headers { get; } = new ObservableDictionary<KeyController, ApiParameter>();
        public ObservableDictionary<KeyController, ApiParameter> AuthParameters { get; } = new ObservableDictionary<KeyController, ApiParameter>();
        public ObservableDictionary<KeyController, ApiParameter> AuthHeaders { get; } = new ObservableDictionary<KeyController, ApiParameter>();

        public void AddParameter(ApiParameter parameter)
        {
            int index = Parameters.Count + 1;
            KeyController key = new KeyController($"Parameter {index}", DashShared.UtilShared.GetDeterministicGuid($"Api parameter {index}"));
            parameter.Key = key;
            Inputs.Add(new KeyValuePair<KeyController, IOInfo>(key, new IOInfo(TypeInfo.Text, false)));//TODO This might be able to be parameter.Required
            Parameters[key] = parameter;
        }
        public void RemoveParameter(ApiParameter parameter)
        {
            Inputs.Remove(Inputs.First(i => i.Key.Equals(parameter.Key)));
            Parameters.Remove(parameter.Key);
        }

        public void AddHeader(ApiParameter header)
        {
            int index = Headers.Count + 1;
            KeyController key = new KeyController($"Header {index}", DashShared.UtilShared.GetDeterministicGuid($"Api header {index}"));
            header.Key = key;
            Inputs.Add(new KeyValuePair<KeyController, IOInfo>(key, new IOInfo(TypeInfo.Text, false)));//TODO This might be able to be header.Required
            Headers[key] = header;
        }
        public void RemoveHeader(ApiParameter header)
        {
            Inputs.Remove(Inputs.First(i => i.Key.Equals(header.Key)));
            Headers.Remove(header.Key);
        }

        public void AddAuthParameter(ApiParameter parameter)
        {
            int index = AuthParameters.Count + 1;
            KeyController key = new KeyController($"Auth Parameter {index}", DashShared.UtilShared.GetDeterministicGuid($"Api auth parameter {index}"));
            parameter.Key = key;
            Inputs.Add(new KeyValuePair<KeyController, IOInfo>(key, new IOInfo(TypeInfo.Text, false)));//TODO This might be able to be parameter.Required
            AuthParameters[key] = parameter;
        }
        public void RemoveAuthParameter(ApiParameter parameter)
        {
            Inputs.Remove(Inputs.First(i => i.Key.Equals(parameter.Key)));
            AuthParameters.Remove(parameter.Key);
        }

        public void AddAuthHeader(ApiParameter header)
        {
            int index = AuthHeaders.Count + 1;
            KeyController key = new KeyController($"Auth Header {index}", DashShared.UtilShared.GetDeterministicGuid($"Api auth header {index}"));
            header.Key = key;
            Inputs.Add(new KeyValuePair<KeyController, IOInfo>(key, new IOInfo(TypeInfo.Text, false)));//TODO This might be able to be header.Required
            AuthHeaders[key] = header;
        }
        public void RemoveAuthHeader(ApiParameter header)
        {
            Inputs.Remove(Inputs.First(i => i.Key.Equals(header.Key)));
            AuthHeaders.Remove(header.Key);
        }

        public HttpMethod GetMethodFromString(string methodName)
        {
            methodName = methodName.ToLower();
            if (methodName == "get")
            {
                return HttpMethod.Get;
            }
            if (methodName == "post")
            {
                return HttpMethod.Post;
            }
            throw new ArgumentException();
        }

        private bool BuildParamList(Dictionary<KeyController, FieldControllerBase> inputs, IDictionary<KeyController, ApiParameter> parameters,
            List<KeyValuePair<string, string>> outParameters)
        {
            foreach (var parameter in parameters)
            {
                FieldControllerBase param;
                bool hasValue = inputs.TryGetValue(parameter.Key, out param);
                if (!hasValue)
                {
                    if (parameter.Value.Required)
                    {
                        return false;
                    }

                    continue;
                }
                TextController p = (TextController)param;
                var split = p.Data.Split(':');
                var value = String.Join(":", split.Skip(1));
                outParameters.Add(new KeyValuePair<string, string>(split[0], value));
            }
            return true;
        }


        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var url = (inputs[UrlKey] as TextController).Data;
            var method = (inputs[MethodKey] as TextController).Data;

            bool useAuth = false;
            string authUrl = "", authMethod = "", authKey = "", authSecret = "";
            HttpMethod autHttpMethod = HttpMethod.Get;
            if (inputs.ContainsKey(AuthUrlKey) && inputs.ContainsKey(AuthMethodKey) &&
                inputs.ContainsKey(AuthKeyKey) && inputs.ContainsKey(AuthSecretKey))
            {
                authUrl = (inputs[AuthUrlKey] as TextController).Data;
                authMethod = (inputs[AuthMethodKey] as TextController).Data;
                authKey = (inputs[AuthKeyKey] as TextController).Data;
                authSecret = (inputs[AuthSecretKey] as TextController).Data;
                autHttpMethod = GetMethodFromString(authMethod);
                useAuth = true;
            }

            HttpMethod httpMethod = GetMethodFromString(method);

            var parameters = new List<KeyValuePair<string, string>>();
            var headers = new List<KeyValuePair<string, string>>();


            if (!BuildParamList(inputs, Parameters, parameters)) return;
            if (!BuildParamList(inputs, Headers, headers)) return;

            Uri uri; 
            try { uri = new Uri(url); }
            catch (UriFormatException) { return; }

            var request = new Request(httpMethod, uri).SetHeaders(headers)
                .SetMessageBody(new HttpFormUrlEncodedContent(parameters));

            if (useAuth)
            {
                var authHeaders = new List<KeyValuePair<string, string>>();
                var authParams = new List<KeyValuePair<string, string>>();

                if (!BuildParamList(inputs, AuthHeaders, authHeaders)) return;
                if (!BuildParamList(inputs, AuthParameters, authParams)) return;

                request.SetAuthUri(new Uri(authUrl));
                request.SetAuthMethod(autHttpMethod);
                request.SetAuthHeaders(authHeaders);
                request.SetAuthMessageBody(new HttpFormUrlEncodedContent(authParams));
                request.SetKey(authKey);
                request.SetSecret(authSecret);
            }

            var newRequest = Task.Run(() => request.TrySetResponse()).Result;

            var doc = newRequest.GetResult();
            outputs[OutputKey] = doc;
        }
    }
}
