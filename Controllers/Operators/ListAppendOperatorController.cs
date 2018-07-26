using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.concat, Op.Name.operator_add)]
    public sealed class ListAppendOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController ListAKey = new KeyController("List A");
        public static readonly KeyController ToAppendKey = new KeyController("Element To Append");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("Results");

        public ListAppendOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public ListAppendOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(ListAKey, new IOInfo(TypeInfo.List, true)),
            new KeyValuePair<KeyController, IOInfo>(ToAppendKey, new IOInfo(TypeInfo.Any, true)),
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } =
        new ObservableDictionary<KeyController, DashShared.TypeInfo>()
        {
            [ResultsKey] = TypeInfo.Any
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("List appending", "2F2C4A08-C81D-426E-913D-A5FBE5436619");
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var listA = inputs[ListAKey] as BaseListController;
            var toAppendController = inputs[ToAppendKey];

            var typeList = listA.ListSubTypeInfo;
            var typeElement = toAppendController.TypeInfo;

            if (!typeList.HasFlag(typeElement)) throw new ScriptExecutionException(new InvalidListOperationErrorModel(typeElement, typeList, InvalidListOperationErrorModel.OpError.AppendType));

            var l = (BaseListController) listA.Copy();
            l.AddBase(toAppendController);
            outputs[ResultsKey] = l;
        }

        public override FieldControllerBase GetDefaultController() => new ListAppendOperatorController();
    }
}