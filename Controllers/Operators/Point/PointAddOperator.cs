using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Controllers.Operators.Point
{
    [OperatorType(Op.Name.operator_add)]
    public class PointAddOperator : OperatorController
    {
        public static readonly KeyController AKey = KeyController.Get("A");
        public static readonly KeyController BKey = KeyController.Get("B");


        public static readonly KeyController OutputKey = KeyController.Get("Output");


        public PointAddOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public PointAddOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey = KeyController.Get("PointAdd");

        public override FieldControllerBase GetDefaultController()
        {
            return new PointAddOperator();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.Point, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.Point, true)),

        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.Point,

        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var a = ((PointController)inputs[AKey]).Data;
            var b = ((PointController)inputs[BKey]).Data;
            outputs[OutputKey] = new PointController(a.X + b.X, a.Y + b.Y);
            return Task.CompletedTask;
        }

    }
}
