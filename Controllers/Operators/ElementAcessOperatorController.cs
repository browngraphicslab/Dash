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
        public static readonly KeyController ListKey = new KeyController("6407AD77-89C7-470C-A0FE-1133ADEED75D", "List");
        public static readonly KeyController IndexKey = new KeyController("E800D4F4-AF0D-4848-AA30-0CCBF1014C99", "Index");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("425BEF63-041B-4705-8DAA-AECB9A5BF7CB", "Results");

        public ElementAccessOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

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
        private static readonly KeyController TypeKey = new KeyController("DAB89167-7D62-4EE5-9DCF-D3E0A4ED72F9", "Element Access");
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
