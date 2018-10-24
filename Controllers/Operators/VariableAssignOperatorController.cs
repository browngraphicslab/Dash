using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.var_assign)]
    public class VariableAssignOperatorController : OperatorController
    {

        public static readonly KeyController VariableKey = KeyController.Get("Variable");
        public static readonly KeyController AssignmentKey = KeyController.Get("Assignment");

        public static readonly KeyController OutputKey = KeyController.Get("Output");

        public VariableAssignOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public VariableAssignOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("VariableAssign");

        public override FieldControllerBase GetDefaultController()
        {
            return new VariableAssignOperatorController();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(VariableKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(AssignmentKey, new IOInfo(TypeInfo.Any, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.Any
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var var = (inputs[VariableKey] as TextController)?.Data;
            var assignment = inputs[AssignmentKey];

            scope?.SetVariable(var, assignment);

            outputs[OutputKey] = assignment;
            return Task.CompletedTask;
        }

    }
}
