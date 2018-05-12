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
        }

        public override FieldControllerBase GetDefaultController()
        {
            throw new NotImplementedException();
        }
        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("6277A484-644D-4BC4-8D3C-7F7DFCBA6517", "GetField");

        //Input keys
        public static readonly KeyController KeyNameKey = new KeyController("80628016-F13A-411A-8291-EB8B77391D01", "KeyName");
        public static readonly KeyController InputDocumentKey = new KeyController("C317E592-D663-4B36-9BC7-922EB2A2E92F", "InputDoc");

        //Output keys
        public static readonly KeyController ResultFieldKey = new KeyController("601FF47D-128D-40C6-B06C-1E0D1CBCA133", "ResultField");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputDocumentKey, new IOInfo(TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(KeyNameKey, new IOInfo(TypeInfo.Text, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultFieldKey] = TypeInfo.Any,
        };
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, ScriptState state = null)
        {
            var keyName = (inputs[KeyNameKey] as TextController)?.Data;
            //var dargs = args as DocumentController.DocumentFieldUpdatedEventArgs;
            //if (args != null && dargs == null)
            //{
            //    return;
            //}

            //string updatedKeyName = null;
            //if (dargs != null)
            //{
            //    if (!(dargs.FieldArgs is DocumentController.DocumentFieldUpdatedEventArgs dargs2))
            //    {
            //        return;
            //    }

            //    updatedKeyName = dargs2.Reference.FieldKey.Name;
            //}

            var doc = inputs[InputDocumentKey] as DocumentController;
            if (!string.IsNullOrEmpty(keyName) && doc != null)
            {
                var fields = doc.EnumFields().ToArray();
                foreach (var key in fields) //check exact string equality
                {
                    if (key.Key.Name.Replace(" ", "").Equals(keyName))
                    {
                        //if (updatedKeyName?.Equals(keyName) ?? true)
                        {
                            outputs[ResultFieldKey] = key.Value.DereferenceToRoot(new Context(doc));
                        }

                        return;
                    }
                }

                foreach (var key in fields) //check to lower string equality
                {
                    if (key.Key.Name.Replace(" ", "").ToLower().Equals(keyName.ToLower()))
                    {
                        //if (updatedKeyName?.ToLower().Equals(keyName) ?? true)
                        {
                            outputs[ResultFieldKey] = key.Value.DereferenceToRoot(new Context(doc));
                        }

                        return;
                    }
                }


                foreach (var key in fields) //check exact string contains
                {
                    if (key.Key.Name.Replace(" ", "").Contains(keyName) && keyName.Length  >= 3)
                    {
                        //if (updatedKeyName?.Contains(keyName) ?? true)
                        {
                            outputs[ResultFieldKey] = key.Value.DereferenceToRoot(new Context(doc));
                        }

                        return;
                    }
                }

                foreach (var key in fields) //check to lower stirng contains
                {
                    if (key.Key.Name.Replace(" ", "").ToLower().Contains(keyName.ToLower()) && keyName.Length >= 3)
                    {
                        //if (updatedKeyName?.ToLower().Contains(keyName) ?? true)
                        {
                            outputs[ResultFieldKey] = key.Value.DereferenceToRoot(new Context(doc));
                        }

                        return;
                    }
                }

                outputs[ResultFieldKey] = new TextController("Key Not Found");

            }
        }
    }
}
