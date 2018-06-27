namespace Dash
{
    [OperatorType("numnegate")]
    public class NumNegateOperatorController : BinaryOperatorControllerBase<NumberController, NumberController>
    {
        public NumNegateOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public NumNegateOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("81AAA0CD-B68B-44E5-A5F7-BFAA2A453F12", "Numerical Negation");

        public override FieldControllerBase Compute(NumberController left, NumberController right) => new NumberController(-1 * right.Data);

        public override FieldControllerBase GetDefaultController() => new GreaterThanOperatorController();
    }
}
