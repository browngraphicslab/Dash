using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    class ZipOperatorFieldController : OperatorFieldModelController
    {
        public static readonly KeyController AKey = new KeyController("0252A0F7-E6A3-498E-A728-8B23B16FA0E5", "Input A");
        public static readonly KeyController BKey = new KeyController("BC72A0FF-C707-488E-A03A-29A6515AB441", "Input B");

        public static readonly KeyController OutputKey = new KeyController("24AC6CAE-F977-450F-9658-35B36C53001D", "Output");

        public ZipOperatorFieldController() : base(new OperatorFieldModel("zip"))
        {
        }

        public ZipOperatorFieldController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldModelController Copy()
        {
            return new ZipOperatorFieldController(OperatorFieldModel);
        }

        public override ObservableDictionary<KeyController, TypeInfo> Inputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [AKey] = TypeInfo.Collection,
            [BKey] = TypeInfo.Collection
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.Collection
        };

        private static readonly List<KeyController> ExcludedKeys = new List<KeyController> {KeyStore.ActiveLayoutKey};

        public override void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
        {
            var aDocs = (inputs[AKey] as DocumentCollectionFieldModelController).GetDocuments();
            var bDocs = (inputs[BKey] as DocumentCollectionFieldModelController).GetDocuments();
            int count = Math.Min(aDocs.Count, bDocs.Count);
            var newDocs = new List<DocumentController>(count);

            for (int i = 0; i < count; ++i)
            {
                var aDoc = aDocs[i];
                var bDoc = bDocs[i];
                var fields = new Dictionary<KeyController, FieldModelController>();
                AddFields(fields, aDoc);
                AddFields(fields, bDoc);
                var newDoc = new DocumentController(fields, DocumentType.DefaultType);
                newDocs.Add(newDoc);
            }

            outputs[OutputKey] = new DocumentCollectionFieldModelController(newDocs);
        }

        private void AddFields(Dictionary<KeyController, FieldModelController> fields, DocumentController doc)
        {
            foreach (var field in doc.EnumFields())
            {
                if (ExcludedKeys.Contains(field.Key)) continue;
                fields[field.Key] = field.Value;
            }
        }
    }
}
