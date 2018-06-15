using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DashShared;

namespace Dash
{
    public class MapOperatorController : OperatorController
    {
        public static KeyController InputKey = new KeyController("D7F1CA4D-820F-419E-979A-6A1538E20A5E", "Input Collection");
        public static KeyController OperatorKey = new KeyController("3A2C13C5-33F4-4845-9854-6CEE0E2D9438", "Operator");

        public static KeyController OutputKey = new KeyController("C7CF634D-B8FA-4E0C-A6C0-2FAAEA6B8114", "Output Collection");

        public MapOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public MapOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("A8A3732F-FADE-4504-BC51-4CCF23165E8A", "Map");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputKey, new IOInfo(TypeInfo.List, true)),
            new KeyValuePair<KeyController, IOInfo>(OperatorKey, new IOInfo(TypeInfo.Operator, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.List
        };

        public override FieldControllerBase GetDefaultController()
        {
            return new MapOperatorController();
        }

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, ScriptState state = null)
        {
            var input = (BaseListController) inputs[InputKey];
            var op = (OperatorController)inputs[OperatorKey];
            if (op.Inputs.Count != 1 || op.Outputs.Count != 1)
            {
                return;
            }
            List<FieldControllerBase> outputList = new List<FieldControllerBase>(input.Data.Count);
            Dictionary<KeyController, FieldControllerBase> inDict = new Dictionary<KeyController, FieldControllerBase>();
            Dictionary<KeyController, FieldControllerBase> outDict = new Dictionary<KeyController, FieldControllerBase>();
            var inKey = op.Inputs.First().Key;
            var outKey = op.Outputs.First().Key;
            foreach (var fieldModelController in input.Data)
            {
                inDict[inKey] = fieldModelController;
                op.
                    Execute(inDict, outDict, args);
                outputList.Add(outDict[outKey]);
            }

            outputs[OutputKey] = new ListController<FieldControllerBase>(outputList);//TODO Can this be more specific?
        }
    }
}
