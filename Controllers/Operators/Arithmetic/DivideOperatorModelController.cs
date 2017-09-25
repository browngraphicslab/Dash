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
        public static readonly KeyControllerBase AKey = new KeyControllerBase("AAC1631C-9DC3-48FC-984A-EE0D80C9A397", "A");
        public static readonly KeyControllerBase BKey = new KeyControllerBase("A757D709-8D83-44C9-B047-D5DB6420F51F", "B");

        //Output keys
        public static readonly KeyControllerBase QuotientKey = new KeyControllerBase("DA705E3D-4773-4C7D-B770-536BA321D0FA", "Quotient");
        public static readonly KeyControllerBase RemainderKey = new KeyControllerBase("32208EDB-B673-4957-A0AB-3704A15A1686", "Remainder");

        public override ObservableDictionary<KeyControllerBase, IOInfo> Inputs { get; } = new ObservableDictionary<KeyControllerBase, IOInfo>
        {
            [AKey] = new IOInfo(TypeInfo.Number, true),
            [BKey] = new IOInfo(TypeInfo.Number, true)
        };
        public override ObservableDictionary<KeyControllerBase, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyControllerBase, TypeInfo>
        {
            [QuotientKey] = TypeInfo.Number,
            [RemainderKey] = TypeInfo.Number
        };

        public static int numExecutions = 0;

        public override void Execute(Dictionary<KeyControllerBase, FieldControllerBase> inputs, Dictionary<KeyControllerBase, FieldControllerBase> outputs)
        {
            var numberA = (NumberFieldModelController)inputs[AKey];
            var numberB = (NumberFieldModelController)inputs[BKey];
            Debug.WriteLine("NumExecutions " + ++numExecutions + " " + numberA);

            var a = numberA.Data;
            var b = numberB.Data;

            outputs[QuotientKey] = new NumberFieldModelController(a / b);
            outputs[RemainderKey] = new NumberFieldModelController(a % b);
        }

        public override FieldModelController<OperatorFieldModel> Copy()
        {
            return new DivideOperatorFieldModelController();
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