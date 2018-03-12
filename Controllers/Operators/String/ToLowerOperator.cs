using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class ToLowerOperator : OperatorController
    {
        public ToLowerOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public ToLowerOperator() : base(new OperatorModel(OperatorType.ToLower))
        {

        }

        public override FieldControllerBase GetDefaultController() => new ToLowerOperator();

        // input keys
        public static readonly KeyController InputStringKey = new KeyController("EF3A262E-BE6F-4584-B5B0-822EF14242FB", "Input String");

        // output keys
        public static readonly KeyController OutputStringKey = new KeyController("C13CF242-F8CF-405E-BF85-6BE27A7E09BB", "Output String");


        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [InputStringKey] = new IOInfo(TypeInfo.Text, true),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputStringKey] = TypeInfo.Text,
        };


        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            if (inputs[InputStringKey] is TextController tc)
            {
                outputs[OutputStringKey] = new TextController(tc.TextFieldModel.Data.ToLower());
            }
        }
    }
}
