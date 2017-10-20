﻿using System.Collections.Generic;
using System.Diagnostics;
using DashShared;

namespace Dash
{
    public class AddOperatorFieldModelController : OperatorFieldModelController
    {
        public AddOperatorFieldModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public AddOperatorFieldModelController() : base(new OperatorFieldModel(OperatorType.Add))
        {
        }

        //Input keys
        public static readonly KeyController AKey = new KeyController("942F7A38-3E5D-4CD7-9A88-C61B962511B8", "A");
        public static readonly KeyController BKey = new KeyController("F9B2192D-3DFD-41B8-9A37-56D818153B59", "B");

        //Output keys
        public static readonly KeyController SumKey = new KeyController("7431D567-7582-477B-A372-5964C2D26AE6", "Sum");

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [AKey] = new IOInfo(TypeInfo.Number, true),
            [BKey] = new IOInfo(TypeInfo.Number, true)
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [SumKey] = TypeInfo.Number,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs)
        {
            double sum = 0;
            foreach (var value in inputs.Values)
            {
                if (value is NumberFieldModelController)
                    sum += ((NumberFieldModelController) value).Data;
            }

            outputs[SumKey] = new NumberFieldModelController(sum);
        }

        public override FieldModelController<OperatorFieldModel> Copy()
        {
            return new AddOperatorFieldModelController(OperatorFieldModel);
        }
        public override object GetValue(Context context)
        {
            throw new System.NotImplementedException();
        }
        public override bool SetValue(object value)
        {
            return false;
        }
    }
}