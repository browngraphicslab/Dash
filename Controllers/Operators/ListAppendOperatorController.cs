using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.append, Op.Name.operator_add)]
    public sealed class ListAppendOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController ListAKey = KeyController.Get("List A");
        public static readonly KeyController ToAppendKey = KeyController.Get("Element To Append");

        //Output keys
        public static readonly KeyController ResultsKey = KeyController.Get("Results");

        public ListAppendOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

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
        private static readonly KeyController TypeKey = KeyController.Get("List appending");

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var listA = inputs[ListAKey] as BaseListController;
            var toAppendController = inputs[ToAppendKey];

            var typeList = listA.ListSubTypeInfo;
            var typeElement = toAppendController.TypeInfo;

            if (!typeList.HasFlag(typeElement)) throw new ScriptExecutionException(new InvalidListOperationErrorModel(typeElement, typeList, InvalidListOperationErrorModel.OpError.AppendType));

            var l = (BaseListController) listA.Copy();
            l.AddBase(toAppendController);
            outputs[ResultsKey] = l;
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController() => new ListAppendOperatorController();
    }
}
