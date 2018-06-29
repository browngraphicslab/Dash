﻿namespace Dash
{
    [OperatorType(Op.Name.less_than_equals)]
    public class LessThanEqualsOperatorController : BinaryOperatorControllerBase<NumberController, NumberController>
    {
        public LessThanEqualsOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public LessThanEqualsOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("99DD70C8-A755-4C62-BE3F-839F62302C5D", "Less Than Equals");

        public override FieldControllerBase Compute(NumberController left, NumberController right) => new BoolController(left.Data <= right.Data);

        public override FieldControllerBase GetDefaultController() => new LessThanEqualsOperatorController();
    }
}
