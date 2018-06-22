using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    public class ToLowerOperator : OperatorController
    {
        public ToLowerOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public ToLowerOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();

        }

        public override FieldControllerBase GetDefaultController() => new ToLowerOperator();

        // input keys
        public static readonly KeyController InputStringKey = new KeyController("EF3A262E-BE6F-4584-B5B0-822EF14242FB", "Input String");

        // output keys
        public static readonly KeyController OutputStringKey = new KeyController("C13CF242-F8CF-405E-BF85-6BE27A7E09BB", "Output String");


        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("502F54E3-D2AF-46FF-91E9-42B9A00C7E9D", "ToLower");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController,IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputStringKey, new IOInfo(TypeInfo.Text, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputStringKey] = TypeInfo.Text,
        };


        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, Scope scope = null)
        {
            if (inputs[InputStringKey] is TextController tc)
            {
                outputs[OutputStringKey] = new TextController(tc.TextFieldModel.Data.ToLower());
            }
        }
    }
}
