﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name._string)]
    public class ToStringOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController InputKey = new KeyController("9BEF4C5D-3E1B-4DF8-8CDE-479A66F18080", "Input");

        //Output keys
        public static readonly KeyController ResultStringKey = new KeyController("BD564D24-460A-47EF-9871-FBDADA465812", "String");


        public ToStringOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public ToStringOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldControllerBase GetDefaultController()
        {
            throw new NotImplementedException();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(InputKey, new IOInfo(TypeInfo.Any, true))
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultStringKey] = TypeInfo.Text
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("C9A561E8-D4A1-4C38-A0BD-D9EE3531DACE", "To String");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var input = inputs[InputKey];
            if (input != null)
            {
                var inputString = input.GetValue(null).ToString();
                outputs[ResultStringKey] = new TextController(inputString);
            }
        }
    }
}
