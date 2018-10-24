using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.link_des_text)]
    class LinkDescriptionTextOperator : OperatorController
    {
        public LinkDescriptionTextOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public LinkDescriptionTextOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Link Description Text");

        //Input keys
        public static readonly KeyController DescriptionText = KeyStore.DocumentTextKey;

        //Output keys
        public static readonly KeyController ShowDescription = KeyController.Get("Show Description");


        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(DescriptionText, new IOInfo(TypeInfo.Text, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ShowDescription] = TypeInfo.Bool
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var value = inputs[DescriptionText];
            if (value is TextController rtc)
            {
                string text = rtc.Data;
                var defaultText = "New link description...";
                if (string.IsNullOrEmpty(text) || text == defaultText)
                {
                    outputs[ShowDescription] = new BoolController(false);
                }
                else
                {
                    outputs[ShowDescription] = new BoolController(true);
                }
            }
            else
            {
                outputs[ShowDescription] = new BoolController(false);
            }
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new LinkDescriptionTextOperator();
        }
    }
}
