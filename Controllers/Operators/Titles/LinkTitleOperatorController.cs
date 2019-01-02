using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.link_title)]
    public class LinkTitleOperatorController : OperatorController
    {
        public LinkTitleOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }
        public LinkTitleOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Link Title");

        //Input keys
        public static readonly KeyController Title1Key   = KeyStore.LinkSourceTitleKey;
        public static readonly KeyController RelationKey = KeyStore.LinkTagKey;
        public static readonly KeyController Title2Key   = KeyStore.LinkDestinationTitleKey;

        //Output keys
        public static readonly KeyController ComputedTitle = KeyStore.TitleKey;
        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(Title1Key,   new IOInfo(TypeInfo.PointerReference, true)),
            new KeyValuePair<KeyController, IOInfo>(RelationKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(Title2Key,   new IOInfo(TypeInfo.PointerReference, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ComputedTitle] = TypeInfo.Text,
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            if (inputs[Title1Key]   is TextController title1 &&
                inputs[RelationKey] is TextController relation &&
                inputs[Title2Key]   is TextController title2)
            {
                outputs[ComputedTitle] = new TextController(title1 + " => " + relation + " => " + title2) { ReadOnly = true };
            }
            else
            {
                outputs[ComputedTitle] = new TextController("<>") { ReadOnly = true };
            }
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new LinkTitleOperatorController();
        }
    }
}
