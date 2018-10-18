// ReSharper disable once CheckNamespace

using System;

namespace Dash
{
    [OperatorType(Op.Name.mult, Op.Name.operator_multiply)]
    public sealed class MultiplyOperatorController : BinaryOperatorControllerBase<NumberController, NumberController>
    {
        public MultiplyOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public MultiplyOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Multiply");

        public override FieldControllerBase Compute(NumberController left, NumberController right) => new NumberController(left.Data * right.Data);

        public override FieldControllerBase GetDefaultController() => new MultiplyOperatorController();
    }
}
