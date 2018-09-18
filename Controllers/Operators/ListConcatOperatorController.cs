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
        public static readonly KeyController ListAKey = new KeyController("List A");
        public static readonly KeyController ListBKey = new KeyController("List B");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("Results");

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
        private static readonly KeyController TypeKey = new KeyController("List concatenation", "679ADBE0-AD2C-4776-9672-9AF9759FE37D");
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var listA = (BaseListController) inputs[ListAKey];
            var listB = (BaseListController) inputs[ListBKey];

            var typeA = listA.ListSubTypeInfo;
            var typeB = listB.ListSubTypeInfo;

            if (typeA != typeB) throw new ScriptExecutionException(new InvalidListOperationErrorModel(typeA, typeB, InvalidListOperationErrorModel.OpError.ConcatType));

            var l = (BaseListController) listA.Copy();
            l.AddRange(listB.Data);
            outputs[ResultsKey] = l;
        }

        public override FieldControllerBase GetDefaultController() => new ListConcatOperatorController();
    }
}
