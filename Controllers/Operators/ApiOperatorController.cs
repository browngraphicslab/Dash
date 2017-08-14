using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

    public class ApiOperatorController : OperatorFieldModelController
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

        public ApiOperatorController() : base(new OperatorFieldModel("api"))
        {
        }

        public ApiOperatorController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        private ApiOperatorController(ApiOperatorController copy) : this()
        {
        }

        public override FieldModelController Copy()
        {
            return new ApiOperatorController(this);
        }

        public override ObservableDictionary<KeyController, TypeInfo> Inputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [UrlKey] = TypeInfo.Text,
            [MethodKey] = TypeInfo.Text,
            [AuthUrlKey] = TypeInfo.Text,
            [AuthMethodKey] = TypeInfo.Text
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.Document
        };

        public ObservableDictionary<KeyController, ApiParameter> Parameters { get; } = new ObservableDictionary<KeyController, ApiParameter>();
        public ObservableDictionary<KeyController, ApiParameter> Headers { get; } = new ObservableDictionary<KeyController, ApiParameter>();
        public ObservableDictionary<KeyController, ApiParameter> AuthParameters { get; } = new ObservableDictionary<KeyController, ApiParameter>();
        public ObservableDictionary<KeyController, ApiParameter> AuthHeaders { get; } = new ObservableDictionary<KeyController, ApiParameter>();

        public void AddParameter(ApiParameter parameter)
        {
            int index = Parameters.Count + 1;
            KeyController key = new KeyController(DashShared.Util.GetDeterministicGuid($"Api parameter {index}"), $"Parameter {index}");
            parameter.Key = key;
            Inputs.Add(key, TypeInfo.Text);
            Parameters[key] = parameter;
        }
        public void RemoveParameter(ApiParameter parameter)
        {
            Inputs.Remove(parameter.Key);
            Parameters.Remove(parameter.Key);
        }

        public void AddHeader(ApiParameter header)
        {
            int index = Headers.Count + 1;
            KeyController key = new KeyController(DashShared.Util.GetDeterministicGuid($"Api header {index}"), $"Header {index}");
            header.Key = key;
            Inputs.Add(key, TypeInfo.Text);
            Headers[key] = header;
        }
        public void RemoveHeader(ApiParameter header)
        {
            Inputs.Remove(header.Key);
            Headers.Remove(header.Key);
        }

        public void AddAuthParameter(ApiParameter parameter)
        {
            int index = AuthParameters.Count + 1;
            KeyController key = new KeyController(DashShared.Util.GetDeterministicGuid($"Api auth parameter {index}"), $"Auth Parameter {index}");
            parameter.Key = key;
            Inputs.Add(key, TypeInfo.Text);
            AuthParameters[key] = parameter;
        }
        public void RemoveAuthParameter(ApiParameter parameter)
        {
            Inputs.Remove(parameter.Key);
            AuthParameters.Remove(parameter.Key);
        }

        public void AddAuthHeader(ApiParameter header)
        {
            int index = AuthHeaders.Count + 1;
            KeyController key = new KeyController(DashShared.Util.GetDeterministicGuid($"Api auth header {index}"), $"Auth Header {index}");
            header.Key = key;
            Inputs.Add(key, TypeInfo.Text);
            AuthHeaders[key] = header;
        }
        public void RemoveAuthHeader(ApiParameter header)
        {
            Inputs.Remove(header.Key);
            AuthHeaders.Remove(header.Key);
        }

        private int test = 1;
        public override void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
        {
            var url = (inputs[UrlKey] as TextFieldModelController).Data;
            var method = (inputs[MethodKey] as TextFieldModelController).Data.ToLower();
            HttpMethod httpMethod;
            if (method == "get")
            {
                httpMethod = HttpMethod.Get;
            } else if (method == "post")
            {
                httpMethod = HttpMethod.Post;
            }
            else
            {
                throw new ArgumentException();
            }

            var parameters = new List<KeyValuePair<string, string>>();
            var headers = new List<KeyValuePair<string, string>>();

            foreach (var parameter in Parameters)
            {
                FieldModelController param;
                bool hasValue = inputs.TryGetValue(parameter.Key, out param);
                if (!hasValue)
                {
                    if (parameter.Value.Required)
                    {
                        return;
                    }
                    else
                    {
                        continue;
                    }
                }
                TextFieldModelController p = (TextFieldModelController)param;
                var split = p.Data.Split(':');
                var value = String.Join(":", split.Skip(1));
                parameters.Add(new KeyValuePair<string, string>(split[0], value));
            }

            foreach (var header in Headers)
            {
                FieldModelController head;
                bool hasValue = inputs.TryGetValue(header.Key, out head);
                if (!hasValue)
                {
                    if (header.Value.Required)
                    {
                        return;
                    }
                    else
                    {
                        continue;
                    }
                }
                TextFieldModelController p = (TextFieldModelController)head;
                var split = p.Data.Split(':');
                if (split.Length != 2)
                {
                    continue;
                }
                headers.Add(new KeyValuePair<string, string>(split[0], split[1]));
            }

            var requestTask = new Request(httpMethod, new Uri(url))
                .SetHeaders(headers)
                .SetMessageBody(new HttpFormUrlEncodedContent(parameters))
                .SetAuthUri(null)
                .SetAuthHeaders(new Dictionary<string, string>()).TrySetResponse();

            var request = requestTask.Result;
            var doc = request.GetResult();
            outputs[OutputKey] = new DocumentFieldModelController(doc);
        }
    }
}
