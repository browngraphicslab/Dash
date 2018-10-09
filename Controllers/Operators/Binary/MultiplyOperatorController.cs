// ReSharper disable once CheckNamespace

using System;

namespace Dash
{
    [OperatorType(Op.Name.mult, Op.Name.operator_multiply)]
    public sealed class MultiplyOperatorController : BinaryOperatorControllerBase<NumberController, NumberController>
    {
        public MultiplyOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public MultiplyOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Multiply", new Guid("9F4CB18B-2B00-457F-AA36-69F5CFE70CC6"));

        public override FieldControllerBase Compute(NumberController left, NumberController right) => new NumberController(left.Data * right.Data);

        public override FieldControllerBase GetDefaultController() => new MultiplyOperatorController();
    }
}
