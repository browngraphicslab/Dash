using System;

namespace Dash
{
    [OperatorType(Op.Name.less_than_equals, Op.Name.operator_less_than_equals)]
    public class LessThanEqualsOperatorController : BinaryOperatorControllerBase<NumberController, NumberController>
    {
        public LessThanEqualsOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public LessThanEqualsOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Less Than Equals");

        public override FieldControllerBase Compute(NumberController left, NumberController right) => new BoolController(left.Data <= right.Data);

        public override FieldControllerBase GetDefaultController() => new LessThanEqualsOperatorController();
    }
}
