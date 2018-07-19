using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.coll_title)]
    public class CollectionTitleOperatorController : OperatorController
    {
        public CollectionTitleOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }
        public CollectionTitleOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Collection Title", "775EE4CC-D2A8-4A11-AC3F-EC36C91355DE");

        protected virtual string Prefix() { return "COLLECTION: ";  }

        //Input keys
        public static readonly KeyController CollectionDocsKey = KeyStore.DataKey;
       // public static readonly KeyController CollectionDocsKey = new KeyController("FB7EE0B1-004E-4FE0-B316-FFB909CBEBF2", "Collection Docs");

        //Output keys
        public static readonly KeyController ComputedTitle = new KeyController("Computed Title");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(CollectionDocsKey, new IOInfo(TypeInfo.List, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ComputedTitle] = TypeInfo.Text,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            TextController output = null;
            
            DocumentController firstDoc = null;
            if (inputs[CollectionDocsKey] is ListController<DocumentController> collDocs)
            {
                firstDoc = collDocs.TypedData.Where(dc => !dc.GetHidden()).OrderBy(dc => dc.GetPositionField()?.Data.Y)
                    .FirstOrDefault(dc => dc.GetDataDocument().GetField(KeyStore.TitleKey) != null);

                // bcz: this is a hack to avoid infinite recursion when the first document in a collection
                // is  databox whose DataContext is the collection.  This needs to be replaced with a more general
                // mechanism of identifying and halting an evaluation cycle
                if (firstDoc?.DocumentType.Equals(DataBox.DocumentType) == true)
                    output = new TextController(firstDoc.GetDereferencedField(KeyStore.DataKey, null)?.ToString() ?? "");
                else output = firstDoc?.GetDataDocument().GetDereferencedField<TextController>(KeyStore.TitleKey, null);
            }

            // bcz: this would be useful if we knew what was changed about the list item document.  If the title is changed, we only care about the first document;
            //      however, if something's position changed, then we need to update no matter what since we don't know if the sort ordering has changed.
            //var listArgs = ((args as DocumentController.DocumentFieldUpdatedEventArgs)?.FieldArgs as ListController<DocumentController>.ListFieldUpdatedEventArgs);
            //if (listArgs?.ListAction == ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Content &&
            //    listArgs?.ChangedDocuments.Contains(firstDoc) == false)
            //    return;

            outputs[ComputedTitle] = new TextController(output?.Data ?? "Untitled") { ReadOnly = true };
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new CollectionTitleOperatorController();
        }

    }
}
