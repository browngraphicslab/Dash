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

        public static readonly KeyController UrlKey = new KeyController("662E0839-51A7-4FBA-8BF8-BAE5FE92F701", "Url");
        public static readonly KeyController AuthUrlKey = new KeyController("60159AFB-ADAE-414B-A47B-F9F3272C8681", "Auth Url");
        public static readonly KeyController MethodKey = new KeyController("FBB7AE95-CD1C-4C69-A602-4F2BC2B78A3E", "Method");
        public static readonly KeyController AuthMethodKey = new KeyController("2AA724ED-C282-46AC-A844-053F42A6748F", "Auth Method");

        public static readonly KeyController AuthSecretKey = new KeyController("1CBC001E-6536-4B3C-B870-4682DFEB4158", "Auth Secret");
        public static readonly KeyController AuthKeyKey = new KeyController("564F0A13-4DDD-4446-B8D9-21AA206B62BF", "Auth Key");

        public static readonly KeyController OutputKey = new KeyController("EF1C2E17-3AD2-4780-8219-F4EAC683979D", "Output Document");

        public static readonly KeyController TestKey = new KeyController("51BD8321-C685-43E2-837A-F287421BF7D3", "Output Test");
        public static readonly KeyController Test2Key = new KeyController("A2A60489-5E39-4E12-B886-EFA7A79870D9", "Output Test1");
        public static readonly KeyController Test3Key = new KeyController("FCFEB979-7842-41FA-89FB-3CFC67358B8F", "Output Test2");

        public ApiOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public ApiOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("F0A2B96E-65D9-4E1D-9D3A-2660C7C5C316", "Api");

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
            KeyController key = new KeyController(DashShared.UtilShared.GetDeterministicGuid($"Api parameter {index}"), $"Parameter {index}");
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
            KeyController key = new KeyController(DashShared.UtilShared.GetDeterministicGuid($"Api header {index}"), $"Header {index}");
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
            KeyController key = new KeyController(DashShared.UtilShared.GetDeterministicGuid($"Api auth parameter {index}"), $"Auth Parameter {index}");
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
            KeyController key = new KeyController(DashShared.UtilShared.GetDeterministicGuid($"Api auth header {index}"), $"Auth Header {index}");
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

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
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
