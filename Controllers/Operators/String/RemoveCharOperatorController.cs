﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.operator_divide, Op.Name.remove_exp)]
    public class RemoveCharOperatorController : OperatorController
    {
        public RemoveCharOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public RemoveCharOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Remove all of specified char");

        //Input keys
        public static readonly KeyController SourceKey = KeyController.Get("Source string");
        public static readonly KeyController ToRemoveKey = KeyController.Get("Char(s) to Remove");

        //Output keys
        public static readonly KeyController ComputedResultKey = KeyController.Get("Computed Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(SourceKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(ToRemoveKey, new IOInfo(TypeInfo.List, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo> { [ComputedResultKey] = TypeInfo.Number };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var varText = ((TextController)inputs[SourceKey]).Data;
            var phrasesToRemove = (ListController<TextController>)inputs[ToRemoveKey];

            var absentPhrases = new List<string>();

            var varEditor = new StringBuilder(varText);
            foreach (var tc in phrasesToRemove)
            {
                var phrase = tc.Data;
                if (!varText.Contains(phrase) || phrase == "")
                {
                    absentPhrases.Add(phrase);
                    continue;
                }

                varEditor.Replace(phrase, "");
            }

            //if (varEditor.ToString().Equals(varText)) throw new ScriptExecutionException(new AbsentStringScriptErrorModel(varText, $"[{string.Join(", ", absentPhrases)}]"));
            outputs[ComputedResultKey] = new TextController(varEditor.ToString());
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController() => new RemoveCharOperatorController();
    }
}
