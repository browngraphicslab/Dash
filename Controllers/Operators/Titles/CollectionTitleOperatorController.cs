using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class CollectionTitleOperatorController : OperatorController
    {
        public CollectionTitleOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }
        public CollectionTitleOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("775EE4CC-D2A8-4A11-AC3F-EC36C91355DE", "Collection Title");

        protected virtual string Prefix() { return "COLLECTION: ";  }

        //Input keys
        public static readonly KeyController CollectionDocsKey = new KeyController("FB7EE0B1-004E-4FE0-B316-FFB909CBEBF2", "Collection Docs");

        //Output keys
        public static readonly KeyController ComputedTitle = new KeyController("B8F9AC2E-02F8-4C95-82D8-401BA57053C3", "Computed Title");

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [CollectionDocsKey] = new IOInfo(TypeInfo.List, true),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ComputedTitle] = TypeInfo.Text,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            TextController output = null;

            if (inputs[CollectionDocsKey] is ListController<DocumentController> collDocs)
            {
                var firstDoc = collDocs.TypedData.OrderBy(dc => dc.GetPositionField()?.Data.Y)
                    .FirstOrDefault(dc => dc.GetDataDocument(null).GetField(KeyStore.TitleKey) != null);

                output = firstDoc?.GetDataDocument(null).GetDereferencedField<TextController>(KeyStore.TitleKey, null);
            }


            outputs[ComputedTitle] = new TextController((output ?? new TextController("Untitled")).Data);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new CollectionTitleOperatorController();
        }

    }
    public class GroupTitleOperatorController : CollectionTitleOperatorController
    {

        protected override string Prefix() { return "GROUP: "; }
        public GroupTitleOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }
        public GroupTitleOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("904B0A69-3A1D-4E58-92A4-B472EEACA8FC", "Group Title");

        public override FieldControllerBase GetDefaultController()
        {
            return new GroupTitleOperatorController();
        }

    }
}
