using System;

namespace Dash
{
    [OperatorType(Op.Name.not_equal, Op.Name.operator_not_equal)]
    public class InverseEqualityOperatorController : BinaryOperatorControllerBase<FieldControllerBase, FieldControllerBase>
    {
        public InverseEqualityOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public InverseEqualityOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Not Equal To", new Guid("C15646FF-8D78-4258-B0A1-A71212427159"));

        public override FieldControllerBase Compute(FieldControllerBase left, FieldControllerBase right)
        {
            if (left == null)
            {
                return new BoolController(right != null);
            }

            if (right == null)
            {
                return new BoolController(true);
            }

            return new BoolController( !(left.TypeInfo == right.TypeInfo && left.GetValue(null).Equals(right.GetValue(null))) );
        }

        public override FieldControllerBase GetDefaultController() => new InverseEqualityOperatorController();
    }
}
