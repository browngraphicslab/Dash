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
        public static readonly KeyController InputStringKey = new KeyController("Input String");

        // output keys
        public static readonly KeyController OutputStringKey = new KeyController("Output String");


        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("ToLower", "502F54E3-D2AF-46FF-91E9-42B9A00C7E9D");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController,IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputStringKey, new IOInfo(TypeInfo.Text, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputStringKey] = TypeInfo.Text,
        };
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            if (inputs[InputStringKey] is TextController tc)
            {
                outputs[OutputStringKey] = new TextController(tc.TextFieldModel.Data.ToLower());
            }
        }
    }
}
