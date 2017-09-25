using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class MapOperatorController : OperatorFieldModelController
    {
        public static KeyControllerBase InputKey = new KeyControllerBase("D7F1CA4D-820F-419E-979A-6A1538E20A5E", "Input Collection");
        public static KeyControllerBase OperatorKey = new KeyControllerBase("3A2C13C5-33F4-4845-9854-6CEE0E2D9438", "Operator");

        public static KeyControllerBase OutputKey = new KeyControllerBase("C7CF634D-B8FA-4E0C-A6C0-2FAAEA6B8114", "Output Collection");

        public MapOperatorController() : base(new OperatorFieldModel("map")) { }

        public MapOperatorController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableDictionary<KeyControllerBase, IOInfo> Inputs { get; } = new ObservableDictionary<KeyControllerBase, IOInfo>
        {
            [InputKey] = new IOInfo(TypeInfo.List, true),
            [OperatorKey] = new IOInfo(TypeInfo.Operator, true)
        };

        public override ObservableDictionary<KeyControllerBase, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyControllerBase, TypeInfo>
        {
            [OutputKey] = TypeInfo.List
        };

        public override FieldModelController<OperatorFieldModel> Copy()
        {
            return new MapOperatorController(OperatorFieldModel);
        }
        public override object GetValue(Context context)
        {
            throw new System.NotImplementedException();
        }
        public override bool SetValue(object value)
        {
            return false;
        }

        public override void Execute(Dictionary<KeyControllerBase, FieldControllerBase> inputs, Dictionary<KeyControllerBase, FieldControllerBase> outputs)
        {
            var input = (BaseListFieldModelController) inputs[InputKey];
            var op = (OperatorFieldModelController)inputs[OperatorKey];
            if (op.Inputs.Count != 1 || op.Outputs.Count != 1)
            {
                return;
            }
            List<FieldControllerBase> outputList = new List<FieldControllerBase>(input.Data.Count);
            Dictionary<KeyControllerBase, FieldControllerBase> inDict = new Dictionary<KeyControllerBase, FieldControllerBase>();
            Dictionary<KeyControllerBase, FieldControllerBase> outDict = new Dictionary<KeyControllerBase, FieldControllerBase>();
            var inKey = op.Inputs.First().Key;
            var outKey = op.Outputs.First().Key;
            foreach (var fieldModelController in input.Data)
            {
                inDict[inKey] = fieldModelController;
                op.Execute(inDict, outDict);
                outputList.Add(outDict[outKey]);
            }

            outputs[OutputKey] = new ListFieldModelController<FieldControllerBase>(outputList);//TODO Can this be more specific?
        }
    }
}
