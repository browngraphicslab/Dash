using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("intersectSearch")]
    public class IntersectSearchOperator : OperatorController
    {
        //Input keys
        public static readonly KeyController Dict1Key = new KeyController("4DD8F9C5-4266-4279-9D24-FD5AFBC44369", "Dict1");
        public static readonly KeyController Dict2Key = new KeyController("420EB524-8373-4144-9433-87C0AF6D6CA7", "Dict2");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("8E3931E4-6332-4A52-85F2-EC79031CB520", "DictionaryResults");

        public IntersectSearchOperator() : base(new OperatorModel(OperatorType.IntersectSearch))
        {
        }
        public IntersectSearchOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override bool SetValue(object value)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(Context context)
        {
            throw new NotImplementedException();
        }

        public override FieldModelController<OperatorModel> Copy()
        {
            throw new NotImplementedException();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(Dict1Key, new IOInfo(TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(Dict2Key, new IOInfo(TypeInfo.Document, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultsKey] = TypeInfo.Document
        };
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            var d1 = inputs[Dict1Key] as DocumentController;
            var d2 = inputs[Dict2Key] as DocumentController;
            
            var d3 = new DocumentController();
            foreach (var kvp in d1.EnumFields())
            {
                var l1 = kvp.Value as ListController<DocumentController>;
                var l2 = d2.GetField<ListController<DocumentController>>(kvp.Key);
                if (l1 != null && l2 != null)
                {
                    d3.SetField(kvp.Key, new ListController<DocumentController>(l1.TypedData.Concat(l2.TypedData)), true);
                }
            }

            outputs[ResultsKey] = d3;
        }
    }
}
