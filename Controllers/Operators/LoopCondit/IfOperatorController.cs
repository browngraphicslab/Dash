using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
   // [OperatorType("if")]
    class IfOperatorController 
    {
        //public IfOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        //{
        //}

        //public IfOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        //{
        //}

        //public override KeyController OperatorType { get; } = TypeKey;
        //private static readonly KeyController TypeKey = new KeyController("144E6E9D-E430-443E-85A9-29F87CE99DF5", "If");

        ////Input keys
        //public static readonly KeyController BinaryKey = new KeyController("EDCCAA98-F90A-490B-8F07-ECFF8342C3B3", "Binary");
        //public static readonly KeyController BlockKey = new KeyController("5DF879DC-04DF-467D-93C9-38F4A2AB98CF", "Block");

        ////Output keys
        //public static readonly KeyController ResultKey = new KeyController("982DB305-A593-40B2-A75B-2D65C9212FBD", "Result");

        //public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        //{
        //    new KeyValuePair<KeyController, IOInfo>(BinaryKey, new IOInfo(TypeInfo.Number, true)),
        //    new KeyValuePair<KeyController, IOInfo>(BlockKey, new IOInfo(TypeInfo.Number, true))
        //};
        //public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        //{
        //   // [SumKey] = TypeInfo.Number,
        //};

        //public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, ScriptState state = null)
        //{
        //    double sum = 0;
        //    foreach (var value in inputs.Values)
        //    {
        //        if (value is NumberController controller)
        //        {
        //            sum += controller.Data;
        //        }
        //        else if (value is TextController text)
        //        {
        //            double d;
        //            if (double.TryParse(text.Data, out d))
        //            {
        //                sum += d;
        //            }
        //        }
        //    }

        //   // outputs[SumKey] = new NumberController(sum);
        //}

        //public override FieldControllerBase GetDefaultController()
        //{
        //    return new AddOperatorController();
        //}
    }
}
