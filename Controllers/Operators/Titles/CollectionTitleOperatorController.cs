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
        private string _prefix = "";
        public CollectionTitleOperatorController(string prefix = "COLLECTION: ") : base(new OperatorModel(OperatorType.CollectionTitle))
        {
            _prefix = prefix;
        }


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

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs)
        {
            TextController output = null;

            if (inputs[CollectionDocsKey] is ListController<DocumentController> collDocs)
            {
                var firstDoc = collDocs.TypedData.OrderBy(dc => dc.GetPositionField().Data.Y)
                    .FirstOrDefault(dc => dc.GetDataDocument(null).GetField(KeyStore.TitleKey) != null);

                output = firstDoc?.GetDataDocument(null).GetDereferencedField<TextController>(KeyStore.TitleKey, null);
            }


            outputs[ComputedTitle] = new TextController(_prefix + ((output ?? new TextController("Untitled")).Data));
        }

        public override FieldModelController<OperatorModel> Copy()
        {
            return new CollectionTitleOperatorController();
        }

        public override bool SetValue(object value)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(Context context)
        {
            throw new NotImplementedException();
        }

    }
}
