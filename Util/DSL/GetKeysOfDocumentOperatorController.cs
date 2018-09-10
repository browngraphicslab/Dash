﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
            SaveOnServer();
        }

        public override FieldControllerBase GetDefaultController()
        {
            throw new NotImplementedException();
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Get Keys Of Document", new Guid("0D40C96F-2088-4601-A74A-AB582C369BD4"));

        //Input keys
        public static readonly KeyController InputDocumentKey = new KeyController("InputDoc");

        //Output keys
        public static readonly KeyController ResultKeysKey = new KeyController("Keys");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputDocumentKey, new IOInfo(TypeInfo.Document, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKeysKey] = TypeInfo.List,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var doc = inputs[InputDocumentKey] as DocumentController;
            if (doc != null)
            {
                var names = doc.EnumFields().Select(i => i.Key.Name).ToArray();
                var nameControllers = names.Select(n => new TextController(n)).ToList();
                var list = new ListController<TextController>();
                list.AddRange(nameControllers);
                outputs[ResultKeysKey] = list;
            }
            else
            {
                outputs[ResultKeysKey] = new ListController<FieldControllerBase>();
            }
        }
    }
}
