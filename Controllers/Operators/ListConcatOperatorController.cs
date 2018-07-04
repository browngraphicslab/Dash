using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.concat, Op.Name.operator_add)]
    public sealed class ListConcatOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController ListAKey = new KeyController("0DC4697F-9C89-430D-A55D-1E2C49EDC1BD", "List A");
        public static readonly KeyController ListBKey = new KeyController("1B3E2713-191A-4C4A-929B-0441D21812D4", "List B");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("EA6E7AB1-4DB1-4A8D-890E-939CED23432D", "Results");

        public ListConcatOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

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
        private static readonly KeyController TypeKey = new KeyController("679ADBE0-AD2C-4776-9672-9AF9759FE37D", "List concatenation");
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var listA = (BaseListController) inputs[ListAKey];
            var listB = (BaseListController) inputs[ListBKey];

            var typeA = listA.ListSubTypeInfo;
            var typeB = listB.ListSubTypeInfo;

            if (typeA != typeB) throw new ScriptExecutionException(new InvalidListOperationErrorModel(typeA, typeB, InvalidListOperationErrorModel.OpError.ConcatType));

            listA.AddRange(listB.Data);
            outputs[ResultsKey] = listA;
        }

        public override FieldControllerBase GetDefaultController() => new ListConcatOperatorController();
    }
}