using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.substring)]
    public sealed class SubstringOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController StringKey = new KeyController("F416E497-A989-47E7-99F0-75E5C4B4EB04", "String");
        public static readonly KeyController StartingIndexKey = new KeyController("B0E959D3-56C8-4875-9CE7-D8C51B2EBD2A", "Index");
        public static readonly KeyController LengthKey = new KeyController("D5466D7B-6860-41E4-ACB8-A1A49FA7AB10", "Length");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("322E8675-A36C-4719-B59A-BB8438DC8C70", "Results");

        public SubstringOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public SubstringOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(StringKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(StartingIndexKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(LengthKey, new IOInfo(TypeInfo.Number, true)),
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } =
        new ObservableDictionary<KeyController, DashShared.TypeInfo>()
        {
            [ResultsKey] = TypeInfo.Text
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("A41EC14D-6E29-43D0-A9CF-C6751F5D732B", "Substring extraction");
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var stringToEdit = (inputs[StringKey] as TextController)?.Data;
            var startingIndex = (int)((NumberController)inputs[StartingIndexKey]).Data;
            var length = (int)((NumberController)inputs[LengthKey]).Data;

            if (startingIndex + length > stringToEdit?.Length) throw new ScriptExecutionException(new IndexOutOfBoundsErrorModel(startingIndex, stringToEdit.Length, length));

            FieldControllerBase output = null;
            if (stringToEdit != null) output = new TextController(stringToEdit.Substring(startingIndex, length));
            outputs[ResultsKey] = output;
        }

        public override FieldControllerBase GetDefaultController() => new SubstringOperatorController();
    }

    [OperatorType(Op.Name.substring)]
    public sealed class DefaultSubstringOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController StringKey = new KeyController("0EB2BAC2-D3E9-495F-B6C1-19BDF17324C7", "String");
        public static readonly KeyController StartingIndexKey = new KeyController("559B9428-CEFF-43E5-8DA0-0AD05F6AF3FF", "Index");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("4C25B9E7-F4AE-48A5-8558-18AA519FC70F", "Results");

        public DefaultSubstringOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public DefaultSubstringOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(StringKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(StartingIndexKey, new IOInfo(TypeInfo.Number, true)),
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } =
        new ObservableDictionary<KeyController, DashShared.TypeInfo>()
        {
            [ResultsKey] = TypeInfo.Text
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("F03BF0D6-411D-4788-B359-AB26B882202A", "Substring extraction");
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var stringToEdit = (inputs[StringKey] as TextController)?.Data;
            var startingIndex = (int)((NumberController)inputs[StartingIndexKey]).Data;

            if (startingIndex >= stringToEdit?.Length) throw new ScriptExecutionException(new IndexOutOfBoundsErrorModel((int)startingIndex, stringToEdit.Length));

            FieldControllerBase output = null;
            if (stringToEdit != null) output = new TextController(stringToEdit.Substring(startingIndex));
            outputs[ResultsKey] = output;
        }

        public override FieldControllerBase GetDefaultController() => new SubstringOperatorController();
    }
}