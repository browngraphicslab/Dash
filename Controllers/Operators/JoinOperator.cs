using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Dash
{
    [OperatorType(Op.Name.join)]
    public sealed class JoinOperator : OperatorController
    {
        public static readonly KeyController SourceTableKey = KeyController.Get("TableOne");
        public static readonly KeyController TargetTableKey = KeyController.Get("TableTwo");
        public static readonly KeyController SourceKeyKey = KeyController.Get("KeyOne");
        public static readonly KeyController TargetKeyKey = KeyController.Get("KeyTwo");
        public static readonly KeyController ScopeKey = KeyController.Get("Scope");
        public static readonly KeyController InPlaceKey = KeyController.Get("InPlace");

        public static readonly KeyController GencollectionKey = KeyController.Get("GenCollection");


        public JoinOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public JoinOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Join Tables");

        public override FieldControllerBase GetDefaultController()
        {
            return new JoinOperator();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(SourceTableKey, new IOInfo(DashShared.TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(TargetTableKey, new IOInfo(DashShared.TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(SourceKeyKey, new IOInfo(DashShared.TypeInfo.Key, true)),
            new KeyValuePair<KeyController, IOInfo>(TargetKeyKey, new IOInfo(DashShared.TypeInfo.Key, true)),
            new KeyValuePair<KeyController, IOInfo>(ScopeKey, new IOInfo(DashShared.TypeInfo.Bool, false)),
            new KeyValuePair<KeyController, IOInfo>(InPlaceKey, new IOInfo(DashShared.TypeInfo.Bool, false))
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
        {
            [GencollectionKey] = DashShared.TypeInfo.Document,
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var sourceTable = (DocumentController)inputs[SourceTableKey];
            var targetTable = (DocumentController)inputs[TargetTableKey];
            var sourceKey = (KeyController)inputs[SourceKeyKey];
            var targetKey = (KeyController)inputs[TargetKeyKey];

            bool innerJoin = true;
            bool inPlace = false;

            if (inputs.ContainsKey(ScopeKey)) innerJoin = ((BoolController)inputs[ScopeKey]).Data;
            if (inputs.ContainsKey(InPlaceKey)) inPlace = ((BoolController)inputs[InPlaceKey]).Data;

            var gencollection = Execute(sourceTable, targetTable, sourceKey, targetKey, innerJoin, inPlace);
            outputs[GencollectionKey] = gencollection;

            return Task.CompletedTask;
        }

        public DocumentController Execute(DocumentController sourceTable, DocumentController targetTable, KeyController sourceKey, KeyController targetKey, bool innerJoin, bool inPlace)
        {
            var generatedCollection = inPlace ? sourceTable : (DocumentController)sourceTable.Copy();
            var sourceDocs = generatedCollection.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            var targetDocs = targetTable.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            var toFilterOut = new List<DocumentController>();

            foreach (var row in sourceDocs)
            {
                var dataDoc = row.GetDataDocument();
                var sourceFields = dataDoc.EnumFields().Select(kv => kv.Key).ToList();

                if (!sourceFields.Contains(sourceKey)) continue;

                var valToMatch = dataDoc.GetField(sourceKey).GetValue();
                var matchedDoc = targetDocs.FirstOrDefault(k => k.GetDataDocument().GetField(targetKey).GetValue().Equals(valToMatch))?.GetDataDocument();

                if (matchedDoc == null)
                {
                    if (innerJoin) toFilterOut.Add(row);
                    continue;
                }
 
                var targetFields = matchedDoc.EnumFields().Select(kv => kv.Key).ToList();

                foreach (var tarKey in targetFields)
                {
                    if (sourceFields.Contains(tarKey)) continue;
                    dataDoc.SetField(tarKey, matchedDoc.GetField(tarKey), true);
                }

                row.SetField(targetKey, matchedDoc.GetField(targetKey), true);
                dataDoc.SetField(targetKey, matchedDoc.GetField(targetKey), true);
            }
            toFilterOut.ForEach(doc => generatedCollection.RemoveFromListField(KeyStore.DataKey, doc));

            var sourceTitle = sourceTable.GetDereferencedField(KeyStore.TitleKey, null);
            var targetTitle = targetTable.GetDereferencedField(KeyStore.TitleKey, null);
            var joinMessage = $"Generated by joining on {sourceKey} in '{sourceTitle}' and {targetKey} in '{targetTitle}'";
            generatedCollection.SetField<TextController>(KeyStore.JoinInfoKey, joinMessage, true);
            if (!generatedCollection.Title.Contains("(Joined)")) generatedCollection.SetField<TextController>(KeyStore.TitleKey, $"{sourceTitle} (Joined)", true);

            return generatedCollection;
        }

    }
}
