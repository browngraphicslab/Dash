using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.map)]
    public sealed class MapOperator : OperatorController
    {

        //Input keys
        public static readonly KeyController ListKey = KeyController.Get("List");
        public static readonly KeyController LambdaKey = KeyController.Get("Lambda");

        //Output keys
        public static readonly KeyController ResultListKey = KeyController.Get("ResultList");

        public MapOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public MapOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override FieldControllerBase GetDefaultController() => throw new NotImplementedException();

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(ListKey, new IOInfo(TypeInfo.List, true)),
            new KeyValuePair<KeyController, IOInfo>(LambdaKey, new IOInfo(TypeInfo.Operator, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultListKey] = TypeInfo.List
        };

        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey = KeyController.Get("Lambda Map");

        public override async Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var inputList = inputs[ListKey] as IListController;
            var lambda = inputs[LambdaKey] as OperatorController;

            var outputList = new ListController<FieldControllerBase>();

            if (inputList != null && lambda != null && inputList.Count > 0 && lambda.Inputs.Count == 1)
            {
                foreach (var field in inputList.AsEnumerable().ToArray())
                {
                    var res = await OperatorScript.Run(lambda, new List<FieldControllerBase> {field}, new Scope());
                    if (res != null) outputList.Add(res);
                }
            }

            outputs[ResultListKey] = outputList;
        }
    }
}
