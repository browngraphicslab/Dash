﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Controllers.Operators
{
    [OperatorType(Op.Name.where)]
	public sealed class WhereOperator : OperatorController
	{
        public static readonly KeyController InputListKey = KeyController.Get("InputList");
        public static readonly KeyController LambdaKey = KeyController.Get("Lambda");

        public static readonly KeyController OutputListKey = KeyController.Get("OutputList");

        public WhereOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Where Operator");

        public override FieldControllerBase GetDefaultController()
        {
            return new WhereOperator();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputListKey, new IOInfo(TypeInfo.List, true)),
            new KeyValuePair<KeyController, IOInfo>(LambdaKey, new IOInfo(TypeInfo.Operator, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputListKey] = TypeInfo.List,
        };

        public override async Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var inputList = inputs[InputListKey] as IListController;
            var lambda = inputs[LambdaKey] as OperatorController;

            var outputList = new ListController<FieldControllerBase>();

            if (inputList != null && lambda != null && inputList.Count > 0 && lambda.Inputs.Count == 1)
            {
                foreach (var field in inputList.AsEnumerable().ToArray())
                {
                    var res = await OperatorScript.Run(lambda, new List<FieldControllerBase> { field }, new DictionaryScope());
                    if (res is BoolController b&& b.Data) outputList.Add(field);
                }
            }

            outputs[OutputListKey] = outputList;
        }

	}
}
