using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Controllers.Operators.Point
{
    [OperatorType(Op.Name.x)]
    public class XOperator : OperatorController
    {
        public static readonly KeyController PointKey = KeyController.Get("Point");


        public static readonly KeyController XCoordKey = KeyController.Get("XCoord");


        public XOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public XOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("XCoordinate");

        public override FieldControllerBase GetDefaultController()
        {
            return new XOperator();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(PointKey, new IOInfo(TypeInfo.Point, true)),

        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [XCoordKey] = TypeInfo.Number,

        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var p = ((PointController)inputs[PointKey]).Data;
            outputs[XCoordKey] = new NumberController(p.X);
            return Task.CompletedTask;
        }

    }
}
