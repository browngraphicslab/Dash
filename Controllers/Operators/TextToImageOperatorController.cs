using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    [OperatorType("textToImage")]
    public class TextToImageOperatorController : OperatorController
    {
        public TextToImageOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public TextToImageOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("F42451A5-6831-4E4B-9889-DE2EE5316BA0", "Add");

        //Input keys
        public static readonly KeyController AKey = new KeyController("942F7A38-3E5D-4CD7-9A88-C61B962511B8", "A");

        //Output keys
        public static readonly KeyController SumKey = new KeyController("7431D567-7582-477B-A372-5964C2D26AE6", "Sum");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.Number, true))
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [SumKey] = TypeInfo.Number,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            double sum = 0;
            foreach (var value in inputs.Values)
            {
                var controller = value as NumberController;
                if (controller != null)
                    sum += controller.Data;
            }

            outputs[SumKey] = new NumberController(sum);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new AddOperatorController();
        }
    }
}