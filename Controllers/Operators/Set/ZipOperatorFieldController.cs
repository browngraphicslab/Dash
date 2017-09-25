using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class ZipOperatorFieldController : OperatorFieldModelController
    {
        public static readonly KeyControllerBase AKey = new KeyControllerBase("0252A0F7-E6A3-498E-A728-8B23B16FA0E5", "Input A");
        public static readonly KeyControllerBase BKey = new KeyControllerBase("BC72A0FF-C707-488E-A03A-29A6515AB441", "Input B");

        public static readonly KeyControllerBase OutputKey = new KeyControllerBase("24AC6CAE-F977-450F-9658-35B36C53001D", "Output");

        public ZipOperatorFieldController() : base(new OperatorFieldModel("zip"))
        {
        }

        public ZipOperatorFieldController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldModelController<OperatorFieldModel> Copy()
        {
            return new ZipOperatorFieldController(OperatorFieldModel);
        }
        public override object GetValue(Context context)
        {
            throw new System.NotImplementedException();
        }
        public override bool SetValue(object value)
        {
            return false;
        }

        public override ObservableDictionary<KeyControllerBase, IOInfo> Inputs { get; } = new ObservableDictionary<KeyControllerBase, IOInfo>
        {
            [AKey] = new IOInfo(TypeInfo.Collection, true),
            [BKey] = new IOInfo(TypeInfo.Collection, true)
        };
        public override ObservableDictionary<KeyControllerBase, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyControllerBase, TypeInfo>
        {
            [OutputKey] = TypeInfo.Collection
        };

        private static readonly List<KeyControllerBase> ExcludedKeys = new List<KeyControllerBase> {KeyStore.ActiveLayoutKey};

        public override void Execute(Dictionary<KeyControllerBase, FieldControllerBase> inputs, Dictionary<KeyControllerBase, FieldControllerBase> outputs)
        {
            var aDocs = (inputs[AKey] as DocumentCollectionFieldModelController).GetDocuments();
            var bDocs = (inputs[BKey] as DocumentCollectionFieldModelController).GetDocuments();
            int count = Math.Min(aDocs.Count, bDocs.Count);
            var newDocs = new List<DocumentController>(count);

            for (int i = 0; i < count; ++i)
            {
                var aDoc = aDocs[i];
                var bDoc = bDocs[i];
                var fields = new Dictionary<KeyControllerBase, FieldControllerBase>();
                AddFields(fields, aDoc);
                AddFields(fields, bDoc);
                var newDoc = new DocumentController(fields, DocumentType.DefaultType);
                newDocs.Add(newDoc);
            }

            outputs[OutputKey] = new DocumentCollectionFieldModelController(newDocs);
        }

        private void AddFields(Dictionary<KeyControllerBase, FieldControllerBase> fields, DocumentController doc)
        {
            foreach (var field in doc.EnumFields())
            {
                if (ExcludedKeys.Contains(field.Key)) continue;
                fields[field.Key] = field.Value;
            }
        }
    }
}
