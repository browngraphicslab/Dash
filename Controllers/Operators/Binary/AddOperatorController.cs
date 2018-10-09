using System;

namespace Dash
{
    [OperatorType(Op.Name.operator_add, Op.Name.add)]
    public class AddOperatorController : BinaryOperatorControllerBase<FieldControllerBase, FieldControllerBase>
    {
        public AddOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public AddOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Add", new Guid("5C121004-6C32-4BB7-9CBF-C4A6573376EF"));

        public override FieldControllerBase Compute(FieldControllerBase left, FieldControllerBase right)
        {
            double sum = 0;

            if (left is NumberController numLeft) sum += numLeft.Data;
            else if (left is TextController text && double.TryParse(text.Data, out var numLeftConvert)) sum += numLeftConvert;

            if (right is NumberController numRight) sum += numRight.Data;
            else if (right is TextController text && double.TryParse(text.Data, out var numRightConvert)) sum += numRightConvert;

            return new NumberController(sum);
        }

        public override FieldControllerBase GetDefaultController() => new AddOperatorController();
    }
}
