using System.Collections.Generic;
using System.Diagnostics;
using DashShared;

namespace Dash
{

    class UnionOperatorFieldModelController : OperatorFieldModelController
    {
        //Input keys
        public static readonly Key AKey = new Key("FBEBB4BE-5077-4ADC-8DAD-61142C301F61", "Input A");
        public static readonly Key BKey = new Key("740CE0AA-C7FD-4E78-9FC7-C7ED5E828260", "Input B");

        //Output keys
        public static readonly Key UnionKey = new Key("914B682E-E30C-46C5-80E2-7EC6B0B5C0F6", "Union");

        public override ObservableDictionary<Key, TypeInfo> Inputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
            [AKey] = TypeInfo.Collection,
            [BKey] = TypeInfo.Collection
        };

        public override ObservableDictionary<Key, TypeInfo> Outputs { get; } = new ObservableDictionary<Key, TypeInfo>
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

        public override void Execute(Dictionary<Key, FieldModelController> inputs, Dictionary<Key, FieldModelController> outputs)
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
            return new UnionOperatorFieldModelController(OperatorFieldModel);
        }
    }

}
