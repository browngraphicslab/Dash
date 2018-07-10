using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    [OperatorType("concat")]
    public class ConcatOperatorController : OperatorController
    {
        public static readonly KeyController AKey = new KeyController("A");
        public static readonly KeyController BKey = new KeyController("B");

        public static readonly KeyController OutputKey = new KeyController("Output");

        public ConcatOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public ConcatOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Concat", "F69DF9CF-5B51-482D-AE1E-40B3266930CB");

        public override FieldControllerBase GetDefaultController()
        {
            return new ConcatOperatorController();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.Text, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.Text
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, ScriptState state = null)
        {
            var a = (inputs[AKey] as TextController).Data;
            var b = (inputs[BKey] as TextController).Data;
            outputs[OutputKey] = new TextController(a + b);
        }
    }
}
