using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.set_field)]
    public class SetFieldOperatorController : OperatorController
    {
        public SetFieldOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public SetFieldOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldControllerBase GetDefaultController()
        {
            throw new NotImplementedException();
        }
        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("SetField");

        //Input keys

        public static readonly KeyController InputDocumentKey = KeyController.Get("InputDoc");
        public static readonly KeyController KeyNameKey = KeyController.Get("KeyName");
        public static readonly KeyController FieldValueKey = KeyController.Get("FieldValue");

        //Output keys
        public static readonly KeyController ResultDocKey = KeyController.Get("ResultDoc");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputDocumentKey, new IOInfo(TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(KeyNameKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(FieldValueKey, new IOInfo(TypeInfo.Any, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultDocKey] = TypeInfo.Document,
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var inputDoc = inputs[InputDocumentKey] as DocumentController;
            var keyName = (inputs[KeyNameKey] as TextController)?.Data;
            var fieldValue = inputs[FieldValueKey];

            if (inputDoc == null) throw new ScriptExecutionException(new SetFieldFailedScriptErrorModel(KeyNameKey.Name, fieldValue.GetValue().ToString()));

            var success = inputDoc.SetField(KeyController.Get(keyName), fieldValue, true);

            var feedback = success ? $"{keyName} successfully set to " : $"Could not set {keyName} to "; 

            outputs[ResultDocKey] = new TextController(feedback + (fieldValue?.GetValue() ?? "null"));

            return Task.CompletedTask;
        }
    }
}
