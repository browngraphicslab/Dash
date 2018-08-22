using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.links)]
    public sealed class GetLinksOperator : OperatorController
    {
        //Input keys
        public static readonly KeyController InputDocKey = new KeyController("InputDoc");
        public static readonly KeyController GetLinkToKey = new KeyController("GetLinkTo");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("Results");

        public GetLinksOperator() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public GetLinksOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(InputDocKey, new IOInfo(TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(GetLinkToKey, new IOInfo(TypeInfo.Bool, false)),
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } =
        new ObservableDictionary<KeyController, DashShared.TypeInfo>()
        {
            [ResultsKey] = TypeInfo.Any
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("GetLinks", "5271BC68-C6ED-45BD-B70E-CA1CCFC85CEB");
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var doc = (DocumentController)inputs[InputDocKey];
            var use = inputs.TryGetValue(GetLinkToKey, out var field) ? (bool?)((BoolController)field).Data : null;

            var docs = new ListController<DocumentController>();

            if (use == null || use == true)
            {
                AddLinks(doc, true, docs);
            }

            if (use == null || use == false)
            {
                AddLinks(doc, false, docs);
            }

            outputs[ResultsKey] = docs;
        }

        private void AddLinks(DocumentController doc, bool to, ListController<DocumentController> docs)
        {
            var key = to ? KeyStore.LinkToKey : KeyStore.LinkFromKey;
            var dir = to ? LinkDirection.ToDestination : LinkDirection.ToSource;
            var docLinks = doc.GetDataDocument().GetLinks(key);
            if (docLinks != null)
            {
                foreach (var link in docLinks)
                {
                    var ldoc = link.GetLinkedDocument(dir);
                    docs.Add(ldoc.GetRegionDefinition() ?? ldoc);
                }
            }

            var regions = doc.GetDataDocument().GetRegions();
            if (regions != null)
            {
                foreach (var documentController in regions)
                {
                    var regionLinks = documentController.GetDataDocument().GetLinks(key);
                    if (regionLinks != null)
                    {
                        foreach (var link in regionLinks)
                        {
                            var ldoc = link.GetLinkedDocument(dir);
                            docs.Add(ldoc.GetRegionDefinition() ?? ldoc);
                        }
                    }
                }
            }
        }

        public override FieldControllerBase GetDefaultController() => new ElementAccessOperatorController();
    }
}
