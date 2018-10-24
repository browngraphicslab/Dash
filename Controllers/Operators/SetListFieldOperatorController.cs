using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.set_list_field)]
   public class SetListFieldOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController VariableNameKey = KeyController.Get("VariableName");
        public static readonly KeyController VariableKey = KeyController.Get("Variable");
        public static readonly KeyController IndexKey = KeyController.Get("Index");
        public static readonly KeyController ValueKey = KeyController.Get("Value");

        //Output keys
        public static readonly KeyController ResultsKey = KeyController.Get("Results");

        public SetListFieldOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }
        public SetListFieldOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(VariableNameKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(VariableKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(IndexKey, new IOInfo(TypeInfo.Number, true)),
             new KeyValuePair<KeyController, IOInfo>(ValueKey, new IOInfo(TypeInfo.Any, true)),
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } =
        new ObservableDictionary<KeyController, DashShared.TypeInfo>()
        {
            [ResultsKey] = TypeInfo.Any
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Set Field");

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            //TODO ScriptLang - Why is varName needed here
            var varName = (inputs[VariableNameKey] as TextController)?.Data;
            var varList = (inputs[VariableKey] as BaseListController)?.Data;
            var varIndex = (int)(inputs[IndexKey] as NumberController)?.Data;
            var newValue = inputs[ValueKey];

            if (varName != null && varList != null && newValue != null)
            {
                //set given element in list to desired value
                varList[varIndex] = newValue;

                //save new list into scope using varName
                scope.SetVariable(varName, new ListController<FieldControllerBase>(varList));
            }
            
            outputs[ResultsKey] = newValue;
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new ElementAccessOperatorController();
        }
    
    }
}
