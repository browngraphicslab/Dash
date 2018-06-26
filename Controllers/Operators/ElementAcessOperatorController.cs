using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("elementAccess")]
    public class ElementAccessOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController VariableKey = new KeyController("6407AD77-89C7-470C-A0FE-1133ADEED75D", "Variable");
        public static readonly KeyController IndexKey = new KeyController("E800D4F4-AF0D-4848-AA30-0CCBF1014C99", "Index");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("425BEF63-041B-4705-8DAA-AECB9A5BF7CB", "Results");

        public ElementAccessOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }
        public ElementAccessOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(VariableKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(IndexKey, new IOInfo(TypeInfo.Number, true)),
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } =
        new ObservableDictionary<KeyController, DashShared.TypeInfo>()
        {
            [ResultsKey] = TypeInfo.Any
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("DAB89167-7D62-4EE5-9DCF-D3E0A4ED72F9", "Element Access");
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var varList = (inputs[VariableKey] as BaseListController)?.Data;
            var varIndex = (inputs[IndexKey] as NumberController)?.Data;

            FieldControllerBase output = null;
            if (varList != null && varIndex != null)
            {
                output = varList[(int)varIndex];
            }
            outputs[ResultsKey] = output;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new ElementAccessOperatorController();
        }
    }
}