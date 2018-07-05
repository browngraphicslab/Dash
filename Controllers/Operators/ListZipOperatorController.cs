using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.zip)]
    public sealed class ListZipOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController ListAKey = new KeyController("A5166155-A69E-431E-8636-B5108409B66B", "List A");
        public static readonly KeyController ListBKey = new KeyController("A4F2C32E-2D2F-40DB-9779-2C2F15A11749", "List B");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("5D45C3CB-B03C-4F41-B915-9E8688882D03", "Results");

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
        private static readonly KeyController TypeKey = new KeyController("2F2C4A08-C81D-426E-913D-A5FBE5436619", "List appending");
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
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
        }

        public override FieldControllerBase GetDefaultController() => new ListAppendOperatorController();
    }
}