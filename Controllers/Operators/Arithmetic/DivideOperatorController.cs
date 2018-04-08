using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using DashShared;

namespace Dash
{
    [OperatorType("div")]
    public class DivideOperatorController : OperatorController
    {

        public DivideOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public DivideOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        //Input keys
        public static readonly KeyController AKey = new KeyController("AAC1631C-9DC3-48FC-984A-EE0D80C9A397", "A");
        public static readonly KeyController BKey = new KeyController("A757D709-8D83-44C9-B047-D5DB6420F51F", "B");

        //Output keys
        public static readonly KeyController QuotientKey =
            new KeyController("DA705E3D-4773-4C7D-B770-536BA321D0FA", "Quotient");

        public static readonly KeyController RemainderKey =
            new KeyController("32208EDB-B673-4957-A0AB-3704A15A1686", "Remainder");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.Number, true))
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [QuotientKey] = TypeInfo.Number,
            [RemainderKey] = TypeInfo.Number
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("7169B1E9-957A-49DC-91F6-16364A1AB576", "Divide");

        public static int numExecutions = 0;

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            var numberA = (NumberController) inputs[AKey];
            var numberB = (NumberController) inputs[BKey];
            //Debug.WriteLine("NumExecutions " + ++numExecutions + " " + numberA);

            var a = numberA.Data;
            var b = numberB.Data;

            outputs[QuotientKey] = new NumberController(a / b);
            outputs[RemainderKey] = new NumberController(a % b);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new DivideOperatorController();
        }
    }
}