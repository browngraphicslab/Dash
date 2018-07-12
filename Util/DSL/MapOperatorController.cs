using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.map)]
    public class MapOperator : OperatorController
    {

        //Input keys
        public static readonly KeyController ListKey = new KeyController("List");
        public static readonly KeyController LambdaVariableNameKey = new KeyController("LambdaVariableName");
        public static readonly KeyController LambdaFuncKey = new KeyController("LambdaFunc");

        //Output keys
        public static readonly KeyController ResultListKey = new KeyController("ResultList", "87F48D1A-F958-4020-8F47-2F247BA7D66C");

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
            new KeyController("Lambda Map", "E119C98C-6A29-4D10-978C-8E8049330D92");

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
