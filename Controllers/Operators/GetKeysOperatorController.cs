using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.get_keys)]
    public class GetKeysOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController CollectionKey = KeyController.Get("Collection");

        //Output keys
        public static readonly KeyController ResultDocumentKey = KeyController.Get("ResultDocument");

        public GetKeysOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
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
        private static KeyController TypeKey = KeyController.Get("Get Keys", new Guid("0FE2858F-CB94-4163-B4CD-CA84F99438E4"));

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
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
            return Task.CompletedTask;
        }
    }
}
