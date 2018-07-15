using System;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.exp, Op.Name.operator_exponential)]
    public sealed class ExponentialOperatorContorller : BinaryOperatorControllerBase<NumberController, NumberController>
    {
        public ExponentialOperatorContorller(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public ExponentialOperatorContorller() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Exponential", "041AE6CF-A5AA-43C1-B088-1B42DF4777DE");

        public override FieldControllerBase Compute(NumberController left, NumberController right) => new NumberController(Math.Pow(left.Data, right.Data));

        public override FieldControllerBase GetDefaultController() => new ExponentialOperatorContorller();
    }
}