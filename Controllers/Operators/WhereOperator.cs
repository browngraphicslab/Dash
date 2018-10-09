using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Controllers.Operators
{
    [OperatorType(Op.Name.where)]
	public sealed class WhereOperator : OperatorController
	{
        public static readonly KeyController InputListKey = new KeyController("InputList");
        public static readonly KeyController LambdaKey = new KeyController("Lambda");

        public static readonly KeyController OutputListKey = new KeyController("OutputList");

        public WhereOperator() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Where Operator", new Guid("6faf4dbe-9666-49a7-8908-a271b6f47b4e"));

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
            var inputList = inputs[InputListKey] as BaseListController;
            var lambda = inputs[LambdaKey] as OperatorController;

            var outputList = new ListController<FieldControllerBase>();

            if (inputList != null && lambda != null && inputList.Count > 0 && lambda.Inputs.Count == 1)
            {
                foreach (var field in inputList.Data.ToArray())
                {
                    var res = await OperatorScript.Run(lambda, new List<FieldControllerBase> { field }, new Scope());
                    if (res is BoolController b&& b.Data) outputList.Add(field);
                }
            }

            outputs[OutputListKey] = outputList;
        }

	}
}
