﻿using System.Collections.Generic;
using System.Diagnostics;
using DashShared;

namespace Dash
{
    public class SubtractOperatorFieldModelController : OperatorFieldModelController
    {
        public SubtractOperatorFieldModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }
        public SubtractOperatorFieldModelController() : base(new OperatorFieldModel("Subtract"))
        {
        }

        //Input keys
        public static readonly KeyController AKey = new KeyController("AE143AA5-4959-4B17-9FB4-B7BAB045366F", "A");
        public static readonly KeyController BKey = new KeyController("AE898286-9726-4BA9-9323-687609FF94F2", "B");

        //Output keys
        public static readonly KeyController DifferenceKey = new KeyController("851F7905-1363-4077-BF05-149327A55C34", "Difference");

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [AKey] = new IOInfo(TypeInfo.Number, true),
            [BKey] = new IOInfo(TypeInfo.Number, true)
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [DifferenceKey] = TypeInfo.Number,
        };

        public override void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
        {
            var numberA = (NumberFieldModelController)inputs[AKey];
            var numberB = (NumberFieldModelController)inputs[BKey];

            var a = numberA.Data;
            var b = numberB.Data;

            outputs[DifferenceKey] = new NumberFieldModelController(a - b);
        }

        public override FieldModelController Copy()
        {
            return new SubtractOperatorFieldModelController(OperatorFieldModel);
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