// ReSharper disable once CheckNamespace

using System;

namespace Dash
{
    [OperatorType(Op.Name.negate, Op.Name.operator_negate)]
    public sealed class NegationOperatorController : UnaryOperatorControllerBase<NumberController>
    {
        public NegationOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public NegationOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Negate");

        public override FieldControllerBase Compute(NumberController inContent) => new NumberController(-inContent.Data);

        public override FieldControllerBase GetDefaultController() => new NegationOperatorController();
    }
}
