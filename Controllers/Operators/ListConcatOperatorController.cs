﻿﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.append, Op.Name.operator_add)]
    public sealed class ListConcatOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController ListAKey = KeyController.Get("List A");
        public static readonly KeyController ListBKey = KeyController.Get("List B");

        //Output keys
        public static readonly KeyController ResultsKey = KeyController.Get("Results");

        public ListConcatOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public ListConcatOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(ListAKey, new IOInfo(TypeInfo.List, true)),
            new KeyValuePair<KeyController, IOInfo>(ListBKey, new IOInfo(TypeInfo.List, true)),
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } =
        new ObservableDictionary<KeyController, DashShared.TypeInfo>()
        {
            [ResultsKey] = TypeInfo.List
        };

        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey = KeyController.Get("List concatenation", new Guid("679ADBE0-AD2C-4776-9672-9AF9759FE37D"));
        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var listA = (BaseListController) inputs[ListAKey];
            var listB = (BaseListController) inputs[ListBKey];

            var typeA = listA.ListSubTypeInfo;
            var typeB = listB.ListSubTypeInfo;

            if (typeA != typeB) throw new ScriptExecutionException(new InvalidListOperationErrorModel(typeA, typeB, InvalidListOperationErrorModel.OpError.ConcatType));

            var l = (BaseListController) listA.Copy();
            l.AddRange(listB.Data);
            outputs[ResultsKey] = l;
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController() => new ListConcatOperatorController();
    }
}
