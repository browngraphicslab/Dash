using System;

namespace Dash
{
    [OperatorType(Op.Name.divide, Op.Name.div, Op.Name.operator_divide)]
    public class DivideOperatorController : BinaryOperatorControllerBase<NumberController, NumberController>
    {
        public DivideOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public DivideOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey =
            KeyController.Get("Divide");

        public override FieldControllerBase Compute(NumberController left, NumberController right) => new NumberController(left.Data / right.Data);

        public override FieldControllerBase GetDefaultController() => new DivideOperatorController();
    }
}
