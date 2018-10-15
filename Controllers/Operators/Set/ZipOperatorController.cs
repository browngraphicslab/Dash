using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class ZipOperatorController : OperatorController
    {
        public static readonly KeyController AKey = KeyController.Get("Input A");
        public static readonly KeyController BKey = KeyController.Get("Input B");

        public static readonly KeyController OutputKey = KeyController.Get("Output");

        public ZipOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {

        }

        public ZipOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.List, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.List, true))
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.List
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Zip", new Guid("FA39D712-E1AA-4740-8CC9-C3201708A1F5"));

        private static readonly List<KeyController> ExcludedKeys = new List<KeyController>();

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var aDocs = (inputs[AKey] as ListController<DocumentController>).GetElements();
            var bDocs = (inputs[BKey] as ListController<DocumentController>).GetElements();
            int count = Math.Min(aDocs.Count, bDocs.Count);
            var newDocs = new List<DocumentController>(count);

            for (int i = 0; i < count; ++i)
            {
                var aDoc = aDocs[i];
                var bDoc = bDocs[i];
                var fields = new Dictionary<KeyController, FieldControllerBase>();
                AddFields(fields, aDoc);
                AddFields(fields, bDoc);
                var newDoc = new DocumentController(fields, DocumentType.DefaultType);
                newDocs.Add(newDoc);
            }

            outputs[OutputKey] = new ListController<DocumentController>(newDocs);
            return Task.CompletedTask;
        }

        private void AddFields(Dictionary<KeyController, FieldControllerBase> fields, DocumentController doc)
        {
            foreach (var field in doc.EnumFields())
            {
                if (ExcludedKeys.Contains(field.Key)) continue;
                fields[field.Key] = field.Value;
            }
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new ZipOperatorController();
        }
    }
}
