using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Controllers.Operators
{
    [OperatorType(Op.Name.follow_links)]
    public class FollowLinksOperator : OperatorController
    {
        public static readonly KeyController DocKey = KeyController.Get("Doc");


        public static readonly KeyController OutKey = KeyController.Get("Out");


        public FollowLinksOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public FollowLinksOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Follow Document Links");

        public override FieldControllerBase GetDefaultController()
        {
            return new FollowLinksOperator();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(DocKey, new IOInfo(TypeInfo.Document, true)),

        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutKey] = TypeInfo.Number,

        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var doc = (DocumentController)inputs[DocKey];
            NumberController outField = Execute(doc);
            outputs[OutKey] = outField;

            return Task.CompletedTask;
        }

        public NumberController Execute(DocumentController doc)
        {
            var dataDoc = doc.GetDataDocument();
            foreach (var link in dataDoc.GetLinks(KeyStore.LinkToKey))
            {
                if (link.GetDataDocument().GetLinkTag()?.Data == "Content")
                {
                    var target = link.GetLinkedDocument(LinkDirection.ToDestination);
                    SplitFrame.OpenInActiveFrame(target);
                    return new NumberController(0);
                }
            }
            foreach (var link in dataDoc.GetLinks(KeyStore.LinkFromKey))
            {
                if (link.GetDataDocument().GetLinkTag()?.Data == "Content")
                {
                    var target = link.GetLinkedDocument(LinkDirection.ToSource);
                    SplitFrame.OpenInActiveFrame(target);
                    return new NumberController(0);
                }
            }
            return new NumberController(0);
        }

    }
}
