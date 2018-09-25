using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash.Controllers.Operators.Point
{
    [OperatorType(Op.Name.x)]
    public class XOperator : OperatorController
    {
        public static readonly KeyController PointKey = new KeyController("Point");


        public static readonly KeyController XCoordKey = new KeyController("XCoord");


        public XOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public XOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("XCoordinate", "1ec86c4f-a5d8-418f-ab50-e525cf38d498");

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

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var p = ((PointController)inputs[PointKey]).Data;
            outputs[XCoordKey] = new NumberController(p.X);
        }

    }
}
