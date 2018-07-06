using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    [OperatorType("add")]
    public class AddOperatorController : OperatorController
    {
        public AddOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public AddOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Add", "5C121004-6C32-4BB7-9CBF-C4A6573376EF");

        //Input keys
        public static readonly KeyController AKey = new KeyController("A");
        public static readonly KeyController BKey = new KeyController("B");

        //Output keys
        public static readonly KeyController SumKey = new KeyController("Sum");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.Number, true))
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [SumKey] = TypeInfo.Number,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, ScriptState state = null)
        {
            double sum = 0;
            foreach (var value in inputs.Values)
            {
                if (value is NumberController controller)
                {
                    sum += controller.Data;
                }else if (value is TextController text)
                {
                    double d;
                    if (double.TryParse(text.Data, out d))
                    {
                        sum += d;
                    }
                }
            }

            outputs[SumKey] = new NumberController(sum);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new AddOperatorController();
        }
    }
}