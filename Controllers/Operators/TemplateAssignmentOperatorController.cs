using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.apply, Op.Name.set_template)]
    public sealed class TemplateAssignmentOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController DocKey = new KeyController("501F0C8E-D5A5-4989-BEEA-69FC774E25F8", "Document Reference");
        public static readonly KeyController TemplateKey = new KeyController("653DFD50-E5D0-4207-A61C-DC1F4851C1CB", "Template");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("5CC0DDB3-E921-4032-9514-BB4034B254B6", "Results");

        public TemplateAssignmentOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public TemplateAssignmentOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(DocKey, new IOInfo(TypeInfo.Any, true)),
            new KeyValuePair<KeyController, IOInfo>(TemplateKey, new IOInfo(TypeInfo.Document, true)),
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } =
        new ObservableDictionary<KeyController, DashShared.TypeInfo>()
        {
            [ResultsKey] = TypeInfo.Document
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("53F162D1-3D49-4872-B0E2-2A1FBEB463E4", "Apply Template");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var template = (DocumentController) inputs[TemplateKey];

            FieldControllerBase output = null;
            FieldControllerBase docInput = inputs[DocKey];
            switch (docInput)
            {
                case DocumentController workingDoc:
                    output = ApplyTemplate(workingDoc, template);
                    break;
                case ListController<DocumentController> workingDocs:
                    var outdocs = new ListController<DocumentController>();
                    foreach (DocumentController wd in workingDocs)
                    {
                        outdocs.Add(ApplyTemplate(wd, template));
                    }

                    output = outdocs;
                    break;
            }

            if (output != null)
            {
                outputs[ResultsKey] = output;
            }
        }

        private static DocumentController ApplyTemplate(DocumentController workingDoc, DocumentController template)
        {
            if (!(workingDoc.GetField(KeyStore.PositionFieldKey) is PointController point)) return null;

            DocumentController dataDoc = template.GetDataInstance();
            dataDoc.SetField(KeyStore.DocumentContextKey, workingDoc.GetDataDocument(), true);
            dataDoc.SetField(KeyStore.PositionFieldKey, point, true);


            var fields = template.GetField<ListController<DocumentController>>(KeyStore.DataKey).Select(doc => doc.GetField<PointerReferenceController>(KeyStore.DataKey)?.FieldKey);
            var missingfields = new ListController<KeyController>();
            foreach (KeyController field in fields)
            {
                if (workingDoc.GetField(field) == null)
                {
                    missingfields.Add(field);
                }
            }

            var outDoc = new DocumentController { DocumentType = DashConstants.TypeStore.FieldContentNote };

            var caption = "Template applied successfully";

            if (missingfields.Count > 0)
            {
                caption = "Missing fields in target document:";
                outDoc.SetField(KeyStore.DataKey, missingfields, true);
            }

            outDoc.SetField(KeyStore.TitleKey, new TextController(caption), true);
           // workingDoc.SetField(KeyStore.ActiveLayoutKey, dataDoc, true);
            throw new System.Exception("ActiveLayoutKey code has not been updated yet");

            return outDoc;
        }

        public override FieldControllerBase GetDefaultController() => new TemplateAssignmentOperatorController();
    }
}
