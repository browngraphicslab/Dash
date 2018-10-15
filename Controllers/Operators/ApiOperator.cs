using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Web.Http;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.api)]
    public class ApiOperator : OperatorController
    {
        public static readonly KeyController UrlKey = new KeyController("Url");
        public static readonly KeyController ParametersKey = new KeyController("Parameters");
        public static readonly KeyController MethodKey = new KeyController("Method");
        public static readonly KeyController AuthKey = new KeyController("Authentication");


        public static readonly KeyController ResultKey = new KeyController("Result");


        public ApiOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public ApiOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("API", new Guid("8e8eb4d9-92d7-462d-8039-e4337d696d6a"));

        public override FieldControllerBase GetDefaultController()
        {
            return new ApiOperator();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(UrlKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(ParametersKey, new IOInfo(TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(AuthKey, new IOInfo(TypeInfo.Document, false)),
            new KeyValuePair<KeyController, IOInfo>(MethodKey, new IOInfo(TypeInfo.Text, false))

        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Any,

        };

        public override async Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var url = (TextController)inputs[UrlKey];
            var parameters = (DocumentController)inputs[ParametersKey];
            var method = inputs[MethodKey] as TextController;
            var authentication = inputs[AuthKey] as DocumentController;
            var result = await Execute(url, parameters, method, authentication);
            outputs[ResultKey] = result;
        }

        public async Task<FieldControllerBase> Execute(TextController url, DocumentController parameters, TextController method = null, DocumentController authentication = null)
        {
            var httpMethod = HttpMethod.Get;
            if (method != null && method.Data.ToLower() == "post")
            {
                httpMethod = HttpMethod.Post;
            }

            var request = new Request(httpMethod, new Uri(url.Data));

            var headers = new Dictionary<string, string>();

            if (authentication != null)
            {
                var type = authentication.GetField<TextController>(new KeyController("auth_type"))?.Data;
                if (type != null)
                {
                    switch (type.ToLower())
                    {
                    case "basic":
                        var user = authentication.GetField<TextController>(new KeyController("user"))?.Data;
                        var password = authentication.GetField<TextController>(new KeyController("password"))?.Data;
                        if (user != null && password != null)
                        {
                            var s = $"{user}:{password}";
                            headers["Authorization"] =
                                $"Basic {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s))}";
                        }
                        break;
                    }
                }
            }

            var param = parameters.EnumDisplayableFields().Select(kvp => new KeyValuePair<string, string>(kvp.Key.Name, kvp.Value.ToString()));
            var content = new HttpFormUrlEncodedContent(param);
            request.SetMessageBody(content);
            request.SetHeaders(headers);
            var response = await request.TrySetResponse();
            //var request = new Request(httpMethod, uri).SetHeaders(headers)
            //    .SetMessageBody(new HttpFormUrlEncodedContent(parameters));

            //if (useAuth)
            //{
            //    var authHeaders = new List<KeyValuePair<string, string>>();
            //    var authParams = new List<KeyValuePair<string, string>>();

            //    if (!BuildParamList(inputs, AuthHeaders, authHeaders)) return;
            //    if (!BuildParamList(inputs, AuthParameters, authParams)) return;

            //    request.SetAuthUri(new Uri(authUrl));
            //    request.SetAuthMethod(autHttpMethod);
            //    request.SetAuthHeaders(authHeaders);
            //    request.SetAuthMessageBody(new HttpFormUrlEncodedContent(authParams));
            //    request.SetKey(authKey);
            //    request.SetSecret(authSecret);
            //}

            return response.GetResult();
        }

    }
}
