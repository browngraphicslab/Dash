using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.substring)]
    public sealed class SubstringOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController StringKey = KeyController.Get("String");
        public static readonly KeyController StartingIndexKey = KeyController.Get("Index");
        public static readonly KeyController LengthKey = KeyController.Get("Length");

        //Output keys
        public static readonly KeyController ResultsKey = KeyController.Get("Results");

        public SubstringOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

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

        private static readonly KeyController TypeKey = KeyController.Get("Substring extraction 1");

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var stringToEdit = (inputs[StringKey] as TextController)?.Data;
            var startingIndex = (int)((NumberController)inputs[StartingIndexKey]).Data;
            var length = (int)((NumberController)inputs[LengthKey]).Data;

            if (startingIndex + length > stringToEdit?.Length) throw new ScriptExecutionException(new IndexOutOfBoundsErrorModel(startingIndex, stringToEdit.Length, length));

            FieldControllerBase output = null;
            if (stringToEdit != null) output = new TextController(stringToEdit.Substring(startingIndex, length));
            outputs[ResultsKey] = output;
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController() => new SubstringOperatorController();
    }

    [OperatorType(Op.Name.substring)]
    public sealed class DefaultSubstringOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController StringKey = KeyController.Get("String");
        public static readonly KeyController StartingIndexKey = KeyController.Get("Index");

        //Output keys
        public static readonly KeyController ResultsKey = KeyController.Get("Results");

        public DefaultSubstringOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

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

        private static readonly KeyController TypeKey = KeyController.Get("Substring extraction 2");

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var stringToEdit = (inputs[StringKey] as TextController)?.Data;
            var startingIndex = (int)((NumberController)inputs[StartingIndexKey]).Data;

            if (startingIndex >= stringToEdit?.Length) throw new ScriptExecutionException(new IndexOutOfBoundsErrorModel((int)startingIndex, stringToEdit.Length));

            FieldControllerBase output = null;
            if (stringToEdit != null) output = new TextController(stringToEdit.Substring(startingIndex));
            outputs[ResultsKey] = output;
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController() => new SubstringOperatorController();
    }
}
