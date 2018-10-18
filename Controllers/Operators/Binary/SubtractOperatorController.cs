using System;

namespace Dash
{
    [OperatorType(Op.Name.minus, Op.Name.subtract, Op.Name.operator_subtract)]
    public class SubtractOperatorController : BinaryOperatorControllerBase<NumberController, NumberController>
    {
        public SubtractOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public SubtractOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Subtract");

        public override FieldControllerBase Compute(NumberController left, NumberController right) => new NumberController(left.Data - right.Data);

        public override FieldControllerBase GetDefaultController() => new SubtractOperatorController();
    }
}
