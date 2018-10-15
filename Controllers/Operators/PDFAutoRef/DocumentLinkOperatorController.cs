using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.link)]
    public sealed class DocumentLinkOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController SourceDocKey = KeyController.Get("Source Doc");
        public static readonly KeyController SourceStartIndKey = KeyController.Get("Source Start Index");
        public static readonly KeyController SourceEndIndKey = KeyController.Get("Source End Index");

        public static readonly KeyController TargetDocKey = KeyController.Get("Target Doc");
        public static readonly KeyController TargetStartIndKey = KeyController.Get("Target Start Index");
        public static readonly KeyController TargetEndIndKey = KeyController.Get("Target End Index");

        public static readonly KeyController LinkTypeKey = KeyController.Get("Link Type");

        //Output keys
        public static readonly KeyController SuccessKey = KeyController.Get("Success");

        public DocumentLinkOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public DocumentLinkOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public override FieldControllerBase GetDefaultController() => new DocumentLinkOperatorController();

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(SourceDocKey, new IOInfo(TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(TargetDocKey, new IOInfo(TypeInfo.Document, true)),

            new KeyValuePair<KeyController, IOInfo>(SourceStartIndKey, new IOInfo(TypeInfo.Number, false)),
            new KeyValuePair<KeyController, IOInfo>(SourceEndIndKey, new IOInfo(TypeInfo.Number, false)),

            new KeyValuePair<KeyController, IOInfo>(TargetStartIndKey, new IOInfo(TypeInfo.Number, false)),
            new KeyValuePair<KeyController, IOInfo>(TargetEndIndKey, new IOInfo(TypeInfo.Number, false)),

            new KeyValuePair<KeyController, IOInfo>(LinkTypeKey, new IOInfo(TypeInfo.Text, false))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [SuccessKey] = TypeInfo.Bool
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Text Link Documents", new Guid("3830D32F-FC05-427B-9761-A47DFCEA503B"));

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var sourceDoc = inputs[SourceDocKey] as DocumentController;
            var targetDoc = inputs[TargetDocKey] as DocumentController;

            var sStart = (int?)(inputs[SourceStartIndKey] as NumberController)?.Data;
            var sEnd = (int?)(inputs[SourceEndIndKey] as NumberController)?.Data;

            var tStart = (int?)(inputs[TargetStartIndKey] as NumberController)?.Data;
            var tEnd = (int?)(inputs[TargetEndIndKey] as NumberController)?.Data;

            var linkType = (TextController) inputs[LinkTypeKey];

            AnnotationOverlay.LinkRegion(sourceDoc, targetDoc, sStart, sEnd, tStart, tEnd, linkType.Data);
            return Task.CompletedTask;
        }
    }
}
