using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public static readonly KeyController CollectionDocsKey = KeyStore.DataKey;
       // public static readonly KeyController CollectionDocsKey = new KeyController("FB7EE0B1-004E-4FE0-B316-FFB909CBEBF2", "Collection Docs");

        //Output keys
        public static readonly KeyController ComputedTitle = new KeyController("B8F9AC2E-02F8-4C95-82D8-401BA57053C3", "Computed Title");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(CollectionDocsKey, new IOInfo(TypeInfo.List, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ComputedTitle] = TypeInfo.Text,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            TextController output = null;

            DocumentController firstDoc = null;
            if (inputs[CollectionDocsKey] is ListController<DocumentController> collDocs)
            {
                firstDoc = collDocs.TypedData.OrderBy(dc => dc.GetPositionField()?.Data.Y)
                    .FirstOrDefault(dc => dc.GetDataDocument().GetField(KeyStore.TitleKey) != null);

                output = firstDoc?.GetDataDocument().GetDereferencedField<TextController>(KeyStore.TitleKey, null);
            }

            // bcz: this would be useful if we knew what was changed about the list item document.  If the title is changed, we only care about the first document;
            //      however, if something's position changed, then we need to update no matter what since we don't know if the sort ordering has changed.
            //var listArgs = ((args as DocumentController.DocumentFieldUpdatedEventArgs)?.FieldArgs as ListController<DocumentController>.ListFieldUpdatedEventArgs);
            //if (listArgs?.ListAction == ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Content &&
            //    listArgs?.ChangedDocuments.Contains(firstDoc) == false)
            //    return;


            outputs[ComputedTitle] = new TextController((output ?? new TextController("Untitled")).Data);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new CollectionTitleOperatorController();
        }

    }
}
