using System;
using System.Collections.Generic;
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
        public static readonly KeyController MethodKey = new KeyController("FBB7AE95-CD1C-4C69-A602-4F2BC2B78A3E", "Method");

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
            [MethodKey] = TypeInfo.Text
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.Document
        };

        private Dictionary<KeyController, ApiParameter> _parameters = new Dictionary<KeyController, ApiParameter>();
        private Dictionary<KeyController, ApiParameter> _headers = new Dictionary<KeyController, ApiParameter>();

        public void AddParameter(ApiParameter parameter)
        {
            int index = _parameters.Count + 1;
            KeyController key = new KeyController(DashShared.Util.GetDeterministicGuid($"Api parameter {index}"), $"Parameter {index}");
            parameter.Key = key;
            _parameters[key] = parameter;
        }
        public void RemoveParameter(ApiParameter parameter)
        {
            _parameters.Remove(parameter.Key);
        }
        public void AddHeader(ApiParameter header)
        {
            int index = _headers.Count + 1;
            KeyController key = new KeyController(DashShared.Util.GetDeterministicGuid($"Api header {index}"), $"Header {index}");
            header.Key = key;
            _headers[key] = header;
        }
        public void RemoveHeader(ApiParameter header)
        {
            _headers.Remove(header.Key);
        }

        private int test = 1;
        public override void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
        {
            var fields = new Dictionary<KeyController, FieldModelController>
            {
                [TestKey] = new TextFieldModelController("Test"),
                [Test2Key] = new NumberFieldModelController(54),
                [Test3Key] = new TextFieldModelController($"{test++}")
            };
            var document = new DocumentController(fields, DocumentType.DefaultType);
            outputs[OutputKey] = new DocumentFieldModelController(document);
            return;
            var url = (inputs[UrlKey] as TextFieldModelController).Data;
            var method = (inputs[MethodKey] as TextFieldModelController).Data;
            var methodEnum = (HttpMethod)Enum.Parse(typeof(HttpMethod), method, true);

            var parameters = new List<KeyValuePair<string, string>>();
            var headers = new List<KeyValuePair<string, string>>();

            var request = new Request(methodEnum, new Uri(url))
                .SetHeaders(headers)
                .SetMessageBody(new HttpFormUrlEncodedContent(parameters)).TrySetResponse();

            if (request != null)
            {
                var doc = request.Result.GetResult();
                doc.SetField(KeyStore.DataKey, new TextFieldModelController("Test"), true);
                outputs[OutputKey] = new DocumentFieldModelController(doc);
            }
        }
    }
}
