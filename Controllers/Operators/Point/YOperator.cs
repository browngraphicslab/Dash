﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Controllers.Operators.Point
{
    [OperatorType(Op.Name.y)]
    public class YOperator : OperatorController
    {
        public static readonly KeyController PointKey = KeyController.Get("Point");


        public static readonly KeyController YCoordKey = KeyController.Get("YCoord");


        public YOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public YOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("YCoordinate");

        public override FieldControllerBase GetDefaultController()
        {
            return new YOperator();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(PointKey, new IOInfo(TypeInfo.Point, true)),

        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [YCoordKey] = TypeInfo.Number,

        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var p = ((PointController)inputs[PointKey]).Data;
            outputs[YCoordKey] = new NumberController(p.Y);
            return Task.CompletedTask;
        }

    }
}
