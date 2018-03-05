using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class ZipOperatorController : OperatorController
    {
        public static readonly KeyController AKey = new KeyController("0252A0F7-E6A3-498E-A728-8B23B16FA0E5", "Input A");
        public static readonly KeyController BKey = new KeyController("BC72A0FF-C707-488E-A03A-29A6515AB441", "Input B");

        public static readonly KeyController OutputKey = new KeyController("24AC6CAE-F977-450F-9658-35B36C53001D", "Output");

        public ZipOperatorController() : base(new OperatorModel(OperatorType.Zip))
        {
        }

        public ZipOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [AKey] = new IOInfo(TypeInfo.List, true),
            [BKey] = new IOInfo(TypeInfo.List, true)
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.List
        };

        private static readonly List<KeyController> ExcludedKeys = new List<KeyController> {KeyStore.ActiveLayoutKey};

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
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
        }

        private void AddFields(Dictionary<KeyController, FieldControllerBase> fields, DocumentController doc)
        {
            foreach (var field in doc.EnumFields())
            {
                if (ExcludedKeys.Contains(field.Key)) continue;
                fields[field.Key] = field.Value;
            }
        }

        public override void Init()
        {
            throw new NotImplementedException();
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new ZipOperatorController();
        }
    }
}
