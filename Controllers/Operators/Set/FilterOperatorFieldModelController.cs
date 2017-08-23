using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash.Models;
using Dash.StaticClasses;
using DashShared;

namespace Dash
{
    class FilterOperatorFieldModelController : OperatorFieldModelController
    {
        public static readonly DocumentType FilterType = new DocumentType("B82CEB25-47C1-4575-83A7-B527F8C0E7FD", "Filter");
        public static readonly DocumentType FilterParams = new DocumentType("62BADA87-D54D-42B8-9F4C-8A33B776C6C7", "Filter Params");

        //Input Keys
        public static readonly KeyController InputCollection = new KeyController("EB742A82-EA9E-4D23-B841-B927615ADB53", "Input Collection");
        public static readonly KeyController FilterTypeKey = new KeyController("BB52FCB0-65FB-4C7F-89BC-71510CDDFF37", "Filter Type");
        public static readonly KeyController KeyNameKey = new KeyController("0FEB77C7-F92A-46B6-A069-94E283EE1655", "Key Name");
        public static readonly KeyController FilterValueKey = new KeyController("6D1D5CBC-11CF-4E6A-8269-4C047AC4DF99", "Filter Value");

        //Output Keys
        public static readonly KeyController OutputCollection = new KeyController("DF1C5189-65D6-47F5-A0CC-7D3658DFB29B", "Output Collection");

        public FilterOperatorFieldModelController() : base(new OperatorFieldModel("Filter"))
        {
        }

        public override ObservableDictionary<KeyController, TypeInfo> Inputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [InputCollection] = TypeInfo.Collection,
            [FilterTypeKey] = TypeInfo.Text,
            [KeyNameKey] = TypeInfo.Text,
            [FilterValueKey] = TypeInfo.Text
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputCollection] = TypeInfo.Collection
        };

        public override void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
        {
            var docs = (inputs[InputCollection] as DocumentCollectionFieldModelController).GetDocuments();

            FilterModel.FilterType filterType;
            bool success = Enum.TryParse((inputs[FilterTypeKey] as TextFieldModelController)?.Data, out filterType);
            if (!success)
            {
                outputs[OutputCollection] = new DocumentCollectionFieldModelController();
                return;
            }
            var keyName = (inputs[KeyNameKey] as TextFieldModelController)?.Data ?? "";
            var value = (inputs[FilterValueKey] as TextFieldModelController)?.Data ?? "";

            FilterModel model = new FilterModel(filterType, keyName, value);

            var filteredDocs = FilterUtils.Filter(docs, model);
            outputs[OutputCollection] = new DocumentCollectionFieldModelController(filteredDocs);
        }

        public override FieldModelController Copy()
        {
            return new FilterOperatorFieldModelController();
        }

        public override object GetValue()
        {
            throw new System.NotImplementedException();
        }
        public override void SetValue(object value)
        {
            throw new System.NotImplementedException();
        }
    }
}
