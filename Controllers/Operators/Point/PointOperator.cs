using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.point)]
    public sealed class PointOperator : OperatorController
    {

        public static readonly KeyController XKey = KeyController.Get("X");
        public static readonly KeyController YKey = KeyController.Get("Y");

        public static readonly KeyController PointKey = KeyController.Get("Point");

        public PointOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public PointOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new PointOperator();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(XKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(YKey, new IOInfo(TypeInfo.Number, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [PointKey] = TypeInfo.Point
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("PointType");

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var x = ((NumberController) inputs[XKey]).Data;
            var y = ((NumberController) inputs[YKey]).Data;
            outputs[PointKey] = new PointController(x, y);
            return Task.CompletedTask;
        }
    }
}
