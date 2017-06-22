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

        public override List<Key> Inputs { get; } = new List<Key> {AKey, BKey};

        public override List<Key> Outputs { get; } = new List<Key> {IntersectionKey};

        public override Dictionary<Key, FieldModel> Execute(Dictionary<Key, ReferenceFieldModel> inputReferences)
        {
            DocumentEndpoint docEnd = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            DocumentCollectionFieldModel setA = docEnd.GetFieldInDocument(inputReferences[AKey]) as DocumentCollectionFieldModel;
            DocumentCollectionFieldModel setB = docEnd.GetFieldInDocument(inputReferences[BKey]) as DocumentCollectionFieldModel;
            DocumentCollectionFieldModel intersection = new DocumentCollectionFieldModel(setA.Documents.Intersect(setB.Documents).ToList());
            return new Dictionary<Key, FieldModel>
            {
                {IntersectionKey, intersection}
            };
        }
    }
}
