﻿using System;
namespace Dash
{
    [OperatorType("lessthan")]
    public class LessThanOperatorController : BinaryOperatorControllerBase<NumberController, NumberController>
    {
        public LessThanOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public LessThanOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("BEF33148-2A18-445D-B0A0-C41E46A495B3", "Less Than");

        public override FieldControllerBase Compute(NumberController left, NumberController right) => new BoolController(left.Data < right.Data);

        public override FieldControllerBase GetDefaultController() => new LessThanOperatorController();
    }
}
