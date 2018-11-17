using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.get_keys, Op.Name.keys)]
    public class GetKeysOfDocumentOperatorController : OperatorController
    {
        public GetKeysOfDocumentOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }


        public GetKeysOfDocumentOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override FieldControllerBase GetDefaultController()
        {
            throw new NotImplementedException();
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Get Keys Of Document");

        //Input keys
        public static readonly KeyController InputDocumentKey = KeyController.Get("InputDoc");

        //Output keys
        public static readonly KeyController ResultKeysKey = KeyController.Get("Keys");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputDocumentKey, new IOInfo(TypeInfo.Document, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKeysKey] = TypeInfo.List,
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var doc = inputs[InputDocumentKey] as DocumentController;
            if (doc != null)
            {
                var names = doc.EnumDisplayableFields().Select(i => i.Key.Name).ToArray();
                var nameControllers = names.Select(n => new TextController(n)).ToList();
                var list = new ListController<TextController>();
                list.AddRange(nameControllers);
                outputs[ResultKeysKey] = list;
            }
            else
            {
                outputs[ResultKeysKey] = new ListController<FieldControllerBase>();
            }
            return Task.CompletedTask;
        }
    }
}
