using System;

namespace Dash
{
    [OperatorType(Op.Name.divide, Op.Name.div, Op.Name.operator_divide)]
    public class DivideOperatorController : BinaryOperatorControllerBase<NumberController, NumberController>
    {
        public DivideOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public DivideOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey =
            new KeyController("Divide", new Guid("E1127484-9AC5-45BE-AB55-5923DED25688"));

        public override FieldControllerBase Compute(NumberController left, NumberController right) => new NumberController(left.Data / right.Data);

        public override FieldControllerBase GetDefaultController() => new DivideOperatorController();
    }
}
