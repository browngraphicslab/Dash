using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.join)]
    public sealed class JoinOperator : OperatorController
    {
        public static readonly KeyController SourceTableKey = new KeyController("TableOne");
        public static readonly KeyController TargetTableKey = new KeyController("TableTwo");
        public static readonly KeyController SourceKeyKey = new KeyController("KeyOne");
        public static readonly KeyController TargetKeyKey = new KeyController("KeyTwo");
        public static readonly KeyController OptionsKey = new KeyController("Options");

        public static readonly KeyController GencollectionKey = new KeyController("GenCollection");


        public JoinOperator() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public JoinOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Join Tables", new Guid("50b36009-0a53-4790-b6fe-0a9007db4d92"));

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
            new KeyValuePair<KeyController, IOInfo>(OptionsKey, new IOInfo(DashShared.TypeInfo.Text, false))
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

            TextController optionsMessage = null;
            if (inputs.ContainsKey(OptionsKey)) optionsMessage = (TextController)inputs[OptionsKey];

            var gencollection = Execute(sourceTable, targetTable, sourceKey, targetKey, optionsMessage);
            outputs[GencollectionKey] = gencollection;

            return Task.CompletedTask;
        }

        public DocumentController Execute(DocumentController sourceTable, DocumentController targetTable, KeyController sourceKey, KeyController targetKey, TextController opMess)
        {
            // if join mode is unspecified or "New", edit a copy of the source table. 
            // If set to "Add", will add the fields to matched documents in the original table, and no filtering takes place
            var addMode = opMess != null && opMess.Data.Equals("Append");
            var generatedCollection = addMode ? sourceTable : (DocumentController)sourceTable.Copy();
            var sourceDocs = generatedCollection.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            var targetDocs = targetTable.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            var toFilterOut = new List<DocumentController>();

            foreach (var row in sourceDocs)
            {
                var dataDoc = row.GetDataDocument();
                var sourceFields = dataDoc.EnumFields().Select(kv => kv.Key).ToList();

                if (!sourceFields.Contains(sourceKey)) continue;

                var valToMatch = dataDoc.GetField(sourceKey).GetValue(null);
                var matchedDoc = targetDocs.FirstOrDefault(k => k.GetDataDocument().GetField(targetKey).GetValue(null).Equals(valToMatch))?.GetDataDocument();

                if (matchedDoc == null)
                {
                    if (!addMode) toFilterOut.Add(row);
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
            generatedCollection.SetField<TextController>(KeyStore.TitleKey, $"{sourceTitle} (Joined)", true);

            return generatedCollection;
        }

    }
}
