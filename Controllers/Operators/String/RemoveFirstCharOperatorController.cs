﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.subtract, Op.Name.minus, Op.Name.operator_subtract)]
    public class RemoveFirstCharOperatorController : OperatorController
    {
        public RemoveFirstCharOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public RemoveFirstCharOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("ABDD7B87-DFDC-4684-B0FA-26379DBA8407", "Remove first char occurence");

        //Input keys
        public static readonly KeyController SourceKey = new KeyController("F83B78FF-C629-4289-953F-42AD4EEB163C", "Source string");
        public static readonly KeyController ToRemoveKey = new KeyController("8276AE8A-71FB-4191-8532-FA897011E399", "Char to Remove");

        //Output keys
        public static readonly KeyController ComputedResultKey = new KeyController("47D9B9E6-070D-448D-BF6A-9C51A3E88C4E", "Computed Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(SourceKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(ToRemoveKey, new IOInfo(TypeInfo.Text, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo> { [ComputedResultKey] = TypeInfo.Number };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var varText = ((TextController)inputs[SourceKey]).Data;
            var assignText = ((TextController)inputs[ToRemoveKey]).Data;

            var varEditor = new StringBuilder(varText);
            var toRemove = assignText.ToCharArray();
            foreach (var c in toRemove)
            {
                var index = varEditor.ToString().IndexOf(c);
                if (index != -1) varEditor.Remove(index, 1);
            }
            outputs[ComputedResultKey] = new TextController(varEditor.ToString());
        }

        public override FieldControllerBase GetDefaultController() => new RemoveFirstCharOperatorController();
    }
}