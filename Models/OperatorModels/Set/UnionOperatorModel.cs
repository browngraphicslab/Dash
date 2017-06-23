using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash.Models.OperatorModels.Set
{
    class UnionOperatorModel : OperatorFieldModel
    {
        //Input keys
        public static readonly Key AKey = new Key("178123E8-4E64-44D9-8F05-509B2F097B7D", "Input A");
        public static readonly Key BKey = new Key("0B9C67F7-3FB7-400A-B016-F12C048325BA", "Input B");

        //Output keys
        public static readonly Key UnionKey = new Key("914B682E-E30C-46C5-80E2-7EC6B0B5C0F6", "Union");

        public override List<Key> InputKeys { get; } = new List<Key> {AKey, BKey};

        public override List<Key> OutputKeys { get; } = new List<Key> {UnionKey};

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

        public override void Execute(IDictionary<Key, FieldModel> fields)
        {
            DocumentCollectionFieldModel setA = fields[AKey] as DocumentCollectionFieldModel;
            DocumentCollectionFieldModel setB = fields[BKey] as DocumentCollectionFieldModel;
            //DocumentCollectionFieldModel union = new DocumentCollectionFieldModel(setA.Documents.Union(setB.Documents).ToList());
            (fields[UnionKey] as DocumentCollectionFieldModel).SetDocuments(setA.Documents.Union(setB.Documents).ToList());
        }
    }
}
