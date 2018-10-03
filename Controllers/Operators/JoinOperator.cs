using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.join)]
    public class JoinOperator : OperatorController
    {
        public static readonly KeyController SourceTableKey = new KeyController("Tableone");
        public static readonly KeyController TargetTableKey = new KeyController("Tabletwo");
        public static readonly KeyController SourceKeyKey = new KeyController("Keyone");
        public static readonly KeyController TargetKeyKey = new KeyController("Keytwo");


        public static readonly KeyController GencollectionKey = new KeyController("Gencollection");


        public JoinOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public JoinOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Join Tables", "50b36009-0a53-4790-b6fe-0a9007db4d92");

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
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
        {
            [GencollectionKey] = DashShared.TypeInfo.Document,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var sourceTable = (DocumentController)inputs[SourceTableKey];
            var targetTable = (DocumentController)inputs[TargetTableKey];
            var sourceKey = (KeyController)inputs[SourceKeyKey];
            var targetKey = (KeyController)inputs[TargetKeyKey];
            var gencollection = Execute(sourceTable, targetTable, sourceKey, targetKey);
            outputs[GencollectionKey] = gencollection;
        }

        public DocumentController Execute(DocumentController sourceTable, DocumentController targetTable, KeyController sourceKey, KeyController targetKey)
        {
            var generatedCollection = (DocumentController) sourceTable.Copy();
            var sourceDocs = generatedCollection.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            var targetDocs = targetTable.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);

            foreach (var row in sourceDocs)
            {
                var valToMatch = row.GetField<TextController>(sourceKey).Data;
                var matchedDoc = targetDocs.First(k => k.GetField<TextController>(sourceKey).Data.Equals(valToMatch));
                row.SetField(targetKey, matchedDoc.GetField(targetKey), true);
            }

            MainPage.Instance.ViewModel.AddDocument(generatedCollection);

            return generatedCollection;
        }

    }
}
