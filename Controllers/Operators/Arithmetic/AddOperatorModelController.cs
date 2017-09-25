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
        public static readonly KeyControllerBase AKey = new KeyControllerBase("942F7A38-3E5D-4CD7-9A88-C61B962511B8", "A");
        public static readonly KeyControllerBase BKey = new KeyControllerBase("F9B2192D-3DFD-41B8-9A37-56D818153B59", "B");

        //Output keys
        public static readonly KeyControllerBase SumKey = new KeyControllerBase("7431D567-7582-477B-A372-5964C2D26AE6", "Sum");

        public override ObservableDictionary<KeyControllerBase, IOInfo> Inputs { get; } = new ObservableDictionary<KeyControllerBase, IOInfo>
        {
            [AKey] = new IOInfo(TypeInfo.Number, true),
            [BKey] = new IOInfo(TypeInfo.Number, true)
        };
        public override ObservableDictionary<KeyControllerBase, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyControllerBase, TypeInfo>
        {
            [SumKey] = TypeInfo.Number,
        };

        public override void Execute(Dictionary<KeyControllerBase, FieldControllerBase> inputs, Dictionary<KeyControllerBase, FieldControllerBase> outputs)
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
            return new AddOperatorModelController(OperatorFieldModel);
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