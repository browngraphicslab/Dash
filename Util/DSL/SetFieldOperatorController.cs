using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("setField")]
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
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, ScriptState state = null)
        {
            var inputDoc = inputs[InputDocumentKey] as DocumentController;
            var fieldName = inputs[KeyNameKey] as TextController;
            var fieldValue = inputs[FieldValueKey];
            if (inputDoc != null && fieldName != null && fieldValue != null)
            {
                var inps = new Dictionary<KeyController, FieldControllerBase>();
                inps[GetFieldOperatorController.KeyNameKey] = fieldName;
                inps[GetFieldOperatorController.InputDocumentKey] = inputDoc;
                var outs = new Dictionary<KeyController, FieldControllerBase>();
                var getFieldController = new GetFieldOperatorController();
                getFieldController.Execute(inps, outs, args);

                var outValue = outs[GetFieldOperatorController.ResultFieldKey];
                var set = false;
                if (outValue != null)
                {
                    foreach(var field in inputDoc.EnumFields())
                    {
                        if (field.Value.Equals(outValue))
                        {
                            inputDoc.SetField(field.Key, fieldValue, true);
                            set = true;
                            break;
                        }
                    }
                }

                if (!set)
                {
                    inputDoc.SetField(new KeyController(fieldName.Data, UtilShared.GenerateNewId()), fieldValue, true);
                }
            }

            outputs[ResultDocKey] = inputDoc;
        }
    }
}
