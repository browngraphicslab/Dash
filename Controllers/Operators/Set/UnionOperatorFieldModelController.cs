using System.Collections.Generic;
using System.Diagnostics;
using DashShared;

namespace Dash
{

    class UnionOperatorFieldModelController : OperatorFieldModelController
    {
        //Input keys
        public static readonly KeyController AKey = new KeyController("FBEBB4BE-5077-4ADC-8DAD-61142C301F61", "Input A");
        public static readonly KeyController BKey = new KeyController("740CE0AA-C7FD-4E78-9FC7-C7ED5E828260", "Input B");

        //Output keys
        public static readonly KeyController UnionKey = new KeyController("914B682E-E30C-46C5-80E2-7EC6B0B5C0F6", "Union");

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [AKey] = new IOInfo(TypeInfo.Collection, true),
            [BKey] = new IOInfo(TypeInfo.Collection, true)
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [UnionKey] = TypeInfo.Collection
        };

        public UnionOperatorFieldModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }

        public UnionOperatorFieldModelController() : base(new OperatorFieldModel("Union"))
        {
        }

        public override void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
        {
            DocumentCollectionFieldModelController setA = (DocumentCollectionFieldModelController) inputs[AKey];
            DocumentCollectionFieldModelController setB = (DocumentCollectionFieldModelController)inputs[BKey];

            // Union by comparing all fields 
            List<DocumentController> bigSet = setA.GetDocuments();
            bigSet.AddRange(setB.GetDocuments());
            HashSet<DocumentController> result = new HashSet<DocumentController>(bigSet);
            HashSet<DocumentController> same = Util.GetIntersection(setA, setB);
            result.ExceptWith(same);
            //(doc.GetDereferencedField(UnionKey, DocContextList) as DocumentCollectionFieldModelController).SetDocuments(result.ToList());
            outputs[UnionKey] = new DocumentCollectionFieldModelController(result);
            //Debug.WriteLine("union count :" + result.Count);

            // Union by Document ID 
            //(doc.GetField(UnionKey) as DocumentCollectionFieldModelController).SetDocuments(setA.GetDocuments().Union(setB.GetDocuments()).ToList());

        }

        public override FieldModelController Copy()
        {
            //return new UnionOperatorFieldModelController(OperatorFieldModel);
            return new UnionOperatorFieldModelController();
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
