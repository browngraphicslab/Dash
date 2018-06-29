﻿namespace Dash
{
    [OperatorType(Op.Name.equal, Op.Name.operator_equal)]
    public class EqualityOperatorController : BinaryOperatorControllerBase<FieldControllerBase, FieldControllerBase>
    {
        public EqualityOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public EqualityOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("850C513F-F38D-4279-B9D4-EED858BCAA6A", "Equal To");

        public override FieldControllerBase Compute(FieldControllerBase left, FieldControllerBase right)
        {
            return new BoolController((left.TypeInfo == right.TypeInfo) && left.GetValue(null).Equals(right.GetValue(null)));
        }

        public override FieldControllerBase GetDefaultController() => new EqualityOperatorController();
    }
}
