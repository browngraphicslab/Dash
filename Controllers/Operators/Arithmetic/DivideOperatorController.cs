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
        public static readonly KeyController AKey = new KeyController("A");
        public static readonly KeyController BKey = new KeyController("B");

        //Output keys
        public static readonly KeyController QuotientKey = new KeyController("Quotient");
        public static readonly KeyController RemainderKey = new KeyController("Remainder");

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
        private static readonly KeyController TypeKey = new KeyController("Divide", "7169B1E9-957A-49DC-91F6-16364A1AB576");

        public static int numExecutions = 0;

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, ScriptState state = null)
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