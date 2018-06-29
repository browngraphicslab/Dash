using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.operator_divide, Op.Name.remove_char)]
    public class RemoveCharOperatorController : OperatorController
    {
        public RemoveCharOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public RemoveCharOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("A1AE7EAF-4C95-42C9-AFF9-845673B87042", "Remove all of specified char");

        //Input keys
        public static readonly KeyController SourceKey = new KeyController("C91910E2-E3C8-4B71-A5FF-3765E7B3AC16", "Source string");
        public static readonly KeyController ToRemoveKey = new KeyController("7D4EEE8C-57DC-4663-A78D-B126C3DFF731", "Char(s) to Remove");

        //Output keys
        public static readonly KeyController ComputedResultKey = new KeyController("61232552-7337-42C7-AA6C-58A16893849D", "Computed Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(SourceKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(ToRemoveKey, new IOInfo(TypeInfo.Text, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo> { [ComputedResultKey] = TypeInfo.Number };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var varText = ((TextController)inputs[SourceKey]).Data;
            var removeText = ((TextController)inputs[ToRemoveKey]).Data;

            var varEditor = new StringBuilder(varText);
            var toRemove = removeText.ToCharArray();
            foreach (var c in toRemove) { varEditor.Replace(c.ToString(), ""); }

            if (varEditor.ToString().Equals(varText)) throw new ScriptExecutionException(new AbsentStringScriptErrorModel(varText, removeText));

            outputs[ComputedResultKey] = new TextController(varEditor.ToString());
        }

        public override FieldControllerBase GetDefaultController() => new RemoveCharOperatorController();
    }
}