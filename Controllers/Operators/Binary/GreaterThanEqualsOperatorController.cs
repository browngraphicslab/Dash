using System;

namespace Dash
{
    [OperatorType(Op.Name.greater_than_equals, Op.Name.operator_greater_than_equals)]
    public class GreaterThanEqualsOperatorController : BinaryOperatorControllerBase<NumberController, NumberController>
    {
        public GreaterThanEqualsOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public GreaterThanEqualsOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Greater Than Equals");

        public override FieldControllerBase Compute(NumberController left, NumberController right) => new BoolController(left.Data >= right.Data);

        public override FieldControllerBase GetDefaultController() => new GreaterThanEqualsOperatorController();
    }
}
