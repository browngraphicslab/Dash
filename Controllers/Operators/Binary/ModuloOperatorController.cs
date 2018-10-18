using System;

namespace Dash
{
    [OperatorType(Op.Name.mod, Op.Name.modulo, Op.Name.operator_modulo)]
    public class ModuloOperatorController : BinaryOperatorControllerBase<NumberController, NumberController>
    {
        public ModuloOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public ModuloOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Modulo");

        public override FieldControllerBase Compute(NumberController left, NumberController right) => new NumberController(left.Data % right.Data);

        public override FieldControllerBase GetDefaultController() => new ModuloOperatorController();
    }
}
