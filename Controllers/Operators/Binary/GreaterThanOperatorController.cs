using System;

namespace Dash
{
    [OperatorType(Op.Name.greater_than, Op.Name.operator_greater_than)]
    public class GreaterThanOperatorController : BinaryOperatorControllerBase<NumberController, NumberController>
    {
        public GreaterThanOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public GreaterThanOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Greater Than", new Guid("3F198F30-652F-4151-AA6F-D0C648813CDF"));

        public override FieldControllerBase Compute(NumberController left, NumberController right) => new BoolController(left.Data > right.Data);

        public override FieldControllerBase GetDefaultController() => new GreaterThanOperatorController();
    }
}
