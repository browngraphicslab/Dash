using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.zip)]
    public sealed class ListZipOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController ListAKey = new KeyController("List A");
        public static readonly KeyController ListBKey = new KeyController("List B");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("Results");

        public ListZipOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public ListZipOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
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
        private static readonly KeyController TypeKey = new KeyController("List zip", new Guid("B4F07219-AA26-4E71-965E-CBDF6D44708E"));

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var listA = (BaseListController) inputs[ListAKey];
            var listB = (BaseListController) inputs[ListBKey];

            var typeA = listA.ListSubTypeInfo;
            var typeB = listB.ListSubTypeInfo;
            if (typeA != typeB) throw new ScriptExecutionException(new InvalidListOperationErrorModel(typeA, typeB, InvalidListOperationErrorModel.OpError.ZipType));

            var countA = listA.Count;
            var countB = listB.Count;
            if (countA != countB) throw new ScriptExecutionException(new InvalidListOperationErrorModel(typeA, typeB, InvalidListOperationErrorModel.OpError.ZipLength, countA, countB));

            var actualA = listA?.Data;
            var actualB = listB?.Data;
            var type = listA?.ListSubTypeInfo;
            var count = actualA?.Count;

            var zipped = new ListController<FieldControllerBase>();
            for (var i = 0; i < count; i++)
            {
                switch (type)
                {
                    case TypeInfo.Text:
                    {
                        var elA = ((TextController)actualA[i]).Data;
                        var elB = ((TextController)actualB[i]).Data;
                        zipped.Add(new TextController(elA + elB));
                            break;
                    }
                    case TypeInfo.Number:
                    {
                        var elA = ((NumberController)actualA[i]).Data;
                        var elB = ((NumberController)actualB[i]).Data;
                        zipped.Add(new NumberController(elA + elB));
                        break;
                    }
                }
            }

            outputs[ResultsKey] = zipped;
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController() => new ListAppendOperatorController();
    }
}
