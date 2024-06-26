﻿using System;

namespace Dash
{
    [OperatorType(Op.Name.operator_add, Op.Name.add)]
    public class AddOperatorController : BinaryOperatorControllerBase<FieldControllerBase, FieldControllerBase>
    {
        public AddOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public AddOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Add");

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
