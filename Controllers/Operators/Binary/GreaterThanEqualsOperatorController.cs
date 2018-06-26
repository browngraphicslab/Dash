namespace Dash
{
    [OperatorType("greaterthanequals")]
    public class GreaterThanEqualsOperatorController : BinaryOperatorControllerBase<NumberController, NumberController>
    {
        public GreaterThanEqualsOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public GreaterThanEqualsOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("74D140BB-5962-4DBB-B9E1-6C2C0DC5A77B", "Greater Than Equals");

        public override FieldControllerBase Compute(NumberController left, NumberController right) => new BoolController(left.Data >= right.Data);

        public override FieldControllerBase GetDefaultController() => new GreaterThanEqualsOperatorController();
    }
}
