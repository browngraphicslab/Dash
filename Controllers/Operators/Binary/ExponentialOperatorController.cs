using System;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.exp, Op.Name.operator_exponential)]
    public sealed class ExponentialOperatorContorller : BinaryOperatorControllerBase<NumberController, NumberController>
    {
        public ExponentialOperatorContorller(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public ExponentialOperatorContorller() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Exponential");

        public override FieldControllerBase Compute(NumberController left, NumberController right) => new NumberController(Math.Pow(left.Data, right.Data));

        public override FieldControllerBase GetDefaultController() => new ExponentialOperatorContorller();
    }
}
