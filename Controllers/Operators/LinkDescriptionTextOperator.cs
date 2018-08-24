﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            SaveOnServer();
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Link Description Text", "6A81D1DC-E26D-43E5-856E-E4634A46354D");

        //Input keys
        public static readonly KeyController DescriptionText = KeyStore.DocumentTextKey;

        //Output keys
        public static readonly KeyController ShowDescription = new KeyController("Show Description");


        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(DescriptionText, new IOInfo(TypeInfo.Text, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ShowDescription] = TypeInfo.Bool
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
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
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new LinkDescriptionTextOperator();
        }
    }
}