using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("unionSearch")]
    public class UnionSearchOperator : OperatorController
    {
        //Input keys
        public static readonly KeyController Dict1Key = new KeyController("Dict1");
        public static readonly KeyController Dict2Key = new KeyController("Dict2");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("DictionaryResults");

        public UnionSearchOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }
        public UnionSearchOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Union Search", "C814865A-0173-4581-8533-9CB045E0338F");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(Dict1Key, new IOInfo(TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(Dict2Key, new IOInfo(TypeInfo.Document, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultsKey] = TypeInfo.Document
        };
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, ScriptState state = null)
        {
            var d1 = inputs[Dict1Key] as DocumentController;
            var d2 = inputs[Dict2Key] as DocumentController;

            var d3 = new DocumentController();

            foreach (var kvp in d1.EnumFields())
            {
                var l1 = kvp.Value as ListController<DocumentController>;
                if(l1 == null) continue;
                var l3 = d3.GetField<ListController<DocumentController>>(kvp.Key);

                if (l3 == null)
                {
                    d3.SetField(kvp.Key, 
                        new ListController<DocumentController>(l1.TypedData), true);
                }
            }

            foreach (var kvp in d2.EnumFields())
            {
                var l2 = kvp.Value as ListController<DocumentController>;
                if(l2 == null) continue;
                var l3 = d3.GetField<ListController<DocumentController>>(kvp.Key);

                if (l3 == null)
                {
                    d3.SetField(kvp.Key, 
                        new ListController<DocumentController>(l2.TypedData), true);
                }
                else
                {
                    foreach (var documentController in l2.TypedData)
                    {
                        if (!l3.TypedData.Contains(documentController))
                        {
                            l3.Add(documentController);
                        }
                    }
                    //TODO Can this just be "l3.AddRange(l2.TypedData);"?
                }
            }

            outputs[ResultsKey] = d3;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new UnionSearchOperator();
        }
    }
}
