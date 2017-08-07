using System.Collections.Generic;
using System.Diagnostics;
using DashShared;

namespace Dash
{
    public class AddOperatorModelController : OperatorFieldModelController
    {
        public AddOperatorModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }
        public AddOperatorModelController() : base(new OperatorFieldModel("Add"))
        {
        }

        //Input keys
        public static readonly KeyController AKey = new KeyController("942F7A38-3E5D-4CD7-9A88-C61B962511B8", "A");
        public static readonly KeyController BKey = new KeyController("F9B2192D-3DFD-41B8-9A37-56D818153B59", "B");

        //Output keys
        public static readonly KeyController SumKey = new KeyController("7431D567-7582-477B-A372-5964C2D26AE6", "Sum");

        public override ObservableDictionary<KeyController, TypeInfo> Inputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [AKey] = TypeInfo.Number,
            [BKey] = TypeInfo.Number
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [SumKey] = TypeInfo.Number,
        };

        public override void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
        {
            double sum = 0;
            foreach (var value in inputs.Values)
            {
                if (value is NumberFieldModelController)
                    sum += ((NumberFieldModelController) value).Data;
            }

            outputs[SumKey] = new NumberFieldModelController(sum);
        }

        public override FieldModelController Copy()
        {
            return new AddOperatorModelController(OperatorFieldModel);
        }
    }
}