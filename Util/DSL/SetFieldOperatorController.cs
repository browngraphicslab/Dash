using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.set_field)]
    public class SetFieldOperatorController : OperatorController
    {
        public SetFieldOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public SetFieldOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldControllerBase GetDefaultController()
        {
            throw new NotImplementedException();
        }
        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("SetField", "8EAF5DD0-6E8E-4102-BDF2-E82F3BC6BCC3");

        //Input keys

        public static readonly KeyController InputDocumentKey = new KeyController("InputDoc");
        public static readonly KeyController KeyNameKey = new KeyController("KeyName");
        public static readonly KeyController FieldValueKey = new KeyController("FieldValue");

        //Output keys
        public static readonly KeyController ResultDocKey = new KeyController("ResultDoc");

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

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var inputDoc = inputs[InputDocumentKey] as DocumentController;
            var keyName = (inputs[KeyNameKey] as TextController)?.Data.Replace("_", " ");
            var fieldValue = inputs[FieldValueKey];

            if (inputDoc == null) throw new ScriptExecutionException(new SetFieldFailedScriptErrorModel(KeyNameKey.Name, fieldValue.GetValue(null).ToString()));

            var success = inputDoc.SetField(new KeyController(keyName), fieldValue, true);

            var feedback = success ? $"{keyName} successfully set to " : $"Could not set {keyName} to "; 

            outputs[ResultDocKey] = new TextController(feedback + fieldValue.GetValue(null));

        }
    }
}
