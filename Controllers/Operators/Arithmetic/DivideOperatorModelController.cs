using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation.Metadata;
using DashShared;

namespace Dash
{
    public class DivideOperatorFieldModelController : OperatorFieldModelController
    {

        public DivideOperatorFieldModelController() : base(new OperatorFieldModel("Divide"))
        {
        }

        public DivideOperatorFieldModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        //Input keys
        public static readonly KeyController AKey = new KeyController("AAC1631C-9DC3-48FC-984A-EE0D80C9A397", "A");
        public static readonly KeyController BKey = new KeyController("A757D709-8D83-44C9-B047-D5DB6420F51F", "B");

        //Output keys
        public static readonly KeyController QuotientKey = new KeyController("DA705E3D-4773-4C7D-B770-536BA321D0FA", "Quotient");
        public static readonly KeyController RemainderKey = new KeyController("32208EDB-B673-4957-A0AB-3704A15A1686", "Remainder");

        public override ObservableDictionary<KeyController, TypeInfo> Inputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [AKey] = TypeInfo.Number,
            [BKey] = TypeInfo.Number
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [QuotientKey] = TypeInfo.Number,
            [RemainderKey] = TypeInfo.Number
        };

        public override void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
        {
            var numberA = (NumberFieldModelController) inputs[AKey];
            var numberB = (NumberFieldModelController) inputs[BKey];

            var a = numberA.Data;
            var b = numberB.Data;

            outputs[QuotientKey] = new NumberFieldModelController(a / b);
            outputs[RemainderKey] = new NumberFieldModelController(a % b);
        }

        public override FieldModelController Copy()
        {
            return new DivideOperatorFieldModelController();
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