using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("map")]
    public class MapOperator : OperatorController
    {

        //Input keys
        public static readonly KeyController ListKey = new KeyController("E7F792F8-3AD0-4284-BBF6-B7F2E2799F1A", "List");
        public static readonly KeyController LambdaVariableNameKey = new KeyController("EDC4CE8E-5F2F-4F8F-AAF9-6749D736285D", "LambdaVariableName");
        public static readonly KeyController LambdaFuncKey = new KeyController("0DF88C0A-1F18-422F-9D8F-400F526828F9", "LambdaFunc");

        //Output keys
        public static readonly KeyController ResultListKey = new KeyController("87F48D1A-F958-4020-8F47-2F247BA7D66C", "ResultList");

        public MapOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public MapOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }


        public override FieldControllerBase GetDefaultController()
        {
            throw new NotImplementedException();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(ListKey, new IOInfo(TypeInfo.List, true)),
            new KeyValuePair<KeyController, IOInfo>(LambdaVariableNameKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(LambdaFuncKey, new IOInfo(TypeInfo.Text, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultListKey] = TypeInfo.List
        };

        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey =
            new KeyController("E119C98C-6A29-4D10-978C-8E8049330D92", "Map");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var inputList = inputs[ListKey] as BaseListController;
            var lambdaString = inputs[LambdaFuncKey] as TextController;
            var variableName = inputs[LambdaVariableNameKey] as TextController;

            if (inputList != null && lambdaString != null && variableName != null)
            {
                var outputList = new ListController<FieldControllerBase>();

                foreach (var obj in inputList.Data.ToArray())
                {
                    var newScope = new Scope();
                    newScope.SetVariable(variableName.Data, obj);
                    var dsl = new DSL(newScope);
                    var executed = dsl.Run(lambdaString.Data, false);
                    outputList.Add(executed);
                }

                outputs[ResultListKey] = outputList;
            }
            else
            {
                outputs[ResultListKey] = new ListController<FieldControllerBase>();
            }
        }
    }
}
