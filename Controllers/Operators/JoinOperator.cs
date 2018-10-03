using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.join)]
    public class JoinOperator : OperatorController
    {
        public static readonly KeyController TableoneKey = new KeyController("Tableone");
        public static readonly KeyController TabletwoKey = new KeyController("Tabletwo");
        public static readonly KeyController KeyoneKey = new KeyController("Keyone");
        public static readonly KeyController KeytwoKey = new KeyController("Keytwo");


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
            new KeyValuePair<KeyController, IOInfo>(TableoneKey, new IOInfo(DashShared.TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(TabletwoKey, new IOInfo(DashShared.TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(KeyoneKey, new IOInfo(DashShared.TypeInfo.Key, true)),
            new KeyValuePair<KeyController, IOInfo>(KeytwoKey, new IOInfo(DashShared.TypeInfo.Key, true)),
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
        {
            [GencollectionKey] = DashShared.TypeInfo.Document,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var tableone = (DocumentController)inputs[TableoneKey];
            var tabletwo = (DocumentController)inputs[TabletwoKey];
            var keyone = (KeyController)inputs[KeyoneKey];
            var keytwo = (KeyController)inputs[KeytwoKey];
            var gencollection = Execute(tableone, tabletwo, keyone, keytwo);
            outputs[GencollectionKey] = gencollection;
        }

        public DocumentController Execute(DocumentController tableone, DocumentController tabletwo, KeyController keyone, KeyController keytwo)
        {
            return new DocumentController();
        }

    }
}
