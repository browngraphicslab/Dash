using System.Collections.Generic;
using System.Diagnostics;
using DashShared;

namespace Dash
{
    public class IntersectionOperatorModelController : OperatorFieldModelController

    {
        //Input keys
        public static readonly KeyControllerBase AKey = new KeyControllerBase("178123E8-4E64-44D9-8F05-509B2F097B7D", "Input A");
        public static readonly KeyControllerBase BKey = new KeyControllerBase("0B9C67F7-3FB7-400A-B016-F12C048325BA", "Input B");

        //Output keys
        public static readonly KeyControllerBase IntersectionKey = new KeyControllerBase("95E14D4F-362A-4B4F-B0CD-78A4F5B47A92", "Intersection");

        public override ObservableDictionary<KeyControllerBase, IOInfo> Inputs { get; } = new ObservableDictionary<KeyControllerBase, IOInfo>
        {
            [AKey] = new IOInfo(TypeInfo.Collection, true),
            [BKey] = new IOInfo(TypeInfo.Collection, true)
        };

        public override ObservableDictionary<KeyControllerBase, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyControllerBase, TypeInfo>
        {
            [IntersectionKey] = TypeInfo.Collection
        };

        public IntersectionOperatorModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public IntersectionOperatorModelController() : base(new OperatorFieldModel("Intersection"))
        {
        }

        public override void Execute(Dictionary<KeyControllerBase, FieldControllerBase> inputs, Dictionary<KeyControllerBase, FieldControllerBase> outputs)
        {
            DocumentCollectionFieldModelController setA = (DocumentCollectionFieldModelController) inputs[AKey];
            DocumentCollectionFieldModelController setB = (DocumentCollectionFieldModelController) inputs[BKey];

            // Intersect by comparing all fields 
            HashSet<DocumentController> result = Util.GetIntersection(setA, setB); 
            //(doc.GetDereferencedField(IntersectionKey, docContextList) as DocumentCollectionFieldModelController).SetDocuments(result.ToList());
            outputs[IntersectionKey] = new DocumentCollectionFieldModelController(result);
            //Debug.WriteLine("intersection count :" + result.Count);

            // Intersect by Document ID 
            //(doc.GetField(IntersectionKey) as DocumentCollectionFieldModelController).SetDocuments(setA.GetDocuments().Intersect(setB.GetDocuments()).ToList());
        }

        public override FieldModelController<OperatorFieldModel> Copy()
        {
            return new IntersectionOperatorModelController(OperatorFieldModel);
        }
        public override object GetValue(Context context)
        {
            throw new System.NotImplementedException();
        }
        public override bool SetValue(object value)
        {
            return false;
        }
    }
}
