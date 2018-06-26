namespace Dash
{
    [OperatorType("mod")]
    public class ModuloOperatorController : BinaryOperatorControllerBase<NumberController, NumberController>
    {
        public ModuloOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public ModuloOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("A5ED8B69-CDFB-4A84-9E81-0FC8031FB710", "Modulo");

        public override FieldControllerBase Compute(NumberController left, NumberController right) => new NumberController(left.Data % right.Data);

        public override FieldControllerBase GetDefaultController() => new ModuloOperatorController();
    }
}