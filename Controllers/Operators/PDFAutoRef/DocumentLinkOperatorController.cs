using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.link)]
    public sealed class DocumentLinkOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController StartIndKey = new KeyController("Start Index");
        public static readonly KeyController EndIndKey = new KeyController("End Index");
        public static readonly KeyController SourceDocKey = new KeyController("Source Doc");
        public static readonly KeyController TargetDocKey = new KeyController("Target Doc");
        public static readonly KeyController LinkTypeKey = new KeyController("Link Type");

        //Output keys
        public static readonly KeyController SuccessKey = new KeyController("Success");

        public DocumentLinkOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public DocumentLinkOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public override FieldControllerBase GetDefaultController() => new DocumentLinkOperatorController();

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(StartIndKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(EndIndKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(SourceDocKey, new IOInfo(TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(TargetDocKey, new IOInfo(TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(LinkTypeKey, new IOInfo(TypeInfo.Text, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [SuccessKey] = TypeInfo.Bool
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Text Link Documents", "AF60BBE3-1691-4582-B860-DE328E497C9D");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var startIndex = (int)((inputs[StartIndKey] as NumberController)?.Data ?? -1);
            var endIndex = (int)((inputs[EndIndKey] as NumberController)?.Data ?? -1);
            var sourceDoc = inputs[SourceDocKey] as DocumentController;
            var targetDoc = inputs[TargetDocKey] as DocumentController;
            var linkType = (TextController) inputs[LinkTypeKey];

            NewAnnotationOverlay.LinkRegion(startIndex, endIndex, sourceDoc, targetDoc, linkType.Data);
        }
    }
}
