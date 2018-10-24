using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.element_access, Op.Name.index, Op.Name.operator_modulo)]
    public sealed class ElementAccessOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController ListKey = KeyController.Get("List");
        public static readonly KeyController IndexKey = KeyController.Get("Index");

        //Output keys
        public static readonly KeyController ResultsKey = KeyController.Get("Results");

        public ElementAccessOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public ElementAccessOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(ListKey, new IOInfo(TypeInfo.Any, true)),
            new KeyValuePair<KeyController, IOInfo>(IndexKey, new IOInfo(TypeInfo.Number, true)),
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } =
        new ObservableDictionary<KeyController, DashShared.TypeInfo>()
        {
            [ResultsKey] = TypeInfo.Any
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Element Access");
        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var indexable = inputs[ListKey];
            var varIndex = ((NumberController)inputs[IndexKey]).Data;

            outputs[ResultsKey] = null;

            //TODO make different operators for string and list (use overrides instead of type checking)
            switch (indexable)
            {
                case BaseListController listToIndex:
                    if (varIndex >= listToIndex?.Count) throw new ScriptExecutionException(new IndexOutOfBoundsErrorModel((int)varIndex, listToIndex.Count));
                    outputs[ResultsKey] = listToIndex.Data[(int)varIndex];
                    break;
                case TextController stringToIndex:
                    var length = stringToIndex.Data.Length;
                    if (varIndex < 0) varIndex = varIndex % length + length;
                    if (varIndex >= length) throw new ScriptExecutionException(new IndexOutOfBoundsErrorModel((int)varIndex, length));
                    outputs[ResultsKey] = new TextController(stringToIndex.Data[(int)varIndex].ToString());
                    break;
            }
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController() => new ElementAccessOperatorController();
    }
}
