using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.get_field)]
    public sealed class GetFieldOperatorController : OperatorController
    {
        public GetFieldOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }
        public GetFieldOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override FieldControllerBase GetDefaultController() => throw new NotImplementedException();

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("GetField");

        //Input keys
        public static readonly KeyController KeyNameKey = KeyController.Get("KeyName");
        public static readonly KeyController InputDocumentKey = KeyController.Get("InputDoc");

        //Output keys
        public static readonly KeyController ResultFieldKey = KeyController.Get("ResultField");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputDocumentKey, new IOInfo(TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(KeyNameKey, new IOInfo(TypeInfo.Text, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultFieldKey] = TypeInfo.Any,
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var keyName = (inputs[KeyNameKey] as TextController)?.Data;

            var doc = inputs[InputDocumentKey] as DocumentController;
            if (!string.IsNullOrEmpty(keyName) && doc != null)
            {
                var field = doc.GetDereferencedField(KeyController.Get(keyName), null);
                if (field != null)
                {
                    outputs[ResultFieldKey] = field;
                    return Task.CompletedTask;
                }
                var sb = new StringBuilder();
                var pattern = @"([a-z])([A-Z])";
                var matches = Regex.Matches(keyName, pattern);
                var prevIndex = 0;
                if (matches.Any())
                {
                    foreach (Match match in matches)
                    {
                        var caml = match.Groups.First();
                        var startIndex = caml.Captures.First().Index;
                        sb.Append(keyName.Substring(prevIndex, startIndex - prevIndex));
                        if (startIndex == prevIndex) continue;
                        sb.Append(keyName[startIndex] + " " + keyName[startIndex + 1]);
                        prevIndex = startIndex + 2;

                    }
                }
                sb.Append(keyName.Substring(prevIndex));

                var newKeyName = sb.ToString();
                outputs[ResultFieldKey] = doc.GetDereferencedField(KeyController.Get(newKeyName), null) ?? doc.GetDataDocument().GetDereferencedField(KeyController.Get(newKeyName), null);
            }

            
            return Task.CompletedTask;
        }
    }
}
