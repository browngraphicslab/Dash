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
    class FilterOperator : OperatorFieldModelController
    {
        public static readonly DocumentType FilterType = new DocumentType("B82CEB25-47C1-4575-83A7-B527F8C0E7FD", "Filter");
        public static readonly DocumentType FilterParams = new DocumentType("62BADA87-D54D-42B8-9F4C-8A33B776C6C7", "Filter Params");

        //Input Keys
        public static readonly Key InputCollection = new Key("EB742A82-EA9E-4D23-B841-B927615ADB53", "Input Collection");
        public static readonly Key FilterTypeKey = new Key("BB52FCB0-65FB-4C7F-89BC-71510CDDFF37", "Filter Type");
        public static readonly Key KeyNameKey = new Key("0FEB77C7-F92A-46B6-A069-94E283EE1655", "Key Name");
        public static readonly Key FilterValueKey = new Key("6D1D5CBC-11CF-4E6A-8269-4C047AC4DF99", "Filter Value");

        //Output Keys
        public static readonly Key OutputCollection = new Key("DF1C5189-65D6-47F5-A0CC-7D3658DFB29B", "Output Collection");

        public FilterOperator(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableDictionary<Key, TypeInfo> Inputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
            [InputCollection] = TypeInfo.Collection,
            [FilterTypeKey] = TypeInfo.Text,
            [KeyNameKey] = TypeInfo.Text,
            [FilterValueKey] = TypeInfo.Text
        };

        public override ObservableDictionary<Key, TypeInfo> Outputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
            [OutputCollection] = TypeInfo.Collection
        };

        public override void Execute(Dictionary<Key, FieldModelController> inputs, Dictionary<Key, FieldModelController> outputs)
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
            return new FilterOperator(OperatorFieldModel);
        }

    }
}
