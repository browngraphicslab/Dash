﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DashShared;

namespace Dash
{
    [OperatorType("getKeys")]
    public class GetKeysOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController CollectionKey = new KeyController("2EA230C0-CDED-4B0F-AE3F-0129E89CC211", "Collection");

        //Output keys
        public static readonly KeyController ResultDocumentKey = new KeyController("C3F0663C-3797-4F90-BD9E-0F2DC9908C5B", "ResultDocument");

        public GetKeysOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public GetKeysOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new GetKeysOperatorController();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(CollectionKey, new IOInfo(TypeInfo.List, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultDocumentKey] = TypeInfo.Document
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static KeyController TypeKey = new KeyController("0FE2858F-CB94-4163-B4CD-CA84F99438E4", "Get Keys");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, ScriptState state = null)
        {
            var inputCollection = inputs[CollectionKey] as BaseListController;

            if (inputCollection != null)
            {
                var allDocs =
                    inputCollection.Data.SelectMany(
                        i => (i as DocumentController)?.GetDataDocument().GetField<ListController<DocumentController>>(KeyStore.DataKey)?.Data
                            ?? new List<FieldControllerBase>() {i}).ToArray();

                var allKeys = allDocs.SelectMany(i => ((i as DocumentController)?.GetDataDocument().EnumDisplayableFields() ?? new List<KeyValuePair<KeyController, FieldControllerBase>>())).ToArray();
                var newDoc = new DocumentController();
                foreach (var key in allKeys)
                {
                    if (newDoc.GetField(key.Key) == null)
                    {
                        newDoc.SetField(key.Key, new TextController("temp field"), true);
                    }
                }
                outputs[ResultDocumentKey] = newDoc;
            }
            else
            {
                outputs[ResultDocumentKey] = new TextController("");
            }
        }
    }
}
