using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash.Models.OperatorModels.Set
{
    class IntersectionOperatorModel : OperatorFieldModel
    {
        //Input keys
        public static readonly Key AKey = new Key("178123E8-4E64-44D9-8F05-509B2F097B7D", "Input A");
        public static readonly Key BKey = new Key("0B9C67F7-3FB7-400A-B016-F12C048325BA", "Input B");

        //Output keys
        public static readonly Key IntersectionKey = new Key("95E14D4F-362A-4B4F-B0CD-78A4F5B47A92", "Intersection");

        public override List<Key> InputKeys { get; } = new List<Key> {AKey, BKey};

        public override List<Key> OutputKeys { get; } = new List<Key> {IntersectionKey};
        public override List<FieldModel> GetNewInputFields()
        {
            return new List<FieldModel>
            {
                new DocumentCollectionFieldModel(new List<DocumentModel>()), new DocumentCollectionFieldModel(new List<DocumentModel>())
            };
        }

        public override List<FieldModel> GetNewOutputFields()
        {
            return new List<FieldModel>
            {
                new DocumentCollectionFieldModel(new List<DocumentModel>())
            };
        }

        public override void Execute(DocumentModel doc)
        {
            DocumentCollectionFieldModel setA = doc.Field(AKey) as DocumentCollectionFieldModel;
            DocumentCollectionFieldModel setB = doc.Field(BKey) as DocumentCollectionFieldModel;
            
            (doc.Field(IntersectionKey) as DocumentCollectionFieldModel).SetDocuments(setA.Documents.Intersect(setB.Documents).ToList());
        }
    }
}
