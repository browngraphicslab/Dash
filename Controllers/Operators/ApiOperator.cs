﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Dash.Models;
using Dash.StaticClasses;
using DashShared;

namespace Dash
{
    class ApiOperator : OperatorFieldModelController
    {
        public static readonly DocumentType ApiType = new DocumentType("B82CEB25-47C1-4575-83A7-B527F8C0E7FD", "Api");
        public static readonly DocumentType ApiParams = new DocumentType("62BADA87-D54D-42B8-9F4C-8A33B776C6C7", "Filter Params");

        //Input Keys
        public static KeyController BaseUrlKey = new KeyController("C20E4B2B-A633-4C2C-ACBF-757FF6AC8E5A", "Base URL");
        public static KeyController HttpMethodKey = new KeyController("1CE4047D-1813-410B-804E-BA929D8CB4A4", "Http Method");
        public static KeyController HeadersKey = new KeyController("6E9D9F12-E978-4E61-85C7-707A0C13EFA7", "Headers");
        public static KeyController ParametersKey = new KeyController("654A4BDF-1AE0-432A-9C90-CCE9B4809870", "Parameter");

        public static KeyController AuthHttpMethodKey = new KeyController("D37CCAC0-ABBC-4861-BEB4-8C079049DCF8", "Auth Method");
        public static KeyController AuthBaseUrlKey = new KeyController("7F8709B6-2C9B-43D0-A86C-37F3A1517884", "Auth URL");
        public static KeyController AuthKey = new KeyController("1E5B5398-9349-4585-A420-EDBFD92502DE", "Auth Key");
        public static KeyController AuthSecretKey = new KeyController("A690EFD0-FF35-45FF-9795-372D0D12711E", "Auth Secret");
        public static KeyController AuthHeadersKey = new KeyController("E1773B06-F54C-4052-B888-AE85278A7F88", "Auth Header");
        public static KeyController AuthParametersKey = new KeyController("CD546F0B-A0BA-4C3B-B683-5B2A0C31F44E", "Auth Parameter");

        public static KeyController KeyTextKey = new KeyController("7A90DCD7-0A05-479E-A4BE-B06B98599F3D", "Key");
        public static KeyController ValueTextKey = new KeyController("E8976EF0-FB5A-4462-9333-719B8C8F91C0", "Value");
        public static KeyController RequiredKey = new KeyController("D4FCBA25-B540-4E17-A17A-FCDE775B97F9", "Required");
        public static KeyController DisplayKey = new KeyController("2B80D6A8-4224-4EC7-9BDF-DFD2CC20E463", "Display");

        //Output Keys
        public static readonly KeyController OutputCollection = new KeyController("DF1C5189-65D6-47F5-A0CC-7D3658DFB29B", "Output Collection");

        public ApiOperator(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableDictionary<KeyController, TypeInfo> Inputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [BaseUrlKey] = TypeInfo.Text,
            [HttpMethodKey] = TypeInfo.Number,
            [HeadersKey] = TypeInfo.Collection,
            [ParametersKey] = TypeInfo.Collection,
            [AuthHttpMethodKey] = TypeInfo.Number,
            [AuthBaseUrlKey] = TypeInfo.Text,
            [AuthKey] = TypeInfo.Text,
            [AuthSecretKey] = TypeInfo.Collection,
            [AuthParametersKey] = TypeInfo.Collection,
            [KeyTextKey] = TypeInfo.Text,
            [ValueTextKey] = TypeInfo.Text,
            [RequiredKey] = TypeInfo.Text,
            [DocumentCollectionFieldModelController.CollectionKey] = TypeInfo.Collection
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputCollection] = TypeInfo.Collection
        };

        public static void ForceUpdate(DocumentFieldReference docFieldRef)
        {
            var opDoc = ContentController.GetController<DocumentController>(docFieldRef.DocumentId);
            opDoc.Execute(null, true);
        }

        public override void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
        {

            outputs[OutputCollection] = new DocumentCollectionFieldModelController();

            outputs[OutputCollection] =
            inputs[DocumentCollectionFieldModelController.CollectionKey] as
                DocumentCollectionFieldModelController;
        }

        private HttpMethod GetRequestType(NumberFieldModelController controller)
        {
            if (controller.Data == 0)
                return Windows.Web.Http.HttpMethod.Get;

            return Windows.Web.Http.HttpMethod.Post;
        }

        public override FieldModelController Copy()
        {
            return new ApiOperator(OperatorFieldModel);
        }

        private Dictionary<string, string> ConvertDocumentCollectionToStringDictionary(List<DocumentController> docs)
        {
            var ret = new Dictionary<string, string>();
            foreach (var doc in docs)
            {
                if (doc.GetField(ApiDocumentModel.KeyTextKey, true) != null &&
                    doc.GetField(ApiDocumentModel.ValueTextKey, true) != null)
                {
                    ret[(doc.GetField(ApiDocumentModel.KeyTextKey, true) as TextFieldModelController).Data] =
                        (doc.GetField(ApiDocumentModel.ValueTextKey, true) as TextFieldModelController).Data;
                }
            }
            return ret;
        }
    }
}
