using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.var_assign)]
    public class VariableAssignOperatorController : OperatorController
    {

        public static readonly KeyController VariableKey = new KeyController("20859151-FBBC-4267-8008-E91A2CD3D61A", "Variable");
        public static readonly KeyController AssignmentKey = new KeyController("B9230797-EFE1-4231-9AD7-9F5C401F44D0", "Assignment");

        public static readonly KeyController OutputKey = new KeyController("E0B86647-5A1C-40B4-B4AD-0A738EB85CA9", "Output");

        public VariableAssignOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public VariableAssignOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("D0BA93CC-9585-4E36-BE05-C15C586AF9FD", "VariableAssign");

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

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, 
            Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var var = (inputs[VariableKey] as TextController)?.Data;
            var assignment = inputs[AssignmentKey];

            scope?.SetVariable(var, assignment);

            outputs[OutputKey] = assignment;
        }

    }
}
