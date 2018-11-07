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
        public static readonly KeyController ListAKey = KeyController.Get("List A");
        public static readonly KeyController ListBKey = KeyController.Get("List B");

        //Output keys
        public static readonly KeyController ResultsKey = KeyController.Get("Results");

        public ListZipOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

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
        private static readonly KeyController TypeKey = KeyController.Get("List zip");

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var listA = (IListController) inputs[ListAKey];
            var listB = (IListController) inputs[ListBKey];

            var typeA = listA.ListSubTypeInfo;
            var typeB = listB.ListSubTypeInfo;
            if (typeA != typeB) throw new ScriptExecutionException(new InvalidListOperationErrorModel(typeA, typeB, InvalidListOperationErrorModel.OpError.ZipType));

            var countA = listA.Count;
            var countB = listB.Count;
            if (countA != countB) throw new ScriptExecutionException(new InvalidListOperationErrorModel(typeA, typeB, InvalidListOperationErrorModel.OpError.ZipLength, countA, countB));

            var type = listA.ListSubTypeInfo;
            var count = listA.Count;

            var zipped = new ListController<FieldControllerBase>();
            for (var i = 0; i < count; i++)
            {
                switch (type)
                {
                    case TypeInfo.Text:
                    {
                        var elA = ((TextController)listA.GetValue(i)).Data;
                        var elB = ((TextController)listB.GetValue(i)).Data;
                        zipped.Add(new TextController(elA + elB));
                            break;
                    }
                    case TypeInfo.Number:
                    {
                        var elA = ((NumberController)listA.GetValue(i)).Data;
                        var elB = ((NumberController)listB.GetValue(i)).Data;
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
