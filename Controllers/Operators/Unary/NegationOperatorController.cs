﻿// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.negate, Op.Name.operator_negate)]
    public sealed class NegationOperatorController : UnaryOperatorControllerBase<NumberController>
    {
        public NegationOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public NegationOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController( "Negate", "A17762A4-F8CA-4061-8554-0D4EC82E6733");

        public override FieldControllerBase Compute(NumberController inContent) => new NumberController(-1 * inContent.Data);

        public override FieldControllerBase GetDefaultController() => new NegationOperatorController();
    }
}