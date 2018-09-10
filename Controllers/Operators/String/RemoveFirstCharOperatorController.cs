using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.operator_subtract, Op.Name.remove_first_exp)]
    public class RemoveFirstCharOperatorController : OperatorController
    {
        public RemoveFirstCharOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public RemoveFirstCharOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Remove first char occurence", new Guid("ABDD7B87-DFDC-4684-B0FA-26379DBA8407"));

        //Input keys
        public static readonly KeyController SourceKey = new KeyController("Source string");
        public static readonly KeyController ToRemoveKey = new KeyController("Char to Remove");

        //Output keys
        public static readonly KeyController ComputedResultKey = new KeyController("Computed Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(SourceKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(ToRemoveKey, new IOInfo(TypeInfo.List, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo> { [ComputedResultKey] = TypeInfo.Number };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var varText = ((TextController)inputs[SourceKey]).Data;
            var phrasesToRemove = (ListController<TextController>)inputs[ToRemoveKey];

            var absentPhrases = new List<string>();

            var varEditor = new StringBuilder(varText);
            foreach (var tc in phrasesToRemove)
            {
                var phrase = tc.Data;
                var index = varEditor.ToString().IndexOf(phrase, StringComparison.Ordinal);
                if (index == -1)
                {
                    absentPhrases.Add(phrase);
                    continue;
                }

                varEditor.Remove(index, phrase.Length);
            }

            if (varEditor.ToString().Equals(varText)) throw new ScriptExecutionException(new AbsentStringScriptErrorModel(varText, $"[{string.Join(", ", absentPhrases)}]"));

            outputs[ComputedResultKey] = new TextController(varEditor.ToString());
        }

        public override FieldControllerBase GetDefaultController() => new RemoveFirstCharOperatorController();
    }
}
