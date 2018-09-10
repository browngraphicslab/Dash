using System;
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
        public static readonly KeyController StringKey = new KeyController("String");
        public static readonly KeyController StartingIndexKey = new KeyController("Index");
        public static readonly KeyController LengthKey = new KeyController("Length");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("Results");

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

        private static readonly KeyController TypeKey = new KeyController("Substring extraction 1", new Guid("A41EC14D-6E29-43D0-A9CF-C6751F5D732B"));
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
        public static readonly KeyController StringKey = new KeyController("String");
        public static readonly KeyController StartingIndexKey = new KeyController("Index");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("Results");

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

        private static readonly KeyController TypeKey = new KeyController("Substring extraction 2", new Guid("F03BF0D6-411D-4788-B359-AB26B882202A"));
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
