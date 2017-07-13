using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DashShared;

namespace Dash.Models.OperatorModels.Set
{
    class IntersectionOperatorModelController : OperatorFieldModelController

    {
        //Input keys
        public static readonly Key AKey = new Key("178123E8-4E64-44D9-8F05-509B2F097B7D", "Input A");
        public static readonly Key BKey = new Key("0B9C67F7-3FB7-400A-B016-F12C048325BA", "Input B");

        //Output keys
        public static readonly Key IntersectionKey = new Key("95E14D4F-362A-4B4F-B0CD-78A4F5B47A92", "Intersection");

        public override ObservableDictionary<Key, TypeInfo> Inputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
            [AKey] = TypeInfo.Collection,
            [BKey] = TypeInfo.Collection
        };

        public override ObservableDictionary<Key, TypeInfo> Outputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
            [IntersectionKey] = TypeInfo.Collection
        };

        public IntersectionOperatorModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override void Execute(DocumentController doc, IEnumerable<DocumentController> docContextList)
        {
            DocumentCollectionFieldModelController setA = doc.GetDereferencedField(AKey, docContextList) as DocumentCollectionFieldModelController;
            DocumentCollectionFieldModelController setB = doc.GetDereferencedField(BKey, docContextList) as DocumentCollectionFieldModelController;
            if (setA.InputReference == null || setB.InputReference == null)//One or more of the inputs isn't set yet
            {
                return;
            }

            // Intersect by comparing all fields 
            HashSet<DocumentController> result = Util.GetIntersection(setA, setB); 
            //(doc.GetDereferencedField(IntersectionKey, docContextList) as DocumentCollectionFieldModelController).SetDocuments(result.ToList());
            doc.SetField(IntersectionKey, new DocumentCollectionFieldModelController(result), true);
            Debug.WriteLine("intersection count :" + result.Count);

            // Intersect by Document ID 
            //(doc.GetField(IntersectionKey) as DocumentCollectionFieldModelController).SetDocuments(setA.GetDocuments().Intersect(setB.GetDocuments()).ToList());
        }
    }
}
