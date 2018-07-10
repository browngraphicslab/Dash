using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("getField")]
    public class GetFieldOperatorController : OperatorController
    {
        public GetFieldOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }
        public GetFieldOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public override FieldControllerBase GetDefaultController()
        {
            throw new NotImplementedException();
        }
        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("GetField", "6277A484-644D-4BC4-8D3C-7F7DFCBA6517");

        //Input keys
        public static readonly KeyController KeyNameKey = new KeyController("KeyName");
        public static readonly KeyController InputDocumentKey = new KeyController("InputDoc");

        //Output keys
        public static readonly KeyController ResultFieldKey = new KeyController("ResultField");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputDocumentKey, new IOInfo(TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(KeyNameKey, new IOInfo(TypeInfo.Text, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultFieldKey] = TypeInfo.Any,
        };
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, ScriptState state = null)
        {
            var keyName = (inputs[KeyNameKey] as TextController)?.Data;

            string updatedKeyName = null;
            if (args != null)
            {
                if (!(args.FieldArgs is DocumentController.DocumentFieldUpdatedEventArgs dargs))
                {
                    return;
                }

                updatedKeyName = dargs.Reference.FieldKey.Name;
            }

            var doc = inputs[InputDocumentKey] as DocumentController;
            if (!string.IsNullOrEmpty(keyName) && doc != null)
            {
                var fields = doc.EnumFields().ToArray();

                var controller = FindInDocFields(fields, keyName);

                outputs[ResultFieldKey] = controller ?? new TextController();
            }
        }

        private FieldControllerBase FindInDocFields(KeyValuePair<KeyController, FieldControllerBase>[] fields, string keyName)
        {
            foreach (var key in fields) //check exact string equality
            {
                if (key.Key.Name.Replace(" ", "").Equals(keyName))
                {

                    return key.Value.DereferenceToRoot(new Context(/*doc*/));
                }
            }

            foreach (var key in fields) //check to lower string equality
            {
                if (key.Key.Name.Replace(" ", "").ToLower().Equals(keyName.ToLower()))
                {
                    return key.Value.DereferenceToRoot(new Context(/*doc*/));
                }
            }

            return null;
        }
    }
}
