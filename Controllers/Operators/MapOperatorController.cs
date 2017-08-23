using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    class MapOperatorController : OperatorFieldModelController
    {
        public static KeyController InputKey = new KeyController("D7F1CA4D-820F-419E-979A-6A1538E20A5E", "Input Collection");
        public static KeyController OperatorKey = new KeyController("3A2C13C5-33F4-4845-9854-6CEE0E2D9438", "Operator");

        public static KeyController OutputKey = new KeyController("C7CF634D-B8FA-4E0C-A6C0-2FAAEA6B8114", "Output Collection");

        public MapOperatorController() : base(new OperatorFieldModel("map")) { }

        public MapOperatorController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableDictionary<KeyController, TypeInfo> Inputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [InputKey] = TypeInfo.List,
            [OperatorKey] = TypeInfo.Operator
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.List
        };

        public override FieldModelController Copy()
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

        public override void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
        {
            var input = (BaseListFieldModelController) inputs[InputKey];
            var op = (OperatorFieldModelController)inputs[OperatorKey];
            if (op.Inputs.Count != 1 || op.Outputs.Count != 1)
            {
                return;
            }
            List<FieldModelController> outputList = new List<FieldModelController>(input.Data.Count);
            Dictionary<KeyController, FieldModelController> inDict = new Dictionary<KeyController, FieldModelController>();
            Dictionary<KeyController, FieldModelController> outDict = new Dictionary<KeyController, FieldModelController>();
            var inKey = op.Inputs.First().Key;
            var outKey = op.Outputs.First().Key;
            foreach (var fieldModelController in input.Data)
            {
                inDict[inKey] = fieldModelController;
                op.Execute(inDict, outDict);
                outputList.Add(outDict[outKey]);
            }

            outputs[OutputKey] = new ListFieldModelController<FieldModelController>(outputList);//TODO Can this be more specific?
        }
    }
}
