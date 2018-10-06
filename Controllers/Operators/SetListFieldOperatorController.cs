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
        public static readonly KeyController VariableNameKey = new KeyController("E98CE259-9A48-4F7D-970B-A3740C1DBA54", "VariableName");
        public static readonly KeyController VariableKey = new KeyController("53A07A39-5D02-4357-941C-98D145CB2541", "Variable");
        public static readonly KeyController IndexKey = new KeyController("AB4E9D5C-A5EA-4FA9-A47A-36268FA4DE64", "Index");
        public static readonly KeyController ValueKey = new KeyController("0C574103-B233-40CC-A8CE-91E815A9F9F3", "Value");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("BB4F8C51-F982-48CA-9497-502DF8CE6277", "Results");

        public SetListFieldOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
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
        private static readonly KeyController TypeKey = new KeyController("DAB89167-7D62-4EE5-9DCF-D3E0A4ED72F9", "Element Access");

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
