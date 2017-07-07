using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DashShared;

namespace Dash
{
    
    class UnionOperatorFieldModelController : OperatorFieldModelController
    {
        //Input keys
        public static readonly Key AKey = new Key("178123E8-4E64-44D9-8F05-509B2F097B7D", "Input A");
        public static readonly Key BKey = new Key("0B9C67F7-3FB7-400A-B016-F12C048325BA", "Input B");

        //Output keys
        public static readonly Key UnionKey = new Key("914B682E-E30C-46C5-80E2-7EC6B0B5C0F6", "Union");

        public override List<Key> InputKeys { get; } = new List<Key> {AKey, BKey};

        public override List<Key> OutputKeys { get; } = new List<Key> {UnionKey};

        public override List<FieldModelController> GetNewInputFields()
        {
            return new List<FieldModelController>
            {
                new DocumentCollectionFieldModelController(new List<DocumentController>()), new DocumentCollectionFieldModelController(new List<DocumentController>())
            };
        }

        public override List<FieldModelController> GetNewOutputFields()
        {
            return new List<FieldModelController>
            {
                new DocumentCollectionFieldModelController(new List<DocumentController>())
            };
        }

        public override void Execute(DocumentController doc, IEnumerable<DocumentController> docContextList)
        {
            DocumentCollectionFieldModelController setA = doc.GetDereferencedField(AKey, docContextList) as DocumentCollectionFieldModelController;
            DocumentCollectionFieldModelController setB = doc.GetDereferencedField(BKey, docContextList) as DocumentCollectionFieldModelController;

            // Union by comparing all fields 
            List<DocumentController> bigSet = setA.GetDocuments();
            bigSet.AddRange(setB.GetDocuments());
            HashSet<DocumentController> result = new HashSet<DocumentController>(bigSet);
            HashSet<DocumentController> same = Util.GetIntersection(setA, setB); 
            result.ExceptWith(same); 
            (doc.GetDereferencedField(UnionKey, DocContextList) as DocumentCollectionFieldModelController).SetDocuments(result.ToList());
            Debug.WriteLine("union count :" + result.Count);

            // Union by Document ID 
            //(doc.GetField(UnionKey) as DocumentCollectionFieldModelController).SetDocuments(setA.GetDocuments().Union(setB.GetDocuments()).ToList());

        }

        public UnionOperatorFieldModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }
    }
    
}
