using System;

namespace Dash
{
    [OperatorType(Op.Name.equal, Op.Name.operator_equal)]
    public class EqualityOperatorController : BinaryOperatorControllerBase<FieldControllerBase, FieldControllerBase>
    {
        public EqualityOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public EqualityOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Equal To", new Guid("850C513F-F38D-4279-B9D4-EED858BCAA6A"));

        public override FieldControllerBase Compute(FieldControllerBase left, FieldControllerBase right)
        {
            if (left == null)
            {
                return new BoolController(right == null);
            }

            if (right == null)
            {
                return new BoolController(false);
            }

            return new BoolController((left.TypeInfo == right.TypeInfo) && left.GetValue(null).Equals(right.GetValue(null)));
        }

        public override FieldControllerBase GetDefaultController() => new EqualityOperatorController();
    }
}
