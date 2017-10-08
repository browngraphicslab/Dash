using System.Collections.Generic;
using System.Diagnostics;
using DashShared;

namespace Dash
{
    class IntersectionOperatorModelController : OperatorFieldModelController

    {
        //Input keys
        public static readonly KeyController AKey = new KeyController("178123E8-4E64-44D9-8F05-509B2F097B7D", "Input A");
        public static readonly KeyController BKey = new KeyController("0B9C67F7-3FB7-400A-B016-F12C048325BA", "Input B");

        //Output keys
        public static readonly KeyController IntersectionKey = new KeyController("95E14D4F-362A-4B4F-B0CD-78A4F5B47A92", "Intersection");

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [AKey] = new IOInfo(TypeInfo.Collection, true),
            [BKey] = new IOInfo(TypeInfo.Collection, true)
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [IntersectionKey] = TypeInfo.Collection
        };

        public IntersectionOperatorModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public IntersectionOperatorModelController() : base(new OperatorFieldModel("Intersection"))
        {
        }

        public override void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
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

        public override FieldModelController Copy()
        {
            //return new IntersectionOperatorModelController(OperatorFieldModel);
            return new IntersectionOperatorModelController();
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
