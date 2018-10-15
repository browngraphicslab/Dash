using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Controllers.Operators
{
    [OperatorType(Op.Name.sum)]
    public sealed class Sum : OperatorController
    {
        public static readonly KeyController InputlistKey = KeyController.Get("Inputlist");

        public static readonly KeyController SumKey = KeyController.Get("Resultlist");

        public Sum() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public Sum(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("SumOperator", new Guid("20d9d82d-8374-4fcd-ae7c-d6239f545e07"));

        public override FieldControllerBase GetDefaultController() => new Sum();

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputlistKey, new IOInfo(TypeInfo.List, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [SumKey] = TypeInfo.Number,
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var inputList = inputs[InputlistKey] as BaseListController;
            double sum = 0;

            if (inputList != null)
            {
                foreach (var value in inputList.Data.ToArray())
                {
                    switch (value)
                    {
                        case NumberController num:
                            sum += num.Data;
                            break;
                        case TextController textNum:
                            if (double.TryParse(textNum.Data, out var parsed)) sum += parsed;
                            break;
                    }
                }
            }

            outputs[SumKey] = new NumberController(sum);
            return Task.CompletedTask;
        }

    }
}
