using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Controllers.Operators.Point
{
    [OperatorType(Op.Name.y)]
    public class YOperator : OperatorController
    {
        public static readonly KeyController PointKey = new KeyController("Point");


        public static readonly KeyController YCoordKey = new KeyController("YCoord");


        public YOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public YOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("YCoordinate", "098a1b23-20a1-4623-8460-2d848280b1b2");

        public override FieldControllerBase GetDefaultController()
        {
            return new YOperator();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(PointKey, new IOInfo(TypeInfo.Point, true)),

        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [YCoordKey] = TypeInfo.Number,

        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var p = ((PointController)inputs[PointKey]).Data;
            outputs[YCoordKey] = new NumberController(p.Y);
            return Task.CompletedTask;
        }

    }
}
