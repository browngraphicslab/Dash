using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using DashShared;

namespace Dash
{
    [OperatorType("mult")]
    public class MultiplyOperatorController : OperatorController
    {

        public MultiplyOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public MultiplyOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Multiply", "518988DD-4C30-4AE6-AF7F-3532B7A71C7B");

        //Input keys
        public static readonly KeyController AKey = new KeyController("A");
        public static readonly KeyController BKey = new KeyController("B");

        //Output keys
        public static readonly KeyController ProductKey = new KeyController("Product");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.Number, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ProductKey] = TypeInfo.Number,
        };

        public static int numExecutions = 0;

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, ScriptState state = null)
        {
            var numberA = (NumberController)inputs[AKey];
            var numberB = (NumberController)inputs[BKey];
            //Debug.WriteLine("NumExecutions " + ++numExecutions + " " + numberA);

            var a = numberA.Data;
            var b = numberB.Data;

            outputs[ProductKey] = new NumberController(a * b);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new MultiplyOperatorController();
        }
    }
}